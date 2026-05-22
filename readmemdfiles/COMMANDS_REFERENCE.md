# ScreenTimeMonitor - Commands Reference

## 🎓 Configuration

The application now supports **dual-mode deployment**:

### Configuration File: `appsettings.json`

Located in the project root, controls:
- **AppMode**: Set to `"School"` or `"Personal"`
- **Database Path**: Default is `./data/screentime_monitor.db` (relative to app directory)
- **Log Directory**: Default is `./logs`
- **Service Settings**: Pipe name, timeouts, refresh rates

To use **personal/production mode** with absolute paths, edit `appsettings.json`:
```json
{
  "AppMode": "Personal",
  "Paths": {
    "DatabasePath": "C:\\ProgramData\\ScreenTimeMonitor\\screentime_monitor.db",
    "DataDirectory": "C:\\ProgramData\\ScreenTimeMonitor",
    "LogDirectory": "C:\\ProgramData\\ScreenTimeMonitor\\Logs"
  }
}
```

---

## Essential Commands

### 1️⃣ Navigate to Project
```powershell
cd "C:\Users\PC\Downloads\School Files\Operating System Project"
```

### 2️⃣ Stop Running Processes
```powershell
Get-Process | Where-Object { $_.ProcessName -like "*ScreenTimeMonitor*" -or $_.ProcessName -eq "dotnet" } | Stop-Process -Force -ErrorAction SilentlyContinue
```

### 3️⃣ Build Solution
```powershell
dotnet build -c Debug
```

### 4️⃣ Start Service (New Window)
```powershell
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PWD\ScreenTimeMonitor.Service'; dotnet run -c Debug"
```

### 5️⃣ Start UI (New Window)
```powershell
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PWD\ScreenTimeMonitor.UI.WPF'; dotnet run -c Debug"
```

### 6️⃣ Run Tests
```powershell
dotnet test ScreenTimeMonitor.Tests\ScreenTimeMonitor.Tests.csproj -c Debug
```

### 7️⃣ Check Database (Now Portable)
```powershell
# School Mode - Check relative path
$dbPath = Join-Path (Get-Location) "data\screentime_monitor.db"
if (Test-Path $dbPath) {
    Get-Item $dbPath | Select-Object FullName, Length, LastWriteTime
} else {
    Write-Host "Database not found at $dbPath (will be created on first service run)" -ForegroundColor Yellow
}
```

---

## Step-by-Step Workflow

**Run each command in order:**

```powershell
# Step 1: Navigate to project
cd "C:\Users\PC\Downloads\School Files\Operating System Project"
```

```powershell
# Step 2: Stop all running processes
Get-Process | Where-Object { $_.ProcessName -like "*ScreenTimeMonitor*" -or $_.ProcessName -eq "dotnet" } | Stop-Process -Force -ErrorAction SilentlyContinue
```

```powershell
# Step 3: Build the solution
dotnet build -c Debug
```

```powershell
# Step 4: Start the service (in new window)
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PWD\ScreenTimeMonitor.Service'; dotnet run -c Debug"
```

```powershell
# Step 5: Wait for service to start (wait ~5 seconds before running UI)
Start-Sleep -Seconds 5
```

```powershell
# Step 6: Start the UI (in new window)
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PWD\ScreenTimeMonitor.UI.WPF'; dotnet run -c Debug"
```

---

## Quick Copy & Paste (All at Once)
```powershell
cd "C:\Users\PC\Downloads\School Files\Operating System Project"; Get-Process | Where-Object { $_.ProcessName -like "*ScreenTimeMonitor*" -or $_.ProcessName -eq "dotnet" } | Stop-Process -Force -ErrorAction SilentlyContinue; dotnet build -c Debug; Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PWD\ScreenTimeMonitor.Service'; dotnet run -c Debug"; Start-Sleep -Seconds 5; Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PWD\ScreenTimeMonitor.UI.WPF'; dotnet run -c Debug"
```

### Run All Tests with Coverage
```powershell
cd "C:\Users\PC\Downloads\School Files\Operating System Project"
dotnet test ScreenTimeMonitor.Tests\ScreenTimeMonitor.Tests.csproj -c Debug --collect:"XPlat Code Coverage"
```

### Clean Build (Remove bin/obj folders)
```powershell
cd "C:\Users\PC\Downloads\School Files\Operating System Project"
Get-ChildItem -Path . -Include bin,obj -Recurse -Directory | Remove-Item -Recurse -Force
dotnet clean
Write-Host "✓ Clean complete" -ForegroundColor Green
```

### View Database Content (School Mode - Relative Path)
```powershell
# Check for database in ./data directory
$dbPath = Join-Path (Get-Location) "data\screentime_monitor.db"
if (Test-Path $dbPath) {
    Get-Item $dbPath | Select-Object FullName, @{Name="SizeMB";Expression={[math]::Round($_.Length/1MB, 2)}}, LastWriteTime
} else {
    Write-Host "Database not found at $dbPath (will be created on first service run)" -ForegroundColor Yellow
}
```

---

## ✅ What's New in This Update

### Portability Improvements
- ✅ **Relative Path Support**: Database and logs now use `./data` and `./logs` directories (relative to app location)
- ✅ **Configuration System**: `appsettings.json` controls all paths and settings
- ✅ **School-Friendly**: No admin rights required, works in any directory
- ✅ **Personal-Ready**: Still supports absolute paths via config change

### Affected Files
- `appsettings.json` (project root) - New centralized configuration
- `SettingsManager.cs` - Enhanced with `ResolvePath()` and config loading
- `Constants.cs` - Paths now relative
- All `appsettings.json` files - Updated paths

### Migration Notes
- Old hardcoded paths in `C:\ProgramData\` still work if you revert the config
- Existing databases will be found if in the right location
- First run will create `./data` and `./logs` directories automatically

---

## Notes

- **Service must start before UI** - The UI connects to the service via Named Pipes. Wait 2-3 seconds after starting the service before starting the UI.
- **Separate windows** - Both service and UI run in separate windows so you can see their logs.
- **Database location** - Database is created at `./data/screentime_monitor.db` by default (relative to app directory).
- **Stop processes** - Always stop running instances before rebuilding to avoid file locking issues.
- **Configuration** - Edit `appsettings.json` in the project root to change paths or app mode.

