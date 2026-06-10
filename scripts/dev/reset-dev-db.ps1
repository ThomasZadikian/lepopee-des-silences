#Requires -Version 5.1
<#
.SYNOPSIS
    Resets local development databases.
.DESCRIPTION
    Stops containers, deletes volumes, and restarts containers with clean databases.
#>

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

Write-Host "=== Reset Local Dev Databases ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "WARNING: This will delete ALL local development database data." -ForegroundColor Red
Write-Host ""

$confirmation = Read-Host "Continue? (y/N)"
if ($confirmation -ne "y") {
    Write-Host "Aborted." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "Stopping containers and removing volumes..." -ForegroundColor Yellow
docker compose -f "$repoRoot\docker-compose.dev.yml" down -v
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ERROR: Failed to stop containers." -ForegroundColor Red
    exit 1
}

Write-Host "Starting fresh containers..." -ForegroundColor Yellow
docker compose -f "$repoRoot\docker-compose.dev.yml" up -d
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ERROR: Failed to start containers." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== Databases reset complete ===" -ForegroundColor Cyan
Write-Host "  Game Engine DB: localhost:5432 (clean)" -ForegroundColor Green
Write-Host "  Player DB:      localhost:5433 (clean)" -ForegroundColor Green
Write-Host ""
Write-Host "Run .\scripts\dev\apply-migrations.ps1 to apply EF migrations." -ForegroundColor Yellow
