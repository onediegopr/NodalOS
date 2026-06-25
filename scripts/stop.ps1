param([switch]$RemoveVolumes)

$root = Split-Path -Parent $PSScriptRoot
Push-Location $root

Write-Host "Stopping NODAL OS services..."
if ($RemoveVolumes) {
    docker compose down -v
} else {
    docker compose down
}

Pop-Location
Write-Host "Done."
