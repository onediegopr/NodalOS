#Requires -Version 7.2
<#
.SYNOPSIS
    Downloads PaddleOCR ONNX models for NODAL OS local OCR.
.DESCRIPTION
    Reads paddleocr-onnx-model-manifest.json and downloads each model from its configured source URL.
    Computes SHA-256 and validates against the manifest.
    Requires explicit -Confirm flag; will not run automatically.
    Does not use API keys, SaaS OCR, or customer data.
#>
param(
    [Parameter(Mandatory = $true)]
    [switch]$Confirm,

    [string]$ManifestPath = "$PSScriptRoot\paddleocr-onnx-model-manifest.json",
    [string]$OutputDirectory = "$PSScriptRoot",
    [long]$MaxTotalSizeBytes = 314572800
)

$ErrorActionPreference = "Stop"

if (-not $Confirm) {
    Write-Error "This script requires explicit -Confirm. It will download binary model files from external sources."
    exit 1
}

if (-not (Test-Path $ManifestPath)) {
    Write-Error "Manifest not found: $ManifestPath"
    exit 1
}

$manifest = Get-Content -Raw -Path $ManifestPath | ConvertFrom-Json -AsHashtable
$models = $manifest["models"]
$totalDownloaded = 0L

foreach ($model in $models) {
    $sourceUrl = $model.source.url
    $localRelativePath = $model.localRelativePath
    $expectedHash = $model.integrity.checksum
    $expectedSize = $model.integrity.fileSizeBytes
    $modelId = $model.modelId

    if ([string]::IsNullOrWhiteSpace($sourceUrl)) {
        Write-Warning "$modelId : no source URL configured; skipping."
        continue
    }

    $repoRoot = Resolve-Path -Path (Join-Path $PSScriptRoot "..\..\..\..")
    $outputPath = Join-Path -Path $repoRoot -ChildPath $localRelativePath

    $dir = Split-Path -Parent $outputPath
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }

    Write-Host "Downloading $modelId from $sourceUrl ..."
    Invoke-WebRequest -Uri $sourceUrl -OutFile $outputPath -UseBasicParsing -MaximumRedirection 5

    $fileInfo = Get-Item $outputPath
    $actualSize = $fileInfo.Length
    $totalDownloaded += $actualSize

    if ($expectedSize -gt 0 -and $actualSize -ne $expectedSize) {
        Remove-Item $outputPath -Force
        Write-Error "$modelId : size mismatch. Expected $expectedSize, got $actualSize. File removed."
        exit 1
    }

    $actualHash = (Get-FileHash -Path $outputPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if (-not [string]::IsNullOrWhiteSpace($expectedHash) -and $actualHash -ne $expectedHash.ToLowerInvariant()) {
        Remove-Item $outputPath -Force
        Write-Error "$modelId : SHA-256 mismatch. Expected $expectedHash, got $actualHash. File removed."
        exit 1
    }

    Write-Host "$modelId : OK ($actualSize bytes, SHA-256 $actualHash)"
}

if ($totalDownloaded -gt $MaxTotalSizeBytes) {
    Write-Error "Total downloaded size $totalDownloaded exceeds limit $MaxTotalSizeBytes. Run rollback-models.ps1 to clean up."
    exit 1
}

Write-Host "All models downloaded and verified. Total bytes: $totalDownloaded"
