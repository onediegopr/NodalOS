$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Join-Path $env:RUNNER_TEMP "nodal-release-contract"
$output = Join-Path $root "package"
$evidencePath = Join-Path $root "release-contract-evidence.json"
$logPath = Join-Path $root "release-contract-smoke.log"
$packageName = "NODALOS.PrivateBeta"
$version = "0.1.0.4"
$subject = "CN=NODAL OS Release Contract Test"
$distributionBase = "https://updates.example.invalid/nodal-os/private"
$certificate = $null
$trustedThumbprint = $null
$installedPackage = $null

New-Item -ItemType Directory -Path $root -Force | Out-Null
Start-Transcript -Path $logPath -Force | Out-Null
try {
    Get-AppxPackage -Name $packageName -ErrorAction SilentlyContinue |
        ForEach-Object { Remove-AppxPackage -Package $_.PackageFullName -ErrorAction Stop }

    $certificate = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject $subject `
        -FriendlyName "NODAL OS Release Contract Test" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -KeyAlgorithm RSA `
        -KeyLength 3072 `
        -HashAlgorithm SHA256 `
        -KeyExportPolicy Exportable `
        -NotAfter (Get-Date).AddDays(2)

    $pfxPassphrase = [Guid]::NewGuid().ToString("N") + [Guid]::NewGuid().ToString("N")
    $securePassphrase = ConvertTo-SecureString $pfxPassphrase -AsPlainText -Force
    $pfxPath = Join-Path $root "release-contract.pfx"
    $cerPath = Join-Path $root "release-contract.cer"
    Export-PfxCertificate -Cert $certificate -FilePath $pfxPath -Password $securePassphrase | Out-Null
    Export-Certificate -Cert $certificate -FilePath $cerPath -Type CERT | Out-Null
    $trusted = Import-Certificate -FilePath $cerPath -CertStoreLocation "Cert:\LocalMachine\TrustedPeople"
    $trustedThumbprint = $trusted.Thumbprint

    $env:NODAL_OS_SIGNING_PFX_PASSWORD = $pfxPassphrase
    ./eng/packaging/build-msix.ps1 `
        -Version $version `
        -OutputDirectory $output `
        -SigningPfxPath $pfxPath `
        -DistributionBaseUri $distributionBase
    if ($LASTEXITCODE -ne 0) {
        throw "External-signing package build failed with exit code $LASTEXITCODE."
    }

    $manifestPath = Join-Path $output "nodal-os-update-manifest.json"
    $appInstallerPath = Join-Path $output "NodalOS.appinstaller"
    $msixPath = Join-Path $output "NodalOS-$version-win-x64.msix"
    $bundlePath = Join-Path $output "NodalOS-$version-win-x64-private-beta.zip"
    foreach ($path in @($manifestPath, $appInstallerPath, $msixPath, $bundlePath)) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Release-contract output is missing: $path"
        }
    }

    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
    if ($manifest.version -ne $version -or
        $manifest.packageName -ne $packageName -or
        $manifest.publisher -ne $subject -or
        $manifest.signingMode -ne "external" -or
        $null -ne $manifest.testCertificateFile -or
        $manifest.productAuthorityGranted) {
        throw "External-signing update manifest does not preserve the release contract."
    }

    $appInstallerText = Get-Content $appInstallerPath -Raw
    $expectedAppInstallerUri = "$distributionBase/NodalOS.appinstaller"
    $expectedPackageUri = "$distributionBase/NodalOS-$version-win-x64.msix"
    foreach ($expected in @(
        "Uri=\"$expectedAppInstallerUri\"",
        "Name=\"$packageName\"",
        "Publisher=\"$subject\"",
        "Version=\"$version\"",
        "Uri=\"$expectedPackageUri\""
    )) {
        if ($appInstallerText -notmatch [regex]::Escape($expected)) {
            throw "Appinstaller metadata is missing expected value: $expected"
        }
    }
    if ($appInstallerText -match "http://") {
        throw "Appinstaller metadata must use HTTPS paths."
    }

    $signature = Get-AuthenticodeSignature $msixPath
    if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid -or
        $signature.SignerCertificate.Subject -ne $subject) {
        throw "Externally supplied signing identity was not preserved in the MSIX signature."
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead($bundlePath)
    try {
        $entries = @($archive.Entries | ForEach-Object { $_.FullName })
        foreach ($required in @(
            "NodalOS-$version-win-x64.msix",
            "nodal-os-update-manifest.json",
            "NodalOS.appinstaller",
            "Install-NodalOS.ps1",
            "Uninstall-NodalOS.ps1",
            "README-INSTALL.txt"
        )) {
            if ($entries -notcontains $required) {
                throw "External-signing bundle is missing $required."
            }
        }
        if ($entries | Where-Object { $_ -match "(?i)[.](cer|pfx)$" }) {
            throw "External-signing bundle leaked a certificate or private-key container."
        }
    }
    finally {
        $archive.Dispose()
    }

    & (Join-Path $output "Install-NodalOS.ps1")
    $installedPackage = Get-AppxPackage -Name $packageName -ErrorAction Stop
    if (-not $installedPackage -or $installedPackage.Version.ToString() -ne $version) {
        throw "Externally signed package did not install with the expected identity and version."
    }
    & (Join-Path $output "Uninstall-NodalOS.ps1")
    if (Get-AppxPackage -Name $packageName -ErrorAction SilentlyContinue) {
        throw "Externally signed package remained installed after bundled uninstall."
    }
    $installedPackage = $null

    $evidence = [ordered]@{
        schemaVersion = 1
        product = "NODAL OS"
        validationKind = "external-signing-and-https-appinstaller"
        packageVersion = $version
        packageName = $packageName
        publisher = $subject
        signingMode = "external"
        distributionBaseUri = $distributionBase
        appInstallerUri = $expectedAppInstallerUri
        packageUri = $expectedPackageUri
        msixSha256 = (Get-FileHash $msixPath -Algorithm SHA256).Hash.ToLowerInvariant()
        bundleSha256 = (Get-FileHash $bundlePath -Algorithm SHA256).Hash.ToLowerInvariant()
        testCertificateBundled = $false
        privateKeyBundled = $false
        installPassed = $true
        uninstallPassed = $true
        productAuthorityGranted = $false
        publicDistributionAuthorized = $false
    }
    $evidence | ConvertTo-Json -Depth 4 | Set-Content $evidencePath -Encoding utf8NoBOM
    Write-Host "NODAL_OS_EXTERNAL_SIGNING_UPDATE_CONTRACT=PASS"
}
finally {
    Stop-Transcript -ErrorAction SilentlyContinue | Out-Null
    Get-AppxPackage -Name $packageName -ErrorAction SilentlyContinue |
        ForEach-Object { Remove-AppxPackage -Package $_.PackageFullName -ErrorAction SilentlyContinue }
    if ($trustedThumbprint) {
        $trustedPath = "Cert:\LocalMachine\TrustedPeople\$trustedThumbprint"
        if (Test-Path $trustedPath) { Remove-Item $trustedPath -Force }
    }
    if ($certificate) {
        $currentUserPath = "Cert:\CurrentUser\My\$($certificate.Thumbprint)"
        if (Test-Path $currentUserPath) { Remove-Item $currentUserPath -Force }
        $certificate.Dispose()
    }
    Remove-Item Env:NODAL_OS_SIGNING_PFX_PASSWORD -ErrorAction SilentlyContinue
    Remove-Item (Join-Path $root "release-contract.pfx") -Force -ErrorAction SilentlyContinue
    Remove-Item (Join-Path $root "release-contract.cer") -Force -ErrorAction SilentlyContinue
    if (Test-Path $output) { Remove-Item $output -Recurse -Force }
}
