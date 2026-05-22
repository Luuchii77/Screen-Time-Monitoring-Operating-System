# Fix Database Schema Script
# This script recreates the database with the correct schema (snake_case column names)

$ErrorActionPreference = "Stop"

Write-Host "=== ScreenTimeMonitor Database Schema Fix ===" -ForegroundColor Cyan
Write-Host ""

$dbPath = "C:\ProgramData\ScreenTimeMonitor\screentime_monitor.db"
$dbDir = Split-Path $dbPath -Parent

# Check if database exists
if (Test-Path $dbPath) {
    Write-Host "Found existing database: $dbPath" -ForegroundColor Yellow
    
    # Get size
    $dbInfo = Get-Item $dbPath
    Write-Host "  Size: $([math]::Round($dbInfo.Length / 1KB, 2)) KB" -ForegroundColor Gray
    Write-Host "  Last Modified: $($dbInfo.LastWriteTime)" -ForegroundColor Gray
    Write-Host ""
    
    # Backup the database
    $backupPath = "$dbPath.backup.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    Write-Host "Creating backup: $backupPath" -ForegroundColor Yellow
    Copy-Item $dbPath $backupPath
    Write-Host "  Backup created!" -ForegroundColor Green
    Write-Host ""
    
    # Ask for confirmation
    $response = Read-Host "Delete existing database and recreate with correct schema? (Y/N)"
    if ($response -ne "Y" -and $response -ne "y") {
        Write-Host "Cancelled." -ForegroundColor Yellow
        exit 0
    }
    
    # Stop any running service instances
    Write-Host "`nStopping any running service instances..." -ForegroundColor Yellow
    Get-Process | Where-Object { $_.ProcessName -like "*ScreenTimeMonitor*" } | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    Write-Host "  Stopped." -ForegroundColor Green
    Write-Host ""
    
    # Delete the database
    Write-Host "Deleting old database..." -ForegroundColor Yellow
    Remove-Item $dbPath -Force
    Write-Host "  Deleted!" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "Database not found. Will be created on next service start." -ForegroundColor Yellow
    Write-Host ""
}

Write-Host "=== Next Steps ===" -ForegroundColor Cyan
Write-Host "1. Rebuild the service:" -ForegroundColor Yellow
Write-Host "   dotnet build ScreenTimeMonitor.Service\ScreenTimeMonitor.Service.csproj" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Start the service (it will create the database with correct schema):" -ForegroundColor Yellow
Write-Host "   cd ScreenTimeMonitor.Service" -ForegroundColor Gray
Write-Host "   dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "The database will be automatically created with the correct schema." -ForegroundColor Green
Write-Host ""

