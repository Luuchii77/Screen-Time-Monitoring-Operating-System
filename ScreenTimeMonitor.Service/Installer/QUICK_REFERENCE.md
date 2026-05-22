# Phase 4 Quick Reference - Windows Service Integration

## Quick Start (3 Steps)

### 1. Build
```bash
cd "c:\Users\PC\Downloads\School Files\Operating System Project"
dotnet build -c Release
```

### 2. Install (Run as Administrator)
```powershell
cd ScreenTimeMonitor.Service\Installer
powershell -ExecutionPolicy Bypass -File install-service.ps1 `
    -ServicePath "..\bin\Release\net8.0-windows\ScreenTimeMonitor.Service.exe"
```

### 3. Verify
```powershell
powershell -ExecutionPolicy Bypass -File manage-service.ps1 -Action status
```

---

## Common Commands

### View Service Status
```powershell
cd ScreenTimeMonitor.Service\Installer
powershell -ExecutionPolicy Bypass -File manage-service.ps1 -Action status
```

### Start Service (Admin Required)
```powershell
powershell -ExecutionPolicy Bypass -File manage-service.ps1 -Action start
```

### Stop Service (Admin Required)
```powershell
powershell -ExecutionPolicy Bypass -File manage-service.ps1 -Action stop
```

### Restart Service (Admin Required)
```powershell
powershell -ExecutionPolicy Bypass -File manage-service.ps1 -Action restart
```

### Check Service Health
```powershell
powershell -ExecutionPolicy Bypass -File manage-service.ps1 -Action health
```

### View Recent Logs
```powershell
powershell -ExecutionPolicy Bypass -File manage-service.ps1 -Action logs
```

### Uninstall Service (Admin Required)
```powershell
powershell -ExecutionPolicy Bypass -File uninstall-service.ps1
```

### Uninstall and Remove Data (Admin Required)
```powershell
powershell -ExecutionPolicy Bypass -File uninstall-service.ps1 -RemoveData
```

---

## Files Added (Phase 4)

| File | Purpose | Lines |
|------|---------|-------|
| `Utilities/EventLogSetup.cs` | Windows Event Log integration | 125 |
| `Installer/install-service.ps1` | Service installation script | 320+ |
| `Installer/uninstall-service.ps1` | Service uninstallation script | 200+ |
| `Installer/manage-service.ps1` | Service management commands | 320+ |
| `Installer/install-service.bat` | User-friendly installer | 150+ |
| `Installer/README.md` | Comprehensive guide | 500+ |
| `Program.cs` | Updated for Event Log | 104 |

**Total: ~1,600+ lines**

---

## Key Locations

| Item | Location |
|------|----------|
| Service Data | `C:\ProgramData\ScreenTimeMonitor` |
| Logs | `C:\ProgramData\ScreenTimeMonitor\Logs` |
| Database | `C:\ProgramData\ScreenTimeMonitor\*.db` |
| Executable | `..\bin\Release\net8.0-windows\ScreenTimeMonitor.Service.exe` |
| Scripts | `Installer\*.ps1` |

---

## Event Log Viewing

### Using Event Viewer (GUI)
1. Press `Win + X`
2. Select "Event Viewer"
3. Navigate to: Windows Logs → Application
4. Filter by source: "ScreenTimeMonitor"

### Using PowerShell
```powershell
# View last 20 events
Get-EventLog -LogName Application -Source "ScreenTimeMonitor" -Newest 20

# View errors only
Get-EventLog -LogName Application -Source "ScreenTimeMonitor" -EntryType Error
```

---

## Troubleshooting

### Service Won't Start
```powershell
# Check permissions
Get-Service ScreenTimeMonitor | Select-Object -Property StartName, Status

# Check Event Log for errors
Get-EventLog -LogName Application -Source "ScreenTimeMonitor" -Newest 5

# Check data directory
Test-Path "C:\ProgramData\ScreenTimeMonitor"
```

### Service Status Unknown
```powershell
# Restart it
powershell -ExecutionPolicy Bypass -File manage-service.ps1 -Action restart

# Or reinstall it
powershell -ExecutionPolicy Bypass -File uninstall-service.ps1
powershell -ExecutionPolicy Bypass -File install-service.ps1 -ServicePath "path\to\exe"
```

### Installation Failed
1. Ensure running as Administrator
2. Verify .NET 8.0 is installed: `dotnet --version`
3. Check if service already exists: `Get-Service ScreenTimeMonitor`
4. If exists, uninstall first: `powershell -ExecutionPolicy Bypass -File uninstall-service.ps1`

---

## Service Properties

| Property | Value |
|----------|-------|
| Service Name | ScreenTimeMonitor |
| Display Name | Screen Time Monitor |
| Startup Type | Automatic |
| Runs As | SYSTEM Account |
| Error Recovery | Auto-restart (5 sec delay) |
| Event Log | Application (ScreenTimeMonitor source) |
| Data Path | C:\ProgramData\ScreenTimeMonitor |

---

## Build Information

**Status:** ✅ Complete (0 Errors)

```
Release Build Result:
  Errors: 0
  Warnings: 96 (non-critical XML documentation)
  Time: 3.35 seconds
  Output: bin/Release/net8.0-windows/ScreenTimeMonitor.Service.exe
```

---

## Next Phase

**Phase 5: UI & Testing**
- Console UI for system testing
- IPC client implementation
- Health check verification
- Unit tests
- Integration tests

---

**Last Updated:** December 5, 2025  
**Phase Status:** ✅ COMPLETE
