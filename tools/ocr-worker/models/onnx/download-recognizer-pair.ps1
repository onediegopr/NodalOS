#Requires -Version 7.2
<#
.SYNOPSIS
    Downloads candidate recognizer+dictionary pairs for NODAL OS local OCR experiments.
.DESCRIPTION
    Downloads only pinned candidate sources from paddleocr-recognizer-pair-candidates-manifest.json.
    Verifies SHA-256 and configured sizes. Does not modify current detector/classifier/current recognizer.
#>
param(
    [Parameter(Mandatory = $true)]
    [switch]$Confirm,

    [string]$ManifestPath = "$PSScriptRoot\paddleocr-recognizer-pair-candidates-manifest.json",
    [string]$PairId = "rapidocr-modelscope-ppocrv5-en-mobile-onnx",
    [long]$MaxTotalSizeBytes = 104857600
)

$ErrorActionPreference = "Stop"

if (-not $Confirm) {
    Write-Error "This script requires explicit -Confirm. It downloads candidate OCR model files from pinned sources."
    exit 1
}

if (-not (Test-Path $ManifestPath)) {
    Write-Error "Manifest not found: $ManifestPath"
    exit 1
}

function Resolve-RepoPath([string]$relativePath) {
    $repoRoot = Resolve-Path -Path (Join-Path $PSScriptRoot "..\..\..\..")
    return Join-Path -Path $repoRoot -ChildPath $relativePath
}

function Download-And-Verify([string]$id, [string]$url, [string]$relativePath, [string]$expectedHash, [long]$expectedSize) {
    if ([string]::IsNullOrWhiteSpace($url) -or [string]::IsNullOrWhiteSpace($expectedHash)) {
        Write-Error "$id : source URL and SHA-256 are required."
        exit 1
    }

    $outputPath = Resolve-RepoPath $relativePath
    $dir = Split-Path -Parent $outputPath
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }

    Write-Host "Downloading $id from $url ..."
    Invoke-WebRequest -Uri $url -OutFile $outputPath -UseBasicParsing -MaximumRedirection 5

    $actualSize = (Get-Item $outputPath).Length
    if ($expectedSize -gt 0 -and $actualSize -ne $expectedSize) {
        Remove-Item -LiteralPath $outputPath -Force
        Write-Error "$id : size mismatch. Expected $expectedSize, got $actualSize. File removed."
        exit 1
    }

    $actualHash = (Get-FileHash -Path $outputPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualHash -ne $expectedHash.ToLowerInvariant()) {
        Remove-Item -LiteralPath $outputPath -Force
        Write-Error "$id : SHA-256 mismatch. Expected $expectedHash, got $actualHash. File removed."
        exit 1
    }

    Write-Host "$id : OK ($actualSize bytes, SHA-256 $actualHash)"
    return $actualSize
}

$manifest = Get-Content -Raw -Path $ManifestPath | ConvertFrom-Json
$pair = @($manifest.pairs | Where-Object { $_.pairId -eq $PairId })[0]
if ($null -eq $pair) {
    Write-Error "Pair not found in manifest: $PairId"
    exit 1
}

$total = 0L
$total += Download-And-Verify $pair.recognizerModel.modelId $pair.recognizerModel.sourceUrl $pair.recognizerModel.localRelativePath $pair.recognizerModel.sha256 ([long]$pair.recognizerModel.fileSizeBytes)
$total += Download-And-Verify $pair.dictionary.dictionaryId $pair.dictionary.sourceUrl $pair.dictionary.localRelativePath $pair.dictionary.sha256 ([long]$pair.dictionary.fileSizeBytes)

if ($total -gt $MaxTotalSizeBytes) {
    Write-Error "Total downloaded size $total exceeds limit $MaxTotalSizeBytes. Run rollback-recognizer-pair.ps1 for this candidate."
    exit 1
}

Write-Host "Recognizer pair downloaded and verified. Pair=$PairId TotalBytes=$total"
