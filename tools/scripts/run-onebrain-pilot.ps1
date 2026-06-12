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
Write-Host "No browser will be opened automatically."
Write-Host "Allowed recipes are enforced by OneBrain.Pilot."

& $Dotnet run --project src/OneBrain.Pilot -- --root $Root --dotnet $Dotnet --urls $Urls
exit $LASTEXITCODE
