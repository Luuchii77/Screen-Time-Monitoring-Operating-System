@echo off
REM ============================================================================
REM Screen Time Monitor Service - Quick Install (Batch Script)
REM ============================================================================
REM This batch script provides an easy way to install the service
REM Simply run this file from the Installer directory
REM
REM Requirements:
REM - Administrator privileges
REM - .NET 8.0 runtime installed
REM ============================================================================

setlocal enabledelayedexpansion

REM Check for administrator privileges
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo.
    echo ERROR: This script must be run as Administrator
    echo Please right-click and select "Run as administrator"
    pause
    exit /b 1
)

echo.
echo ============================================================
echo Screen Time Monitor Service - Installation
echo ============================================================
echo.

REM Get the directory where this script is located
set SCRIPT_DIR=%~dp0

REM Check if we're in the Installer directory
if not exist "%SCRIPT_DIR%install-service.ps1" (
    echo ERROR: install-service.ps1 not found
    echo Please run this script from the Installer directory
    pause
    exit /b 1
)

REM Look for the executable in standard build locations
set EXE_PATH=

REM First, check if user wants to specify custom path
echo.
echo Do you want to:
echo 1. Auto-locate the executable (search common build paths)
echo 2. Specify the path manually
echo.
set /p choice="Enter choice (1 or 2): "

if "%choice%"=="2" (
    echo.
    set /p EXE_PATH="Enter full path to ScreenTimeMonitor.Service.exe: "
) else (
    REM Try to find the executable in common build paths
    echo.
    echo Searching for executable...
    
    set BASE_DIR=%SCRIPT_DIR%..
    
    if exist "!BASE_DIR!\bin\Release\net8.0-windows\ScreenTimeMonitor.Service.exe" (
        set "EXE_PATH=!BASE_DIR!\bin\Release\net8.0-windows\ScreenTimeMonitor.Service.exe"
        echo Found executable: !EXE_PATH!
    ) else if exist "!BASE_DIR!\bin\Debug\net8.0-windows\ScreenTimeMonitor.Service.exe" (
        set "EXE_PATH=!BASE_DIR!\bin\Debug\net8.0-windows\ScreenTimeMonitor.Service.exe"
        echo Found executable (Debug): !EXE_PATH!
    ) else (
        echo Could not find executable automatically
        echo Please specify the path manually
        echo.
        set /p EXE_PATH="Enter full path to ScreenTimeMonitor.Service.exe: "
    )
)

REM Validate the path
if not exist "!EXE_PATH!" (
    echo.
    echo ERROR: Executable not found at: !EXE_PATH!
    pause
    exit /b 1
)

REM Check if file is actually an exe
if not "!EXE_PATH:~-4!"==".exe" (
    echo.
    echo ERROR: Specified file is not an executable (.exe)
    pause
    exit /b 1
)

REM Get full path
for %%i in ("!EXE_PATH!") do set "EXE_PATH=%%~fi"

echo.
echo ============================================================
echo Installation Configuration
echo ============================================================
echo Service Executable: !EXE_PATH!
echo.

REM Run the PowerShell installation script
echo Running PowerShell installation script...
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "!SCRIPT_DIR!install-service.ps1" -ServicePath "!EXE_PATH!"

if %errorLevel% equ 0 (
    echo.
    echo ============================================================
    echo Installation completed successfully!
    echo ============================================================
    echo.
    echo Service Status Commands:
    echo   Check status:   manage-service.ps1 -Action status
    echo   Check health:   manage-service.ps1 -Action health
    echo   View logs:      manage-service.ps1 -Action logs
    echo.
) else (
    echo.
    echo ============================================================
    echo Installation failed!
    echo ============================================================
    echo.
)

pause
