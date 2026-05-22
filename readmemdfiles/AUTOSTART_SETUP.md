# 🚀 ScreenTimeMonitor - Autostart Setup Guide

## Overview

This guide shows you how to set up ScreenTimeMonitor to automatically launch when you restart your PC. You have two options:

### Option 1: UI Auto-Launch Only (Recommended for Testing)
- **UI App** launches automatically on startup
- **Service** runs in background when UI is active
- No admin rights required
- Perfect for testing and development

### Option 2: Full Windows Service (Production)
- **Both UI and Service** launch automatically
- Service runs 24/7 in background
- Requires admin rights for installation
- Better for long-term monitoring

---

## ✅ Prerequisites

Before starting, ensure you have:

1. **.NET 8.0 SDK installed**
   ```powershell
   dotnet --version
   ```
   Should show version 8.0 or higher

2. **PowerShell 5.1 or higher**
   ```powershell
   $PSVersionTable.PSVersion
   ```

3. **Project extracted/cloned** to:
   ```
   C:\Users\PC\Downloads\School Files\Operating System Project
   ```

---

## 🔧 Quick Setup (2 Minutes)

### Step 1: Run the Autostart Setup Script

Open PowerShell and navigate to the project root:

```powershell
cd "C:\Users\PC\Downloads\School Files\Operating System Project"
.\setup-autostart.ps1
```

**What this script does:**
- ✅ Builds the project in Release mode
- ✅ Creates Windows registry entries for auto-launch
- ✅ Creates a startup shortcut in Windows Startup folder
- ✅ Creates a quick-launch batch file

### Step 2: Restart Your Computer

Simply restart Windows. The ScreenTimeMonitor UI will automatically launch in the system tray.

```powershell
Restart-Computer -Force
```

Or manually shut down and restart.

### Step 3: Verify It Works

After restart, you should see:
1. ScreenTimeMonitor UI window appears
2. System tray icon shows the application is running
3. Monitor your screen time immediately!

---

## 🧪 Testing Before Restart

If you want to test without restarting, run the quick-launch script:

```powershell
.\start-monitor.bat
```

This launches both the service and UI so you can test the application.

---

## 🔐 Option 2: Windows Service Installation (Admin)

For a more robust setup where the service runs 24/7 in the background:

### Step 1: Build in Release Mode

```powershell
cd "C:\Users\PC\Downloads\School Files\Operating System Project"
dotnet build -c Release
```

### Step 2: Run Service Setup as Administrator

Open PowerShell **as Administrator** and run:

```powershell
.\setup-service.ps1
```

**Important:** This requires admin privileges. If you're on a school PC, you may need IT approval.

### Step 3: Verify Service Status

```powershell
Get-Service -Name ScreenTimeMonitor
```

Should show:
```
Status   Name                 DisplayName
------   ----                 -----------
Running  ScreenTimeMonitor    Screen Time Monitor Service
```

---

## 📋 Complete Installation Steps (Detailed)

### For Option 1 (UI Auto-Launch):

```powershell
# 1. Navigate to project
cd "C:\Users\PC\Downloads\School Files\Operating System Project"

# 2. Run setup script
.\setup-autostart.ps1

# 3. Restart computer
Restart-Computer -Force

# OR test first without restart:
.\start-monitor.bat
```

### For Option 2 (Full Windows Service):

```powershell
# 1. Navigate to project
cd "C:\Users\PC\Downloads\School Files\Operating System Project"

# 2. Build solution
dotnet build -c Release

# 3. Open PowerShell as Administrator
# (Right-click PowerShell → Run as Administrator)

# 4. Run service setup
.\setup-service.ps1

# 5. Restart computer
Restart-Computer -Force
```

---

## ❌ Disabling Autostart

If you want to remove the autostart:

### Method 1: Using Registry (Easiest)

```powershell
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v ScreenTimeMonitor /f
```

### Method 2: Using System Settings

1. Press `Win + R`
2. Type `msconfig`
3. Click **Startup** tab
4. Find "ScreenTimeMonitor"
5. Click **Disable**
6. Restart computer

### Method 3: Remove Startup Folder Shortcut

1. Press `Win + R`
2. Type: `shell:startup`
3. Delete "ScreenTimeMonitor UI.lnk"
4. Restart computer

### Method 4: Uninstall Service (If Installed)

```powershell
# Stop the service
Stop-Service -Name ScreenTimeMonitor

# Remove the service
sc.exe delete ScreenTimeMonitor
```

---

## 🛠️ Troubleshooting

### Problem: Nothing launches on restart

**Solution:**
1. Check if scripts ran successfully - look for registry entry:
   ```powershell
   Get-ItemProperty "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
   ```

2. Manually test:
   ```powershell
   .\start-monitor.bat
   ```

3. Check application can be built:
   ```powershell
   dotnet build -c Release
   ```

### Problem: Service won't start

**Solution:**
1. Check build succeeded:
   ```powershell
   Test-Path "ScreenTimeMonitor.Service\bin\Release\net8.0\ScreenTimeMonitor.Service.exe"
   ```

2. Check service exists:
   ```powershell
   Get-Service -Name ScreenTimeMonitor
   ```

3. Check service logs:
   ```powershell
   Get-EventLog -LogName Application -Source ScreenTimeMonitor -Newest 10
   ```

### Problem: Permission denied on startup script

**Solution:**
Enable execution policy temporarily:
```powershell
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process
.\setup-autostart.ps1
```

### Problem: .NET 8.0 SDK not found

**Solution:**
1. Install from: https://dotnet.microsoft.com/download/dotnet/8.0
2. Download **SDK** (not Runtime)
3. Install and restart PowerShell
4. Verify:
   ```powershell
   dotnet --version
   ```

---

## 📊 What Happens on Startup

### Option 1 (UI Auto-Launch):
```
Windows Starts
    ↓
Registry entry triggers (HKCU\Software\Microsoft\Windows\CurrentVersion\Run)
    ↓
ScreenTimeMonitor.UI.WPF.exe launches
    ↓
Service starts in background (when needed)
    ↓
Monitoring begins
```

### Option 2 (Full Service):
```
Windows Starts
    ↓
Windows Service starts automatically
    ↓
ScreenTimeMonitor Service (monitoring in background)
    ↓
Registry entry triggers
    ↓
ScreenTimeMonitor.UI.WPF.exe launches
    ↓
Full monitoring with UI and background service
```

---

## 🔍 Verifying Autostart Works

After setting up autostart, verify with:

### Check Registry Entry
```powershell
Get-ItemProperty "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" | Select-Object ScreenTimeMonitor
```

### Check Startup Folder
```powershell
Get-ChildItem "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup" | Select-Object Name
```

### If Service Installed
```powershell
Get-Service -Name ScreenTimeMonitor | Select-Object Name, Status, StartType
```

### View Recent Log Files
```powershell
Get-ChildItem "ScreenTimeMonitor.Service\logs" | Sort-Object LastWriteTime -Descending | Select-Object -First 5
```

---

## 💡 Pro Tips

### Tip 1: System Tray Integration
The application minimizes to system tray on startup. Click the icon to show/hide the window.

### Tip 2: Disable on Specific Days
If you want to disable monitoring temporarily:
1. Open the UI application
2. Use Settings → Disable Monitoring
3. Re-enable whenever needed

### Tip 3: Custom Startup Parameters
To modify startup behavior, edit `appsettings.json`:
```json
{
  "AppMode": "School",
  "Paths": {
    "DatabasePath": "./data/screentime_monitor.db"
  }
}
```

### Tip 4: Check if App is Running
```powershell
Get-Process | Where-Object { $_.ProcessName -like "*ScreenTimeMonitor*" }
```

### Tip 5: Clean Up Logs
Logs are stored in `./logs/` folder. Safe to delete old logs:
```powershell
Remove-Item "ScreenTimeMonitor.Service\logs\*" -Recurse -Force
```

---

## ✨ Next Steps

After autostart is configured:

1. **Restart your PC** to verify everything works
2. **Review monitoring data** through the UI dashboard
3. **Configure settings** as needed in the application
4. **Share results** for your project presentation

---

## 📞 Support

If you encounter issues:

1. Check [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md) for deployment details
2. Review [TECHNICAL_RECOMMENDATIONS.md](TECHNICAL_RECOMMENDATIONS.md) for architecture
3. Check logs in `ScreenTimeMonitor.Service/logs/` folder
4. Verify .NET 8.0 SDK is installed correctly

---

**Happy Monitoring!** 🎉
