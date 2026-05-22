# ScreenTimeMonitor - Development Run Script
# This script builds and runs the ScreenTimeMonitor service and UI for development

param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

# Set project root (adjust if needed)
$projectRoot = "C:\Users\PC\Downloads\School Files\Operating System Project"
Set-Location $projectRoot

Write-Host "=== ScreenTimeMonitor Development Run ===" -ForegroundColor Cyan
Write-Host "Project Root: $projectRoot" -ForegroundColor Gray
Write-Host "Configuration: $Configuration" -ForegroundColor Gray
Write-Host ""

# Function to check if a process is running
function Test-ProcessRunning {
    param([string]$ProcessName)
    $process = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue
    return $null -ne $process
}

# Function to stop running processes
function Stop-ScreenTimeMonitorProcesses {
    Write-Host "Checking for running processes..." -ForegroundColor Yellow
    
    $processes = @("ScreenTimeMonitor.Service", "ScreenTimeMonitor.UI.WPF", "ScreenTimeMonitor.UI")
    $found = $false
    
    foreach ($procName in $processes) {
        $proc = Get-Process -Name $procName -ErrorAction SilentlyContinue
        if ($proc) {
            Write-Host "  Stopping $procName (PID: $($proc.Id))..." -ForegroundColor Yellow
            Stop-Process -Name $procName -Force -ErrorAction SilentlyContinue
            $found = $true
        }
    }
    
    if ($found) {
        Start-Sleep -Seconds 2
        Write-Host "  Processes stopped." -ForegroundColor Green
    } else {
        Write-Host "  No running processes found." -ForegroundColor Gray
    }
    Write-Host ""
}

# Step 1: Stop any running instances
Write-Host "Step 1: Stopping any running instances..." -ForegroundColor Yellow
Stop-ScreenTimeMonitorProcesses

# Step 2: Build Solution
Write-Host "Step 2: Building solution..." -ForegroundColor Yellow
try {
    dotnet build "ScreenTimeMonitor.sln" -c $Configuration --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed! Exit code: $LASTEXITCODE" -ForegroundColor Red
        exit 1
    }
    Write-Host "Build succeeded!" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "Build error: $_" -ForegroundColor Red
    exit 1
}

# Step 3: Start Service (in new window)
Write-Host "Step 3: Starting Service in new window..." -ForegroundColor Yellow
$servicePath = Join-Path $projectRoot "ScreenTimeMonitor.Service\ScreenTimeMonitor.Service.csproj"
$serviceCmd = "Write-Host '=== ScreenTimeMonitor Service ===' -ForegroundColor Cyan; dotnet run --project '$servicePath' -c $Configuration"

Start-Process -FilePath powershell -ArgumentList "-NoExit", "-Command", $serviceCmd -WindowStyle Normal
Write-Host "  Service window opened. Waiting 3 seconds for service to initialize..." -ForegroundColor Gray
Start-Sleep -Seconds 3
Write-Host "  Service started!" -ForegroundColor Green
Write-Host ""

# Step 4: Start UI (in new window)
Write-Host "Step 4: Starting UI in new window..." -ForegroundColor Yellow
$uiPath = Join-Path $projectRoot "ScreenTimeMonitor.UI.WPF\ScreenTimeMonitor.UI.WPF.csproj"
$uiCmd = "Write-Host '=== ScreenTimeMonitor UI ===' -ForegroundColor Cyan; dotnet run --project '$uiPath' -c $Configuration"

Start-Process -FilePath powershell -ArgumentList "-NoExit", "-Command", $uiCmd -WindowStyle Normal
Write-Host "  UI window opened!" -ForegroundColor Green
Write-Host ""

# Step 5: Check Database
Write-Host "Step 5: Checking database..." -ForegroundColor Yellow
$dbPath = "C:\ProgramData\ScreenTimeMonitor\screentime_monitor.db"

if (Test-Path $dbPath) {
    $dbInfo = Get-Item $dbPath
    Write-Host "  Database found!" -ForegroundColor Green
    Write-Host "  Path: $($dbInfo.FullName)" -ForegroundColor Gray
    Write-Host "  Size: $([math]::Round($dbInfo.Length / 1KB, 2)) KB" -ForegroundColor Gray
    Write-Host "  Last Modified: $($dbInfo.LastWriteTime)" -ForegroundColor Gray
} else {
    Write-Host "  Database not found (will be created on first run)" -ForegroundColor Yellow
}
Write-Host ""

# Summary
Write-Host "=== Setup Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Service and UI are running in separate windows." -ForegroundColor Cyan
Write-Host "To stop them, close the windows or run:" -ForegroundColor Yellow
Write-Host "  Get-Process | Where-Object { `$_.ProcessName -like '*ScreenTimeMonitor*' } | Stop-Process -Force" -ForegroundColor Gray
Write-Host ""
Write-Host "To check database again:" -ForegroundColor Yellow
Write-Host "  Test-Path 'C:\ProgramData\ScreenTimeMonitor\screentime_monitor.db'" -ForegroundColor Gray
Write-Host "  Get-Item 'C:\ProgramData\ScreenTimeMonitor\screentime_monitor.db' | Select-Object FullName, Length, LastWriteTime" -ForegroundColor Gray
Write-Host ""

