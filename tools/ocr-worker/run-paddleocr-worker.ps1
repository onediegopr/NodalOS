#Requires -Version 5.1
<#
.SYNOPSIS
    Runs the PaddleOCR local worker in controlled mode.
.DESCRIPTION
    Only accepts redacted crop JSON. No secrets. No SaaS. No raw persistence.
    Writes JSON result to stdout. Production public OCR remains disabled.
.PARAMETER RequestJson
    JSON request following nodal-paddleocr-worker.v1 contract.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RequestJson
)

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot
$VenvPath = Join-Path $Root ".venv"
$WorkerPath = Join-Path $Root "paddleocr_worker.py"

if (-not (Test-Path (Join-Path $VenvPath "Scripts\python.exe"))) {
    Write-Error "PaddleOCR venv not found. Run setup-paddleocr.ps1 first." -ErrorAction Stop
}

if (-not (Test-Path $WorkerPath)) {
    Write-Error "Worker script not found: $WorkerPath" -ErrorAction Stop
}

$py = Join-Path $VenvPath "Scripts\python.exe"
$encoded = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($RequestJson))
& $py $WorkerPath --request-base64 $encoded
