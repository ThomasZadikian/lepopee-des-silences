#Requires -Version 5.1
<#
.SYNOPSIS
    Starts the full local development environment.
.DESCRIPTION
    Launches Docker infrastructure, Game Engine API, Catalog API, Player API, and web client.
#>

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

Write-Host "=== Leds Local Dev Environment ===" -ForegroundColor Cyan
Write-Host ""

# 1. Check Docker
Write-Host "[1/5] Checking Docker..." -ForegroundColor Yellow
try {
    docker info | Out-Null
    Write-Host "  Docker is available." -ForegroundColor Green
} catch {
    Write-Host "  ERROR: Docker is not running. Please start Docker Desktop first." -ForegroundColor Red
    exit 1
}

# 2. Start infrastructure
Write-Host "[2/5] Starting PostgreSQL containers..." -ForegroundColor Yellow
docker compose -f "$repoRoot\docker-compose.dev.yml" up -d
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ERROR: Failed to start Docker containers." -ForegroundColor Red
    exit 1
}
Write-Host "  PostgreSQL containers started." -ForegroundColor Green
Write-Host "    - Game Engine DB: localhost:5432" -ForegroundColor DarkGray
Write-Host "    - Player DB:      localhost:5433" -ForegroundColor DarkGray

# 3. Start Game Engine API
Write-Host "[3/5] Starting Game Engine API..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$repoRoot\services\game-engine'; dotnet run --project src\Leds.GameEngine.Api --launch-profile http"
Write-Host "  Game Engine API starting at http://localhost:5187" -ForegroundColor Green

# 4. Start Catalog API
Write-Host "[4/5] Starting Catalog API..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$repoRoot\services\catalog'; dotnet run --project src\Leds.Catalog.Api --launch-profile http"
Write-Host "  Catalog API starting at http://localhost:5193" -ForegroundColor Green

# 5. Start Player API
Write-Host "[5/5] Starting Player API..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$repoRoot\services\player'; dotnet run --project src\Leds.Player.Api --launch-profile http"
Write-Host "  Player API starting at http://localhost:5189" -ForegroundColor Green

# 6. Start web client
Write-Host "[6/6] Starting web client..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$repoRoot\apps\game-client'; npm run dev"
Write-Host "  Web client starting at http://localhost:5173" -ForegroundColor Green

Write-Host ""
Write-Host "=== All services started ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Services:" -ForegroundColor White
Write-Host "  Game Engine API : http://localhost:5187/swagger" -ForegroundColor DarkGray
Write-Host "  Catalog API     : http://localhost:5193/swagger" -ForegroundColor DarkGray
Write-Host "  Player API      : http://localhost:5189/swagger" -ForegroundColor DarkGray
Write-Host "  Web Client      : http://localhost:5173" -ForegroundColor DarkGray
Write-Host ""
Write-Host "To stop: .\scripts\dev\stop-dev.ps1" -ForegroundColor Yellow
Write-Host "To reset DB: .\scripts\dev\reset-dev-db.ps1" -ForegroundColor Yellow
