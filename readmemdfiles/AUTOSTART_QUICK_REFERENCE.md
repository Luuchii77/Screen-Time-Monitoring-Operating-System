# 🎯 Autostart Setup - Quick Reference

## One-Liner Setup (Fastest)

```powershell
cd "C:\Users\PC\Downloads\School Files\Operating System Project"; .\setup-autostart.ps1
```

Then restart your PC.

---

## The 3 Ways to Set Up Autostart

| Method | Time | Requires Admin | Service Background | Command |
|--------|------|---|---|---|
| **Registry Entry** | 30 sec | No | No | `.\setup-autostart.ps1` |
| **Startup Folder** | 30 sec | No | No | (automatic with above) |
| **Windows Service** | 2 min | **Yes** | **Yes** | `.\setup-service.ps1` |

---

## What Gets Created

### Option 1: Registry + Startup Folder
```
HKCU:\Software\Microsoft\Windows\CurrentVersion\Run
  ├─ ScreenTimeMonitor = "C:\path\to\ScreenTimeMonitor.UI.WPF.exe"

%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup
  └─ ScreenTimeMonitor UI.lnk (shortcut)
```

### Option 2: Windows Service (if you run setup-service.ps1)
```
Services:
  └─ ScreenTimeMonitor (Automatic startup)
```

---

## Verification Commands

```powershell
# Check if registry entry exists
Get-ItemProperty "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" | Select-Object ScreenTimeMonitor

# Check if startup shortcut exists
Test-Path "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup\ScreenTimeMonitor UI.lnk"

# Check if service exists (if installed)
Get-Service -Name ScreenTimeMonitor -ErrorAction SilentlyContinue

# Test launch manually
".\start-monitor.bat"
```

---

## Disable/Remove Autostart

```powershell
# Option A: Delete registry entry (instant)
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v ScreenTimeMonitor /f

# Option B: Delete startup shortcut
Remove-Item "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup\ScreenTimeMonitor UI.lnk"

# Option C: Uninstall service (if installed)
Stop-Service -Name ScreenTimeMonitor
sc.exe delete ScreenTimeMonitor
```

---

## Service Commands (If Installed)

```powershell
# Start service
Start-Service -Name ScreenTimeMonitor

# Stop service
Stop-Service -Name ScreenTimeMonitor

# Check status
Get-Service -Name ScreenTimeMonitor

# Restart service
Restart-Service -Name ScreenTimeMonitor

# View service logs
Get-EventLog -LogName Application -Source ScreenTimeMonitor -Newest 20

# Remove service
Stop-Service -Name ScreenTimeMonitor
sc.exe delete ScreenTimeMonitor
```

---

## Troubleshooting Commands

```powershell
# Check if .NET is installed
dotnet --version

# Check if executable exists
Test-Path "ScreenTimeMonitor.UI.WPF\bin\Release\net8.0\ScreenTimeMonitor.UI.WPF.exe"
Test-Path "ScreenTimeMonitor.Service\bin\Release\net8.0\ScreenTimeMonitor.Service.exe"

# Rebuild if needed
dotnet build -c Release

# Check running processes
Get-Process | Where-Object { $_.ProcessName -like "*ScreenTimeMonitor*" }

# Check Event Log for errors
Get-EventLog -LogName Application -Newest 50 | Where-Object { $_.Source -like "*Screen*" }
```

---

## Files Created

| File | Purpose |
|------|---------|
| `setup-autostart.ps1` | Main setup script - creates registry + shortcuts |
| `setup-service.ps1` | Service installation (requires admin) |
| `start-monitor.bat` | Quick manual launch script |
| `AUTOSTART_SETUP.md` | Full detailed guide |
| This file | Quick reference |

---

## Expected Startup Sequence

**After you restart your PC:**

1. Windows boots
2. Registry entry triggers (after ~30 seconds)
3. `ScreenTimeMonitor.UI.WPF.exe` launches
4. Application appears (may be minimized to system tray)
5. Service starts in background
6. Monitoring begins

**Total time:** ~1-2 minutes after login

---

## Common Issues & Fixes

| Issue | Fix |
|-------|-----|
| "Permission denied" | Run PowerShell as Administrator |
| ".NET not found" | Install .NET 8.0 SDK |
| "Executable not found" | Run `dotnet build -c Release` |
| Nothing happens on restart | Check registry with verification commands above |
| Service won't start | Check Event Log for error messages |
| Want to disable it | Run any "Disable" command from above |

---

## Important Paths

```powershell
# Project root
C:\Users\PC\Downloads\School Files\Operating System Project

# UI executable (after build)
ScreenTimeMonitor.UI.WPF\bin\Release\net8.0\ScreenTimeMonitor.UI.WPF.exe

# Service executable (after build)
ScreenTimeMonitor.Service\bin\Release\net8.0\ScreenTimeMonitor.Service.exe

# Database (created at runtime)
ScreenTimeMonitor.Service\data\screentime_monitor.db

# Logs (created at runtime)
ScreenTimeMonitor.Service\logs\
```

---

## Next Steps

- [ ] Run `.\setup-autostart.ps1`
- [ ] Restart computer
- [ ] Verify app launches automatically
- [ ] Test the monitoring UI
- [ ] (Optional) Run `.\setup-service.ps1` for background service

---

For detailed help, see [AUTOSTART_SETUP.md](AUTOSTART_SETUP.md)
