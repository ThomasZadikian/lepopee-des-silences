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

# 2. Stop this repo's dotnet/node processes, matched by command line rather
#    than process name or window title: start-dev.ps1's `dotnet run` executes
#    each API's native apphost directly (e.g. Leds.GameEngine.Api.exe), which
#    never shows up under Get-Process -Name "dotnet"; and node.exe running
#    inside a PowerShell window has no MainWindowTitle of its own. Both of
#    those matched nothing, silently leaving every service running.
Write-Host "Stopping dotnet/node processes for this repo..." -ForegroundColor Yellow
$repoRootPattern = [regex]::Escape($repoRoot)
$serviceProcesses = Get-CimInstance Win32_Process `
    -Filter "Name = 'dotnet.exe' OR Name = 'node.exe' OR Name LIKE 'Leds.%.exe'" `
    -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -and $_.CommandLine -match $repoRootPattern }
if ($serviceProcesses) {
    $serviceProcesses | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
    Write-Host "  ✓ Stopped $($serviceProcesses.Count) process(es)." -ForegroundColor Green
} else {
    Write-Host "  No dotnet/node processes found." -ForegroundColor DarkGray
}

# 3. Close the separate PowerShell windows start-dev.ps1 opened for each
#    service (killing the dotnet/node child above does not close the parent
#    window that hosts it).
Write-Host "Closing service PowerShell windows..." -ForegroundColor Yellow
$shellWindows = Get-CimInstance Win32_Process `
    -Filter "Name = 'powershell.exe' OR Name = 'pwsh.exe'" `
    -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -and $_.CommandLine -match $repoRootPattern -and $_.ProcessId -ne $PID }
if ($shellWindows) {
    $shellWindows | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
    Write-Host "  ✓ Closed $($shellWindows.Count) window(s)." -ForegroundColor Green
} else {
    Write-Host "  No service windows found." -ForegroundColor DarkGray
}

# 4. Stop Docker containers
Write-Host "Stopping Docker containers..." -ForegroundColor Yellow
docker compose -f "$repoRoot\docker-compose.dev.yml" down

Write-Host ""
Write-Host "=== All services stopped ===" -ForegroundColor Cyan
