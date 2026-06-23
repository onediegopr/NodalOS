param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [string]$StagingRelativePath = 'artifacts\manual-qa\public-variant-staging'
)

$ErrorActionPreference = 'Stop'

$extensionRoot = Join-Path $RepositoryRoot 'browser-extension\onebrain-chrome-lab'
$stagingRoot = Join-Path $RepositoryRoot $StagingRelativePath
$publicManifest = Join-Path $extensionRoot 'manifest.public.json'
$stagingManifest = Join-Path $stagingRoot 'manifest.json'

if (-not (Test-Path -LiteralPath $publicManifest)) {
    throw "Public manifest not found: $publicManifest"
}

if (Test-Path -LiteralPath $stagingRoot) {
    Remove-Item -LiteralPath $stagingRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $stagingRoot | Out-Null

$filesToCopy = @(
    'sidepanel.html',
    'sidepanel.css',
    'sidepanel.js',
    'service_worker.js',
    'content_script.js',
    'recipe_core.js',
    'README.md'
)

foreach ($fileName in $filesToCopy) {
    $source = Join-Path $extensionRoot $fileName
    if (Test-Path -LiteralPath $source) {
        Copy-Item -LiteralPath $source -Destination (Join-Path $stagingRoot $fileName)
    }
}

Copy-Item -LiteralPath $publicManifest -Destination $stagingManifest

$manifest = Get-Content -LiteralPath $stagingManifest -Raw | ConvertFrom-Json
$manifestRaw = Get-Content -LiteralPath $stagingManifest -Raw

if ($manifestRaw -match '"http://\*/\*"' -or $manifestRaw -match '"https://\*/\*"') {
    throw 'Staging manifest contains wildcard host permissions.'
}

if ($manifest.PSObject.Properties.Name -contains 'content_scripts') {
    $contentScriptsJson = $manifest.content_scripts | ConvertTo-Json -Depth 10
    if ($contentScriptsJson -match '"http://\*/\*"' -or $contentScriptsJson -match '"https://\*/\*"') {
        throw 'Staging manifest contains wildcard content script matches.'
    }
}

$fileList = Get-ChildItem -LiteralPath $stagingRoot -File |
    Sort-Object Name |
    ForEach-Object {
        [pscustomobject]@{
            name = $_.Name
            length = $_.Length
        }
    }

[pscustomobject]@{
    stagingPath = $stagingRoot
    manifestName = $manifest.name
    usesPublicManifest = $true
    hasWildcardHostPermissions = $false
    hasWildcardContentScriptMatches = $false
    files = $fileList
} | ConvertTo-Json -Depth 5
