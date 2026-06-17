#Requires -Version 7.2
<#
.SYNOPSIS
    Rolls back candidate recognizer+dictionary files only.
.DESCRIPTION
    Deletes files listed in the recognizer pair candidate manifest. It refuses to delete current
    detector/classifier/current recognizer model paths.
#>
param(
    [Parameter(Mandatory = $true)]
    [switch]$Confirm,

    [string]$ManifestPath = "$PSScriptRoot\paddleocr-recognizer-pair-candidates-manifest.json",
    [string]$PairId = "rapidocr-modelscope-ppocrv5-en-mobile-onnx"
)

$ErrorActionPreference = "Stop"

if (-not $Confirm) {
    Write-Error "This script requires explicit -Confirm. It deletes only candidate recognizer pair files."
    exit 1
}

if (-not (Test-Path $ManifestPath)) {
    Write-Error "Manifest not found: $ManifestPath"
    exit 1
}

$repoRoot = Resolve-Path -Path (Join-Path $PSScriptRoot "..\..\..\..")
$protected = @(
    (Join-Path $repoRoot "tools/ocr-worker/models/onnx/ch_PP-OCRv4_det.onnx"),
    (Join-Path $repoRoot "tools/ocr-worker/models/onnx/ch_PP-OCRv4_rec.onnx"),
    (Join-Path $repoRoot "tools/ocr-worker/models/onnx/ch_ppocr_mobile_v2.0_cls.onnx")
) | ForEach-Object { [IO.Path]::GetFullPath($_) }

$manifest = Get-Content -Raw -Path $ManifestPath | ConvertFrom-Json
$pair = @($manifest.pairs | Where-Object { $_.pairId -eq $PairId })[0]
if ($null -eq $pair) {
    Write-Error "Pair not found in manifest: $PairId"
    exit 1
}

$targets = @(
    (Join-Path $repoRoot $pair.recognizerModel.localRelativePath),
    (Join-Path $repoRoot $pair.dictionary.localRelativePath)
) | ForEach-Object { [IO.Path]::GetFullPath($_) }

foreach ($target in $targets) {
    if ($protected -contains $target) {
        Write-Error "Refusing to delete protected current model path: $target"
        exit 1
    }

    if (Test-Path $target) {
        Remove-Item -LiteralPath $target -Force
        Write-Host "Removed candidate file: $target"
    } else {
        Write-Host "Candidate file already absent: $target"
    }
}

Write-Host "Recognizer pair rollback complete: $PairId"
