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

$tracked = @(& git ls-files -- $ArtifactRoot | Where-Object { $_ -and $_.Trim() })
$kept = [System.Collections.Generic.List[object]]::new()
$removable = [System.Collections.Generic.List[object]]::new()

function Find-ReferencesOutsideArtifacts {
    param([string[]]$Needles)

    foreach ($needle in ($Needles | Where-Object { $_ } | Select-Object -Unique)) {
        try {
            $matches = @(& git grep -l -F -- $needle -- ":!$ArtifactRoot/**" 2>$null)
            if ($matches.Count -gt 0) {
                return @($matches)
            }
        }
        catch {
            # git grep returns a non-zero exit code when no matches exist.
        }
    }

    return @()
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
        $references = Find-ReferencesOutsideArtifacts -Needles @(
            $normalized,
            $relativeToRoot,
            $basename
        )

        if ($references.Count -gt 0) {
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

$reportDirectory = Split-Path -Parent $ReportPath
if ($reportDirectory) {
    New-Item -ItemType Directory -Force -Path $reportDirectory | Out-Null
}

$timestamp = [DateTimeOffset]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add("# NODAL OS Artifact Cleanup Report")
$lines.Add("")
$lines.Add("Generated: $timestamp")
$lines.Add("")
$lines.Add("## Summary")
$lines.Add("")
$lines.Add("- Tracked files inspected: $($tracked.Count)")
$lines.Add("- Kept: $($kept.Count)")
$lines.Add("- Removed or proposed for removal: $($removable.Count)")
$lines.Add("- Mode: $(if ($Apply) { 'APPLY' } else { 'DRY-RUN' })")
$lines.Add("")
$lines.Add("## Policy")
$lines.Add("")
$lines.Add("Files are kept when they are explicit fixtures/golden files or when their full path, artifact-relative path or basename is referenced outside `artifacts/agent-operations`. Other tracked micro-hito evidence is classified as historical transient output and removed from the active repository moving forward.")
$lines.Add("")
$lines.Add("## Kept files")
$lines.Add("")
foreach ($item in ($kept | Sort-Object Path | Select-Object -First 300)) {
    $lines.Add("- `$($item.Path)` — $($item.Reason)")
}
if ($kept.Count -gt 300) {
    $lines.Add("- ... $($kept.Count - 300) additional kept files omitted from this report")
}
$lines.Add("")
$lines.Add("## Removed files")
$lines.Add("")
foreach ($item in ($removable | Sort-Object Path | Select-Object -First 500)) {
    $lines.Add("- `$($item.Path)` — $($item.Reason)")
}
if ($removable.Count -gt 500) {
    $lines.Add("- ... $($removable.Count - 500) additional removed files omitted from this report")
}
$lines.Add("")
$lines.Add("## Guardrails")
$lines.Add("")
$lines.Add("- No source, tests, fixtures or golden files are removed by this script.")
$lines.Add("- Full-path, relative-path and basename references are retained.")
$lines.Add("- The cleanup does not rewrite Git history.")
$lines.Add("- Product/runtime/release authority is unchanged.")

[System.IO.File]::WriteAllLines(
    (Join-Path $repoRoot $ReportPath),
    $lines,
    [System.Text.UTF8Encoding]::new($false))

Write-Host "Artifact cleanup complete. inspected=$($tracked.Count) kept=$($kept.Count) removable=$($removable.Count) apply=$Apply"
