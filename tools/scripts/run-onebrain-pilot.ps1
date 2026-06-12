param(
  [string]$Root = "C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo",
  [string]$Dotnet = "C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Herramientas\dotnet-sdk-11.0.100-preview.5.26302.115-win-x64\dotnet.exe",
  [string]$Urls = "http://127.0.0.1:5084"
)

$ErrorActionPreference = "Stop"

Set-Location $Root

$remote = git remote get-url origin
if ($remote -notmatch "onediegopr/OneBrain(\.git)?$") {
  throw "Unexpected repository remote: $remote"
}

if (-not (Test-Path $Dotnet)) {
  throw "Portable dotnet not found: $Dotnet"
}

Write-Host "ONE_BRAIN_PILOT_URL=$Urls"
Write-Host "PILOT_ROUTE_HOME=$Urls/"
Write-Host "PILOT_ROUTE_RECIPES=$Urls/recipes"
Write-Host "PILOT_ROUTE_VARIABLES=$Urls/variables"
Write-Host "PILOT_ROUTE_MEMORY=$Urls/memory"
Write-Host "PILOT_ROUTE_APP_PROFILES=$Urls/app-profiles"
Write-Host "PILOT_ROUTE_APPROVALS=$Urls/approvals/demo"
Write-Host "PILOT_ROUTE_EXECUTOR_HARNESS=$Urls/executor-harness"
Write-Host "PILOT_ROUTE_RUNS=$Urls/runs"
Write-Host "PILOT_ROUTE_AI_CONFIG=$Urls/ai/config"
Write-Host "PILOT_ROUTE_AI_AUDIT=$Urls/ai/audit"
Write-Host "No browser will be opened automatically."
Write-Host "Allowed recipes are enforced by OneBrain.Pilot."

& $Dotnet run --project src/OneBrain.Pilot -- --root $Root --dotnet $Dotnet --urls $Urls
exit $LASTEXITCODE
