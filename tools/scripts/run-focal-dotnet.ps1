#requires -Version 7.0
<#
.SYNOPSIS
    Local-only helper for focal dotnet build/test commands.

.DESCRIPTION
    Runs one bounded dotnet build or focal dotnet test command with an explicit
    timeout, optional single retry and narrow process-tree cleanup. This helper
    is for operator-run local validation only. It does not enable CI enforcement
    and must not be used as a broad suite gate.

.EXAMPLE
    pwsh -File tools/scripts/run-focal-dotnet.ps1 -Mode build -Project src/OneBrain.Core/OneBrain.Core.csproj -TimeoutSeconds 120

.EXAMPLE
    pwsh -File tools/scripts/run-focal-dotnet.ps1 -Mode test -Project tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj -Filter "FullyQualifiedName~ApprovalExecutionDesignOnlyProtectedCommonBoundaryD10Tests" -TimeoutSeconds 180
#>
[CmdletBinding()]
param(
    [ValidateSet('build', 'test')]
    [string]$Mode = 'test',

    [Parameter(Mandatory = $true)]
    [string]$Project,

    [string]$Filter = '',

    [int]$TimeoutSeconds = 120,

    [switch]$Retry,

    [switch]$ListTests,

    [ValidateSet('quiet', 'minimal', 'normal', 'detailed', 'diagnostic')]
    [string]$Verbosity = 'normal',

    [string]$Dotnet = 'dotnet'
)

$ErrorActionPreference = 'Stop'

function Write-Status([string]$status, [string]$message) {
    Write-Host ("[{0}] {1}" -f $status, $message)
}

function Resolve-RepoPath([string]$path) {
    if ([System.IO.Path]::IsPathRooted($path)) {
        return $path
    }

    return Join-Path (Get-Location).Path $path
}

function Get-ChildProcessIds([int]$parentId) {
    $children = Get-CimInstance Win32_Process -Filter "ParentProcessId = $parentId" -ErrorAction SilentlyContinue
    foreach ($child in $children) {
        [int]$child.ProcessId
        Get-ChildProcessIds -parentId ([int]$child.ProcessId)
    }
}

function Stop-ProcessTree([int]$processId) {
    $ids = @(Get-ChildProcessIds -parentId $processId) + $processId
    foreach ($id in ($ids | Select-Object -Unique | Sort-Object -Descending)) {
        Stop-Process -Id $id -Force -ErrorAction SilentlyContinue
    }
}

function Invoke-DotnetAttempt([string[]]$arguments, [int]$timeoutSeconds, [int]$attempt) {
    Write-Status 'RUN' ("attempt={0} timeout={1}s dotnet {2}" -f $attempt, $timeoutSeconds, ($arguments -join ' '))

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $Dotnet
    foreach ($argument in $arguments) {
        [void]$startInfo.ArgumentList.Add($argument)
    }
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.UseShellExecute = $false
    $startInfo.WorkingDirectory = (Get-Location).Path

    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    [void]$process.Start()

    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    $completed = $process.WaitForExit($timeoutSeconds * 1000)

    if (-not $completed) {
        Write-Status 'TIMEOUT' ("attempt={0} exceeded {1}s; stopping process tree for PID {2}" -f $attempt, $timeoutSeconds, $process.Id)
        Stop-ProcessTree -processId $process.Id
        [void]$process.WaitForExit(5000)
        Write-Host $stdoutTask.GetAwaiter().GetResult()
        [Console]::Error.WriteLine($stderrTask.GetAwaiter().GetResult())
        return 124
    }

    Write-Host $stdoutTask.GetAwaiter().GetResult()
    $stderr = $stderrTask.GetAwaiter().GetResult()
    if (-not [string]::IsNullOrWhiteSpace($stderr)) {
        [Console]::Error.WriteLine($stderr)
    }

    return $process.ExitCode
}

if ($TimeoutSeconds -lt 15) {
    throw 'TimeoutSeconds must be at least 15.'
}

$projectPath = Resolve-RepoPath $Project
if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Project not found: $Project"
}

if ($Mode -eq 'test' -and [string]::IsNullOrWhiteSpace($Filter)) {
    throw 'Focal test mode requires -Filter. Broad suite execution is not a safe local default.'
}

if ($Mode -eq 'test' -and -not $ListTests -and $Filter -eq 'FullyQualifiedName~ReentryDecisionPacketReadOnly') {
    throw 'Known broad Reentry execution filter is unsafe locally. Use a concrete class filter or -ListTests for discovery only.'
}

Write-Host 'NODAL OS local focal dotnet runner helper'
Write-Host 'Scope: local/operator-run only; no CI enforcement; prefer focal filters.'
Write-Host 'Broad execution filters are not local validation gates.'

$arguments = @($Mode, $Project, '--no-restore')
if ($Mode -eq 'build') {
    $arguments += @('-m:1', '-p:BuildInParallel=false', '-p:UseSharedCompilation=false', '-nr:false')
} else {
    $arguments += @('--no-build')
    if ($ListTests) {
        $arguments += '--list-tests'
    }
    $arguments += @('--filter', $Filter)
}
$arguments += @('-v:' + $Verbosity)

$maxAttempts = if ($Retry) { 2 } else { 1 }
$exitCode = 1

try {
    for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
        $exitCode = Invoke-DotnetAttempt -arguments $arguments -timeoutSeconds $TimeoutSeconds -attempt $attempt
        if ($exitCode -eq 0) {
            Write-Status 'PASS' ("attempt={0} exitCode=0" -f $attempt)
            exit 0
        }

        if ($attempt -lt $maxAttempts) {
            Write-Status 'RETRY' ("attempt={0} exitCode={1}; one controlled retry enabled" -f $attempt, $exitCode)
        }
    }

    Write-Status 'FAIL' ("exitCode={0}" -f $exitCode)
    exit $exitCode
}
finally {
    Write-Status 'CLEANUP' 'dotnet build-server shutdown'
    & $Dotnet build-server shutdown | Write-Host
}
