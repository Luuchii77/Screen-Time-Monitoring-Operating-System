# Comprehensive Fix Script for ScreenTimeMonitor
# Fixes database schema and IPC issues

$ErrorActionPreference = "Stop"

Write-Host "=== ScreenTimeMonitor Comprehensive Fix ===" -ForegroundColor Cyan
Write-Host ""

$projectRoot = "C:\Users\PC\Downloads\School Files\Operating System Project"
$dbPath = "C:\ProgramData\ScreenTimeMonitor\screentime_monitor.db"
$dbDir = Split-Path $dbPath -Parent

Set-Location $projectRoot

# Step 1: Stop all running instances
Write-Host "Step 1: Stopping all running instances..." -ForegroundColor Yellow
$processes = Get-Process | Where-Object { $_.ProcessName -like "*ScreenTimeMonitor*" }
if ($processes) {
    foreach ($proc in $processes) {
        Write-Host "  Stopping $($proc.ProcessName) (PID: $($proc.Id))..." -ForegroundColor Gray
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    }
    Start-Sleep -Seconds 3
    Write-Host "  All processes stopped!" -ForegroundColor Green
} else {
    Write-Host "  No running processes found." -ForegroundColor Gray
}
Write-Host ""

# Step 2: Backup and delete old database
Write-Host "Step 2: Fixing database schema..." -ForegroundColor Yellow
if (Test-Path $dbPath) {
    $dbInfo = Get-Item $dbPath
    Write-Host "  Found database: $dbPath" -ForegroundColor Gray
    Write-Host "  Size: $([math]::Round($dbInfo.Length / 1KB, 2)) KB" -ForegroundColor Gray
    Write-Host "  Last Modified: $($dbInfo.LastWriteTime)" -ForegroundColor Gray
    
    # Backup
    $backupPath = "$dbPath.backup.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    Write-Host "  Creating backup: $backupPath" -ForegroundColor Gray
    Copy-Item $dbPath $backupPath -Force
    Write-Host "  Backup created!" -ForegroundColor Green
    
    # Delete old database
    Write-Host "  Deleting old database (will be recreated with correct schema)..." -ForegroundColor Gray
    Remove-Item $dbPath -Force
    Write-Host "  Old database deleted!" -ForegroundColor Green
} else {
    Write-Host "  Database not found (will be created on service start)." -ForegroundColor Gray
}
Write-Host ""

# Step 3: Rebuild service
Write-Host "Step 3: Rebuilding service..." -ForegroundColor Yellow
try {
    dotnet build ScreenTimeMonitor.Service\ScreenTimeMonitor.Service.csproj --verbosity minimal 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  Build succeeded!" -ForegroundColor Green
    } else {
        Write-Host "  Build failed! Exit code: $LASTEXITCODE" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "  Build error: $_" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 4: Instructions
Write-Host "=== Next Steps ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Start the service in a new window:" -ForegroundColor Yellow
Write-Host "   Start-Process powershell -ArgumentList '-NoExit', '-Command', `"cd '$PWD\ScreenTimeMonitor.Service'; dotnet run`"" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Wait 5 seconds for the service to initialize and create the database" -ForegroundColor Yellow
Write-Host ""
Write-Host "3. Start the UI in another new window:" -ForegroundColor Yellow
Write-Host "   Start-Process powershell -ArgumentList '-NoExit', '-Command', `"cd '$PWD\ScreenTimeMonitor.UI.WPF'; dotnet run`"" -ForegroundColor Gray
Write-Host ""
Write-Host "4. The UI should auto-connect to the service" -ForegroundColor Yellow
Write-Host ""
Write-Host "5. Use some apps and switch between them - sessions should be captured" -ForegroundColor Yellow
Write-Host ""
Write-Host "6. Check App Usage History tab after a few minutes" -ForegroundColor Yellow
Write-Host ""
Write-Host "=== Fix Complete ===" -ForegroundColor Green
Write-Host ""

