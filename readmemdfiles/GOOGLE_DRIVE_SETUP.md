# Screen Time Monitor - Google Drive Migration Guide

## ✅ Ready for Multi-Device Use!

Your project is fully portable and ready for Google Drive. This guide explains how to use it across multiple devices.

---

## 📱 Setup on Google Drive

### Step 1: Upload to Google Drive
1. Open Google Drive
2. Create a new folder: `ScreenTimeMonitor`
3. Upload the entire project folder to Google Drive
   - **Important:** The `.gitignore` file ensures `data/` and `logs/` folders are excluded
   - Only source code (~5-10 MB) is uploaded, not the database or logs

### Step 2: Sync to Another Device
1. Install Google Drive Desktop (Windows) or Google Drive for Mac
2. Navigate to: `Google Drive > ScreenTimeMonitor`
3. The project automatically syncs to your other device

---

## 🚀 Quick Setup on Any Device

### Option A: Automated Setup (Recommended)
**Windows:**
```powershell
# Open PowerShell in the project root and run:
.\setup.ps1
```

**Or use the batch file:**
```cmd
setup.bat
```

### Option B: Manual Setup
```powershell
cd "path\to\ScreenTimeMonitor"
dotnet build -c Debug
# Then in two separate PowerShell windows:
# Window 1: cd ScreenTimeMonitor.Service && dotnet run -c Debug
# Window 2: cd ScreenTimeMonitor.UI.WPF && dotnet run -c Debug
```

---

## 📁 Project Structure (Google Drive Friendly)

```
ScreenTimeMonitor/
├── .gitignore                    # Prevents data/ and logs/ from syncing
├── setup.ps1                     # Automated setup script
├── setup.bat                     # Batch setup script
├── appsettings.json             # Config (portable paths)
├── ScreenTimeMonitor.sln
├── ScreenTimeMonitor.Service/   # Service application
├── ScreenTimeMonitor.UI.WPF/    # UI application
├── ScreenTimeMonitor.Tests/     # Test suite
├── Database/                     # Schema files
└── docs/                         # Documentation
```

**Files NOT synced to Google Drive (by .gitignore):**
- `data/` - Local database (created on first run)
- `logs/` - Application logs (created at runtime)
- `bin/` and `obj/` - Build artifacts
- `.vs/` - IDE cache

---

## 💾 Database Portability

### Option 1: Fresh Database on Each Device (Default)
- Each device gets its own database file at `./data/screentime_monitor.db`
- App starts tracking immediately when you connect
- **Pros:** Simple, independent tracking per device
- **Cons:** History doesn't sync between devices

### Option 2: Shared Database (Advanced)
If you want the same database on all devices:
1. On Device A: Close the application
2. Copy `./data/screentime_monitor.db` from Device A
3. Paste it into `./data/` folder on Device B (create folder if needed)
4. Start the application on Device B
5. Now both devices share the same usage history

---

## 🔄 Workflow: Device A → Device B

### Device A (Main Machine):
1. Run the application normally
2. Track usage as usual
3. Close the application when done
4. Google Drive automatically syncs changes

### Device B (Secondary Machine):
1. Open Google Drive Desktop
2. Wait for `ScreenTimeMonitor` folder to sync (~1-2 minutes)
3. Open PowerShell in the project folder
4. Run: `.\setup.ps1`
5. Start the Service and UI
6. Start tracking!

---

## ⚙️ Configuration

The app is already configured for portability in **School Mode**:

**appsettings.json:**
```json
{
  "AppMode": "School",
  "Paths": {
    "DatabasePath": "./data/screentime_monitor.db",
    "DataDirectory": "./data",
    "LogDirectory": "./logs"
  }
}
```

All paths are **relative** (`./`), so the app works anywhere you extract it.

---

## 🆘 Troubleshooting

### Issue: "Database locked" error
**Solution:** Make sure only one instance of the application is running per device.

### Issue: No data syncing between devices
**This is by design** - each device has its own database. If you want shared data:
1. Use Option 2 above (copy database file)
2. Or use the same device

### Issue: Google Drive shows "Syncing..." forever
**Solution:** 
- Make sure `.gitignore` is included (prevents large log files from syncing)
- Close the application before checking Google Drive
- Wait 2-3 minutes for sync to complete

---

## 📝 Important Notes

1. **Always close the application before moving to another device**
   - Prevents database corruption

2. **The `.gitignore` is essential**
   - Prevents `data/` and `logs/` from syncing
   - These are created fresh on each device

3. **NuGet packages auto-restore**
   - First run takes longer (~1-2 minutes) while packages download
   - Subsequent runs are fast

4. **Each device gets independent tracking**
   - Unless you manually copy the database file
   - Good for privacy, each device has its own usage history

---

## 🎯 Best Practices

✅ **DO:**
- Close the app before switching devices
- Let Google Drive sync before opening on another device
- Run `setup.ps1` on each new device
- Keep `.gitignore` in the project (don't delete it)

❌ **DON'T:**
- Run the app simultaneously on multiple devices
- Delete the `appsettings.json` file
- Manually move `data/` folder to Google Drive (excluded by design)

---

## 📚 Additional Resources

- **README.md** - General project information
- **COMMANDS_REFERENCE.md** - All available commands
- **Database Schema** - See `Database/schema-sqlite.sql`

---

## ✨ You're All Set!

Your project is ready for:
- ✅ Google Drive storage
- ✅ Multi-device sync
- ✅ Portable execution
- ✅ Zero configuration needed on new devices

Just run `setup.ps1` and start tracking! 🚀
