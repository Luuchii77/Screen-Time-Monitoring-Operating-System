# VS Code Extensions Installer Guide

## Overview

This guide explains how to use the automated extension installers to set up all required VS Code extensions for the ScreenTimeMonitor project.

Two installer scripts are provided:
- **install-extensions.bat** - Batch file (simple, works on all Windows versions)
- **install-extensions.ps1** - PowerShell script (advanced, more features)

---

## Quick Start

### Option 1: Using Batch File (Easiest)

1. **Open Command Prompt** (cmd.exe)
2. **Navigate to project folder**:
   ```cmd
   cd "C:\Users\PC\Downloads\School Files\Operating System Project"
   ```

3. **Run the installer**:
   ```cmd
   install-extensions.bat
   ```

4. **Wait for completion** - Script will install all extensions automatically
5. **Restart VS Code** completely (close and reopen)

### Option 2: Using PowerShell (Recommended)

1. **Open PowerShell** as Administrator
2. **Navigate to project folder**:
   ```powershell
   cd "C:\Users\PC\Downloads\School Files\Operating System Project"
   ```

3. **Run the installer**:
   ```powershell
   .\install-extensions.ps1
   ```

4. **Optional: Install optional extensions**:
   ```powershell
   .\install-extensions.ps1 -IncludeOptional
   ```

5. **Wait for completion** - Script will install all extensions automatically
6. **Restart VS Code** completely (close and reopen)

### Option 3: From VS Code Terminal

1. **Open VS Code**
2. **Open Integrated Terminal**: Ctrl + `
3. **Run the installer**:
   ```powershell
   .\install-extensions.ps1
   ```

---

## Extensions Installed

### Required Extensions (Automatically Installed)

| Extension | Publisher | Purpose |
|-----------|-----------|---------|
| **C# Dev Kit** | ms-dotnettools.csharp | Official C# support, IntelliSense, debugging |
| **C# Extensions** | kreativ-software.csharp-extensions | Additional C# tools and snippets |
| **NuGet Package Manager** | jmrog.vscode-nuget-package-manager | Manage C# NuGet dependencies |
| **SQLTools** | mtxr.sqltools | Database management UI |
| **SQLTools SQLite** | mtxr.sqltools-driver-sqlite | SQLite database support |
| **REST Client** | humao.rest-client | Test APIs directly in VS Code |
| **Thunder Client** | rangav.vscode-thunder-client | Alternative API testing tool |
| **PowerShell** | ms-vscode.powershell | PowerShell script support |

### Optional Extensions (Manual or with -IncludeOptional)

| Extension | Publisher | Purpose |
|-----------|-----------|---------|
| **GitLens** | eamodio.gitlens | Enhanced Git integration and blame |
| **Draw.io Integration** | hediet.vscode-drawio | Create diagrams and flowcharts |
| **Better Comments** | aaron-bond.better-comments | Improved comment highlighting |
| **Code Spell Checker** | streetsidesoftware.code-spell-checker | Spell checking for code |
| **Pylance** | ms-python.vscode-pylance | Python language support (optional) |

---

## Installation Methods Detailed

### Batch File (`install-extensions.bat`)

**Pros:**
- ✅ Simple and straightforward
- ✅ Works on all Windows versions
- ✅ No special permissions usually needed
- ✅ Clear progress messages

**Cons:**
- ❌ Less flexible (no options)
- ❌ Can't install optional extensions selectively
- ❌ Limited error reporting

**How It Works:**
1. Checks if VS Code is installed
2. Installs each extension one by one
3. Shows success/failure for each extension
4. Provides completion summary

### PowerShell Script (`install-extensions.ps1`)

**Pros:**
- ✅ More advanced features
- ✅ Better error handling and reporting
- ✅ Color-coded output (easier to read)
- ✅ Optional flags for selective installation
- ✅ Can pause and ask before continuing

**Cons:**
- ⚠️ Requires PowerShell (usually pre-installed on Windows 10/11)
- ⚠️ May require execution policy changes (see below)

**How It Works:**
1. Checks if VS Code is installed
2. Installs required extensions with progress
3. Offers to install optional extensions
4. Provides detailed installation summary

### PowerShell Execution Policy

If you get an error about execution policy, run one of these commands first:

**Option A: Allow scripts for this session only** (safest)
```powershell
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process
```

**Option B: Allow scripts for current user**
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

**Then run the installer:**
```powershell
.\install-extensions.ps1
```

---

## Troubleshooting

### Issue: "VS Code is not installed or not in PATH"

**Solution 1: Add VS Code to PATH**
1. Open VS Code
2. Press `Ctrl+Shift+P`
3. Search for "Shell Command: Install 'code' command in PATH"
4. Click it
5. Try the installer again

**Solution 2: Reinstall VS Code**
1. Download from https://code.visualstudio.com/
2. During installation, check "Add to PATH"
3. Complete installation
4. Try the installer again

### Issue: "Extension installation failed"

**Possible causes:**
- Network connection issue
- VS Code not running in correct mode
- Extension temporarily unavailable

**Solutions:**
1. Check internet connection
2. Close all VS Code windows
3. Wait a moment and try again
4. Install extensions manually (see below)

### Issue: PowerShell execution policy error

**Solution:**
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser -Force
.\install-extensions.ps1
```

---

## Manual Installation

If the automatic installers don't work, you can install extensions manually:

### Method 1: VS Code Extensions Panel

1. Open VS Code
2. Press `Ctrl+Shift+X` to open Extensions
3. Search for each extension name
4. Click "Install" button

### Method 2: Command Line (One at a time)

```powershell
# C# Dev Kit
code --install-extension ms-dotnettools.csharp

# NuGet Package Manager
code --install-extension jmrog.vscode-nuget-package-manager

# SQLTools
code --install-extension mtxr.sqltools
code --install-extension mtxr.sqltools-driver-sqlite

# REST Client
code --install-extension humao.rest-client

# Thunder Client
code --install-extension rangav.vscode-thunder-client

# PowerShell
code --install-extension ms-vscode.powershell
```

### Method 3: Extensions JSON File

Create or edit `.vscode/extensions.json` in your project:

```json
{
  "recommendations": [
    "ms-dotnettools.csharp",
    "kreativ-software.csharp-extensions",
    "jmrog.vscode-nuget-package-manager",
    "mtxr.sqltools",
    "mtxr.sqltools-driver-sqlite",
    "humao.rest-client",
    "rangav.vscode-thunder-client",
    "ms-vscode.powershell"
  ]
}
```

VS Code will suggest these extensions when you open the project.

---

## Verifying Installation

### Check Installed Extensions

1. **In VS Code:**
   - Press `Ctrl+Shift+X` to open Extensions panel
   - Look for "Installed" section on the left sidebar
   - You should see all extensions listed

2. **In PowerShell:**
   ```powershell
   code --list-extensions
   ```

3. **Expected output (or similar):**
   ```
   ms-dotnettools.csharp
   kreativ-software.csharp-extensions
   jmrog.vscode-nuget-package-manager
   mtxr.sqltools
   mtxr.sqltools-driver-sqlite
   humao.rest-client
   rangav.vscode-thunder-client
   ms-vscode.powershell
   ```

---

## After Installation

### 1. Restart VS Code
- Close all VS Code windows
- Reopen VS Code
- Wait for extensions to initialize (look for "Initializing..." at bottom)

### 2. Create Extensions Configuration (Optional)

Save recommended extensions for your team:

**File:** `.vscode/extensions.json`
```json
{
  "recommendations": [
    "ms-dotnettools.csharp",
    "kreativ-software.csharp-extensions",
    "jmrog.vscode-nuget-package-manager",
    "mtxr.sqltools",
    "mtxr.sqltools-driver-sqlite",
    "humao.rest-client",
    "rangav.vscode-thunder-client",
    "ms-vscode.powershell"
  ]
}
```

When teammates open the project, VS Code will recommend these extensions.

### 3. Configure Extensions (Optional)

#### C# Dev Kit
- Automatically configured
- IntelliSense should work immediately

#### SQLTools
1. Press `Ctrl+Shift+P`
2. Search "SQLTools: Add new connection"
3. Select "SQLite"
4. Point to your database file

#### REST Client
- Create files with `.rest` extension
- Write HTTP requests and test APIs

---

## PowerShell Script Options

Run with flags to customize behavior:

```powershell
# Install with optional extensions included
.\install-extensions.ps1 -IncludeOptional

# Verbose output (more detailed information)
.\install-extensions.ps1 -Verbose

# Both options together
.\install-extensions.ps1 -IncludeOptional -Verbose
```

---

## For Project Teams

### Share These Scripts

Include both scripts in your project root so team members can run them:

```
ScreenTimeMonitor/
├── install-extensions.bat      ← Run this (Windows)
├── install-extensions.ps1      ← Or this (PowerShell)
├── EXTENSIONS_INSTALLER.md     ← This guide
├── .vscode/
│   └── extensions.json         ← Recommendations file
└── ... (other files)
```

### Team Workflow

1. Developer clones project
2. Runs `install-extensions.bat` or `.ps1`
3. All necessary extensions install automatically
4. Everyone has consistent development environment
5. No manual extension hunting! 🎉

---

## FAQs

**Q: Do I need admin rights?**
A: Not usually, but if you get permission errors, run PowerShell as Administrator.

**Q: Can I use these scripts on Mac/Linux?**
A: These are Windows-specific. Mac/Linux users should run VS Code commands directly or use their native package managers.

**Q: Do all extensions need to be installed?**
A: The required extensions are essential. Optional ones enhance experience but aren't necessary.

**Q: How long does installation take?**
A: 2-5 minutes depending on internet speed and number of extensions.

**Q: Can I run the installer multiple times?**
A: Yes, it's safe. VS Code will skip already-installed extensions.

**Q: Will this affect my existing VS Code setup?**
A: No, it only adds extensions. Your existing settings and extensions remain unchanged.

---

## Support

If you encounter issues:

1. **Check VS Code is in PATH:**
   ```powershell
   where code
   ```

2. **Verify PowerShell version (if using .ps1):**
   ```powershell
   $PSVersionTable.PSVersion
   ```

3. **Check VS Code version:**
   ```cmd
   code --version
   ```

4. **View extension logs in VS Code:**
   - Help → Toggle Developer Tools
   - Check Console tab for errors

---

## References

- [VS Code Command Line](https://code.visualstudio.com/docs/editor/command-line)
- [VS Code Extension API](https://code.visualstudio.com/api)
- [PowerShell Execution Policies](https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_execution_policies)

---

**Created:** December 12, 2025  
**Last Updated:** December 12, 2025  
**Status:** Ready to Use
