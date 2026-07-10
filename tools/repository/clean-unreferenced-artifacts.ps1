param(
    [switch]$Apply,
    [string]$ArtifactRoot = "artifacts/agent-operations",
    [string]$ReportPath = "docs/audit/artifact-cleanup-report.md"
)

$ErrorActionPreference = "Stop"
$repoRoot = (& git rev-parse --show-toplevel).Trim()
if (-not $repoRoot) {
    throw "Repository root not found."
}

Set-Location $repoRoot

$baseSha = $env:ARTIFACT_CLEANUP_BASE_SHA
if ($baseSha) {
    & git cat-file -e "$baseSha^{commit}" 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "Artifact cleanup base commit is unavailable: $baseSha"
    }
}

$tracked = @(& git ls-files -- $ArtifactRoot | Where-Object { $_ -and $_.Trim() })
$kept = [System.Collections.Generic.List[object]]::new()
$removable = [System.Collections.Generic.List[object]]::new()

# Build the external-reference corpus once. This avoids one repository-wide
# git-grep process per artifact while preserving conservative reference checks.
$referenceFiles = @()
try {
    $referenceFiles = @(& git grep -I -l -e . -- ":!$ArtifactRoot/**" 2>$null)
}
catch {
    $referenceFiles = @()
}

$corpusBuilder = [System.Text.StringBuilder]::new()
foreach ($referenceFile in $referenceFiles) {
    try {
        $absolute = Join-Path $repoRoot $referenceFile
        if ([System.IO.File]::Exists($absolute)) {
            [void]$corpusBuilder.AppendLine([System.IO.File]::ReadAllText($absolute))
        }
    }
    catch {
        Write-Warning "Unable to inspect reference file: $referenceFile"
    }
}
$referenceCorpus = $corpusBuilder.ToString()

function Test-ExternalReference {
    param([string[]]$Needles)

    foreach ($needle in ($Needles | Where-Object { $_ } | Select-Object -Unique)) {
        if ($referenceCorpus.Contains($needle, [StringComparison]::Ordinal)) {
            return $true
        }
    }

    return $false
}

foreach ($file in $tracked) {
    $normalized = $file.Replace("\", "/")
    $reason = $null

    if ($normalized -match "(^|/)(fixtures?|golden)(/|$)" -or
        $normalized -match "(?i)(fixture|golden)[-_].*\.(json|md|html|txt|png)$") {
        $reason = "fixture-or-golden"
    }

    if (-not $reason) {
        $relativeToRoot = $normalized.Substring($ArtifactRoot.TrimEnd("/").Length).TrimStart("/")
        $basename = [System.IO.Path]::GetFileName($normalized)
        if (Test-ExternalReference -Needles @($normalized, $relativeToRoot, $basename)) {
            $reason = "referenced-outside-artifacts"
        }
    }

    if ($reason) {
        $kept.Add([pscustomobject]@{ Path = $normalized; Reason = $reason })
    }
    else {
        $removable.Add([pscustomobject]@{ Path = $normalized; Reason = "unreferenced-historical-evidence" })
    }
}

if ($Apply -and $removable.Count -gt 0) {
    $paths = @($removable | ForEach-Object { $_.Path })
    for ($offset = 0; $offset -lt $paths.Count; $offset += 100) {
        $end = [Math]::Min($offset + 99, $paths.Count - 1)
        $chunk = @($paths[$offset..$end])
        & git rm --quiet -- $chunk
        if ($LASTEXITCODE -ne 0) {
            throw "git rm failed for artifact cleanup chunk starting at index $offset."
        }
    }
}

$removedFromBase = @()
$baseTrackedCount = $tracked.Count
if ($baseSha) {
    $baseTrackedCount = @(& git ls-tree -r --name-only $baseSha -- $ArtifactRoot | Where-Object { $_ -and $_.Trim() }).Count
    $removedFromBase = @(& git diff --diff-filter=D --name-only $baseSha -- $ArtifactRoot |
        Where-Object { $_ -and $_.Trim() } |
        ForEach-Object { $_.Replace("\", "/") } |
        Sort-Object -Unique)
}
elseif ($Apply) {
    $removedFromBase = @($removable | ForEach-Object { $_.Path } | Sort-Object -Unique)
}

$reportDirectory = Split-Path -Parent $ReportPath
if ($reportDirectory) {
    New-Item -ItemType Directory -Force -Path $reportDirectory | Out-Null
}

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('# NODAL OS Artifact Cleanup Report')
$lines.Add('')
$lines.Add('Generated deterministically by `tools/repository/clean-unreferenced-artifacts.ps1`.')
$lines.Add('')
$lines.Add('## Summary')
$lines.Add('')
$lines.Add("- Base tracked artifacts: $baseTrackedCount")
$lines.Add("- Resulting tracked artifacts: $($kept.Count)")
$lines.Add("- External text files inspected for references: $($referenceFiles.Count)")
$lines.Add("- Removed from the pull-request base: $($removedFromBase.Count)")
$lines.Add("- Mode: $(if ($Apply) { 'APPLY' } else { 'DRY-RUN' })")
$lines.Add('')
$lines.Add('## Policy')
$lines.Add('')
$lines.Add('Files are kept when they are explicit fixtures/golden files or when their full path, artifact-relative path or basename is referenced outside `artifacts/agent-operations`. Other tracked micro-hito evidence is classified as historical transient output and removed from the active repository moving forward.')
$lines.Add('')
$lines.Add('## Kept files')
$lines.Add('')
foreach ($item in ($kept | Sort-Object Path | Select-Object -First 300)) {
    $lines.Add('- `' + $item.Path + '` — ' + $item.Reason)
}
if ($kept.Count -gt 300) {
    $lines.Add("- ... $($kept.Count - 300) additional kept files omitted from this report")
}
$lines.Add('')
$lines.Add('## Removed files')
$lines.Add('')
foreach ($path in ($removedFromBase | Select-Object -First 500)) {
    $lines.Add('- `' + $path + '` — unreferenced-historical-evidence')
}
if ($removedFromBase.Count -gt 500) {
    $lines.Add("- ... $($removedFromBase.Count - 500) additional removed files omitted from this report")
}
$lines.Add('')
$lines.Add('## Guardrails')
$lines.Add('')
$lines.Add('- No source, tests, fixtures or golden files are removed by this script.')
$lines.Add('- Full-path, relative-path and basename references are retained.')
$lines.Add('- The cleanup does not rewrite Git history.')
$lines.Add('- Product/runtime/release authority is unchanged.')

[System.IO.File]::WriteAllLines(
    (Join-Path $repoRoot $ReportPath),
    $lines,
    [System.Text.UTF8Encoding]::new($false))

Write-Host "Artifact cleanup complete. base=$baseTrackedCount resulting=$($kept.Count) removedFromBase=$($removedFromBase.Count) apply=$Apply"
