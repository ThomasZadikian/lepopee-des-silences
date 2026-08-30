#Requires -Version 5.1
<#
.SYNOPSIS
    Starts the full local development environment (optimized version).
.DESCRIPTION
    Launches Docker infrastructure, then starts all APIs and web client in background jobs.
    All output is logged to files for easy debugging.
#>

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$logDir = Join-Path $repoRoot "logs"

# Create logs directory if it doesn't exist
if (!(Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir -Force | Out-Null
}

Write-Host "=== Leds Local Dev Environment (Optimized) ===" -ForegroundColor Cyan
Write-Host ""

# 1. Check Docker
Write-Host "[1/6] Checking Docker..." -ForegroundColor Yellow
try {
    docker info | Out-Null
    Write-Host "  [OK] Docker is available." -ForegroundColor Green
} catch {
    Write-Host "  [ERROR] Docker is not running. Please start Docker Desktop first." -ForegroundColor Red
    exit 1
}

# 2. Start infrastructure
Write-Host "[2/6] Starting PostgreSQL containers..." -ForegroundColor Yellow
docker compose -f "$repoRoot\docker-compose.dev.yml" up -d
if ($LASTEXITCODE -ne 0) {
    Write-Host "  [ERROR] Failed to start Docker containers." -ForegroundColor Red
    exit 1
}
Write-Host "  [OK] PostgreSQL containers started." -ForegroundColor Green
Write-Host "    - Game Engine DB: localhost:5432" -ForegroundColor DarkGray
Write-Host "    - Player DB:      localhost:5433" -ForegroundColor DarkGray
Write-Host "    - Catalog DB:     localhost:5434" -ForegroundColor DarkGray

# 3. Start Game Engine API
Write-Host "[3/6] Starting Game Engine API..." -ForegroundColor Yellow
$gameEngineLog = Join-Path $logDir "game-engine.log"
$gameEngineJob = Start-Job -ScriptBlock {
    param($repoRoot, $logFile)
    Set-Location "$repoRoot\services\game-engine"
    dotnet run --project src\Leds.GameEngine.Api --launch-profile http 2>&1 | Tee-Object -FilePath $logFile
} -ArgumentList $repoRoot, $gameEngineLog
Write-Host "  [OK] Game Engine API starting at http://localhost:5187" -ForegroundColor Green
Write-Host "    Log: $gameEngineLog" -ForegroundColor DarkGray

# 4. Start Catalog API
Write-Host "[4/6] Starting Catalog API..." -ForegroundColor Yellow
$catalogLog = Join-Path $logDir "catalog.log"
$catalogJob = Start-Job -ScriptBlock {
    param($repoRoot, $logFile)
    Set-Location "$repoRoot\services\catalog"
    dotnet run --project src\Leds.Catalog.Api --launch-profile http 2>&1 | Tee-Object -FilePath $logFile
} -ArgumentList $repoRoot, $catalogLog
Write-Host "  [OK] Catalog API starting at http://localhost:5193" -ForegroundColor Green
Write-Host "    Log: $catalogLog" -ForegroundColor DarkGray

# 5. Start Player API
Write-Host "[5/6] Starting Player API..." -ForegroundColor Yellow
$playerLog = Join-Path $logDir "player.log"
$playerJob = Start-Job -ScriptBlock {
    param($repoRoot, $logFile)
    Set-Location "$repoRoot\services\player"
    dotnet run --project src\Leds.Player.Api --launch-profile http 2>&1 | Tee-Object -FilePath $logFile
} -ArgumentList $repoRoot, $playerLog
Write-Host "  [OK] Player API starting at http://localhost:5189" -ForegroundColor Green
Write-Host "    Log: $playerLog" -ForegroundColor DarkGray

# 6. Start web client
Write-Host "[6/6] Starting web client..." -ForegroundColor Yellow
$frontendLog = Join-Path $logDir "frontend.log"
$frontendJob = Start-Job -ScriptBlock {
    param($repoRoot, $logFile)
    Set-Location "$repoRoot\apps\game-client"
    npm run dev 2>&1 | Tee-Object -FilePath $logFile
} -ArgumentList $repoRoot, $frontendLog
Write-Host "  [OK] Web client starting at http://localhost:5173" -ForegroundColor Green
Write-Host "    Log: $frontendLog" -ForegroundColor DarkGray

Write-Host ""
Write-Host "=== All services started ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Services:" -ForegroundColor White
Write-Host "  Game Engine API : http://localhost:5187/swagger" -ForegroundColor DarkGray
Write-Host "  Catalog API     : http://localhost:5193/swagger" -ForegroundColor DarkGray
Write-Host "  Player API      : http://localhost:5189/swagger" -ForegroundColor DarkGray
Write-Host "  Web Client      : http://localhost:5173" -ForegroundColor DarkGray
Write-Host ""
Write-Host "Background Jobs:" -ForegroundColor White
Write-Host "  Game Engine : Job ID $($gameEngineJob.Id)" -ForegroundColor DarkGray
Write-Host "  Catalog     : Job ID $($catalogJob.Id)" -ForegroundColor DarkGray
Write-Host "  Player      : Job ID $($playerJob.Id)" -ForegroundColor DarkGray
Write-Host "  Frontend    : Job ID $($frontendJob.Id)" -ForegroundColor DarkGray
Write-Host ""
Write-Host "To view logs:" -ForegroundColor Yellow
Write-Host "  Get-Content $logDir\game-engine.log -Wait" -ForegroundColor DarkGray
Write-Host "  Get-Content $logDir\catalog.log -Wait" -ForegroundColor DarkGray
Write-Host "  Get-Content $logDir\player.log -Wait" -ForegroundColor DarkGray
Write-Host "  Get-Content $logDir\frontend.log -Wait" -ForegroundColor DarkGray
Write-Host ""
Write-Host "To stop: .\scripts\dev\stop-dev.ps1" -ForegroundColor Yellow
Write-Host "To reset DB: .\scripts\dev\reset-dev-db.ps1" -ForegroundColor Yellow

# Wait a bit for services to start
Write-Host ""
Write-Host "Waiting 10 seconds for services to initialize..." -ForegroundColor Cyan
Start-Sleep -Seconds 10

# Check if services are responding
Write-Host "Checking service health..." -ForegroundColor Yellow
$services = @(
    @{ Name = "Game Engine"; Url = "http://localhost:5187/swagger" },
    @{ Name = "Catalog"; Url = "http://localhost:5193/swagger" },
    @{ Name = "Player"; Url = "http://localhost:5189/swagger" },
    @{ Name = "Frontend"; Url = "http://localhost:5173" }
)

foreach ($service in $services) {
    try {
        $response = Invoke-WebRequest -Uri $service.Url -UseBasicParsing -TimeoutSec 5
        Write-Host "  [OK] $($service.Name): OK ($($response.StatusCode))" -ForegroundColor Green
    } catch {
        Write-Host "  [WARN] $($service.Name): Not responding yet (check logs)" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Setup complete! Services are running in background." -ForegroundColor Cyan
