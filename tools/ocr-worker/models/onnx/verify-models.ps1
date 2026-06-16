#Requires -Version 7.2
<#
.SYNOPSIS
    Verifies PaddleOCR ONNX models against the manifest.
.DESCRIPTION
    Checks existence, size, and SHA-256 for each model in paddleocr-onnx-model-manifest.json.
    Does not execute OCR or use SaaS.
#>
param(
    [string]$ManifestPath = "$PSScriptRoot\paddleocr-onnx-model-manifest.json"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $ManifestPath)) {
    Write-Error "Manifest not found: $ManifestPath"
    exit 1
}

$manifest = Get-Content -Raw -Path $ManifestPath | ConvertFrom-Json -AsHashtable
$models = $manifest["models"]
$repoRoot = Resolve-Path -Path (Join-Path $PSScriptRoot "..\..\..\..")
$allOk = $true

foreach ($model in $models) {
    $modelId = $model.modelId
    $localRelativePath = $model.localRelativePath
    $expectedHash = $model.integrity.checksum
    $expectedSize = $model.integrity.fileSizeBytes
    $outputPath = Join-Path -Path $repoRoot -ChildPath $localRelativePath

    if (-not (Test-Path $outputPath)) {
        Write-Host "$modelId : MISSING ($outputPath)" -ForegroundColor Red
        $allOk = $false
        continue
    }

    $fileInfo = Get-Item $outputPath
    $actualSize = $fileInfo.Length

    if ($expectedSize -gt 0 -and $actualSize -ne $expectedSize) {
        Write-Host "$modelId : SIZE MISMATCH (expected $expectedSize, got $actualSize)" -ForegroundColor Red
        $allOk = $false
        continue
    }

    if ([string]::IsNullOrWhiteSpace($expectedHash)) {
        Write-Host "$modelId : PRESENT but no expected hash configured ($actualSize bytes)" -ForegroundColor Yellow
        continue
    }

    $actualHash = (Get-FileHash -Path $outputPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualHash -ne $expectedHash.ToLowerInvariant()) {
        Write-Host "$modelId : HASH MISMATCH (expected $expectedHash, got $actualHash)" -ForegroundColor Red
        $allOk = $false
        continue
    }

    Write-Host "$modelId : VERIFIED ($actualSize bytes, SHA-256 $actualHash)" -ForegroundColor Green
}

if (-not $allOk) {
    Write-Error "One or more models failed verification."
    exit 1
}

Write-Host "All configured models verified."
