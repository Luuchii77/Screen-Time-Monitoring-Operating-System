# 🚀 ScreenTimeMonitor - Complete Setup Guide

## Welcome!

This guide walks you through setting up the ScreenTimeMonitor project from scratch, including all necessary tools and extensions.

---

## 📋 Setup Checklist

- [ ] **Step 1**: Install .NET 8.0 SDK
- [ ] **Step 2**: Install Visual Studio Code (optional but recommended)
- [ ] **Step 3**: Install VS Code Extensions (automated)
- [ ] **Step 4**: Clone/Extract project
- [ ] **Step 5**: Build and test

---

## Step 1️⃣: Install .NET 8.0 SDK

### Check if Already Installed
```powershell
dotnet --version
```

If you see `8.0.x` or higher, skip to Step 2.

### Download and Install

1. Visit: https://dotnet.microsoft.com/download/dotnet/8.0
2. Download **Windows x64 SDK** (not Runtime)
3. Run installer and follow steps
4. Restart your computer
5. Verify:
   ```powershell
   dotnet --version
   ```

---

## Step 2️⃣: Install Visual Studio Code (Optional)

Visual Studio Code is recommended for easy development, but not required.

### Download
- Visit: https://code.visualstudio.com/
- Download for Windows

### Install
1. Run installer
2. During installation, check: ✓ "Add to PATH"
3. Finish installation
4. Open VS Code to verify

### Note
You can develop with any editor (Visual Studio, Rider, notepad + command line), but this guide assumes VS Code.

---

## Step 3️⃣: Install VS Code Extensions (Automated!)

This is the easiest step. Run ONE command and all extensions install automatically.

### Option A: Batch File (Easiest)

```cmd
cd "C:\Users\PC\Downloads\School Files\Operating System Project"
install-extensions.bat
```

**What it does:**
- ✅ Checks if VS Code is installed
- ✅ Installs 8 required extensions automatically
- ✅ Shows progress for each extension
- ✅ Done in 2-5 minutes

### Option B: PowerShell (Recommended)

```powershell
cd "C:\Users\PC\Downloads\School Files\Operating System Project"
.\install-extensions.ps1
```

**Features:**
- ✅ Color-coded output (easier to read)
- ✅ Better error reporting
- ✅ Option to install optional extensions
- ✅ Same speed as batch

### Option C: PowerShell with Optional Extensions

```powershell
.\install-extensions.ps1 -IncludeOptional
```

**Installs additional:**
- 📌 GitLens (Git integration)
- 📌 Draw.io (Diagrams)
- 📌 Better Comments
- 📌 Spell Checker
- 📌 Python support

### What Gets Installed

| Extension | Why |
|-----------|-----|
| C# Dev Kit | Official C# support, debugging, IntelliSense |
| C# Extensions | Extra C# tools and snippets |
| NuGet Manager | Manage C# dependencies |
| SQLTools | Database management UI |
| SQLTools SQLite | SQLite database support |
| REST Client | Test APIs |
| Thunder Client | Alternative API testing |
| PowerShell | PowerShell script support |

---

## Step 4️⃣: Get the Project

### Option A: Already Have It
If you already have the project folder extracted, just open it in VS Code:

```powershell
code "C:\Users\PC\Downloads\School Files\Operating System Project"
```

### Option B: Clone from GitHub
```powershell
git clone <repository-url>
cd ScreenTimeMonitor
code .
```

### Option C: Download ZIP
1. Download ZIP from repository
2. Extract to a folder
3. Open in VS Code:
   ```powershell
   code "path/to/ScreenTimeMonitor"
   ```

---

## Step 5️⃣: Build and Test

### Open Terminal in VS Code
- Press `Ctrl+`` (backtick) or go to Terminal → New Terminal

### Build the Project
```powershell
dotnet build -c Debug
```

**Expected output:**
```
✓ Build succeeded
```

### Run Tests (Optional)
```powershell
dotnet test
```

### Start the Application
See [COMMANDS_REFERENCE.md](COMMANDS_REFERENCE.md) for detailed run instructions.

**Quick start:**
```powershell
# Terminal 1: Start Service
cd ScreenTimeMonitor.Service
dotnet run -c Debug

# Terminal 2: Start UI (after 3 seconds)
cd ScreenTimeMonitor.UI.WPF
dotnet run -c Debug
```

---

## 🎯 Configuration

### School Mode (Default)
Database stores at: `./data/screentime_monitor.db` (relative path)
- ✅ Works on USB drives
- ✅ No admin rights needed
- ✅ Perfect for assignments

### Personal Mode
Database stores at: `C:\ProgramData\ScreenTimeMonitor\` (absolute path)
- ✅ Integrates with Windows Service
- ✅ Production ready
- ✅ Requires admin setup

To switch modes, edit `appsettings.json` in the project root.

See [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md) for details.

---

## 📚 Documentation

| Document | Purpose |
|----------|---------|
| [COMMANDS_REFERENCE.md](COMMANDS_REFERENCE.md) | Build, run, test commands |
| [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md) | School vs Personal deployment |
| [EXTENSIONS_INSTALLER.md](EXTENSIONS_INSTALLER.md) | Extensions guide (detailed) |
| [EXTENSIONS_QUICK_REF.md](EXTENSIONS_QUICK_REF.md) | Extensions quick reference |
| [VS_CODE_SETUP_GUIDE.md](VS_CODE_SETUP_GUIDE.md) | VS Code configuration |
| [PHASE_1_PORTABILITY_COMPLETE.md](PHASE_1_PORTABILITY_COMPLETE.md) | Configuration system details |

---

## 🔧 Troubleshooting

### "dotnet command not found"
- .NET SDK not installed (go back to Step 1)
- Or not in PATH: restart computer

### "code command not found"
- VS Code not installed (Step 2)
- Or add to PATH: restart computer

### "Extension installation failed"
- Check internet connection
- VS Code might be open: close it, try again
- See [EXTENSIONS_INSTALLER.md](EXTENSIONS_INSTALLER.md) for manual installation

### "Build fails with C# errors"
- Restart VS Code (Ctrl+Shift+P → Developer: Reload Window)
- Wait for C# extension to initialize
- Try `dotnet clean` then `dotnet build`

### "Service connection timeout"
- Service needs to start first
- Wait 3-5 seconds before starting UI
- See [COMMANDS_REFERENCE.md](COMMANDS_REFERENCE.md) for exact timing

---

## ✅ Verification Checklist

After setup, verify everything works:

```powershell
# Check .NET installation
dotnet --version
# Expected: 8.x.x

# Check VS Code
code --version
# Expected: version number

# Check Git (if using)
git --version
# Expected: 2.x.x

# Build the project
cd "C:\Users\PC\Downloads\School Files\Operating System Project"
dotnet build -c Debug
# Expected: "Build succeeded"

# Check extensions (if installed)
code --list-extensions | findstr csharp
# Expected: ms-dotnettools.csharp
```

---

## 🚀 Next Steps

1. **Learn the Project Structure**
   - Read [PROJECT_ANALYSIS.md](PROJECT_ANALYSIS.md) for architecture

2. **Understand Commands**
   - Read [COMMANDS_REFERENCE.md](COMMANDS_REFERENCE.md) for all commands

3. **Run the Application**
   - Follow [COMMANDS_REFERENCE.md](COMMANDS_REFERENCE.md) run instructions

4. **Make Changes**
   - VS Code handles most development tasks
   - IntelliSense and debugging work automatically

5. **Deploy**
   - For school: keep default (relative paths)
   - For personal: follow [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md)

---

## 💡 Pro Tips

### VS Code Shortcuts
- `Ctrl+Shift+X` - Open Extensions
- `Ctrl+K Ctrl+T` - Change theme
- `Ctrl+Shift+P` - Command palette
- `F5` - Start debugging
- `Ctrl+`` - Toggle terminal

### PowerShell Aliases
```powershell
# Quick navigation
cd "C:\Users\PC\Downloads\School Files\Operating System Project"

# Or create alias
Set-Alias proj 'C:\Users\PC\Downloads\School Files\Operating System Project'
proj  # Now you can just type this!
```

### Stay Updated
- Pull latest changes regularly
- Check for .NET updates monthly
- Update extensions regularly (Ctrl+Shift+X → Update All)

---

## 📞 Need Help?

### Common Issues

**Issue**: Errors after pulling code
- **Solution**: `dotnet clean` then `dotnet build`

**Issue**: IntelliSense not working
- **Solution**: Restart VS Code, wait 30 seconds

**Issue**: Port already in use
- **Solution**: Change port in `appsettings.json` or stop other apps

**Issue**: Database file locked
- **Solution**: Stop all running apps, wait 5 seconds, try again

---

## 🎓 For School Projects

### Submission Preparation

1. **Clean before submitting:**
   ```powershell
   dotnet clean
   # Remove bin/ and obj/ folders
   ```

2. **Include documentation:**
   - ✓ README.md
   - ✓ SETUP_GUIDE.md (this file)
   - ✓ COMMANDS_REFERENCE.md

3. **Test on fresh machine if possible:**
   - Extract on different computer
   - Run `install-extensions.bat`
   - Run `dotnet build`
   - Verify it works

4. **Include installer scripts:**
   - `install-extensions.bat` - Makes setup easy
   - `install-extensions.ps1` - Alternative

---

## 📈 Progress Tracking

**Completed:**
- ✅ Configuration system (Phase 1)
- ✅ Portable paths (School/Personal modes)
- ✅ Extension installers (batch + PowerShell)
- ✅ Comprehensive documentation

**Coming Soon:**
- 🔄 Standalone launcher (Phase 2)
- 🔄 Installer script (Phase 3)
- 🔄 Hardware robustness (Phase 4)
- 🔄 Configuration UI (Phase 5)

---

## 📊 System Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| OS | Windows 10 | Windows 11 |
| RAM | 4 GB | 8 GB |
| Disk Space | 2 GB | 5 GB |
| .NET SDK | 8.0.0 | 8.0.x (latest) |
| VS Code | Latest | Latest |

---

## 🎉 You're Ready!

Once you complete all steps:

1. ✅ .NET 8.0 SDK installed
2. ✅ VS Code installed with extensions
3. ✅ Project extracted/cloned
4. ✅ Build successful
5. ✅ Ready to develop!

**Time to completion**: ~15 minutes (mostly waiting for downloads)

---

## 📞 Quick Links

- [.NET Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- [VS Code Download](https://code.visualstudio.com/)
- [Project Repository](https://github.com/yourrepo)
- [Documentation Hub](./README.md)

---

**Created**: December 12, 2025  
**Last Updated**: December 12, 2025  
**Status**: Complete & Ready to Use

Good luck with your project! 🚀
