#!/usr/bin/env pwsh
<#
.SYNOPSIS
    ScreenTimeMonitor - Multi-Device Setup Script
.DESCRIPTION
    Automates setup on any Windows device
    Run this in the project root: .\setup.ps1
#>

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Screen Time Monitor - Setup Script" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Check .NET SDK
Write-Host "[*] Checking .NET SDK installation..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] .NET SDK not found!" -ForegroundColor Red
    Write-Host "Please install .NET 8.0 or later from: https://dotnet.microsoft.com/download" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Host "[OK] .NET SDK found: $dotnetVersion" -ForegroundColor Green
Write-Host ""

# Restore packages
Write-Host "[*] Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] Package restoration failed!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Host "[OK] Packages restored" -ForegroundColor Green
Write-Host ""

# Build
Write-Host "[*] Building solution in Debug mode..." -ForegroundColor Yellow
dotnet build -c Debug
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] Build failed!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Host "[OK] Build successful" -ForegroundColor Green
Write-Host ""

# Create directories
Write-Host "[*] Creating data and logs directories..." -ForegroundColor Yellow
if (-not (Test-Path "data")) {
    New-Item -ItemType Directory -Path "data" -Force | Out-Null
}
if (-not (Test-Path "logs")) {
    New-Item -ItemType Directory -Path "logs" -Force | Out-Null
}
Write-Host "[OK] Directories ready" -ForegroundColor Green
Write-Host ""

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Open two PowerShell windows" -ForegroundColor White
Write-Host "  2. In the first window:" -ForegroundColor White
Write-Host "     cd ScreenTimeMonitor.Service" -ForegroundColor Gray
Write-Host "     dotnet run -c Debug" -ForegroundColor Gray
Write-Host "  3. Wait 3-5 seconds for service startup" -ForegroundColor White
Write-Host "  4. In the second window:" -ForegroundColor White
Write-Host "     cd ScreenTimeMonitor.UI.WPF" -ForegroundColor Gray
Write-Host "     dotnet run -c Debug" -ForegroundColor Gray
Write-Host ""
Write-Host "The application will start tracking when you click 'Connect to Service'!" -ForegroundColor Green
Write-Host ""

Read-Host "Press Enter to exit"
