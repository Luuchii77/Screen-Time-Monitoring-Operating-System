# VS Code Extensions - Quick Reference

## 🚀 Quick Install

### Easiest Method (Batch File)
```cmd
install-extensions.bat
```

### Advanced Method (PowerShell)
```powershell
.\install-extensions.ps1
```

### With Optional Extensions
```powershell
.\install-extensions.ps1 -IncludeOptional
```

---

## 📋 What Gets Installed

### Core Development (8 extensions)
```
✓ C# Dev Kit              → IntelliSense & debugging
✓ C# Extensions           → Extra C# tools
✓ NuGet Manager           → Manage dependencies
✓ SQLTools                → Database UI
✓ SQLTools SQLite         → SQLite support
✓ REST Client             → Test APIs
✓ Thunder Client          → API testing
✓ PowerShell              → Script support
```

### Optional (5 extensions)
```
• GitLens                 → Git integration
• Draw.io                 → Create diagrams
• Better Comments         → Comment highlighting
• Code Spell Checker      → Spelling check
• Pylance                 → Python support
```

---

## ⚡ Troubleshooting

### "VS Code not found in PATH"
```powershell
# Open VS Code
# Ctrl+Shift+P → "Shell Command: Install code command"
```

### PowerShell execution policy error
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Check what's installed
```powershell
code --list-extensions
```

---

## ✅ After Installation

1. **Close and restart VS Code**
2. **Wait for "Initializing..." message to finish**
3. **Press Ctrl+Shift+X to see extensions panel**
4. **Check "Installed" section**

---

## 📂 Files in This Project

- `install-extensions.bat` - Batch installer (Windows cmd)
- `install-extensions.ps1` - PowerShell installer (advanced)
- `EXTENSIONS_INSTALLER.md` - Full documentation
- `EXTENSIONS_QUICK_REF.md` - This file

---

## 🔗 Useful Keyboard Shortcuts

| Action | Shortcut |
|--------|----------|
| Open Extensions | Ctrl+Shift+X |
| Search in Extensions | Ctrl+Shift+X, then type |
| Install from Command Line | `code --install-extension <id>` |
| List Installed | `code --list-extensions` |
| Open Terminal | Ctrl+` |
| Command Palette | Ctrl+Shift+P |

---

## 💡 Pro Tips

1. **Run installer multiple times**: Safe! Skips already installed
2. **Run as Administrator**: If permission errors occur
3. **Close VS Code first**: Before running installer (optional but cleaner)
4. **Wait for initialization**: After restart, VS Code needs time to set up C#

---

**Total Installation Time**: 2-5 minutes  
**Required for Development**: Yes  
**Affects existing setup**: No
