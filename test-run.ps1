# Test Run Script for ScreenTimeMonitor
Write-Host "=== ScreenTimeMonitor Test Run ===" -ForegroundColor Cyan
Write-Host ""

# Stop any running instances
Write-Host "1. Stopping any running instances..." -ForegroundColor Yellow
Stop-Service -Name "ScreenTimeMonitor" -Force -ErrorAction SilentlyContinue
Get-Process -Name "ScreenTimeMonitor.Service" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

# Build the service
Write-Host "`n2. Building service..." -ForegroundColor Yellow
dotnet build ScreenTimeMonitor.Service\ScreenTimeMonitor.Service.csproj --no-incremental
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Check database directory
Write-Host "`n3. Checking database directory..." -ForegroundColor Yellow
$dbPath = "C:\ProgramData\ScreenTimeMonitor"
if (-not (Test-Path $dbPath)) {
    Write-Host "Creating database directory..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $dbPath -Force | Out-Null
}

# Run service with timeout
Write-Host "`n4. Starting service (will run for 10 seconds)..." -ForegroundColor Yellow
Write-Host "Watch for any errors below:`n" -ForegroundColor Cyan

$job = Start-Job -ScriptBlock {
    Set-Location $using:PWD
    dotnet run --project ScreenTimeMonitor.Service\ScreenTimeMonitor.Service.csproj 2>&1
}

Start-Sleep -Seconds 10

Write-Host "`n5. Stopping service..." -ForegroundColor Yellow
Stop-Job $job -ErrorAction SilentlyContinue
Remove-Job $job -Force -ErrorAction SilentlyContinue

# Show output
Write-Host "`n=== Service Output ===" -ForegroundColor Cyan
$output = Receive-Job $job -ErrorAction SilentlyContinue
$output | ForEach-Object { Write-Host $_ }

Write-Host "`n=== Test Complete ===" -ForegroundColor Green
Write-Host "If you see errors above, please share them." -ForegroundColor Yellow

