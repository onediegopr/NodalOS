param(
    [switch]$SkipBuild,
    [switch]$Dev
)

$ErrorActionPreference = "Stop"
Write-Host "╔══════════════════════════════════════╗"
Write-Host "║  NODAL OS — Deployment Script        ║"
Write-Host "╚══════════════════════════════════════╝"

$root = Split-Path -Parent $PSScriptRoot

# Check prerequisites
Write-Host "`n[1/5] Checking prerequisites..."
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw ".NET SDK not found" }
if (-not (Get-Command node -ErrorAction SilentlyContinue)) { throw "Node.js not found" }

$dockerAvailable = Get-Command docker -ErrorAction SilentlyContinue
if (-not $dockerAvailable -and -not $Dev) {
    Write-Warning "Docker not available. Use -Dev for local development mode."
    $Dev = $true
}

# Build .NET Bridge
if (-not $SkipBuild) {
    Write-Host "`n[2/5] Building .NET Bridge..."
    Push-Location "$root\src\OneBrain.ChromeLab.Bridge"
    dotnet publish -c Release -o bin\Release\net11.0\publish
    Pop-Location
    Write-Host "  Bridge build complete."
}

# Install stealth engine deps
Write-Host "`n[3/5] Installing Stealth Engine dependencies..."
Push-Location "$root\stealth-engine"
npm install --no-audit --no-fund 2>$null
Pop-Location
Write-Host "  Dependencies installed."

# Install stealth panel deps
Write-Host "  Installing Stealth Panel dependencies..."
Push-Location "$root\stealth-panel"
npm install --no-audit --no-fund 2>$null
Pop-Location

if ($Dev) {
    Write-Host "`n[4/5] Starting in DEVELOPMENT mode..."
    Write-Host "  Start the bridge: dotnet run --project src/OneBrain.ChromeLab.Bridge/"
    Write-Host "  Start the engine: cd stealth-engine && npm start"
    Write-Host "  Start the panel: cd stealth-panel && npx serve . -p 8789"
} else {
    # Docker mode
    Write-Host "`n[4/5] Building Docker images..."
    Push-Location $root
    docker compose build
    Pop-Location

    Write-Host "`n[5/5] Starting services..."
    Push-Location $root
    docker compose up -d
    Pop-Location

    # Wait for health
    Write-Host "  Waiting for bridge health check..."
    $retries = 0
    do {
        Start-Sleep -Seconds 3
        $retries++
        try {
            $response = Invoke-RestMethod -Uri "http://localhost:8787/health" -TimeoutSec 3 -ErrorAction SilentlyContinue
        } catch {}
    } while (-not $response -and $retries -lt 20)

    if ($response) {
        Write-Host "`n  Bridge is healthy!"
        Write-Host "  Health: http://localhost:8787/health"
        Write-Host "  Metrics: http://localhost:8787/metrics"
        Write-Host "  Stealth Panel: http://localhost:8789"
    } else {
        Write-Warning "  Bridge health check failed. Check logs with: docker compose logs bridge"
    }
}

Write-Host "`nDeployment complete."
