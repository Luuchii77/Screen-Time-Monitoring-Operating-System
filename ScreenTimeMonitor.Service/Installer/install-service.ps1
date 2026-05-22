# ============================================================================
# Screen Time Monitor Service - Installation Script
# ============================================================================
# This script installs the Screen Time Monitor as a Windows Service
# 
# Requirements:
# - Administrator privileges
# - .NET 8.0 runtime installed
# - PowerShell 5.0 or later
#
# Usage: 
#   powershell -ExecutionPolicy Bypass -File install-service.ps1 -ServicePath "C:\path\to\ScreenTimeMonitor.Service.exe"
# ============================================================================

param(
    [Parameter(Mandatory=$true, HelpMessage="Path to ScreenTimeMonitor.Service.exe")]
    [string]$ServicePath,
    
    [Parameter(Mandatory=$false, HelpMessage="Display name for the service (default: Screen Time Monitor)")]
    [string]$DisplayName = "Screen Time Monitor",
    
    [Parameter(Mandatory=$false, HelpMessage="Service name (default: ScreenTimeMonitor)")]
    [string]$ServiceName = "ScreenTimeMonitor",
    
    [Parameter(Mandatory=$false, HelpMessage="Startup type (default: Automatic)")]
    [ValidateSet("Automatic", "Manual", "Disabled")]
    [string]$StartupType = "Automatic",
    
    [Parameter(Mandatory=$false, HelpMessage="Run as account (default: SYSTEM)")]
    [string]$RunAsAccount = "SYSTEM"
)

# ============================================================================
# Helper Functions
# ============================================================================

function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Write-StatusMessage {
    param([string]$Message, [ValidateSet("Success", "Info", "Warning", "Error")]$Status = "Info")
    
    $colors = @{
        Success = "Green"
        Info = "Cyan"
        Warning = "Yellow"
        Error = "Red"
    }
    
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] " -NoNewline
    Write-Host "[$Status] " -ForegroundColor $colors[$Status] -NoNewline
    Write-Host $Message
}

function Test-ServicePath {
    param([string]$Path)
    
    if (-not (Test-Path $Path)) {
        Write-StatusMessage "Service executable not found at: $Path" "Error"
        return $false
    }
    
    if (-not ($Path -like "*.exe")) {
        Write-StatusMessage "Specified path is not an executable" "Error"
        return $false
    }
    
    return $true
}

# ============================================================================
# Validation
# ============================================================================

Write-StatusMessage "Screen Time Monitor Service Installation Script" "Info"
Write-StatusMessage "============================================================" "Info"

# Check for administrator privileges
if (-not (Test-Administrator)) {
    Write-StatusMessage "ERROR: Administrator privileges required to install Windows Service" "Error"
    Write-StatusMessage "Please run PowerShell as Administrator" "Error"
    exit 1
}

# Validate service path
if (-not (Test-ServicePath $ServicePath)) {
    exit 1
}

# Get absolute path
$ServicePath = (Get-Item $ServicePath).FullName
Write-StatusMessage "Service executable: $ServicePath" "Info"

# ============================================================================
# Pre-Installation Checks
# ============================================================================

Write-StatusMessage "" "Info"
Write-StatusMessage "Pre-Installation Checks" "Info"
Write-StatusMessage "============================================================" "Info"

# Check if service already exists
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($existingService) {
    Write-StatusMessage "Service '$ServiceName' already exists" "Warning"
    Write-StatusMessage "Previous state: $($existingService.Status)" "Info"
    
    if ($existingService.Status -eq "Running") {
        Write-StatusMessage "Stopping service..." "Info"
        Stop-Service -Name $ServiceName -Force -ErrorAction Stop
        Write-StatusMessage "Service stopped" "Success"
    }
    
    Write-StatusMessage "Removing existing service..." "Info"
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 1
    Write-StatusMessage "Existing service removed" "Success"
}

# ============================================================================
# Installation
# ============================================================================

Write-StatusMessage "" "Info"
Write-StatusMessage "Installing Service" "Info"
Write-StatusMessage "============================================================" "Info"

try {
    # Create Windows Service using sc.exe
    Write-StatusMessage "Creating service..." "Info"
    $scResult = sc.exe create $ServiceName `
        binPath= $ServicePath `
        DisplayName= $DisplayName `
        start= $StartupType.ToLower()
    
    if ($LASTEXITCODE -eq 0) {
        Write-StatusMessage "Service created successfully" "Success"
    } else {
        Write-StatusMessage "Failed to create service. Exit code: $LASTEXITCODE" "Error"
        Write-StatusMessage "Output: $scResult" "Error"
        exit 1
    }
    
    # Set error recovery actions
    Write-StatusMessage "Configuring error recovery..." "Info"
    sc.exe failure $ServiceName reset= 30 actions= restart/5000/restart/5000/none/30000 | Out-Null
    Write-StatusMessage "Error recovery configured" "Success"
    
    # Set service description
    Write-StatusMessage "Setting service description..." "Info"
    sc.exe description $ServiceName "Monitors screen time and system metrics. Collects application usage and performance data for analysis." | Out-Null
    Write-StatusMessage "Service description set" "Success"
    
    # Add Event Log Source (for Windows Event Log integration)
    Write-StatusMessage "Registering Event Log source..." "Info"
    New-EventLog -LogName Application -Source "ScreenTimeMonitor" -ErrorAction SilentlyContinue | Out-Null
    Write-StatusMessage "Event Log source registered" "Success"
    
    # Grant necessary permissions to data directory
    $dataDir = "C:\ProgramData\ScreenTimeMonitor"
    if (Test-Path $dataDir) {
        Write-StatusMessage "Configuring data directory permissions..." "Info"
        $acl = Get-Acl $dataDir
        $serviceAccount = New-Object System.Security.Principal.NTAccount("NT AUTHORITY\SYSTEM")
        $ace = New-Object System.Security.AccessControl.FileSystemAccessRule(
            $serviceAccount,
            "FullControl",
            "ContainerInherit,ObjectInherit",
            "None",
            "Allow"
        )
        $acl.SetAccessRule($ace)
        Set-Acl -Path $dataDir -AclObject $acl
        Write-StatusMessage "Data directory permissions configured" "Success"
    }
    
    # Start the service
    Write-StatusMessage "Starting service..." "Info"
    Start-Service -Name $ServiceName -ErrorAction Stop
    Start-Sleep -Seconds 2
    
    $service = Get-Service -Name $ServiceName
    if ($service.Status -eq "Running") {
        Write-StatusMessage "Service started successfully" "Success"
    } else {
        Write-StatusMessage "Service failed to start" "Warning"
        Write-StatusMessage "Status: $($service.Status)" "Warning"
    }
}
catch {
    Write-StatusMessage "Installation failed: $_" "Error"
    exit 1
}

# ============================================================================
# Verification
# ============================================================================

Write-StatusMessage "" "Info"
Write-StatusMessage "Installation Summary" "Info"
Write-StatusMessage "============================================================" "Info"

$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($service) {
    Write-StatusMessage "Service Name: $($service.Name)" "Success"
    Write-StatusMessage "Display Name: $($service.DisplayName)" "Success"
    Write-StatusMessage "Status: $($service.Status)" "Success"
    Write-StatusMessage "Startup Type: $(if($service.StartType) { $service.StartType } else { 'Automatic' })" "Success"
    Write-StatusMessage "" "Info"
    Write-StatusMessage "Installation completed successfully!" "Success"
    exit 0
} else {
    Write-StatusMessage "Service installation verification failed" "Error"
    exit 1
}
