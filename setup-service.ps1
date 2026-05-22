#!/usr/bin/env pwsh
<#
.SYNOPSIS
    ScreenTimeMonitor - Windows Service Installation
.DESCRIPTION
    Installs ScreenTimeMonitor as a Windows Service that runs in background
    REQUIRES: Administrator privileges
    
.EXAMPLE
    Run as Administrator:
    .\setup-service.ps1
#>

# Check for admin privileges
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ERROR: This script requires Administrator privileges!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please run PowerShell as Administrator and try again." -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Screen Time Monitor - Service Setup" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Get the project root directory
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$serviceDir = Join-Path $projectRoot "ScreenTimeMonitor.Service"
$serviceExe = Join-Path $serviceDir "bin\Release\net8.0\ScreenTimeMonitor.Service.exe"

Write-Host "[*] Project Root: $projectRoot" -ForegroundColor White
Write-Host ""

# Check if service already exists
Write-Host "[*] Checking for existing service..." -ForegroundColor Yellow
$existingService = Get-Service -Name "ScreenTimeMonitor" -ErrorAction SilentlyContinue

if ($null -ne $existingService) {
    Write-Host "[!] Service 'ScreenTimeMonitor' already exists" -ForegroundColor Yellow
    Write-Host ""
    
    $response = Read-Host "Do you want to reinstall it? (y/n)"
    if ($response -ne "y") {
        Write-Host "Installation cancelled" -ForegroundColor Yellow
        Read-Host "Press Enter to exit"
        exit 0
    }
    
    Write-Host "[*] Stopping existing service..." -ForegroundColor Yellow
    try {
        Stop-Service -Name "ScreenTimeMonitor" -Force -ErrorAction Stop
        Start-Sleep -Seconds 2
    }
    catch {
        Write-Host "[WARN] Could not stop service: $_" -ForegroundColor Yellow
    }
    
    Write-Host "[*] Removing existing service..." -ForegroundColor Yellow
    try {
        sc.exe delete ScreenTimeMonitor
        Start-Sleep -Seconds 2
    }
    catch {
        Write-Host "[WARN] Could not remove service: $_" -ForegroundColor Yellow
    }
}

# Check if executable exists
if (-not (Test-Path $serviceExe)) {
    Write-Host "[ERROR] Service executable not found!" -ForegroundColor Red
    Write-Host "Expected location: $serviceExe" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please build the project first:" -ForegroundColor Yellow
    Write-Host "  cd '$projectRoot'" -ForegroundColor Yellow
    Write-Host "  dotnet build -c Release" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "[OK] Service executable found" -ForegroundColor Green
Write-Host ""

# Create the service
Write-Host "[*] Creating Windows Service..." -ForegroundColor Yellow

$serviceName = "ScreenTimeMonitor"
$displayName = "Screen Time Monitor Service"
$description = "Monitors application usage and system metrics in the background"

try {
    New-Service -Name $serviceName `
                -DisplayName $displayName `
                -Description $description `
                -BinaryPathName $serviceExe `
                -StartupType Automatic `
                -ErrorAction Stop | Out-Null
    
    Write-Host "[OK] Service created successfully" -ForegroundColor Green
}
catch {
    Write-Host "[ERROR] Failed to create service: $_" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""

# Start the service
Write-Host "[*] Starting service..." -ForegroundColor Yellow
try {
    Start-Service -Name $serviceName -ErrorAction Stop
    Start-Sleep -Seconds 3
    
    $service = Get-Service -Name $serviceName
    if ($service.Status -eq "Running") {
        Write-Host "[OK] Service started successfully" -ForegroundColor Green
        Write-Host "    Status: $($service.Status)" -ForegroundColor White
    }
    else {
        Write-Host "[WARN] Service created but status is: $($service.Status)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "[WARN] Could not start service: $_" -ForegroundColor Yellow
    Write-Host "The service was created but you may need to start it manually" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Service Installation Complete!" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "✓ Windows Service installed" -ForegroundColor Green
Write-Host "✓ Service set to start automatically on boot" -ForegroundColor Green
Write-Host ""

Write-Host "📋 Service Details:" -ForegroundColor Cyan
Write-Host "   Name: $serviceName" -ForegroundColor White
Write-Host "   Display Name: $displayName" -ForegroundColor White
Write-Host "   Status: $($service.Status)" -ForegroundColor White
Write-Host "   Startup Type: Automatic" -ForegroundColor White
Write-Host ""

Write-Host "💡 Managing the Service:" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Start service:" -ForegroundColor White
Write-Host "    Start-Service -Name ScreenTimeMonitor" -ForegroundColor Yellow
Write-Host ""
Write-Host "  Stop service:" -ForegroundColor White
Write-Host "    Stop-Service -Name ScreenTimeMonitor" -ForegroundColor Yellow
Write-Host ""
Write-Host "  Check status:" -ForegroundColor White
Write-Host "    Get-Service -Name ScreenTimeMonitor" -ForegroundColor Yellow
Write-Host ""
Write-Host "  View service logs:" -ForegroundColor White
Write-Host "    Get-EventLog -LogName Application -Source ScreenTimeMonitor -Newest 50" -ForegroundColor Yellow
Write-Host ""
Write-Host "  Uninstall service:" -ForegroundColor White
Write-Host "    Stop-Service -Name ScreenTimeMonitor" -ForegroundColor Yellow
Write-Host "    sc.exe delete ScreenTimeMonitor" -ForegroundColor Yellow
Write-Host ""

Read-Host "Press Enter to exit"
