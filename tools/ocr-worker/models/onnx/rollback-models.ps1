#Requires -Version 7.2
<#
.SYNOPSIS
    Removes downloaded PaddleOCR ONNX model files.
.DESCRIPTION
    Deletes only .onnx files under tools/ocr-worker/models/onnx.
    Requires explicit -Confirm flag.
    Does not delete code, manifest, scripts, data, or images.
#>
param(
    [Parameter(Mandatory = $true)]
    [switch]$Confirm
)

$ErrorActionPreference = "Stop"

if (-not $Confirm) {
    Write-Error "This script requires explicit -Confirm. It will delete .onnx model files."
    exit 1
}

$targetDir = Resolve-Path -Path $PSScriptRoot
$files = Get-ChildItem -Path $targetDir -Filter "*.onnx"

foreach ($file in $files) {
    Write-Host "Removing $($file.FullName) ..."
    Remove-Item -Path $file.FullName -Force
}

Write-Host "Rollback complete. Removed $($files.Count) .onnx file(s)."
