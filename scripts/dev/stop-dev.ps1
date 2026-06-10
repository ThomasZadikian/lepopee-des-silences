#Requires -Version 5.1
<#
.SYNOPSIS
    Stops the local development infrastructure.
.DESCRIPTION
    Stops Docker containers. Separate dotnet/npm windows must be closed manually.
#>

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

Write-Host "=== Stopping Leds Dev Environment ===" -ForegroundColor Cyan
Write-Host ""

Write-Host "Stopping Docker containers..." -ForegroundColor Yellow
docker compose -f "$repoRoot\docker-compose.dev.yml" down

Write-Host ""
Write-Host "Docker containers stopped." -ForegroundColor Green
Write-Host ""
Write-Host "NOTE: Any separate PowerShell windows running" -ForegroundColor Yellow
Write-Host "  dotnet run or npm run dev must be closed manually." -ForegroundColor Yellow
Write-Host "  (Press Ctrl+C in each window or close them.)" -ForegroundColor Yellow
