param(
    [string] $Root = "C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo",
    [string] $Dotnet = "C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Herramientas\dotnet-sdk-11.0.100-preview.5.26302.115-win-x64\dotnet.exe"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $Root)) {
    throw "Root path not found: $Root"
}

if (-not (Test-Path -LiteralPath $Dotnet)) {
    throw "Portable dotnet not found: $Dotnet"
}

Set-Location $Root

$remote = git remote get-url origin
if ($remote -notmatch "onediegopr/OneBrain(\.git)?$") {
    throw "Unexpected repository remote: $remote"
}

$recipe = "tools/recipes/demo-product-evidence-report.json"
if (-not (Test-Path -LiteralPath $recipe)) {
    throw "Demo recipe not found: $recipe"
}

Write-Host "ONE BRAIN demo: product evidence report"
Write-Host "Root: $Root"
Write-Host "Dotnet: $Dotnet"
Write-Host "Recipe: $recipe"

& $Dotnet run --project src/OneBrain.Cli -- recipe run $recipe
$code = $LASTEXITCODE
Write-Host "DEMO_EXIT_CODE=$code"
if ($code -ne 0) {
    throw "Demo recipe failed with exit code $code"
}

$reportsDir = Join-Path $Root "artifacts/product-evidence-demo-reports"
if (Test-Path -LiteralPath $reportsDir) {
    $latest = Get-ChildItem -LiteralPath $reportsDir -Filter "*.md" |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($latest) {
        Write-Host "LATEST_DEMO_MARKDOWN=$($latest.FullName)"
    } else {
        Write-Host "LATEST_DEMO_MARKDOWN=<none>"
    }
} else {
    Write-Host "LATEST_DEMO_MARKDOWN=<reports directory missing>"
}
