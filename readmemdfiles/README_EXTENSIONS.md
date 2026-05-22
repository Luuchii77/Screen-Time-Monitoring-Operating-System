# 🎯 VS Code Extensions - Automatic Installer

## What You Get

Two automated scripts to install **all required VS Code extensions** for the ScreenTimeMonitor project in seconds!

```
📦 Files Included:
├── install-extensions.bat          ← Batch file (easiest)
├── install-extensions.ps1          ← PowerShell (advanced)
├── EXTENSIONS_INSTALLER.md         ← Full documentation
├── EXTENSIONS_QUICK_REF.md         ← Quick reference
└── COMPLETE_SETUP_GUIDE.md         ← Full setup guide
```

---

## ⚡ Quick Start (Pick One)

### 🟢 Easiest: Batch File
```cmd
install-extensions.bat
```
✅ Works on all Windows versions  
✅ No special setup needed  
✅ Installs 8 required extensions in 2-5 minutes

### 🔵 Recommended: PowerShell
```powershell
.\install-extensions.ps1
```
✅ Better error handling  
✅ Color-coded output  
✅ Option for optional extensions  
✅ More control and feedback

### 🟣 With Optional Extensions
```powershell
.\install-extensions.ps1 -IncludeOptional
```
✅ All required extensions  
✅ Plus 5 optional ones (GitLens, Draw.io, etc.)

---

## 📋 8 Required Extensions

| # | Extension | Publisher |
|---|-----------|-----------|
| 1 | **C# Dev Kit** | ms-dotnettools.csharp |
| 2 | **C# Extensions** | kreativ-software.csharp-extensions |
| 3 | **NuGet Package Manager** | jmrog.vscode-nuget-package-manager |
| 4 | **SQLTools** | mtxr.sqltools |
| 5 | **SQLTools SQLite** | mtxr.sqltools-driver-sqlite |
| 6 | **REST Client** | humao.rest-client |
| 7 | **Thunder Client** | rangav.vscode-thunder-client |
| 8 | **PowerShell** | ms-vscode.powershell |

---

## 📁 File Guide

### `install-extensions.bat`
**Type:** Batch File  
**Size:** ~4 KB  
**Use:** Simple, automatic extension installation

**How to run:**
```cmd
cd "C:\Users\PC\Downloads\School Files\Operating System Project"
install-extensions.bat
```

**Perfect for:**
- First-time setup
- Beginners
- Quick installation
- Any Windows version

---

### `install-extensions.ps1`
**Type:** PowerShell Script  
**Size:** ~7 KB  
**Use:** Advanced installation with options

**How to run:**
```powershell
cd "C:\Users\PC\Downloads\School Files\Operating System Project"
.\install-extensions.ps1
```

**Features:**
- Color-coded output
- Better error reporting
- Optional extensions
- Progress tracking
- Detailed summary

**Advanced options:**
```powershell
.\install-extensions.ps1 -IncludeOptional    # Add 5 extra extensions
.\install-extensions.ps1 -Verbose             # Detailed output
```

---

### `EXTENSIONS_INSTALLER.md`
**Type:** Full Documentation  
**Size:** ~11 KB  
**Content:**
- Installation methods
- Troubleshooting guide
- Manual installation options
- Extension details
- Setup for teams

**Read this if you:**
- Have installation issues
- Want detailed information
- Need troubleshooting help
- Want to understand what's installed

---

### `EXTENSIONS_QUICK_REF.md`
**Type:** Quick Reference  
**Size:** ~2.6 KB  
**Content:**
- Quick install commands
- Extension list
- Pro tips
- Keyboard shortcuts
- FAQs

**Read this if you:**
- Want the essentials only
- Need quick answers
- Like quick checklists

---

### `COMPLETE_SETUP_GUIDE.md`
**Type:** Full Setup Guide  
**Size:** ~10 KB  
**Content:**
- Step-by-step setup
- .NET installation
- VS Code installation
- Extension installation
- Build and test
- Troubleshooting

**Read this if you:**
- Setting up from scratch
- First time with the project
- Want complete walkthrough
- New to .NET development

---

## 🚀 What Happens When You Run

### Step-by-Step (Batch File)

1. **Check** - Verifies VS Code is installed
2. **Install** - Installs each extension one-by-one
3. **Report** - Shows success/failure for each
4. **Summary** - Provides completion report
5. **Done** - Ready to use!

### Example Output:
```
============================================================================
  ScreenTimeMonitor - VS Code Extensions Installer
============================================================================

[INFO] VS Code detected. Installing extensions...

[1/9] Installing C# Dev Kit (Microsoft) - Official C# Support...
[2/9] Installing C# Extensions (kreativ-software)...
[3/9] Installing NuGet Package Manager...
...
[SUCCESS] All required extensions have been installed.
```

---

## ✅ After Installation

### 1. Restart VS Code
- Close all VS Code windows
- Reopen VS Code
- Wait for "Initializing..." to finish

### 2. Verify Installation
```powershell
# In VS Code or PowerShell:
code --list-extensions
```

### 3. Check Extensions Panel
- Press `Ctrl+Shift+X` in VS Code
- Look for "Installed" section
- You should see all 8 extensions

### 4. Start Developing!
```powershell
code "C:\Users\PC\Downloads\School Files\Operating System Project"
```

---

## 🆘 Troubleshooting

### Problem: "VS Code not found in PATH"

**Solution 1:** Add VS Code to PATH
- Open VS Code
- Press `Ctrl+Shift+P`
- Type "Shell Command: Install code command"
- Click it
- Restart terminal
- Try installer again

**Solution 2:** Reinstall VS Code
- Download from https://code.visualstudio.com/
- During install, check "Add to PATH"
- Restart computer
- Try installer again

### Problem: PowerShell execution policy error

**Solution:**
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
.\install-extensions.ps1
```

### Problem: Extension installation failed

**Solutions:**
1. Check internet connection
2. Close all VS Code windows
3. Try again
4. See `EXTENSIONS_INSTALLER.md` for manual options

### Problem: Extensions don't show up

**Solutions:**
1. Restart VS Code (close and reopen)
2. Wait 30 seconds for initialization
3. Check `code --list-extensions`
4. Clear cache: Delete `.vscode` folder (be careful!)

---

## 📊 Comparison: Batch vs PowerShell

| Feature | Batch | PowerShell |
|---------|-------|-----------|
| Simple to use | ✅ Yes | ✅ Yes |
| Works on all Windows | ✅ Yes | ✅ Yes |
| Color output | ❌ No | ✅ Yes |
| Optional extensions | ❌ No | ✅ Yes |
| Error reporting | ⚠️ Basic | ✅ Detailed |
| Learning curve | ✅ Minimal | ⚠️ Slight |
| Installation time | ~3 min | ~3 min |

**Recommendation:** Start with batch file. If you want more features, use PowerShell.

---

## 🔗 What Each Extension Does

### **C# Dev Kit** 
Essential for C# development
- IntelliSense (code completion)
- Debugging
- Error checking
- Go to definition

### **C# Extensions**
Additional C# productivity tools
- Snippets
- Code generation
- Additional refactoring

### **NuGet Package Manager**
Manage project dependencies
- Install packages
- Update packages
- View package info

### **SQLTools**
Database management UI
- Browse tables
- Run queries
- Manage connections

### **SQLTools SQLite**
SQLite database driver for SQLTools
- SQLite support
- Database visualization

### **REST Client**
Test APIs directly in VS Code
- Write HTTP requests
- Test endpoints
- View responses

### **Thunder Client**
Alternative API testing tool
- Beautiful UI
- Request collections
- Response inspection

### **PowerShell**
PowerShell script support
- Syntax highlighting
- Debugging
- Integration with terminal

---

## 💡 Pro Tips

1. **Run installer multiple times**: It's safe! Skips already-installed extensions

2. **Close VS Code first**: Cleaner installation (optional but recommended)

3. **Run as Administrator**: If you get permission errors

4. **Check the logs**: If something goes wrong, see `EXTENSIONS_INSTALLER.md`

5. **Restart after install**: Required for full functionality

6. **Share with team**: Include these scripts in your project so teammates can install easily

---

## 🎓 For Classroom / Team Use

### Share the Installers

Give your team these files:
```
ScreenTimeMonitor/
├── install-extensions.bat          ← Batch installer
├── install-extensions.ps1          ← PowerShell installer
├── EXTENSIONS_INSTALLER.md         ← Documentation
└── README.md                        ← Project info
```

### Everyone Gets Same Setup

1. Each person runs installer
2. All extensions install automatically
3. Everyone has identical setup
4. No "Works on my machine" issues! 🎉

### Consistency Benefits
- ✅ Same extensions
- ✅ Same configurations
- ✅ Same debugging experience
- ✅ Easy peer programming
- ✅ Less support overhead

---

## 📚 Additional Resources

For more information, see:
- `EXTENSIONS_INSTALLER.md` - Comprehensive guide
- `EXTENSIONS_QUICK_REF.md` - Quick reference
- `COMPLETE_SETUP_GUIDE.md` - Full setup walkthrough
- `COMMANDS_REFERENCE.md` - Build and run commands
- `VS_CODE_SETUP_GUIDE.md` - VS Code configuration

---

## ✨ Summary

**What:** Automated VS Code extension installer  
**How:** Run one command  
**Time:** 2-5 minutes  
**Result:** 8 extensions installed, ready to develop  
**Files:** `install-extensions.bat` or `install-extensions.ps1`

---

**Next Step:** Pick either `.bat` or `.ps1` and run it! 🚀

```cmd
REM Batch version:
install-extensions.bat
```

```powershell
# PowerShell version:
.\install-extensions.ps1
```

**That's it!** Your VS Code is ready for ScreenTimeMonitor development. 🎉

---

**Created:** December 12, 2025  
**Status:** Ready to Use  
**Maintenance:** Minimal - Extensions auto-update
