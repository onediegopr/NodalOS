param(
    [Parameter(Mandatory = $true)]
    [string]$DepsPath,
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory,
    [string]$NuGetPackagesRoot = $env:NUGET_PACKAGES,
    [string]$DotnetRoot
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Read-NuspecMetadata([string]$PackageRoot) {
    $nuspecPath = Get-ChildItem $PackageRoot -Filter "*.nuspec" -File -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if (-not $nuspecPath) {
        return [pscustomobject]@{
            LicenseType = $null
            LicenseValue = $null
            ProjectUrl = $null
            RepositoryUrl = $null
            Copyright = $null
        }
    }

    [xml]$nuspec = Get-Content $nuspecPath.FullName -Raw
    $metadata = $nuspec.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']")
    if (-not $metadata) { throw "NuGet metadata was not found in $($nuspecPath.FullName)." }

    $licenseNode = $metadata.SelectSingleNode("./*[local-name()='license']")
    $licenseUrlNode = $metadata.SelectSingleNode("./*[local-name()='licenseUrl']")
    $projectUrlNode = $metadata.SelectSingleNode("./*[local-name()='projectUrl']")
    $repositoryNode = $metadata.SelectSingleNode("./*[local-name()='repository']")
    $copyrightNode = $metadata.SelectSingleNode("./*[local-name()='copyright']")

    $licenseType = $null
    $licenseValue = $null
    if ($licenseNode) {
        $licenseType = $licenseNode.GetAttribute("type")
        $licenseValue = $licenseNode.InnerText.Trim()
    }
    elseif ($licenseUrlNode) {
        $licenseType = "url"
        $licenseValue = $licenseUrlNode.InnerText.Trim()
    }

    return [pscustomobject]@{
        LicenseType = $licenseType
        LicenseValue = $licenseValue
        ProjectUrl = if ($projectUrlNode) { $projectUrlNode.InnerText.Trim() } else { $null }
        RepositoryUrl = if ($repositoryNode) { $repositoryNode.GetAttribute("url").Trim() } else { $null }
        Copyright = if ($copyrightNode) { $copyrightNode.InnerText.Trim() } else { $null }
    }
}

function Read-NoticeText([string]$Path) {
    $reader = [System.IO.StreamReader]::new($Path, [System.Text.UTF8Encoding]::new($false, $false), $true)
    try {
        return $reader.ReadToEnd().Replace("`r`n", "`n").Replace("`r", "`n")
    }
    finally {
        $reader.Dispose()
    }
}

$DepsPath = [System.IO.Path]::GetFullPath($DepsPath)
$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
if (-not (Test-Path -LiteralPath $DepsPath -PathType Leaf)) {
    throw "Dependency manifest was not found: $DepsPath"
}
if ([string]::IsNullOrWhiteSpace($NuGetPackagesRoot)) {
    $NuGetPackagesRoot = Join-Path $env:USERPROFILE ".nuget/packages"
}
$NuGetPackagesRoot = [System.IO.Path]::GetFullPath($NuGetPackagesRoot)
if ([string]::IsNullOrWhiteSpace($DotnetRoot)) {
    $DotnetRoot = Split-Path (Get-Command dotnet -ErrorAction Stop).Source -Parent
}
$DotnetRoot = [System.IO.Path]::GetFullPath($DotnetRoot)

if (Test-Path $OutputDirectory) { Remove-Item $OutputDirectory -Recurse -Force }
$materialRoot = Join-Path $OutputDirectory "files"
New-Item -ItemType Directory -Path $materialRoot -Force | Out-Null

$deps = Get-Content $DepsPath -Raw | ConvertFrom-Json
$dependencyEntries = @(
    $deps.libraries.PSObject.Properties |
        Where-Object { [string]$_.Value.type -in @("package", "runtimepack") } |
        Sort-Object Name
)
if ($dependencyEntries.Count -eq 0) {
    throw "No package or runtime-pack dependencies were found in $DepsPath."
}

$components = @()
foreach ($library in $dependencyEntries) {
    $kind = [string]$library.Value.type
    $separator = $library.Name.LastIndexOf('/')
    if ($separator -lt 1) { throw "Unexpected dependency key: $($library.Name)" }

    $dependencyId = $library.Name.Substring(0, $separator)
    $packageId = if ($kind -eq "runtimepack" -and $dependencyId.StartsWith("runtimepack.", [StringComparison]::OrdinalIgnoreCase)) {
        $dependencyId.Substring("runtimepack.".Length)
    }
    else {
        $dependencyId
    }
    $version = $library.Name.Substring($separator + 1)
    $candidateRoots = @(
        (Join-Path (Join-Path $NuGetPackagesRoot $packageId.ToLowerInvariant()) $version.ToLowerInvariant()),
        (Join-Path (Join-Path (Join-Path $DotnetRoot "packs") $packageId) $version)
    )
    $packageRoot = $candidateRoots |
        Where-Object { Test-Path -LiteralPath $_ -PathType Container } |
        Select-Object -First 1
    if (-not $packageRoot) {
        throw "Dependency material root is missing for $packageId/$version. Checked: $($candidateRoots -join '; ')"
    }

    $metadata = Read-NuspecMetadata $packageRoot
    $candidateFiles = @(
        Get-ChildItem $packageRoot -File -Recurse |
            Where-Object {
                $relative = [System.IO.Path]::GetRelativePath($packageRoot, $_.FullName).Replace('\', '/')
                $declaredLicense = $metadata.LicenseType -eq "file" -and
                    $relative.Equals($metadata.LicenseValue, [StringComparison]::OrdinalIgnoreCase)
                $noticeName = $_.Name -match '(?i)^(license|licence|notice|third[-_. ]?party|copyright)(?:[._ -].*)?$'
                ($declaredLicense -or $noticeName) -and $_.Length -le 4MB
            } |
            Sort-Object FullName -Unique
    )
    if ($candidateFiles.Count -eq 0) {
        throw "No local license or notice material was found for $packageId/$version."
    }

    $safeComponent = ($packageId + "-" + $version) -replace '[^A-Za-z0-9._-]', '_'
    $componentOutputRoot = Join-Path $materialRoot $safeComponent
    $noticeFiles = @()
    foreach ($file in $candidateFiles) {
        $relative = [System.IO.Path]::GetRelativePath($packageRoot, $file.FullName).Replace('\', '/')
        $copyPath = Join-Path $componentOutputRoot $relative
        New-Item -ItemType Directory -Path (Split-Path $copyPath -Parent) -Force | Out-Null
        Copy-Item $file.FullName $copyPath -Force
        $noticeFiles += [ordered]@{
            relativePath = $relative
            bundlePath = "files/$safeComponent/$relative"
            length = $file.Length
            sha256 = (Get-FileHash $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    }

    $components += [ordered]@{
        dependencyKey = $dependencyId
        id = $packageId
        version = $version
        dependencyType = $kind
        licenseType = $metadata.LicenseType
        licenseValue = $metadata.LicenseValue
        copyright = $metadata.Copyright
        projectUrl = $metadata.ProjectUrl
        repositoryUrl = $metadata.RepositoryUrl
        noticeFiles = $noticeFiles
    }
}

$depsSha256 = (Get-FileHash $DepsPath -Algorithm SHA256).Hash.ToLowerInvariant()
$noticeFileCount = @($components | ForEach-Object { $_.noticeFiles }).Count
$inventory = [ordered]@{
    schemaVersion = 1
    product = "NODAL OS"
    sourceDependencyFile = [System.IO.Path]::GetFileName($DepsPath)
    sourceDependencySha256 = $depsSha256
    componentCount = $components.Count
    noticeFileCount = $noticeFileCount
    components = $components
    legalReviewRequired = $true
    legalApprovalGranted = $false
    publicDistributionAuthorized = $false
}

$inventoryPath = Join-Path $OutputDirectory "third-party-components.json"
$inventoryJson = $inventory | ConvertTo-Json -Depth 8
[System.IO.File]::WriteAllText($inventoryPath, $inventoryJson + "`n", [System.Text.UTF8Encoding]::new($false))

$notice = [System.Text.StringBuilder]::new()
[void]$notice.Append("NODAL OS THIRD-PARTY NOTICE INVENTORY`n")
[void]$notice.Append("======================================`n`n")
[void]$notice.Append("This file is generated from the exact self-contained dependency manifest and locally restored package material.`n")
[void]$notice.Append("It is a technical inventory for review. It is not owner-approved legal text and does not authorize public distribution.`n`n")
[void]$notice.Append("Source dependency manifest: $([System.IO.Path]::GetFileName($DepsPath))`n")
[void]$notice.Append("Source dependency SHA-256: $depsSha256`n")
[void]$notice.Append("Components: $($components.Count)`n")
[void]$notice.Append("Notice/license source files: $noticeFileCount`n`n")

foreach ($component in $components) {
    [void]$notice.Append("================================================================`n")
    [void]$notice.Append("$($component.id) $($component.version)`n")
    [void]$notice.Append("Dependency type: $($component.dependencyType)`n")
    if ($component.licenseType) { [void]$notice.Append("License type: $($component.licenseType)`n") }
    if ($component.licenseValue) { [void]$notice.Append("License value: $($component.licenseValue)`n") }
    if ($component.copyright) { [void]$notice.Append("Copyright: $($component.copyright)`n") }
    if ($component.projectUrl) { [void]$notice.Append("Project: $($component.projectUrl)`n") }
    if ($component.repositoryUrl) { [void]$notice.Append("Repository: $($component.repositoryUrl)`n") }
    [void]$notice.Append("`n")

    foreach ($source in $component.noticeFiles) {
        $sourcePath = Join-Path $OutputDirectory $source.bundlePath
        [void]$notice.Append("--- $($source.relativePath) | SHA-256 $($source.sha256) ---`n")
        [void]$notice.Append((Read-NoticeText $sourcePath))
        if (-not $notice.ToString().EndsWith("`n", [StringComparison]::Ordinal)) {
            [void]$notice.Append("`n")
        }
        [void]$notice.Append("`n")
    }
}

$noticePath = Join-Path $OutputDirectory "THIRD_PARTY_NOTICES.txt"
[System.IO.File]::WriteAllText($noticePath, $notice.ToString(), [System.Text.UTF8Encoding]::new($false))

Write-Host "NODAL_OS_THIRD_PARTY_COMPONENTS=$($components.Count)"
Write-Host "NODAL_OS_THIRD_PARTY_NOTICE_FILES=$noticeFileCount"
Write-Host "NODAL_OS_THIRD_PARTY_NOTICES=$noticePath"
Write-Host "NODAL_OS_THIRD_PARTY_INVENTORY=$inventoryPath"
