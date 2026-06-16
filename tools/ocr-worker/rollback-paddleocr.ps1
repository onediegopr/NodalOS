#Requires -Version 5.1
<#
.SYNOPSIS
    Rolls back the isolated PaddleOCR installation.
.DESCRIPTION
    Removes venv and any temp files. Requires confirmation.
    No secrets. No SaaS. No impact outside tools/ocr-worker.
.PARAMETER Force
    Skip confirmation. Use only in automation with human oversight.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot
$VenvPath = Join-Path $Root ".venv"
$LogPath = Join-Path $Root "rollback-paddleocr.log"

function Write-Log($message) {
    $line = "$(Get-Date -Format o) $message"
    Write-Host $line
    Add-Content -Path $LogPath -Value $line -ErrorAction SilentlyContinue
}

if (-not $Force) {
    $confirm = Read-Host "Remove PaddleOCR venv at $VenvPath ? (yes/no)"
    if ($confirm -ne "yes") {
        Write-Log "Rollback cancelled by user."
        exit 0
    }
}

if ($PSCmdlet.ShouldProcess($VenvPath, "Remove PaddleOCR venv")) {
    if (Test-Path $VenvPath) {
        Remove-Item -Recurse -Force $VenvPath
        Write-Log "Removed $VenvPath"
    } else {
        Write-Log "No venv found at $VenvPath"
    }
}

# Clean temp redacted crop files if any remain.
$tempDir = Join-Path $Root "tmp"
if (Test-Path $tempDir) {
    Get-ChildItem $tempDir -Filter "redacted-crop-*" | Remove-Item -Force -ErrorAction SilentlyContinue
    Write-Log "Cleaned temp redacted crop files"
}

Write-Log "Rollback complete. Production OCR remains disabled."
