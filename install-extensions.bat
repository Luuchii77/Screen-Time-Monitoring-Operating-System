@echo off
REM ============================================================================
REM ScreenTimeMonitor - VS Code Extensions Installer
REM ============================================================================
REM This script automatically installs all required VS Code extensions
REM for the ScreenTimeMonitor project development
REM ============================================================================

setlocal enabledelayedexpansion

REM Colors and formatting
color 0A
echo.
echo ============================================================================
echo  ScreenTimeMonitor - VS Code Extensions Installer
echo ============================================================================
echo.

REM Check if VS Code is installed
where code >nul 2>nul
if errorlevel 1 (
    echo [ERROR] VS Code is not installed or not in PATH
    echo Please install VS Code from: https://code.visualstudio.com/
    echo Or add VS Code to your PATH environment variable
    pause
    exit /b 1
)

echo [INFO] VS Code detected. Installing extensions...
echo.

REM List of extensions to install
REM Format: publisher.extension-name

echo [1/9] Installing C# Dev Kit (Microsoft) - Official C# Support...
code --install-extension ms-dotnettools.csharp
if errorlevel 1 echo [WARNING] Failed to install C# Dev Kit
echo.

echo [2/9] Installing C# Extensions (kreativ-software) - Additional C# Tools...
code --install-extension kreativ-software.csharp-extensions
if errorlevel 1 echo [WARNING] Failed to install C# Extensions
echo.

echo [3/9] Installing NuGet Package Manager (jmrog) - Manage Dependencies...
code --install-extension jmrog.vscode-nuget-package-manager
if errorlevel 1 echo [WARNING] Failed to install NuGet Package Manager
echo.

echo [4/9] Installing SQLTools (mtxr) - Database Management...
code --install-extension mtxr.sqltools
if errorlevel 1 echo [WARNING] Failed to install SQLTools
echo.

echo [5/9] Installing SQLTools SQLite (mtxr) - SQLite Support...
code --install-extension mtxr.sqltools-driver-sqlite
if errorlevel 1 echo [WARNING] Failed to install SQLTools SQLite Driver
echo.

echo [6/9] Installing REST Client (humao) - API Testing...
code --install-extension humao.rest-client
if errorlevel 1 echo [WARNING] Failed to install REST Client
echo.

echo [7/9] Installing Thunder Client (rangav) - API Testing...
code --install-extension rangav.vscode-thunder-client
if errorlevel 1 echo [WARNING] Failed to install Thunder Client
echo.

echo [8/9] Installing Pylance (Microsoft) - Python Support (Optional)...
code --install-extension ms-python.vscode-pylance
if errorlevel 1 echo [WARNING] Failed to install Pylance (Python support)
echo.

echo [9/9] Installing PowerShell (Microsoft) - PowerShell Support...
code --install-extension ms-vscode.powershell
if errorlevel 1 echo [WARNING] Failed to install PowerShell extension
echo.

echo ============================================================================
echo  Optional Extensions (not installed automatically)
echo ============================================================================
echo.
echo These extensions can be installed manually if desired:
echo.
echo - GitLens (eamodio.gitlens)
echo   Better Git integration and blame annotations
echo.
echo - Postman (postman.postman-for-vscode)
echo   Advanced API testing (alternative to REST Client/Thunder Client)
echo.
echo - Draw.io Integration (hediet.vscode-drawio)
echo   Diagram and flowchart creation
echo.
echo - Better Comments (aaron-bond.better-comments)
echo   Improved comment highlighting
echo.
echo - Code Spell Checker (streetsidesoftware.code-spell-checker)
echo   Spelling and grammar checking
echo.

echo ============================================================================
echo  Installation Complete!
echo ============================================================================
echo.
echo [SUCCESS] All required extensions have been installed.
echo.
echo Next Steps:
echo   1. Restart VS Code (Close and reopen)
echo   2. Open your ScreenTimeMonitor project folder
echo   3. VS Code will finish setting up C# and IntelliSense
echo.
echo To view installed extensions in VS Code:
echo   - Press Ctrl+Shift+X to open Extensions panel
echo   - Search for extensions or view "Installed" section
echo.

pause
