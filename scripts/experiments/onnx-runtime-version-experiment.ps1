#Requires -Version 7.2

[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path,
    [string]$BaselineVersion = "1.18.1",
    [string[]]$Versions = @("1.18.1", "1.22.1", "1.23.2", "1.25.0"),
    [string]$SelectedVersion = "",
    [int]$TimeoutSeconds = 900
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repo = (Resolve-Path $RepoRoot).Path
$projectPath = Join-Path $repo "src\OneBrain.BrowserExecutor.Cdp\OneBrain.BrowserExecutor.Cdp.csproj"
$solutionPath = Join-Path $repo "OneBrain.slnx"
$artifactDir = Join-Path $repo "artifacts\ocr-vision-onnx\m234"
$runnerProject = Join-Path $repo "tools\onnx-ocr-probe-runner\OneBrain.Tools.OnnxOcrProbeRunner.csproj"
$filter = "OnnxRuntimeVersionExperiment|RecognizerRuntimeExperiment|RecognizerRuntimeCompatibility|OnnxOcrProbeRunner|OnnxOutOfProcessGuard|OnnxModelVerification"

New-Item -ItemType Directory -Force -Path $artifactDir | Out-Null
$originalProject = Get-Content -LiteralPath $projectPath -Raw

function Set-OnnxRuntimeVersion {
    param([string]$Version)

    $content = Get-Content -LiteralPath $projectPath -Raw
    $updated = $content -replace '(<PackageReference\s+Include="Microsoft\.ML\.OnnxRuntime"\s+Version=")[^"]+(")', "`${1}$Version`${2}"
    if ($updated -eq $content -and $content -notmatch "Microsoft\.ML\.OnnxRuntime") {
        throw "Microsoft.ML.OnnxRuntime PackageReference not found in $projectPath"
    }

    Set-Content -LiteralPath $projectPath -Value $updated -NoNewline
}

function Invoke-Step {
    param(
        [string]$Name,
        [string[]]$Arguments,
        [string]$Version
    )

    $safeName = $Name -replace '[^A-Za-z0-9_.-]', '-'
    $stdout = Join-Path $artifactDir "$Version-$safeName.stdout.txt"
    $stderr = Join-Path $artifactDir "$Version-$safeName.stderr.txt"

    $process = Start-Process -FilePath $Arguments[0] `
        -ArgumentList $Arguments[1..($Arguments.Length - 1)] `
        -WorkingDirectory $repo `
        -NoNewWindow `
        -PassThru `
        -Wait `
        -RedirectStandardOutput $stdout `
        -RedirectStandardError $stderr

    return [ordered]@{
        name = $Name
        exitCode = $process.ExitCode
        stdout = $stdout
        stderr = $stderr
        succeeded = ($process.ExitCode -eq 0)
    }
}

function Read-JsonArray {
    param([string]$Path)

    if (!(Test-Path -LiteralPath $Path)) {
        return @()
    }

    $text = Get-Content -LiteralPath $Path -Raw
    if ([string]::IsNullOrWhiteSpace($text)) {
        return @()
    }

    try {
        $parsed = $text | ConvertFrom-Json
        if ($null -eq $parsed) {
            return @()
        }

        if ($parsed -is [array]) {
            return @($parsed)
        }

        return @($parsed)
    } catch {
        return @()
    }
}

function Get-OptionalProperty {
    param(
        [object]$Object,
        [string]$Name
    )

    if ($null -eq $Object) {
        return $null
    }

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $null
    }

    return $property.Value
}

function Any-OptionalBoolean {
    param(
        [object[]]$Items,
        [string]$Name,
        [bool]$Expected
    )

    foreach ($item in $Items) {
        $value = Get-OptionalProperty -Object $item -Name $Name
        if ($null -ne $value -and [bool]$value -eq $Expected) {
            return $true
        }
    }

    return $false
}

function Get-RecognizerOutcome {
    param([string]$RecognizerStdout)

    $items = Read-JsonArray -Path $RecognizerStdout
    $defaultResults = @($items | Where-Object { $_.SessionOption -eq "Default" })

    $zero = @($defaultResults | Where-Object { $_.TensorKind -eq "Zero" } | Select-Object -First 1)
    $ones = @($defaultResults | Where-Object { $_.TensorKind -eq "Ones" } | Select-Object -First 1)
    $gradient = @($defaultResults | Where-Object { $_.TensorKind -eq "Gradient" } | Select-Object -First 1)
    $crops = @($defaultResults | Where-Object { $_.TensorKind -in @("SyntheticTextCrop", "HighContrastManualCrop", "DetectorDerivedCrop") })
    $runSuccess = @($items | Where-Object {
        $_.Status -in @("RunSucceeded", "OutputMetadataCaptured", "BlockedByDictionaryClassCountMismatch") -or
        ((Get-OptionalProperty -Object $_ -Name "ExitCode") -eq 0 -and
         ([string](Get-OptionalProperty -Object $_ -Name "StdErrSummary")).Contains("stage=run-succeeded"))
    })
    $crashes = @($items | Where-Object { $_.Status -eq "NativeRuntimeCrashContained" })
    $firstCrash = @($crashes | Select-Object -First 1)

    return [ordered]@{
        zeroSucceeded = ($zero.Count -gt 0 -and ($zero[0].Status -in @("RunSucceeded", "OutputMetadataCaptured", "BlockedByDictionaryClassCountMismatch") -or ([string](Get-OptionalProperty -Object $zero[0] -Name "StdErrSummary")).Contains("stage=run-succeeded")))
        onesSucceeded = ($ones.Count -gt 0 -and ($ones[0].Status -in @("RunSucceeded", "OutputMetadataCaptured", "BlockedByDictionaryClassCountMismatch") -or ([string](Get-OptionalProperty -Object $ones[0] -Name "StdErrSummary")).Contains("stage=run-succeeded")))
        gradientSucceeded = ($gradient.Count -gt 0 -and ($gradient[0].Status -in @("RunSucceeded", "OutputMetadataCaptured", "BlockedByDictionaryClassCountMismatch") -or ([string](Get-OptionalProperty -Object $gradient[0] -Name "StdErrSummary")).Contains("stage=run-succeeded")))
        cropSucceeded = (@($crops | Where-Object { $_.Status -in @("RunSucceeded", "OutputMetadataCaptured", "BlockedByDictionaryClassCountMismatch") -or ([string](Get-OptionalProperty -Object $_ -Name "StdErrSummary")).Contains("stage=run-succeeded") }).Count -gt 0)
        anyRunSucceeded = ($runSuccess.Count -gt 0)
        anyCrashContained = ($crashes.Count -gt 0)
        exitCode = if ($firstCrash.Count -gt 0) { Get-OptionalProperty -Object $firstCrash[0] -Name "ExitCode" } else { $null }
        exitCodeHex = if ($firstCrash.Count -gt 0) { Get-OptionalProperty -Object $firstCrash[0] -Name "ExitCodeHex" } else { $null }
        crashStage = if ($firstCrash.Count -gt 0) {
            $stage = Get-OptionalProperty -Object $firstCrash[0] -Name "CrashStage"
            if ([string]::IsNullOrWhiteSpace($stage)) { "RecognitionRun/session.Run" } else { $stage }
        } else { "" }
        parentSurvived = ($items.Count -eq 0 -or !(Any-OptionalBoolean -Items $items -Name "ParentSurvived" -Expected $false))
        tempCleaned = ($items.Count -eq 0 -or !(Any-OptionalBoolean -Items $items -Name "TempFilesCleaned" -Expected $false))
        rawPersisted = (Any-OptionalBoolean -Items $items -Name "RawPersisted" -Expected $true)
        callsSaas = (Any-OptionalBoolean -Items $items -Name "CallsSaas" -Expected $true)
        noAuthority = ($items.Count -eq 0 -or !(Any-OptionalBoolean -Items $items -Name "NoAuthority" -Expected $false))
    }
}

function Get-DetectorOutcome {
    param([string]$DetectorStdout)

    $items = Read-JsonArray -Path $DetectorStdout
    $success = @($items | Where-Object { $_.Status -in @("RunSucceeded", "OutputMetadataCaptured") })
    return ($items.Count -gt 0 -and $success.Count -gt 0)
}

$results = @()

try {
    foreach ($version in $Versions) {
        Write-Host "Testing Microsoft.ML.OnnxRuntime $version"
        Set-OnnxRuntimeVersion -Version $version

        $restore = Invoke-Step -Name "restore" -Version $version -Arguments @("dotnet", "restore", $solutionPath)
        if (!$restore.succeeded) {
            $results += [ordered]@{
                runtimeVersion = $version
                requestedPackageVersion = $version
                restoredPackageVersion = ""
                nativeRuntimeObserved = ""
                restoreSucceeded = $false
                buildSucceeded = $false
                detectorSanitySucceeded = $false
                recognizerZeroSucceeded = $false
                recognizerOnesSucceeded = $false
                recognizerGradientSucceeded = $false
                recognizerCropSucceeded = $false
                anyRecognizerRunSucceeded = $false
                anyRecognizerCrashContained = $false
                exitCode = $null
                exitCodeHex = $null
                crashStage = ""
                parentSurvived = $true
                tempFilesCleaned = $true
                rawPersisted = $false
                callsSaas = $false
                noAuthority = $true
                status = "RuntimeVersionRestoreFailed"
                reason = "restore failed"
                steps = @($restore)
            }
            continue
        }

        $build = Invoke-Step -Name "build" -Version $version -Arguments @("dotnet", "build", $solutionPath, "--no-restore")
        if (!$build.succeeded) {
            $results += [ordered]@{
                runtimeVersion = $version
                requestedPackageVersion = $version
                restoredPackageVersion = $version
                nativeRuntimeObserved = ""
                restoreSucceeded = $true
                buildSucceeded = $false
                detectorSanitySucceeded = $false
                recognizerZeroSucceeded = $false
                recognizerOnesSucceeded = $false
                recognizerGradientSucceeded = $false
                recognizerCropSucceeded = $false
                anyRecognizerRunSucceeded = $false
                anyRecognizerCrashContained = $false
                exitCode = $null
                exitCodeHex = $null
                crashStage = ""
                parentSurvived = $true
                tempFilesCleaned = $true
                rawPersisted = $false
                callsSaas = $false
                noAuthority = $true
                status = "BuildFailed"
                reason = "build failed"
                steps = @($restore, $build)
            }
            continue
        }

        $tests = Invoke-Step -Name "filtered-tests" -Version $version -Arguments @("dotnet", "test", $solutionPath, "--no-build", "--no-restore", "--filter", $filter)
        $detector = Invoke-Step -Name "detector-sanity" -Version $version -Arguments @("dotnet", "run", "--no-build", "--project", $runnerProject, "--", "--detector-crash-probe", "--repo-root", $repo, "--timeout-ms", ($TimeoutSeconds * 1000).ToString())
        $recognizer = Invoke-Step -Name "recognizer-runtime-experiment" -Version $version -Arguments @("dotnet", "run", "--no-build", "--project", $runnerProject, "--", "--recognizer-runtime-experiment", "--repo-root", $repo, "--timeout-ms", ($TimeoutSeconds * 1000).ToString())
        $recognizerOutcome = Get-RecognizerOutcome -RecognizerStdout $recognizer.stdout
        $detectorOk = $detector.succeeded -and (Get-DetectorOutcome -DetectorStdout $detector.stdout)

        $status = if (!$tests.succeeded -or !$detector.succeeded -or !$recognizer.succeeded) {
            "ProbeFailed"
        } elseif ($recognizerOutcome.anyRunSucceeded) {
            "RecognizerRunSucceeded"
        } elseif ($recognizerOutcome.anyCrashContained) {
            "RecognizerNativeRuntimeCrashContained"
        } else {
            "ProbeFailed"
        }

        $results += [ordered]@{
            runtimeVersion = $version
            requestedPackageVersion = $version
            restoredPackageVersion = $version
            nativeRuntimeObserved = "Microsoft.ML.OnnxRuntime $version"
            restoreSucceeded = $true
            buildSucceeded = $true
            detectorSanitySucceeded = $detectorOk
            recognizerZeroSucceeded = $recognizerOutcome.zeroSucceeded
            recognizerOnesSucceeded = $recognizerOutcome.onesSucceeded
            recognizerGradientSucceeded = $recognizerOutcome.gradientSucceeded
            recognizerCropSucceeded = $recognizerOutcome.cropSucceeded
            anyRecognizerRunSucceeded = $recognizerOutcome.anyRunSucceeded
            anyRecognizerCrashContained = $recognizerOutcome.anyCrashContained
            exitCode = $recognizerOutcome.exitCode
            exitCodeHex = $recognizerOutcome.exitCodeHex
            crashStage = $recognizerOutcome.crashStage
            parentSurvived = $recognizerOutcome.parentSurvived
            tempFilesCleaned = $recognizerOutcome.tempCleaned
            rawPersisted = $recognizerOutcome.rawPersisted
            callsSaas = $recognizerOutcome.callsSaas
            noAuthority = $recognizerOutcome.noAuthority
            status = $status
            reason = if ($recognizerOutcome.anyRunSucceeded) { "recognizer run succeeded" } else { "recognizer did not produce a successful run" }
            steps = @($restore, $build, $tests, $detector, $recognizer)
        }
    }
} finally {
    if ([string]::IsNullOrWhiteSpace($SelectedVersion)) {
        Set-Content -LiteralPath $projectPath -Value $originalProject -NoNewline
    } else {
        Set-OnnxRuntimeVersion -Version $SelectedVersion
    }
}

$summary = [ordered]@{
    generatedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    baselineVersion = $BaselineVersion
    versions = $Versions
    selectedVersion = $SelectedVersion
    packageReferenceProject = $projectPath
    revertedToBaseline = [string]::IsNullOrWhiteSpace($SelectedVersion)
    productiveOcrBlocked = $true
    shadowModeBlocked = $true
    cpuProviderOnly = $true
    noSaas = $true
    noRawPersistence = $true
    noAuthority = $true
    results = $results
}

$summaryPath = Join-Path $artifactDir "onnx-runtime-version-experiment-generated-summary.json"
$summary | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $summaryPath
Write-Host "Wrote $summaryPath"
