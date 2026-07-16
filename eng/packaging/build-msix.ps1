param(
    [ValidatePattern('^\d+\.\d+\.\d+(?:\.\d+)?$')]
    [string]$Version = "0.1.0.0",
    [ValidateSet("Release")]
    [string]$Configuration = "Release",
    [ValidateSet("win-x64")]
    [string]$RuntimeIdentifier = "win-x64",
    [string]$OutputDirectory,
    [string]$PackageName = "NODALOS.PrivateBeta",
    [string]$Publisher = "CN=NODAL OS Private Beta",
    [string]$DistributionBaseUri,
    [string]$SigningPfxPath,
    [string]$SigningPfxPassword = $env:NODAL_OS_SIGNING_PFX_PASSWORD,
    [switch]$KeepStaging
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot "artifacts/desktop-package/$Version"
}
$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
$workRoot = Join-Path $OutputDirectory ".work"
$publishRoot = Join-Path $workRoot "publish"
$packageRoot = Join-Path $workRoot "package"
$assetsRoot = Join-Path $packageRoot "Assets"

function Normalize-PackageVersion([string]$Value) {
    $parts = $Value.Split('.')
    if ($parts.Count -eq 3) { $parts += "0" }
    if ($parts.Count -ne 4) { throw "Package version must contain three or four numeric components." }
    foreach ($part in $parts) {
        $number = 0
        if (-not [int]::TryParse($part, [ref]$number) -or $number -lt 0 -or $number -gt 65535) {
            throw "Each package version component must be between 0 and 65535."
        }
    }
    return ($parts -join '.')
}

function Find-WindowsSdkTool([string]$ToolName) {
    $command = Get-Command $ToolName -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }

    $sdkRoot = Join-Path ${env:ProgramFiles(x86)} "Windows Kits/10/bin"
    if (-not (Test-Path $sdkRoot)) { throw "Windows SDK bin directory was not found." }
    $versions = Get-ChildItem $sdkRoot -Directory |
        Where-Object { $_.Name -match '^\d+\.\d+\.\d+\.\d+$' } |
        Sort-Object { [version]$_.Name } -Descending
    foreach ($versionDirectory in $versions) {
        $candidate = Join-Path $versionDirectory.FullName "x64/$ToolName"
        if (Test-Path $candidate) { return $candidate }
    }
    throw "$ToolName was not found in the Windows SDK."
}

function New-NodalLogo([int]$Width, [int]$Height, [string]$Path) {
    Add-Type -AssemblyName System.Drawing.Common
    $bitmap = [System.Drawing.Bitmap]::new($Width, $Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.Clear([System.Drawing.ColorTranslator]::FromHtml("#0D1117"))
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
        $fontSize = [Math]::Max(12, [Math]::Floor([Math]::Min($Width, $Height) * 0.52))
        $font = [System.Drawing.Font]::new("Segoe UI", $fontSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
        $brush = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml("#4F7CFF"))
        $format = [System.Drawing.StringFormat]::new()
        try {
            $format.Alignment = [System.Drawing.StringAlignment]::Center
            $format.LineAlignment = [System.Drawing.StringAlignment]::Center
            $graphics.DrawString("N", $font, $brush, [System.Drawing.RectangleF]::new(0, 0, $Width, $Height), $format)
        }
        finally {
            $format.Dispose()
            $brush.Dispose()
            $font.Dispose()
        }
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

function Escape-Xml([string]$Value) {
    return [System.Security.SecurityElement]::Escape($Value)
}

function Remove-CertificateByThumbprint([string]$StorePath, [string]$Thumbprint) {
    $path = Join-Path $StorePath $Thumbprint
    if (Test-Path $path) { Remove-Item $path -Force }
}

$packageVersion = Normalize-PackageVersion $Version
$assemblyVersion = (($packageVersion.Split('.')[0..2]) -join '.')

if (Test-Path $OutputDirectory) { Remove-Item $OutputDirectory -Recurse -Force }
New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null
New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null
New-Item -ItemType Directory -Path $assetsRoot -Force | Out-Null

Write-Host "Publishing NODAL OS $assemblyVersion for $RuntimeIdentifier..."
& dotnet publish (Join-Path $repoRoot "src/OneBrain.Pilot/OneBrain.Pilot.csproj") `
    --configuration $Configuration `
    --runtime $RuntimeIdentifier `
    --self-contained true `
    --output $publishRoot `
    --nologo `
    -p:Version=$assemblyVersion `
    -p:AssemblyVersion=$packageVersion `
    -p:FileVersion=$packageVersion `
    -p:InformationalVersion="$assemblyVersion+private-beta" `
    -p:PublishTrimmed=false `
    -p:PublishReadyToRun=false `
    -p:DebugType=None `
    -p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE." }
if (-not (Test-Path (Join-Path $publishRoot "OneBrain.Pilot.exe"))) {
    throw "Published application executable was not produced."
}

Copy-Item (Join-Path $publishRoot "*") $packageRoot -Recurse -Force
$packagedRecipes = @(
    "demo-product-evidence-report.json",
    "demo-product-evidence-html-report.json",
    "product-evidence-html-report.json",
    "product-evidence-markdown-report.json"
)
$recipeTarget = Join-Path $packageRoot "tools/recipes"
New-Item -ItemType Directory -Path $recipeTarget -Force | Out-Null
foreach ($recipeName in $packagedRecipes) {
    $recipePath = Join-Path $repoRoot "tools/recipes/$recipeName"
    if (-not (Test-Path $recipePath)) { throw "Required product recipe was not found: $recipeName" }
    Copy-Item $recipePath (Join-Path $recipeTarget $recipeName) -Force
}

New-NodalLogo 50 50 (Join-Path $assetsRoot "StoreLogo.png")
New-NodalLogo 44 44 (Join-Path $assetsRoot "Square44x44Logo.png")
New-NodalLogo 150 150 (Join-Path $assetsRoot "Square150x150Logo.png")
New-NodalLogo 310 310 (Join-Path $assetsRoot "Square310x310Logo.png")
New-NodalLogo 310 150 (Join-Path $assetsRoot "Wide310x150Logo.png")

$signingMode = "test"
$testCertificate = $null
$testCertificatePath = $null
$temporaryPfxPath = $null
$temporaryPfxPassword = $null
$temporaryTrustedPeopleThumbprint = $null
$certificateSubject = $Publisher

if (-not [string]::IsNullOrWhiteSpace($SigningPfxPath)) {
    $SigningPfxPath = [System.IO.Path]::GetFullPath($SigningPfxPath)
    if (-not (Test-Path $SigningPfxPath)) { throw "Signing PFX was not found." }
    if ([string]::IsNullOrWhiteSpace($SigningPfxPassword)) { throw "Signing PFX password is required." }
    $securePassword = ConvertTo-SecureString $SigningPfxPassword -AsPlainText -Force
    $pfxData = Get-PfxData -FilePath $SigningPfxPath -Password $securePassword
    $certificate = $pfxData.EndEntityCertificates | Select-Object -First 1
    if (-not $certificate) { throw "Signing PFX does not contain an end-entity certificate." }
    $certificateSubject = $certificate.Subject
    $signingMode = "external"
}

$templatePath = Join-Path $PSScriptRoot "AppxManifest.xml.template"
$template = Get-Content $templatePath -Raw
$manifest = $template.Replace("@@PACKAGE_NAME@@", (Escape-Xml $PackageName))
$manifest = $manifest.Replace("@@PUBLISHER@@", (Escape-Xml $certificateSubject))
$manifest = $manifest.Replace("@@VERSION@@", $packageVersion)
[System.IO.File]::WriteAllText((Join-Path $packageRoot "AppxManifest.xml"), $manifest, [System.Text.UTF8Encoding]::new($false))

$commitSha = if ($env:GITHUB_SHA) { $env:GITHUB_SHA } else {
    try { (& git -C $repoRoot rev-parse HEAD).Trim() } catch { "unknown" }
}
$buildInfo = [ordered]@{
    schemaVersion = 1
    product = "NODAL OS"
    version = $packageVersion
    runtimeIdentifier = $RuntimeIdentifier
    commitSha = $commitSha
    channel = "private-beta"
    productAuthorityGranted = $false
}
$buildInfo | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $packageRoot "nodal-build-info.json") -Encoding utf8NoBOM

$makeAppx = Find-WindowsSdkTool "MakeAppx.exe"
$signTool = Find-WindowsSdkTool "SignTool.exe"
$msixName = "NodalOS-$packageVersion-$RuntimeIdentifier.msix"
$msixPath = Join-Path $OutputDirectory $msixName
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

Write-Host "Creating MSIX package..."
& $makeAppx pack /d $packageRoot /p $msixPath /o
if ($LASTEXITCODE -ne 0) { throw "MakeAppx failed with exit code $LASTEXITCODE." }

try {
    if ($signingMode -eq "test") {
        $testCertificate = New-SelfSignedCertificate `
            -Type CodeSigningCert `
            -Subject $certificateSubject `
            -FriendlyName "NODAL OS Private Beta Package Test" `
            -CertStoreLocation "Cert:\CurrentUser\My" `
            -KeyAlgorithm RSA `
            -KeyLength 3072 `
            -HashAlgorithm SHA256 `
            -KeyExportPolicy Exportable `
            -NotAfter (Get-Date).AddDays(90)
        $temporaryPfxPassword = [Guid]::NewGuid().ToString("N") + [Guid]::NewGuid().ToString("N")
        $securePassword = ConvertTo-SecureString $temporaryPfxPassword -AsPlainText -Force
        $temporaryPfxPath = Join-Path $workRoot "private-beta-test-signing.pfx"
        $testCertificatePath = Join-Path $OutputDirectory "NodalOS-Private-Beta-Test.cer"
        Export-PfxCertificate -Cert $testCertificate -FilePath $temporaryPfxPath -Password $securePassword | Out-Null
        Export-Certificate -Cert $testCertificate -FilePath $testCertificatePath -Type CERT | Out-Null
        $SigningPfxPath = $temporaryPfxPath
        $SigningPfxPassword = $temporaryPfxPassword
    }

    Write-Host "Signing MSIX package using $signingMode signing..."
    & $signTool sign /fd SHA256 /f $SigningPfxPath /p $SigningPfxPassword $msixPath
    if ($LASTEXITCODE -ne 0) { throw "SignTool sign failed with exit code $LASTEXITCODE." }

    if ($testCertificatePath) {
        $trustedRoot = Import-Certificate -FilePath $testCertificatePath -CertStoreLocation "Cert:\LocalMachine\TrustedPeople"
        $temporaryTrustedPeopleThumbprint = $trustedRoot.Thumbprint
    }
    & $signTool verify /pa /v $msixPath
    if ($LASTEXITCODE -ne 0) { throw "SignTool verification failed with exit code $LASTEXITCODE." }
}
finally {
    if ($temporaryTrustedPeopleThumbprint) {
        Remove-CertificateByThumbprint "Cert:\LocalMachine\TrustedPeople" $temporaryTrustedPeopleThumbprint
    }
    if ($testCertificate) {
        Remove-CertificateByThumbprint "Cert:\CurrentUser\My" $testCertificate.Thumbprint
    }
    if ($temporaryPfxPath -and (Test-Path $temporaryPfxPath)) {
        Remove-Item $temporaryPfxPath -Force
    }
    $SigningPfxPassword = $null
    $temporaryPfxPassword = $null
}

$packageHash = (Get-FileHash $msixPath -Algorithm SHA256).Hash.ToLowerInvariant()
$manualUpdate = [ordered]@{
    schemaVersion = 1
    channel = "private-beta-manual"
    product = "NODAL OS"
    packageName = $PackageName
    publisher = $certificateSubject
    version = $packageVersion
    architecture = "x64"
    packageFile = $msixName
    sha256 = $packageHash
    signingMode = $signingMode
    testCertificateFile = if ($testCertificatePath) { Split-Path $testCertificatePath -Leaf } else { $null }
    minimumWindowsVersion = "10.0.19041.0"
    updatePolicy = "Install a newer package with the same identity and a greater four-part version. No automatic public channel is enabled."
    productAuthorityGranted = $false
    generatedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
}
$updateManifestPath = Join-Path $OutputDirectory "nodal-os-update-manifest.json"
$manualUpdate | ConvertTo-Json -Depth 5 | Set-Content $updateManifestPath -Encoding utf8NoBOM

if (-not [string]::IsNullOrWhiteSpace($DistributionBaseUri)) {
    $baseUri = $null
    if (-not [Uri]::TryCreate($DistributionBaseUri, [UriKind]::Absolute, [ref]$baseUri) -or
        $baseUri.Scheme -ne [Uri]::UriSchemeHttps -or
        -not [string]::IsNullOrEmpty($baseUri.Query) -or
        -not [string]::IsNullOrEmpty($baseUri.Fragment)) {
        throw "DistributionBaseUri must be an absolute HTTPS origin/path without query or fragment."
    }
    $base = $baseUri.AbsoluteUri.TrimEnd('/')
    $appInstaller = @"
<?xml version="1.0" encoding="utf-8"?>
<AppInstaller xmlns="http://schemas.microsoft.com/appx/appinstaller/2021"
  Uri="$base/NodalOS.appinstaller"
  Version="$packageVersion">
  <MainPackage
    Name="$(Escape-Xml $PackageName)"
    Publisher="$(Escape-Xml $certificateSubject)"
    Version="$packageVersion"
    ProcessorArchitecture="x64"
    Uri="$base/$msixName" />
  <UpdateSettings>
    <OnLaunch HoursBetweenUpdateChecks="24" ShowPrompt="true" UpdateBlocksActivation="false" />
  </UpdateSettings>
</AppInstaller>
"@
    [System.IO.File]::WriteAllText((Join-Path $OutputDirectory "NodalOS.appinstaller"), $appInstaller, [System.Text.UTF8Encoding]::new($false))
}

$certificateLeaf = if ($testCertificatePath) { Split-Path $testCertificatePath -Leaf } else { "" }
$installScript = @"
param([switch]`$TrustTestCertificate)
`$ErrorActionPreference = "Stop"
`$root = Split-Path -Parent `$MyInvocation.MyCommand.Path
`$package = Join-Path `$root "$msixName"
`$certificate = Join-Path `$root "$certificateLeaf"
if (Test-Path `$certificate) {
    if (-not `$TrustTestCertificate) {
        throw "This private-beta package uses a test certificate. Re-run with -TrustTestCertificate only on a controlled test device."
    }
    Import-Certificate -FilePath `$certificate -CertStoreLocation "Cert:\LocalMachine\TrustedPeople" | Out-Null
}
Add-AppxPackage -Path `$package -ForceUpdateFromAnyVersion
Write-Host "NODAL OS $packageVersion installed for the current user."
"@
[System.IO.File]::WriteAllText((Join-Path $OutputDirectory "Install-NodalOS.ps1"), $installScript, [System.Text.UTF8Encoding]::new($false))

$uninstallScript = @"
param([switch]`$RemoveUserData)
`$ErrorActionPreference = "Stop"
Get-AppxPackage -Name "$PackageName" | ForEach-Object { Remove-AppxPackage -Package `$_.PackageFullName }
if (`$RemoveUserData) {
    `$dataRoot = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) "NodalOS"
    if (Test-Path `$dataRoot) { Remove-Item `$dataRoot -Recurse -Force }
}
Write-Host "NODAL OS uninstalled for the current user."
"@
[System.IO.File]::WriteAllText((Join-Path $OutputDirectory "Uninstall-NodalOS.ps1"), $uninstallScript, [System.Text.UTF8Encoding]::new($false))

$installReadme = @"
NODAL OS PRIVATE BETA PACKAGE
Version: $packageVersion
Package: $msixName
SHA-256: $packageHash
Signing mode: $signingMode

This package is not a production or public release.
- Test-signed builds require explicit trust of the included certificate on a controlled test device.
- Public distribution requires a CA-trusted or Microsoft-managed signing identity matching the package publisher.
- Install a newer four-part version to update. The current private-beta channel is manual unless a validated HTTPS .appinstaller URI is supplied at build time.
- Uninstall-NodalOS.ps1 removes the package. Pass -RemoveUserData only when local workspaces, evidence references and model configuration should also be removed.
"@
[System.IO.File]::WriteAllText((Join-Path $OutputDirectory "README-INSTALL.txt"), $installReadme, [System.Text.UTF8Encoding]::new($false))

$bundleFiles = @(
    $msixPath,
    $updateManifestPath,
    (Join-Path $OutputDirectory "Install-NodalOS.ps1"),
    (Join-Path $OutputDirectory "Uninstall-NodalOS.ps1"),
    (Join-Path $OutputDirectory "README-INSTALL.txt")
)
if ($testCertificatePath) { $bundleFiles += $testCertificatePath }
$appInstallerPath = Join-Path $OutputDirectory "NodalOS.appinstaller"
if (Test-Path $appInstallerPath) { $bundleFiles += $appInstallerPath }
$bundlePath = Join-Path $OutputDirectory "NodalOS-$packageVersion-$RuntimeIdentifier-private-beta.zip"
Compress-Archive -Path $bundleFiles -DestinationPath $bundlePath -CompressionLevel Optimal -Force

if (-not $KeepStaging -and (Test-Path $workRoot)) {
    Remove-Item $workRoot -Recurse -Force
}

if ($env:GITHUB_OUTPUT) {
    "msix_path=$msixPath" | Out-File $env:GITHUB_OUTPUT -Append -Encoding utf8
    "bundle_path=$bundlePath" | Out-File $env:GITHUB_OUTPUT -Append -Encoding utf8
    "package_version=$packageVersion" | Out-File $env:GITHUB_OUTPUT -Append -Encoding utf8
    "package_sha256=$packageHash" | Out-File $env:GITHUB_OUTPUT -Append -Encoding utf8
    "signing_mode=$signingMode" | Out-File $env:GITHUB_OUTPUT -Append -Encoding utf8
}

Write-Host "NODAL_OS_MSIX=$msixPath"
Write-Host "NODAL_OS_BUNDLE=$bundlePath"
Write-Host "NODAL_OS_PACKAGE_SHA256=$packageHash"
Write-Host "NODAL_OS_SIGNING_MODE=$signingMode"
