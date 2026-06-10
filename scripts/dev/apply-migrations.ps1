#Requires -Version 5.1
<#
.SYNOPSIS
    Applies EF Core migrations for all services with databases.
.DESCRIPTION
    Updates Game Engine and Player databases to the latest migration.
#>

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

Write-Host "=== Applying EF Core Migrations ===" -ForegroundColor Cyan
Write-Host ""

# Game Engine
Write-Host "[1/2] Applying Game Engine migrations..." -ForegroundColor Yellow
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

# Player (when EF is added)
Write-Host "[2/2] Player Service..." -ForegroundColor Yellow
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
