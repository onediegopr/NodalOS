param(
    [string]$RunnerTemp = $env:RUNNER_TEMP,
    [string]$BaseUrl = "http://127.0.0.1:5102",
    [int]$ProviderPort = 5527
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($RunnerTemp)) {
    $RunnerTemp = Join-Path ([System.IO.Path]::GetTempPath()) "nodal-os-ci"
}
New-Item -ItemType Directory -Path $RunnerTemp -Force | Out-Null

$fixtureRoot = Join-Path $RunnerTemp "nodal-real-byok-model-fixture"
$metadataPath = Join-Path $fixtureRoot "models/byok.v1.json"
$secretRoot = Join-Path $fixtureRoot "secrets"
$primaryKey = "primary-smoke-key"
$fallbackKey = "fallback-smoke-key"
$responseContent = "NODAL_BYOK_SMOKE_OK"
$providerUrl = "http://127.0.0.1:$ProviderPort"

if (Test-Path $fixtureRoot) { Remove-Item $fixtureRoot -Recurse -Force }
New-Item -ItemType Directory -Path $fixtureRoot -Force | Out-Null

function Extract-Token([string]$Html) {
    $field = "byokModelToken"
    $match = [regex]::Match($Html, ('name="' + $field + '" value="(?<token>[0-9a-f]+)"'))
    if (-not $match.Success) { throw "BYOK model request token was not rendered." }
    return $match.Groups["token"].Value
}

function Assert-NoLeak([string]$Text, [string]$Surface) {
    foreach ($forbidden in @($primaryKey, $fallbackKey, $responseContent)) {
        if ($Text.IndexOf($forbidden, [StringComparison]::Ordinal) -ge 0) {
            throw "$Surface leaked a credential or provider response."
        }
    }
}

function Start-ProviderFixture {
    $stdout = Join-Path $RunnerTemp "byok-provider-fixture.stdout.log"
    $stderr = Join-Path $RunnerTemp "byok-provider-fixture.stderr.log"
    $process = Start-Process python -ArgumentList @(
        "eng/ci/openai_compatible_fixture_server.py",
        "--port", $ProviderPort
    ) -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr

    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        if ($process.HasExited) { throw "OpenAI-compatible fixture server exited before readiness." }
        try {
            $health = Invoke-RestMethod -Uri "$providerUrl/health" -TimeoutSec 2
            if ($health.status -eq "ready") {
                return [pscustomobject]@{ Process = $process; Stdout = $stdout; Stderr = $stderr }
            }
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    }
    throw "OpenAI-compatible fixture server did not become ready."
}

function Start-Pilot([string]$Suffix) {
    $stdout = Join-Path $RunnerTemp "pilot-real-byok-$Suffix.stdout.log"
    $stderr = Join-Path $RunnerTemp "pilot-real-byok-$Suffix.stderr.log"
    $process = Start-Process dotnet -ArgumentList @(
        "run",
        "--project", "src/OneBrain.Pilot/OneBrain.Pilot.csproj",
        "--configuration", "Release",
        "--no-build",
        "--",
        "--urls", $BaseUrl
    ) -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr

    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        if ($process.HasExited) { throw "Pilot exited before the BYOK model surface became ready." }
        try {
            Invoke-RestMethod -Uri "$BaseUrl/api/models/config" -TimeoutSec 2 | Out-Null
            return [pscustomobject]@{ Process = $process; Stdout = $stdout; Stderr = $stderr }
        }
        catch {
            Start-Sleep -Seconds 1
        }
    }
    throw "BYOK model surface did not become ready."
}

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:NODAL_OS_BYOK_MODEL_METADATA_PATH = $metadataPath
$env:NODAL_OS_BYOK_MODEL_SECRET_ROOT = $secretRoot
$provider = $null
$first = $null
$second = $null

try {
    $provider = Start-ProviderFixture
    $first = Start-Pilot "first"
    $session = [Microsoft.PowerShell.Commands.WebRequestSession]::new()

    $form = Invoke-WebRequest -Uri "$BaseUrl/models/config" -WebSession $session -TimeoutSec 15
    $token = Extract-Token $form.Content
    if ($form.StatusCode -ne 200 -or $form.Content -notmatch 'data-configured="false"') {
        throw "BYOK model surface did not start unconfigured."
    }

    $configuredPage = Invoke-WebRequest `
        -Uri "$BaseUrl/models/config" `
        -Method Post `
        -WebSession $session `
        -Headers @{ Origin = $BaseUrl } `
        -Body @{
            byokModelToken = $token
            primaryProviderId = "primary-provider"
            primaryDisplayName = "Primary Smoke Provider"
            primaryProviderType = "OpenAiCompatibleLocal"
            primaryEndpoint = "$providerUrl/v1"
            primaryModelId = "primary-smoke-model"
            primaryApiKey = $primaryKey
            enableFallback = "on"
            fallbackProviderId = "fallback-provider"
            fallbackDisplayName = "Fallback Smoke Provider"
            fallbackProviderType = "OpenAiCompatibleLocal"
            fallbackEndpoint = "$providerUrl/v1"
            fallbackModelId = "fallback-smoke-model"
            fallbackApiKey = $fallbackKey
            maximumTotalCostUsd = "1"
            perAttemptTimeoutSeconds = "5"
            primaryInputCostPerMillion = "1"
            primaryOutputCostPerMillion = "2"
            fallbackInputCostPerMillion = "0.5"
            fallbackOutputCostPerMillion = "1"
        } `
        -TimeoutSec 30
    if ($configuredPage.StatusCode -ne 200 -or
        $configuredPage.Content -notmatch 'data-configured="true"' -or
        $configuredPage.Content -notmatch 'data-connected="false"') {
        throw "BYOK model configuration was not persisted for connection testing."
    }
    Assert-NoLeak $configuredPage.Content "Configured model page"
    if (-not (Test-Path $metadataPath)) { throw "BYOK model metadata was not persisted." }
    if (-not (Test-Path $secretRoot)) { throw "BYOK secure credential store was not created." }

    $metadata = [System.IO.File]::ReadAllText($metadataPath)
    Assert-NoLeak $metadata "BYOK metadata"
    if ($metadata -notmatch 'primary-provider' -or $metadata -notmatch 'fallback-provider' -or $metadata -notmatch 'dpapi-current-user') {
        throw "BYOK metadata is incomplete or does not use DPAPI references."
    }
    foreach ($secretFile in Get-ChildItem $secretRoot -File -Recurse) {
        $secretProjection = [System.Text.Encoding]::UTF8.GetString([System.IO.File]::ReadAllBytes($secretFile.FullName))
        Assert-NoLeak $secretProjection "Encrypted credential file"
    }

    $token = Extract-Token $configuredPage.Content
    $testedPage = Invoke-WebRequest `
        -Uri "$BaseUrl/models/test" `
        -Method Post `
        -WebSession $session `
        -Headers @{ Origin = $BaseUrl } `
        -Body @{ byokModelToken = $token } `
        -TimeoutSec 30
    if ($testedPage.StatusCode -ne 200 -or
        $testedPage.Content -notmatch 'data-connected="true"' -or
        $testedPage.Content -notmatch 'data-fallback-applied="true"' -or
        $testedPage.Content -notmatch 'data-real-provider-call="true"' -or
        $testedPage.Content -notmatch 'data-network-used="true"' -or
        $testedPage.Content -notmatch 'data-secrets-excluded="true"') {
        throw "Real BYOK connection did not complete through the authorized fallback."
    }
    Assert-NoLeak $testedPage.Content "Tested model page"

    $snapshot = Invoke-RestMethod -Uri "$BaseUrl/api/models/config" -TimeoutSec 20
    if (-not $snapshot.configured -or
        -not $snapshot.connected -or
        -not $snapshot.fallbackApplied -or
        -not $snapshot.realProviderCallAttempted -or
        -not $snapshot.networkUsed -or
        -not $snapshot.secretsExcluded -or
        $snapshot.productAuthorityGranted -or
        $snapshot.selectedProviderId -ne "fallback-provider" -or
        $snapshot.selectedModelId -ne "fallback-smoke-model" -or
        $snapshot.attemptCount -ne 2 -or
        $snapshot.responseSha256.Length -ne 64) {
        throw "BYOK model snapshot boundary flags are invalid."
    }

    $control = Invoke-RestMethod -Uri "$BaseUrl/api/mission-control" -TimeoutSec 20
    if (-not $control.byokConfigured -or
        -not $control.modelConnectionVerified -or
        -not $control.modelFallbackApplied -or
        $control.activeProvider -ne "fallback-provider" -or
        $control.activeModel -ne "fallback-smoke-model" -or
        $control.logicalModel -ne "standard_task" -or
        -not $control.networkUsed -or
        -not $control.externalIoUsed -or
        $control.productAuthorityGranted) {
        throw "Mission Control did not project the verified BYOK route safely."
    }
    $controlPage = Invoke-WebRequest -Uri "$BaseUrl/" -TimeoutSec 20
    if ($controlPage.Content -notmatch 'data-byok-configured="true"' -or
        $controlPage.Content -notmatch 'data-model-connection-verified="true"' -or
        $controlPage.Content -notmatch 'data-model-fallback-applied="true"' -or
        $controlPage.Content -notmatch '/models/config') {
        throw "Mission Control BYOK anchors are missing."
    }
    Assert-NoLeak $controlPage.Content "Mission Control"

    $metadata = [System.IO.File]::ReadAllText($metadataPath)
    Assert-NoLeak $metadata "Tested BYOK metadata"
    if ($metadata -notmatch $snapshot.responseSha256 -or $metadata -notmatch 'byok-model-connection-verification') {
        throw "BYOK metadata does not include the redacted verification result."
    }

    if (-not $first.Process.HasExited) { Stop-Process -Id $first.Process.Id -Force }
    $first = $null
    Start-Sleep -Seconds 1

    $second = Start-Pilot "restart"
    $rehydrated = Invoke-RestMethod -Uri "$BaseUrl/api/models/config" -TimeoutSec 20
    if (-not $rehydrated.configured -or
        -not $rehydrated.connected -or
        -not $rehydrated.rehydrated -or
        -not $rehydrated.fallbackApplied -or
        $rehydrated.selectedProviderId -ne "fallback-provider" -or
        $rehydrated.responseSha256 -ne $snapshot.responseSha256 -or
        $rehydrated.productAuthorityGranted) {
        throw "Verified BYOK model connection did not rehydrate after process restart."
    }

    $restartSession = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
    $clearForm = Invoke-WebRequest -Uri "$BaseUrl/models/config" -WebSession $restartSession -TimeoutSec 15
    $clearToken = Extract-Token $clearForm.Content
    $clearedPage = Invoke-WebRequest `
        -Uri "$BaseUrl/models/clear" `
        -Method Post `
        -WebSession $restartSession `
        -Headers @{ Origin = $BaseUrl } `
        -Body @{ byokModelToken = $clearToken } `
        -TimeoutSec 20
    if ($clearedPage.StatusCode -ne 200 -or $clearedPage.Content -notmatch 'data-configured="false"') {
        throw "BYOK model configuration did not clear safely."
    }
    if (Test-Path $metadataPath) { throw "BYOK metadata remained after clear." }
    if (Test-Path $secretRoot) {
        $remainingSecrets = @(Get-ChildItem $secretRoot -File -Recurse -ErrorAction SilentlyContinue)
        if ($remainingSecrets.Count -ne 0) { throw "BYOK credential references remained after clear." }
    }
}
finally {
    foreach ($entry in @($first, $second, $provider)) {
        if ($null -ne $entry -and -not $entry.Process.HasExited) {
            Stop-Process -Id $entry.Process.Id -Force
        }
        if ($null -ne $entry) {
            Get-Content $entry.Stdout -ErrorAction SilentlyContinue
            Get-Content $entry.Stderr -ErrorAction SilentlyContinue
        }
    }
    Remove-Item Env:NODAL_OS_BYOK_MODEL_METADATA_PATH -ErrorAction SilentlyContinue
    Remove-Item Env:NODAL_OS_BYOK_MODEL_SECRET_ROOT -ErrorAction SilentlyContinue
    if (Test-Path $fixtureRoot) {
        Get-ChildItem $fixtureRoot -File -Recurse -Force -ErrorAction SilentlyContinue | ForEach-Object {
            $_.Attributes = [System.IO.FileAttributes]::Normal
        }
        Remove-Item $fixtureRoot -Recurse -Force
    }
}
