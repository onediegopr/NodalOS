#Requires -Version 5.1
# DEPRECATED: historical diagnostic-only Python worker health check.
# Active OCR path uses ONNX .NET and this script is retained only for historical auditability.
<#
.SYNOPSIS
    Checks whether PaddleOCR local runtime is installed and healthy.
.DESCRIPTION
    No secrets. No network. No SaaS. Reports JSON status for NODAL OS gates.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot
$VenvPath = Join-Path $Root ".venv"

$status = @{
    pythonAvailable = $false
    pythonVersion = $null
    pipAvailable = $false
    venvAvailable = $false
    paddleOcrInstalled = $false
    paddlePaddleInstalled = $false
    venvPath = $VenvPath
    timestamp = (Get-Date -Format o)
}

try {
    if (Get-Command python -ErrorAction SilentlyContinue) {
        $status.pythonAvailable = $true
        $status.pythonVersion = (python --version 2>&1).ToString()
    }
    if (Get-Command pip -ErrorAction SilentlyContinue) {
        $status.pipAvailable = $true
    }
    if ($status.pythonAvailable) {
        $null = python -m venv --help 2>&1
        $status.venvAvailable = $LASTEXITCODE -eq 0
    }

    if (Test-Path (Join-Path $VenvPath "Scripts\python.exe")) {
        $py = Join-Path $VenvPath "Scripts\python.exe"
        $verify = & $py -c "import paddle; import paddleocr; print('OK')" 2>&1
        if ($verify -contains "OK") {
            $status.paddleOcrInstalled = $true
            $status.paddlePaddleInstalled = $true
        }
    }
} catch {
    $status.error = $_.Exception.Message
}

$status | ConvertTo-Json -Depth 3
