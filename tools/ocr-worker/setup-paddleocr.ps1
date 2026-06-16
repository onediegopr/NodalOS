#Requires -Version 5.1
<#
.SYNOPSIS
    Installs PaddleOCR and PaddlePaddle into an isolated local venv.
.DESCRIPTION
    Production-grade local install for NODAL OS OCR worker.
    No secrets. No SaaS. No global system contamination.
    If install fails, exits with non-zero code and leaves a log.
.PARAMETER WhatIf
    Show what would be done without installing.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot
$VenvPath = Join-Path $Root ".venv"
$LogPath = Join-Path $Root "setup-paddleocr.log"

function Write-Log($message) {
    $line = "$(Get-Date -Format o) $message"
    Write-Host $line
    Add-Content -Path $LogPath -Value $line -ErrorAction SilentlyContinue
}

if (-not (Get-Command python -ErrorAction SilentlyContinue)) {
    Write-Log "ERROR: python not found. Install Python 3.9-3.12 (PaddlePaddle does not yet support 3.13)."
    exit 1
}

$pythonVersion = python --version 2>&1
Write-Log "Python version: $pythonVersion"

if ($pythonVersion -match "3\.13") {
    Write-Log "WARNING: Python 3.13 is not supported by PaddlePaddle/PaddleOCR wheels. Blocking install."
    Write-Log "ACTION: Install Python 3.9, 3.10, 3.11 or 3.12 and re-run."
    exit 2
}

if (-not (Get-Command pip -ErrorAction SilentlyContinue)) {
    Write-Log "ERROR: pip not found."
    exit 1
}

if ($WhatIf) {
    Write-Log "WhatIf: would create venv at $VenvPath and install paddlepaddle paddleocr"
    exit 0
}

if (Test-Path $VenvPath) {
    Write-Log "Existing venv found at $VenvPath; removing for clean install."
    Remove-Item -Recurse -Force $VenvPath
}

Write-Log "Creating venv at $VenvPath"
python -m venv $VenvPath
if ($LASTEXITCODE -ne 0) {
    Write-Log "ERROR: venv creation failed."
    exit 1
}

$pip = Join-Path $VenvPath "Scripts\pip.exe"
if (-not (Test-Path $pip)) {
    Write-Log "ERROR: pip not found inside venv at $pip"
    exit 1
}

Write-Log "Upgrading pip"
& $pip install --upgrade pip | Out-String | ForEach-Object { Write-Log $_ }

Write-Log "Installing PaddlePaddle (CPU)"
& $pip install paddlepaddle | Out-String | ForEach-Object { Write-Log $_ }
if ($LASTEXITCODE -ne 0) {
    Write-Log "ERROR: paddlepaddle install failed."
    exit 1
}

Write-Log "Installing PaddleOCR"
& $pip install paddleocr | Out-String | ForEach-Object { Write-Log $_ }
if ($LASTEXITCODE -ne 0) {
    Write-Log "ERROR: paddleocr install failed."
    exit 1
}

Write-Log "Verifying imports"
$pythonVenv = Join-Path $VenvPath "Scripts\python.exe"
$verify = & $pythonVenv -c "import paddle; import paddleocr; print('PADDLEOCR_OK')" 2>&1
Write-Log $verify
if ($verify -notcontains "PADDLEOCR_OK") {
    Write-Log "ERROR: import verification failed."
    exit 1
}

Write-Log "SUCCESS: PaddleOCR installed in $VenvPath"
Write-Log "REMINDER: production public OCR remains disabled by policy gates."
