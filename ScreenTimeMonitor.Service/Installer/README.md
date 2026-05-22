# Screen Time Monitor Service - Installation Guide

## Overview

This directory contains scripts and utilities for installing, uninstalling, and managing the Screen Time Monitor as a Windows Service.

## Files

- **install-service.ps1** - Installs the service and configures auto-start
- **uninstall-service.ps1** - Removes the service and optionally clears data
- **manage-service.ps1** - Provides control commands (start/stop/restart/status)
- **build-release.ps1** - Builds the application and creates the executable
- **README.md** - This file

## System Requirements

- Windows 10 / Windows Server 2016 or later
- .NET 8.0 Runtime (or higher)
- Administrator privileges for installation/uninstallation
- PowerShell 5.0 or later

## Quick Start

### 1. Build the Application

```powershell
# Navigate to the project root
cd "c:\Users\PC\Downloads\School Files\Operating System Project"

# Build the release version
dotnet build -c Release

# The executable will be at:
# ScreenTimeMonitor.Service\bin\Release\net8.0-windows\ScreenTimeMonitor.Service.exe
```

### 2. Install as Windows Service

```powershell
# Run PowerShell as Administrator
# Navigate to the Installer directory
cd "ScreenTimeMonitor.Service\Installer"

# Run the installation script
powershell -ExecutionPolicy Bypass -File install-service.ps1 `
    -ServicePath "C:\path\to\ScreenTimeMonitor.Service.exe"
```

**Installation Parameters:**

```powershell
# All parameters are optional except ServicePath
-ServicePath        # Path to the executable (REQUIRED)
-DisplayName        # Display name in Services (default: "Screen Time Monitor")
-ServiceName        # Windows service name (default: "ScreenTimeMonitor")
-StartupType        # Automatic/Manual/Disabled (default: "Automatic")
-RunAsAccount       # Service account (default: "SYSTEM")
```

**Example with custom parameters:**

```powershell
powershell -ExecutionPolicy Bypass -File install-service.ps1 `
    -ServicePath "C:\Apps\ScreenTimeMonitor.Service.exe" `
    -DisplayName "Application Monitor" `
    -ServiceName "AppMonitor" `
    -StartupType "Automatic"
```

### 3. Verify Installation

```powershell
# Check service status (no admin required)
powershell -ExecutionPolicy Bypass -File manage-service.ps1 -Action status

# View recent logs
powershell -ExecutionPolicy Bypass -File manage-service.ps1 -Action logs

# Check service health
powershell -ExecutionPolicy Bypass -File manage-service.ps1 -Action health
```

## Service Management

### Start Service

```powershell
# Run as Administrator
powershell -ExecutionPolicy Bypass -File manage-service.ps1 -Action start
```

### Stop Service

```powershell
# Run as Administrator
powershell -ExecutionPolicy Bypass -File manage-service.ps1 -Action stop
```

### Restart Service

```powershell
# Run as Administrator
powershell -ExecutionPolicy Bypass -File manage-service.ps1 -Action restart
```

### Enable Auto-Start

```powershell
# Run as Administrator
powershell -ExecutionPolicy Bypass -File manage-service.ps1 -Action enable
```

### Disable Auto-Start

```powershell
# Run as Administrator
powershell -ExecutionPolicy Bypass -File manage-service.ps1 -Action disable
```

### View Logs

```powershell
# No admin required
powershell -ExecutionPolicy Bypass -File manage-service.ps1 -Action logs
```

### Check Health

```powershell
# No admin required
powershell -ExecutionPolicy Bypass -File manage-service.ps1 -Action health
```

## Uninstallation

### Standard Uninstall

```powershell
# Run as Administrator
powershell -ExecutionPolicy Bypass -File uninstall-service.ps1
```

### Uninstall and Remove Data

```powershell
# Run as Administrator - WARNING: This deletes all collected data
powershell -ExecutionPolicy Bypass -File uninstall-service.ps1 -RemoveData
```

**Uninstall Parameters:**

```powershell
-ServiceName    # Windows service name (default: "ScreenTimeMonitor")
-RemoveData     # Delete C:\ProgramData\ScreenTimeMonitor directory [switch]
```

## Service Locations

After installation, the service will use the following directories:

### Data Directory
- **Location:** `C:\ProgramData\ScreenTimeMonitor`
- **Contains:** Database files, configuration
- **Permissions:** SYSTEM account has full control

### Log Directory
- **Location:** `C:\ProgramData\ScreenTimeMonitor\Logs`
- **Contains:** Service log files (.log)
- **Rotation:** Daily logs with timestamp

### Executable Location
- **Path:** Configured during installation
- **Format:** `ScreenTimeMonitor.Service.exe`

## Event Logging

The service logs important events to Windows Event Log:

### Viewing Event Logs

1. **Using Event Viewer:**
   - Press `Win + X`, select "Event Viewer"
   - Navigate to: Windows Logs → Application
   - Filter by source: "ScreenTimeMonitor"

2. **Using PowerShell:**
   ```powershell
   # View last 20 events
   Get-EventLog -LogName Application -Source "ScreenTimeMonitor" -Newest 20

   # View errors only
   Get-EventLog -LogName Application -Source "ScreenTimeMonitor" -EntryType Error
   ```

### Event Types

- **Information:** Service started/stopped normally
- **Warning:** Performance issues, configuration warnings
- **Error:** Critical failures, unhandled exceptions

## Troubleshooting

### Service Won't Start

**Check permissions:**
```powershell
# Verify service permissions
Get-Service ScreenTimeMonitor | Select-Object -Property StartName, Status
```

**Check data directory:**
```powershell
# Ensure directory exists and is accessible
Test-Path "C:\ProgramData\ScreenTimeMonitor"
```

**View error logs:**
```powershell
# Check Windows Event Log
Get-EventLog -LogName Application -Source "ScreenTimeMonitor" -Newest 10
```

**Check .NET Runtime:**
```powershell
# Verify .NET 8.0 is installed
dotnet --version
```

### Service Crashes Immediately

1. Check Event Viewer for error messages
2. Verify database is not corrupted
3. Check file permissions on data directory
4. Review log files in `C:\ProgramData\ScreenTimeMonitor\Logs`

### Can't Install/Uninstall

1. Ensure running PowerShell as Administrator
2. Close all PowerShell windows and try again
3. Ensure service is stopped before uninstalling
4. Check Windows Firewall settings

### Service Running but Not Collecting Data

1. Verify window monitoring is working: Check Event Viewer logs
2. Check database connectivity: Verify SQLite/PostgreSQL is accessible
3. Review application configuration: Check `appsettings.json`
4. Check data directory permissions: `C:\ProgramData\ScreenTimeMonitor`

## Advanced Configuration

### Change Service Account

```powershell
# Run as Administrator
$serviceName = "ScreenTimeMonitor"
$accountName = "DOMAIN\UserAccount"  # or ".\LocalAccount"
$password = "password"

$sc = Get-WmiObject win32_service -filter "name='$serviceName'"
$sc.Change($null,$null,$null,$null,$null,$false,$accountName,$password) | Out-Null
```

### Modify Startup Parameters

Edit `appsettings.json` in the service directory to configure:
- Database connection strings
- Monitoring intervals
- Data retention policies
- Log levels

After changes:
```powershell
powershell -ExecutionPolicy Bypass -File manage-service.ps1 -Action restart
```

### Enable Debug Logging

1. Open `appsettings.json`
2. Change `"LogLevel": "Information"` to `"LogLevel": "Debug"`
3. Save and restart service:
```powershell
powershell -ExecutionPolicy Bypass -File manage-service.ps1 -Action restart
```

## Performance Considerations

### Recommended Settings

- **MaxQueueSize:** 1000 items (default, tunable)
- **DatabaseFlushInterval:** 30 seconds (default)
- **DataRetentionDays:** 90 days (default)

### Resource Usage

- **Memory:** ~50-150 MB (typical usage)
- **CPU:** <1% (idle), 1-5% during peak monitoring
- **Disk I/O:** Minimal, bursty during flush intervals
- **Disk Space:** ~5-20 MB per month (depends on data volume)

## Security Considerations

1. **SYSTEM Account:** Service runs with SYSTEM privilege for OS-level monitoring
2. **Event Log Access:** Only administrators can view detailed logs
3. **Data Directory:** Restricted to SYSTEM account
4. **Network:** Uses Named Pipes for IPC (Windows-only, no network exposure)

## Maintenance

### Backup Data

```powershell
# Backup database and logs
$backupDir = "C:\Backups\ScreenTimeMonitor"
New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
Copy-Item "C:\ProgramData\ScreenTimeMonitor\*" $backupDir -Recurse -Force
```

### Clean Old Data

Data older than 90 days is automatically cleaned up during service startup. To change:

Edit `appsettings.json`:
```json
"MonitoringSettings": {
  "DataRetentionDays": 90
}
```

### View Service Statistics

```powershell
# Check disk usage
(Get-ChildItem "C:\ProgramData\ScreenTimeMonitor" -Recurse | 
 Measure-Object -Property Length -Sum).Sum / 1GB

# Count database records
# Use admin tools or SQL queries for detailed statistics
```

## Support & Debugging

### Collect Diagnostic Information

```powershell
# Create diagnostic package
$diagDir = "C:\Diagnostics\ScreenTimeMonitor_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
New-Item -ItemType Directory -Path $diagDir -Force | Out-Null

# Copy service logs
Copy-Item "C:\ProgramData\ScreenTimeMonitor\Logs" "$diagDir\Logs" -Recurse -ErrorAction SilentlyContinue

# Export Event Log
Get-EventLog -LogName Application -Source "ScreenTimeMonitor" | 
  Export-Csv "$diagDir\EventLog.csv"

# System info
Get-ComputerInfo | Out-File "$diagDir\SystemInfo.txt"

# Service status
Get-Service ScreenTimeMonitor | Out-File "$diagDir\ServiceStatus.txt"
```

## Script Execution Policy

If you encounter execution policy errors:

```powershell
# Temporary bypass (current session only)
powershell -ExecutionPolicy Bypass -File install-service.ps1 ...

# Or set policy permanently (requires admin)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser -Force
```

## Additional Resources

- [Microsoft Windows Service Documentation](https://docs.microsoft.com/en-us/dotnet/framework/windows-services/intro-to-windows-service-applications)
- [PowerShell Service Management](https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.management/manage-service)
- [Windows Event Log](https://docs.microsoft.com/en-us/windows/win32/eventlog/event-logging)

## Version Information

- **Application Version:** 1.0.0
- **Supported OS:** Windows 10+ / Windows Server 2016+
- **.NET Framework:** .NET 8.0+
- **PowerShell:** 5.0+

---

Last Updated: December 5, 2025
