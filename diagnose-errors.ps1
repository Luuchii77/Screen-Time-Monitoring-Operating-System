# Diagnostic Script for ScreenTimeMonitor
Write-Host "=== ScreenTimeMonitor Diagnostic Tool ===" -ForegroundColor Cyan
Write-Host ""

# 1. Check if service is running
Write-Host "1. Checking service status..." -ForegroundColor Yellow
$service = Get-Service -Name "ScreenTimeMonitor" -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "   Service Status: $($service.Status)" -ForegroundColor $(if ($service.Status -eq "Running") { "Green" } else { "Yellow" })
} else {
    Write-Host "   Service not installed" -ForegroundColor Yellow
}

# 2. Check database file
Write-Host "`n2. Checking database..." -ForegroundColor Yellow
$dbPath = "C:\ProgramData\ScreenTimeMonitor\screentime_monitor.db"
if (Test-Path $dbPath) {
    $dbInfo = Get-Item $dbPath
    Write-Host "   Database exists: $dbPath" -ForegroundColor Green
    Write-Host "   Size: $([math]::Round($dbInfo.Length / 1KB, 2)) KB" -ForegroundColor Green
    Write-Host "   Last Modified: $($dbInfo.LastWriteTime)" -ForegroundColor Green
    
    # Try to query the database using sqlite3 command line tool
    try {
        $sqlite3Path = Get-Command sqlite3 -ErrorAction SilentlyContinue
        if (-not $sqlite3Path) {
            Write-Host "   sqlite3 command not found - using .NET method" -ForegroundColor Yellow
            
            # Try using .NET SQLite via the UI assembly
            $uiDllPath = "ScreenTimeMonitor.UI.WPF\bin\Debug\net8.0-windows\win-x64\ScreenTimeMonitor.UI.WPF.dll"
            if (Test-Path $uiDllPath) {
                Add-Type -Path $uiDllPath -ErrorAction SilentlyContinue
            }
            
            # Try loading SQLite from common locations
            $sqliteDllPaths = @(
                "ScreenTimeMonitor.UI.WPF\bin\Debug\net8.0-windows\win-x64\System.Data.SQLite.dll",
                "ScreenTimeMonitor.UI.WPF\bin\Debug\net8.0-windows\win-x64\runtimes\win-x64\native\SQLite.Interop.dll"
            )
            
            foreach ($dllPath in $sqliteDllPaths) {
                if (Test-Path $dllPath) {
                    try {
                        Add-Type -Path $dllPath -ErrorAction Stop
                        break
                    } catch {
                        # Continue trying other paths
                    }
                }
            }
            
            $conn = New-Object System.Data.SQLite.SQLiteConnection("Data Source=$dbPath;Version=3;")
            $conn.Open()
            
            $cmd = $conn.CreateCommand()
            $cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;"
            $reader = $cmd.ExecuteReader()
            $tables = @()
            while ($reader.Read()) {
                $tables += $reader["name"]
            }
            $reader.Close()
            $conn.Close()
            
            Write-Host "   Tables found: $($tables.Count)" -ForegroundColor Green
            Write-Host "   Table names: $($tables -join ', ')" -ForegroundColor Cyan
            
            # Check app_usage_sessions table structure
            if ($tables -contains "app_usage_sessions") {
                $conn.Open()
                $cmd.CommandText = "PRAGMA table_info(app_usage_sessions);"
                $reader = $cmd.ExecuteReader()
                $columns = @()
                while ($reader.Read()) {
                    $columns += $reader["name"]
                }
                $reader.Close()
                $conn.Close()
                Write-Host "   app_usage_sessions columns: $($columns -join ', ')" -ForegroundColor Cyan
                
                # Count records
                $conn.Open()
                $cmd.CommandText = "SELECT COUNT(*) FROM app_usage_sessions;"
                $count = $cmd.ExecuteScalar()
                $conn.Close()
                Write-Host "   Records in app_usage_sessions: $count" -ForegroundColor $(if ($count -gt 0) { "Green" } else { "Yellow" })
            }
        } else {
            # Use sqlite3 command line tool
            $tablesOutput = sqlite3 $dbPath "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;" 2>&1
            if ($LASTEXITCODE -eq 0) {
                $tables = $tablesOutput -split "`n" | Where-Object { $_ -ne "" }
                Write-Host "   Tables found: $($tables.Count)" -ForegroundColor Green
                Write-Host "   Table names: $($tables -join ', ')" -ForegroundColor Cyan
                
                if ($tables -contains "app_usage_sessions") {
                    $columnsOutput = sqlite3 $dbPath "PRAGMA table_info(app_usage_sessions);" 2>&1
                    $columns = ($columnsOutput -split "`n" | ForEach-Object { ($_ -split '\|')[1] }) | Where-Object { $_ -ne "" }
                    Write-Host "   app_usage_sessions columns: $($columns -join ', ')" -ForegroundColor Cyan
                    
                    $countOutput = sqlite3 $dbPath "SELECT COUNT(*) FROM app_usage_sessions;" 2>&1
                    $count = [int]$countOutput
                    Write-Host "   Records in app_usage_sessions: $count" -ForegroundColor $(if ($count -gt 0) { "Green" } else { "Yellow" })
                }
            } else {
                Write-Host "   Error querying database with sqlite3: $tablesOutput" -ForegroundColor Red
            }
        }
    }
    catch {
        Write-Host "   Error querying database: $_" -ForegroundColor Red
        Write-Host "   Note: This is not critical - the database exists and will be queried by the application" -ForegroundColor Yellow
    }
} else {
    Write-Host "   Database does not exist: $dbPath" -ForegroundColor Yellow
}

# 3. Check log files
Write-Host "`n3. Checking log files..." -ForegroundColor Yellow
$logDir = "C:\ProgramData\ScreenTimeMonitor\Logs"
if (Test-Path $logDir) {
    $logFiles = Get-ChildItem $logDir -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 5
    if ($logFiles) {
        Write-Host "   Found $($logFiles.Count) log file(s)" -ForegroundColor Green
        $latestLog = $logFiles[0]
        Write-Host "   Latest log: $($latestLog.Name)" -ForegroundColor Cyan
        Write-Host "   Last modified: $($latestLog.LastWriteTime)" -ForegroundColor Cyan
        
        # Show last 10 lines with errors
        $errorLines = Get-Content $latestLog.FullName | Select-String -Pattern "error|exception|failed" -CaseSensitive:$false | Select-Object -Last 10
        if ($errorLines) {
            Write-Host "`n   Recent errors/warnings:" -ForegroundColor Red
            $errorLines | ForEach-Object { Write-Host "   $_" -ForegroundColor Red }
        }
    } else {
        Write-Host "   No log files found" -ForegroundColor Yellow
    }
} else {
    Write-Host "   Log directory does not exist: $logDir" -ForegroundColor Yellow
}

# 4. Check Event Log
Write-Host "`n4. Checking Windows Event Log..." -ForegroundColor Yellow
try {
    $events = Get-EventLog -LogName Application -Source "ScreenTimeMonitor" -Newest 5 -ErrorAction SilentlyContinue
    if ($events) {
        Write-Host "   Found $($events.Count) recent event(s)" -ForegroundColor Green
        $events | ForEach-Object {
            $color = if ($_.EntryType -eq "Error") { "Red" } elseif ($_.EntryType -eq "Warning") { "Yellow" } else { "Green" }
            Write-Host "   [$($_.TimeGenerated)] $($_.EntryType): $($_.Message.Substring(0, [Math]::Min(100, $_.Message.Length)))" -ForegroundColor $color
        }
    } else {
        Write-Host "   No events found" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "   Could not access Event Log: $_" -ForegroundColor Yellow
}

# 5. Check build output
Write-Host "`n5. Checking build artifacts..." -ForegroundColor Yellow
$serviceExe = "ScreenTimeMonitor.Service\bin\Debug\net8.0-windows\win-x64\ScreenTimeMonitor.Service.exe"
if (Test-Path $serviceExe) {
    Write-Host "   Service executable exists" -ForegroundColor Green
} else {
    Write-Host "   Service executable not found - needs build" -ForegroundColor Red
}

$uiExe = "ScreenTimeMonitor.UI.WPF\bin\Debug\net8.0-windows\win-x64\ScreenTimeMonitor.UI.WPF.exe"
if (Test-Path $uiExe) {
    Write-Host "   UI executable exists" -ForegroundColor Green
} else {
    Write-Host "   UI executable not found - needs build" -ForegroundColor Red
}

Write-Host "`n=== Diagnostic Complete ===" -ForegroundColor Green
Write-Host "Please share any errors shown above." -ForegroundColor Yellow

