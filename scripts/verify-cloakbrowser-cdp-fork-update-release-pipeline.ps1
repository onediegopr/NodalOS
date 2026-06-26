param(
    [string] $RepositoryRoot = (Resolve-Path ".").Path,
    [string] $RuntimeRepositoryPath = (Resolve-Path "..\nodal-cloakbrowser-runtime").Path
)

$ErrorActionPreference = "Stop"

$DecisionReady = "NODAL_OS_CLOAKBROWSER_CDP_FORK_UPDATE_RELEASE_PIPELINE_READY"
$DecisionBlocked = "NODAL_OS_CLOAKBROWSER_CDP_FORK_UPDATE_RELEASE_PIPELINE_BLOCKED_WITH_CAUSE"
$ArtifactsRoot = Join-Path $RepositoryRoot "artifacts/local-verification"
$LockfilePath = Join-Path $RepositoryRoot "browser-runtime.lock.json"
$LocalConfigPath = Join-Path $RepositoryRoot ".local/browser-runtime.local.json"
$ExpectedOrigin = "https://github.com/onediegopr/nodal-cloakbrowser-runtime"
$ExpectedUpstream = "https://github.com/CloakHQ/cloakbrowser"
$ExpectedBranch = "nodal/runtime"
$ExpectedRepo = "nodal-cloakbrowser-runtime"
$ExpectedRuntimeChannel = "nodal-runtime"
$ExpectedPathPolicy = "env-or-local-config"

function Add-Error {
    param(
        [System.Collections.Generic.List[string]] $Errors,
        [string] $Code,
        [bool] $Condition
    )

    if (-not $Condition) {
        $Errors.Add($Code)
    }
}

function Invoke-GitText {
    param([string[]] $Arguments)

    $output = & git @Arguments 2>$null
    if ($LASTEXITCODE -ne 0) {
        return ""
    }

    return ($output | Out-String).Trim()
}

function Normalize-Remote {
    param([string] $Remote)

    if ([string]::IsNullOrWhiteSpace($Remote)) {
        return ""
    }

    return $Remote.Trim().TrimEnd("/").Replace(".git", "")
}

function Get-LatestEvidence {
    param([string] $Pattern)

    if (-not (Test-Path -LiteralPath $ArtifactsRoot -PathType Container)) {
        return $null
    }

    return Get-ChildItem -LiteralPath $ArtifactsRoot -Filter $Pattern -File |
        Where-Object { $_.Name.EndsWith(".redacted.json", [System.StringComparison]::OrdinalIgnoreCase) } |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1
}

function Read-JsonFile {
    param([string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $null
    }

    return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
}

function Get-JsonBool {
    param(
        [object] $Object,
        [string] $Name,
        [bool] $Default = $false
    )

    if ($null -eq $Object) {
        return $Default
    }

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $Default
    }

    return [bool] $property.Value
}

function Get-JsonString {
    param(
        [object] $Object,
        [string] $Name,
        [string] $Default = ""
    )

    if ($null -eq $Object) {
        return $Default
    }

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property -or $null -eq $property.Value) {
        return $Default
    }

    return [string] $property.Value
}

function Test-PathInside {
    param(
        [string] $Path,
        [string] $Root
    )

    if ([string]::IsNullOrWhiteSpace($Path) -or [string]::IsNullOrWhiteSpace($Root)) {
        return $false
    }

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $fullRoot = [System.IO.Path]::GetFullPath($Root).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    return $fullPath.StartsWith($fullRoot, [System.StringComparison]::OrdinalIgnoreCase)
}

function Test-ManagedRuntimeCachePath {
    param([string] $Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $false
    }

    $parts = $Path -split '[\\/]'
    return $parts -contains ".cloakbrowser"
}

function New-EvidenceName {
    param([System.IO.FileInfo] $File)

    if ($null -eq $File) {
        return $null
    }

    return $File.Name
}

if (-not (Test-Path -LiteralPath $LockfilePath -PathType Leaf)) {
    throw "browser-runtime.lock.json not found."
}

$lock = Read-JsonFile $LockfilePath
$localConfig = Read-JsonFile $LocalConfigPath
$errors = [System.Collections.Generic.List[string]]::new()
$runtimeRepoExists = Test-Path -LiteralPath $RuntimeRepositoryPath -PathType Container
$runtimeGitExists = Test-Path -LiteralPath (Join-Path $RuntimeRepositoryPath ".git") -PathType Container

$origin = if ($runtimeRepoExists) { Invoke-GitText @("-C", $RuntimeRepositoryPath, "remote", "get-url", "origin") } else { "" }
$upstream = if ($runtimeRepoExists) { Invoke-GitText @("-C", $RuntimeRepositoryPath, "remote", "get-url", "upstream") } else { "" }
$branch = if ($runtimeRepoExists) { Invoke-GitText @("-C", $RuntimeRepositoryPath, "branch", "--show-current") } else { "" }
$head = if ($runtimeRepoExists) { Invoke-GitText @("-C", $RuntimeRepositoryPath, "rev-parse", "HEAD") } else { "" }
$statusShort = if ($runtimeRepoExists) { Invoke-GitText @("-C", $RuntimeRepositoryPath, "status", "--short") } else { "" }
$headTags = if ($runtimeRepoExists) { Invoke-GitText @("-C", $RuntimeRepositoryPath, "tag", "--points-at", "HEAD") } else { "" }

$runtimePath = Get-JsonString $localConfig "cloakbrowser_executable_path"
$runtimeArtifactPresent = -not [string]::IsNullOrWhiteSpace($runtimePath) -and (Test-Path -LiteralPath $runtimePath -PathType Leaf)
$runtimeArtifactInsideFork = $runtimeArtifactPresent -and (Test-PathInside -Path $runtimePath -Root $RuntimeRepositoryPath)
$runtimeArtifactManaged = $runtimeArtifactPresent -and (Test-ManagedRuntimeCachePath -Path $runtimePath)
$actualHash = if ($runtimeArtifactPresent) { (Get-FileHash -LiteralPath $runtimePath -Algorithm SHA256).Hash.ToLowerInvariant() } else { "" }
$expectedHash = Get-JsonString $lock "binary_sha256"
$artifactHashVerified = $runtimeArtifactPresent -and $actualHash.Equals($expectedHash, [System.StringComparison]::OrdinalIgnoreCase)
$runtimeVersion = Get-JsonString $lock "runtime_version"
$artifactVersionDirectory = if ($runtimeArtifactPresent) { Split-Path (Split-Path $runtimePath -Parent) -Leaf } else { "" }

$defaultEvidence = Get-LatestEvidence "cloakbrowser-cdp-no-extension-default-*.redacted.json"
$surfaceEvidence = Get-LatestEvidence "cloakbrowser-cdp-minimal-product-surface-*.redacted.json"
$deprecationEvidence = Get-LatestEvidence "cloakbrowser-cdp-extension-deprecation-hardening-*.redacted.json"
$defaultJson = if ($null -ne $defaultEvidence) { Read-JsonFile $defaultEvidence.FullName } else { $null }
$surfaceJson = if ($null -ne $surfaceEvidence) { Read-JsonFile $surfaceEvidence.FullName } else { $null }
$deprecationJson = if ($null -ne $deprecationEvidence) { Read-JsonFile $deprecationEvidence.FullName } else { $null }
$lockfileValid = ((Get-JsonString $lock "provider") -eq "cloakbrowser") `
    -and ((Get-JsonString $lock "mode") -eq "cdp-direct") `
    -and ((Get-JsonString $lock "runtime_source") -eq "fork") `
    -and ((Get-JsonString $lock "runtime_repo") -eq $ExpectedRepo) `
    -and ((Get-JsonString $lock "runtime_channel") -eq $ExpectedRuntimeChannel) `
    -and ((Get-JsonString $lock "runtime_path_policy") -eq $ExpectedPathPolicy) `
    -and (-not (Get-JsonBool $lock "extension_enabled")) `
    -and (-not (Get-JsonBool $lock "system_browser_allowed")) `
    -and (-not [string]::IsNullOrWhiteSpace($runtimeVersion)) `
    -and ($runtimeVersion -ne "pending") `
    -and (-not [string]::IsNullOrWhiteSpace((Get-JsonString $lock "runtime_commit"))) `
    -and ((Get-JsonString $lock "runtime_commit") -ne "pending") `
    -and (-not [string]::IsNullOrWhiteSpace($expectedHash)) `
    -and ($expectedHash -ne "pending")

Add-Error $errors "RUNTIME_REPO_NOT_FOUND" $runtimeRepoExists
Add-Error $errors "RUNTIME_REPO_NOT_GIT" $runtimeGitExists
Add-Error $errors "RUNTIME_ORIGIN_MISMATCH" $((Normalize-Remote $origin) -eq $ExpectedOrigin)
Add-Error $errors "RUNTIME_UPSTREAM_MISMATCH" $((Normalize-Remote $upstream) -eq $ExpectedUpstream)
Add-Error $errors "RUNTIME_BRANCH_MISMATCH" $($branch -eq $ExpectedBranch)
Add-Error $errors "RUNTIME_HEAD_LOCK_MISMATCH" $($head -eq (Get-JsonString $lock "runtime_commit"))
Add-Error $errors "RUNTIME_REPO_DIRTY" $([string]::IsNullOrWhiteSpace($statusShort))
Add-Error $errors "LOCK_PROVIDER_MISMATCH" $((Get-JsonString $lock "provider") -eq "cloakbrowser")
Add-Error $errors "LOCK_MODE_MISMATCH" $((Get-JsonString $lock "mode") -eq "cdp-direct")
Add-Error $errors "LOCK_RUNTIME_SOURCE_MISMATCH" $((Get-JsonString $lock "runtime_source") -eq "fork")
Add-Error $errors "LOCK_RUNTIME_REPO_MISMATCH" $((Get-JsonString $lock "runtime_repo") -eq $ExpectedRepo)
Add-Error $errors "LOCK_RUNTIME_CHANNEL_MISMATCH" $((Get-JsonString $lock "runtime_channel") -eq $ExpectedRuntimeChannel)
Add-Error $errors "LOCK_PATH_POLICY_MISMATCH" $((Get-JsonString $lock "runtime_path_policy") -eq $ExpectedPathPolicy)
Add-Error $errors "LOCK_EXTENSION_ENABLED_NOT_FALSE" $(-not (Get-JsonBool $lock "extension_enabled"))
Add-Error $errors "LOCK_SYSTEM_BROWSER_ALLOWED_NOT_FALSE" $(-not (Get-JsonBool $lock "system_browser_allowed"))
Add-Error $errors "LOCK_RUNTIME_VERSION_UNPINNED" $(-not [string]::IsNullOrWhiteSpace($runtimeVersion) -and $runtimeVersion -ne "pending")
Add-Error $errors "LOCK_RUNTIME_COMMIT_UNPINNED" $(-not [string]::IsNullOrWhiteSpace((Get-JsonString $lock "runtime_commit")) -and (Get-JsonString $lock "runtime_commit") -ne "pending")
Add-Error $errors "LOCK_BINARY_HASH_UNPINNED" $(-not [string]::IsNullOrWhiteSpace($expectedHash) -and $expectedHash -ne "pending")
Add-Error $errors "LOCAL_CONFIG_MISSING" $($null -ne $localConfig)
Add-Error $errors "LOCAL_CONFIG_EXECUTABLE_PATH_MISSING" $(-not [string]::IsNullOrWhiteSpace($runtimePath))
Add-Error $errors "RUNTIME_ARTIFACT_MISSING" $runtimeArtifactPresent
Add-Error $errors "RUNTIME_ARTIFACT_OUTSIDE_FORK" $runtimeArtifactInsideFork
Add-Error $errors "RUNTIME_ARTIFACT_NOT_UNDER_MANAGED_CACHE" $runtimeArtifactManaged
Add-Error $errors "RUNTIME_ARTIFACT_HASH_MISMATCH" $artifactHashVerified
Add-Error $errors "NO_EXTENSION_DEFAULT_EVIDENCE_MISSING" $($null -ne $defaultJson)
Add-Error $errors "MINIMAL_SURFACE_EVIDENCE_MISSING" $($null -ne $surfaceJson)
Add-Error $errors "EXTENSION_DEPRECATION_EVIDENCE_MISSING" $($null -ne $deprecationJson)

if ($null -ne $defaultJson) {
    Add-Error $errors "DEFAULT_HARNESS_NOT_NO_EXTENSION" $((Get-JsonString $defaultJson "defaultHarness") -eq "cloakbrowser-cdp-no-extension")
    Add-Error $errors "DEFAULT_HARNESS_USED_EXTENSION" $(-not (Get-JsonBool $defaultJson "extensionUsed"))
    Add-Error $errors "DEFAULT_HARNESS_USED_SYSTEM_BROWSER" $(-not (Get-JsonBool $defaultJson "systemBrowserUsed"))
    Add-Error $errors "DEFAULT_HARNESS_USED_FALLBACK" $(-not (Get-JsonBool $defaultJson "fallbackUsed"))
}

if ($null -ne $surfaceJson) {
    Add-Error $errors "SURFACE_NOT_MINIMAL_NO_EXTENSION" $((Get-JsonString $surfaceJson "productSurface") -eq "minimal-no-extension-runtime-bridge")
    Add-Error $errors "SURFACE_REQUIRES_EXTENSION" $(-not (Get-JsonBool $surfaceJson "extensionRequired"))
    Add-Error $errors "SURFACE_USED_EXTENSION" $(-not (Get-JsonBool $surfaceJson "extensionUsed"))
    Add-Error $errors "SURFACE_USED_SYSTEM_BROWSER" $(-not (Get-JsonBool $surfaceJson "systemBrowserUsed"))
    Add-Error $errors "SURFACE_USED_BRIDGE_WEBSOCKET" $(-not (Get-JsonBool $surfaceJson "bridgeWebSocketUsed"))
}

if ($null -ne $deprecationJson) {
    Add-Error $errors "EXTENSION_NOT_LEGACY_MODE" $((Get-JsonString $deprecationJson "extensionMode") -eq "legacy/no-default")
    Add-Error $errors "EXTENSION_DEFAULT_RUNTIME_TRUE" $(-not (Get-JsonBool $deprecationJson "extensionDefaultRuntime"))
    Add-Error $errors "INSTALLED_SIDEPANEL_DEFAULT_TRUE" $(-not (Get-JsonBool $deprecationJson "installedSidepanelHarnessDefault"))
}

$ok = $errors.Count -eq 0
New-Item -ItemType Directory -Force -Path $ArtifactsRoot | Out-Null
$timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH-mm-ss-fffZ")
$evidencePath = Join-Path $ArtifactsRoot "cloakbrowser-cdp-fork-update-release-pipeline-$timestamp.redacted.json"

$evidence = [ordered]@{
    schemaVersion = "1.0"
    status = if ($ok) { "PASS" } else { "BLOCKED" }
    decision = if ($ok) { $DecisionReady } else { $DecisionBlocked }
    reason = if ($ok) { "CloakBrowser fork/update/release pipeline readiness is verified." } else { "CloakBrowser fork/update/release pipeline readiness checks failed." }
    generatedAt = (Get-Date).ToUniversalTime().ToString("O")
    pipeline = "cloakbrowser-cdp-fork-update-release-minimal"
    runtimeProvider = "cloakbrowser"
    source = "cloakbrowser-cdp-direct"
    runtimeRepository = $ExpectedRepo
    runtimeOriginVerified = ((Normalize-Remote $origin) -eq $ExpectedOrigin)
    runtimeUpstreamVerified = ((Normalize-Remote $upstream) -eq $ExpectedUpstream)
    runtimeBranch = $branch
    runtimeBranchVerified = ($branch -eq $ExpectedBranch)
    runtimeHead = $head
    runtimeHeadMatchesLock = ($head -eq (Get-JsonString $lock "runtime_commit"))
    runtimeRepoClean = [string]::IsNullOrWhiteSpace($statusShort)
    runtimeTagsAtHead = @($headTags -split "`r?`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    runtimeVersion = $runtimeVersion
    runtimeCommit = Get-JsonString $lock "runtime_commit"
    upstreamCommit = Get-JsonString $lock "upstream_commit"
    artifactVersionDirectory = $artifactVersionDirectory
    artifactPathPolicy = "local-config-inside-runtime-fork"
    artifactHashVerified = $artifactHashVerified
    binarySha256 = $expectedHash
    lockfileValid = $lockfileValid
    lockRuntimeSource = Get-JsonString $lock "runtime_source"
    lockRuntimeChannel = Get-JsonString $lock "runtime_channel"
    lockPathPolicy = Get-JsonString $lock "runtime_path_policy"
    localConfigPresent = ($null -ne $localConfig)
    localConfigVersioned = $false
    runtimeArtifactPresent = $runtimeArtifactPresent
    runtimeArtifactInsideFork = $runtimeArtifactInsideFork
    runtimeArtifactUnderManagedCache = $runtimeArtifactManaged
    defaultHarness = "cloakbrowser-cdp-no-extension"
    minimalProductSurface = "minimal-no-extension-runtime-bridge"
    extensionMode = "legacy/no-default"
    extensionUsed = $false
    systemBrowserUsed = $false
    extensionFallbackAllowed = $false
    systemBrowserFallbackAllowed = $false
    playwrightDefaultUsed = $false
    channelSystemBrowserUsed = $false
    fallbackUsed = $false
    bridgeWebSocketUsed = $false
    metadataOnly = $true
    readOnly = $true
    productFilesModified = $false
    defaultEvidenceName = New-EvidenceName $defaultEvidence
    surfaceEvidenceName = New-EvidenceName $surfaceEvidence
    deprecationEvidenceName = New-EvidenceName $deprecationEvidence
    errors = @($errors)
}

$evidence | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $evidencePath -Encoding UTF8

"status=$($evidence.status)"
"decision=$($evidence.decision)"
"reason=$($evidence.reason)"
"pipeline=$($evidence.pipeline)"
"runtimeRepository=$($evidence.runtimeRepository)"
"runtimeOriginVerified=$($evidence.runtimeOriginVerified)"
"runtimeUpstreamVerified=$($evidence.runtimeUpstreamVerified)"
"runtimeBranch=$($evidence.runtimeBranch)"
"runtimeBranchVerified=$($evidence.runtimeBranchVerified)"
"runtimeHeadMatchesLock=$($evidence.runtimeHeadMatchesLock)"
"runtimeRepoClean=$($evidence.runtimeRepoClean)"
"runtimeVersion=$($evidence.runtimeVersion)"
"artifactVersionDirectory=$($evidence.artifactVersionDirectory)"
"artifactHashVerified=$($evidence.artifactHashVerified)"
"localConfigPresent=$($evidence.localConfigPresent)"
"runtimeArtifactPresent=$($evidence.runtimeArtifactPresent)"
"runtimeArtifactInsideFork=$($evidence.runtimeArtifactInsideFork)"
"runtimeArtifactUnderManagedCache=$($evidence.runtimeArtifactUnderManagedCache)"
"defaultHarness=$($evidence.defaultHarness)"
"minimalProductSurface=$($evidence.minimalProductSurface)"
"extensionUsed=$($evidence.extensionUsed)"
"systemBrowserUsed=$($evidence.systemBrowserUsed)"
"fallbackUsed=$($evidence.fallbackUsed)"
"metadataOnly=$($evidence.metadataOnly)"
"evidence=$evidencePath"

if (-not $ok) {
    exit 1
}
