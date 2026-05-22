@echo off
REM ============================================
REM ScreenTimeMonitor - Multi-Device Setup
REM ============================================
REM This script automates the setup on any device
REM Run this on the new machine in the project root

echo.
echo ======================================
echo Screen Time Monitor - Setup Script
echo ======================================
echo.

REM Check if .NET is installed
echo [*] Checking .NET SDK installation...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] .NET SDK not found!
    echo Please install .NET 8.0 or later from: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)
echo [OK] .NET SDK found
echo.

REM Restore NuGet packages
echo [*] Restoring NuGet packages...
dotnet restore
if errorlevel 1 (
    echo [ERROR] Package restoration failed!
    pause
    exit /b 1
)
echo [OK] Packages restored
echo.

REM Build the solution
echo [*] Building solution...
dotnet build -c Debug
if errorlevel 1 (
    echo [ERROR] Build failed!
    pause
    exit /b 1
)
echo [OK] Build successful
echo.

REM Create directories
echo [*] Creating data and logs directories...
if not exist "data" mkdir data
if not exist "logs" mkdir logs
echo [OK] Directories ready
echo.

echo ======================================
echo Setup Complete!
echo ======================================
echo.
echo Next steps:
echo   1. Open two PowerShell windows
echo   2. In the first window, navigate to ScreenTimeMonitor.Service:
echo      cd ScreenTimeMonitor.Service
echo      dotnet run -c Debug
echo   3. Wait 3-5 seconds for service to start
echo   4. In the second window, navigate to ScreenTimeMonitor.UI.WPF:
echo      cd ScreenTimeMonitor.UI.WPF
echo      dotnet run -c Debug
echo.
echo The application will start tracking when you connect to the service!
echo.
pause
