#Requires -Version 5.1
<#
.SYNOPSIS
    Stops the local development infrastructure.
.DESCRIPTION
    Stops Docker containers and all background jobs (dotnet/npm processes).
#>

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

Write-Host "=== Stopping Leds Dev Environment ===" -ForegroundColor Cyan
Write-Host ""

# 1. Stop background jobs
Write-Host "Stopping background jobs..." -ForegroundColor Yellow
$jobs = Get-Job -ErrorAction SilentlyContinue
if ($jobs) {
    $jobs | Stop-Job -ErrorAction SilentlyContinue
    $jobs | Remove-Job -ErrorAction SilentlyContinue
    Write-Host "  ✓ Stopped $($jobs.Count) background job(s)." -ForegroundColor Green
} else {
    Write-Host "  No background jobs found." -ForegroundColor DarkGray
}

# 2. Stop dotnet processes
Write-Host "Stopping dotnet processes..." -ForegroundColor Yellow
$dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
if ($dotnetProcesses) {
    $dotnetProcesses | Stop-Process -Force -ErrorAction SilentlyContinue
    Write-Host "  ✓ Stopped $($dotnetProcesses.Count) dotnet process(es)." -ForegroundColor Green
} else {
    Write-Host "  No dotnet processes found." -ForegroundColor DarkGray
}

# 3. Stop node processes (only those related to our project)
Write-Host "Stopping node processes..." -ForegroundColor Yellow
$nodeProcesses = Get-Process -Name "node" -ErrorAction SilentlyContinue | Where-Object {
    $_.Path -like "*game-client*" -or $_.MainWindowTitle -like "*vite*"
}
if ($nodeProcesses) {
    $nodeProcesses | Stop-Process -Force -ErrorAction SilentlyContinue
    Write-Host "  ✓ Stopped $($nodeProcesses.Count) node process(es)." -ForegroundColor Green
} else {
    Write-Host "  No node processes found." -ForegroundColor DarkGray
}

# 4. Stop Docker containers
Write-Host "Stopping Docker containers..." -ForegroundColor Yellow
docker compose -f "$repoRoot\docker-compose.dev.yml" down

Write-Host ""
Write-Host "=== All services stopped ===" -ForegroundColor Cyan
