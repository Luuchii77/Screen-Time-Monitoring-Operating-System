#!/usr/bin/env pwsh
<#
.SYNOPSIS
    ScreenTimeMonitor - Startup Autolaunch Setup Script
.DESCRIPTION
    Configures ScreenTimeMonitor to automatically launch when Windows starts
    Requires: .NET 8.0 SDK installed
    
.EXAMPLE
    .\setup-autostart.ps1
#>

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Screen Time Monitor - Autostart Setup" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Get the project root directory
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$serviceDir = Join-Path $projectRoot "ScreenTimeMonitor.Service"
$uiDir = Join-Path $projectRoot "ScreenTimeMonitor.UI.WPF"

Write-Host "[*] Project Root: $projectRoot" -ForegroundColor White
Write-Host ""

# Step 1: Build the project
Write-Host "[*] Building project in Release mode..." -ForegroundColor Yellow
Push-Location $projectRoot
dotnet build -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] Build failed!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}
Pop-Location
Write-Host "[OK] Build successful" -ForegroundColor Green
Write-Host ""

# Step 2: Check build outputs
$serviceExe = Join-Path $serviceDir "bin\Release\net8.0\ScreenTimeMonitor.Service.exe"
$uiExe = Join-Path $uiDir "bin\Release\net8.0\ScreenTimeMonitor.UI.WPF.exe"

if (-not (Test-Path $serviceExe)) {
    Write-Host "[ERROR] Service executable not found at: $serviceExe" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $uiExe)) {
    Write-Host "[ERROR] UI executable not found at: $uiExe" -ForegroundColor Red
    exit 1
}

Write-Host "[OK] Executables found:" -ForegroundColor Green
Write-Host "    Service: $serviceExe" -ForegroundColor White
Write-Host "    UI: $uiExe" -ForegroundColor White
Write-Host ""

# Step 3: Create startup registry entries
Write-Host "[*] Setting up startup registry entries..." -ForegroundColor Yellow

# For UI WPF application (User level - no admin needed)
$regPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
$uiAppName = "ScreenTimeMonitor"

# Create registry entry for UI
New-Item -Path $regPath -Force | Out-Null
New-ItemProperty -Path $regPath -Name $uiAppName -Value "`"$uiExe`"" -PropertyType String -Force | Out-Null
Write-Host "[OK] User startup registry entry created for UI" -ForegroundColor Green

# Step 4: Create a startup shortcut in user startup folder
Write-Host ""
Write-Host "[*] Creating startup shortcuts..." -ForegroundColor Yellow

$startupFolder = [System.IO.Path]::Combine($env:APPDATA, "Microsoft\Windows\Start Menu\Programs\Startup")

# Create shortcut function
function Create-Shortcut {
    param(
        [string]$TargetPath,
        [string]$ShortcutPath,
        [string]$Description
    )
    
    $WshShell = New-Object -ComObject WScript.Shell
    $Shortcut = $WshShell.CreateShortcut($ShortcutPath)
    $Shortcut.TargetPath = $TargetPath
    $Shortcut.Description = $Description
    $Shortcut.WorkingDirectory = Split-Path $TargetPath
    $Shortcut.Save()
}

try {
    Create-Shortcut -TargetPath $uiExe `
                    -ShortcutPath (Join-Path $startupFolder "ScreenTimeMonitor UI.lnk") `
                    -Description "Screen Time Monitor UI - Auto-launched on startup"
    Write-Host "[OK] Startup shortcut created for UI" -ForegroundColor Green
}
catch {
    Write-Host "[WARN] Could not create startup shortcut: $_" -ForegroundColor Yellow
}

Write-Host ""

# Step 5: Create a batch file for easy testing
Write-Host "[*] Creating quick-launch batch files..." -ForegroundColor Yellow

$startScript = @"
@echo off
echo Starting Screen Time Monitor Service...
start "" "$serviceExe"
timeout /t 2 /nobreak
echo Starting Screen Time Monitor UI...
start "" "$uiExe"
echo Both applications launched!
pause
"@

$startScriptPath = Join-Path $projectRoot "start-monitor.bat"
Set-Content -Path $startScriptPath -Value $startScript -Encoding ASCII

Write-Host "[OK] Created: start-monitor.bat" -ForegroundColor Green
Write-Host ""

# Step 6: Provide options for service installation (optional, requires admin)
Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Setup Complete!" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "✓ UI Application will launch automatically on startup" -ForegroundColor Green
Write-Host "✓ Quick launch script created: start-monitor.bat" -ForegroundColor Green
Write-Host ""

Write-Host "📋 Next Steps:" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. RESTART YOUR COMPUTER and the UI will automatically launch" -ForegroundColor White
Write-Host ""
Write-Host "2. OR manually test now by running:" -ForegroundColor White
Write-Host "   .\start-monitor.bat" -ForegroundColor Yellow
Write-Host ""
Write-Host "3. OPTIONAL - Install as Windows Service (requires admin):" -ForegroundColor White
Write-Host "   Run PowerShell as Administrator, then:" -ForegroundColor White
Write-Host "   .\setup-service.ps1" -ForegroundColor Yellow
Write-Host ""

Write-Host "📝 Configuration saved to:" -ForegroundColor Cyan
Write-Host "   Registry: HKCU\Software\Microsoft\Windows\CurrentVersion\Run\ScreenTimeMonitor" -ForegroundColor White
Write-Host "   Startup: $startupFolder" -ForegroundColor White
Write-Host ""

Write-Host "💡 To disable autostart later:" -ForegroundColor Cyan
Write-Host "   1. Press Win+R, type 'msconfig'" -ForegroundColor White
Write-Host "   2. Go to Startup tab" -ForegroundColor White
Write-Host "   3. Find 'ScreenTimeMonitor' and disable it" -ForegroundColor White
Write-Host ""

Write-Host "OR delete the registry entry:" -ForegroundColor Cyan
Write-Host "   reg delete HKCU\Software\Microsoft\Windows\CurrentVersion\Run /v ScreenTimeMonitor /f" -ForegroundColor White
Write-Host ""

Read-Host "Press Enter to exit"
