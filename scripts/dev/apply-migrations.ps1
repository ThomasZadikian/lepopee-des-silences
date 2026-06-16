#Requires -Version 5.1
<#
.SYNOPSIS
    Applies EF Core migrations for all services with databases.
.DESCRIPTION
    Updates Game Engine, Catalog and Player databases to the latest migration.
    Requires the matching Postgres containers to be running
    (docker compose -f docker-compose.dev.yml up -d).
#>

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

Write-Host "=== Applying EF Core Migrations ===" -ForegroundColor Cyan
Write-Host ""

# Game Engine
Write-Host "[1/3] Applying Game Engine migrations..." -ForegroundColor Yellow
Push-Location "$repoRoot\services\game-engine"
try {
    dotnet ef database update `
        --project src\Leds.GameEngine.Infrastructure `
        --startup-project src\Leds.GameEngine.Api `
        --context GameEngineDbContext
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ERROR: Game Engine migration failed." -ForegroundColor Red
        exit 1
    }
    Write-Host "  Game Engine database updated." -ForegroundColor Green
} finally {
    Pop-Location
}

# Catalog
Write-Host "[2/3] Applying Catalog migrations..." -ForegroundColor Yellow
if (Test-Path "$repoRoot\services\catalog\src\Leds.Catalog.Infrastructure\Persistence\Migrations") {
    Push-Location "$repoRoot\services\catalog"
    try {
        dotnet ef database update `
            --project src\Leds.Catalog.Infrastructure `
            --startup-project src\Leds.Catalog.Api `
            --context CatalogDbContext
        if ($LASTEXITCODE -ne 0) {
            Write-Host "  ERROR: Catalog migration failed." -ForegroundColor Red
            exit 1
        }
        Write-Host "  Catalog database updated." -ForegroundColor Green
    } finally {
        Pop-Location
    }
} else {
    Write-Host "  Catalog Service has no EF migrations yet. Skipping." -ForegroundColor DarkGray
}

# Player (when EF is added)
Write-Host "[3/3] Player Service..." -ForegroundColor Yellow
if (Test-Path "$repoRoot\services\player\src\Leds.Player.Infrastructure\Persistence\Migrations") {
    Push-Location "$repoRoot\services\player"
    try {
        dotnet ef database update `
            --project src\Leds.Player.Infrastructure `
            --startup-project src\Leds.Player.Api `
            --context PlayerDbContext
        if ($LASTEXITCODE -ne 0) {
            Write-Host "  ERROR: Player migration failed." -ForegroundColor Red
            exit 1
        }
        Write-Host "  Player database updated." -ForegroundColor Green
    } finally {
        Pop-Location
    }
} else {
    Write-Host "  Player Service has no EF migrations yet. Skipping." -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "=== Migrations applied ===" -ForegroundColor Cyan
