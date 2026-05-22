# ============================================================================
# Screen Time Monitor Service - Uninstallation Script
# ============================================================================
# This script uninstalls the Screen Time Monitor Windows Service
# 
# Requirements:
# - Administrator privileges
# - PowerShell 5.0 or later
#
# Usage: 
#   powershell -ExecutionPolicy Bypass -File uninstall-service.ps1 [-ServiceName "ScreenTimeMonitor"]
# ============================================================================

param(
    [Parameter(Mandatory=$false, HelpMessage="Service name (default: ScreenTimeMonitor)")]
    [string]$ServiceName = "ScreenTimeMonitor",
    
    [Parameter(Mandatory=$false, HelpMessage="Remove data directory during uninstall")]
    [switch]$RemoveData = $false
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

# ============================================================================
# Validation
# ============================================================================

Write-StatusMessage "Screen Time Monitor Service Uninstallation Script" "Info"
Write-StatusMessage "============================================================" "Info"

# Check for administrator privileges
if (-not (Test-Administrator)) {
    Write-StatusMessage "ERROR: Administrator privileges required to uninstall Windows Service" "Error"
    Write-StatusMessage "Please run PowerShell as Administrator" "Error"
    exit 1
}

# ============================================================================
# Service Uninstallation
# ============================================================================

Write-StatusMessage "" "Info"
Write-StatusMessage "Uninstalling Service" "Info"
Write-StatusMessage "============================================================" "Info"

$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($null -eq $service) {
    Write-StatusMessage "Service '$ServiceName' not found" "Warning"
} else {
    try {
        # Stop the service if running
        if ($service.Status -eq "Running") {
            Write-StatusMessage "Stopping service..." "Info"
            Stop-Service -Name $ServiceName -Force -ErrorAction Stop
            Start-Sleep -Seconds 2
            Write-StatusMessage "Service stopped" "Success"
        }
        
        # Delete the service
        Write-StatusMessage "Deleting service..." "Info"
        sc.exe delete $ServiceName | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            Write-StatusMessage "Service deleted successfully" "Success"
        } else {
            Write-StatusMessage "Failed to delete service. Exit code: $LASTEXITCODE" "Error"
            exit 1
        }
        
        Start-Sleep -Seconds 1
    }
    catch {
        Write-StatusMessage "Uninstallation failed: $_" "Error"
        exit 1
    }
}

# ============================================================================
# Event Log Source Removal
# ============================================================================

Write-StatusMessage "" "Info"
Write-StatusMessage "Cleaning Up Event Log Source" "Info"
Write-StatusMessage "============================================================" "Info"

try {
    $eventLog = Get-EventLog -LogName Application -Source "ScreenTimeMonitor" -ErrorAction SilentlyContinue
    if ($eventLog) {
        Write-StatusMessage "Removing Event Log source..." "Info"
        Remove-EventLog -Source "ScreenTimeMonitor" -ErrorAction Stop
        Write-StatusMessage "Event Log source removed" "Success"
    }
}
catch {
    Write-StatusMessage "Warning: Could not remove Event Log source: $_" "Warning"
}

# ============================================================================
# Data Directory Removal (Optional)
# ============================================================================

if ($RemoveData) {
    Write-StatusMessage "" "Info"
    Write-StatusMessage "Removing Data" "Info"
    Write-StatusMessage "============================================================" "Info"
    
    $dataDir = "C:\ProgramData\ScreenTimeMonitor"
    
    if (Test-Path $dataDir) {
        try {
            Write-StatusMessage "Removing data directory: $dataDir" "Info"
            Remove-Item -Path $dataDir -Recurse -Force -ErrorAction Stop
            Write-StatusMessage "Data directory removed" "Success"
        }
        catch {
            Write-StatusMessage "Warning: Could not remove data directory: $_" "Warning"
        }
    }
}

# ============================================================================
# Verification
# ============================================================================

Write-StatusMessage "" "Info"
Write-StatusMessage "Uninstallation Summary" "Info"
Write-StatusMessage "============================================================" "Info"

$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($null -eq $service) {
    Write-StatusMessage "Service has been uninstalled" "Success"
    Write-StatusMessage "Uninstallation completed successfully!" "Success"
    exit 0
} else {
    Write-StatusMessage "Service still exists (may require reboot)" "Warning"
    Write-StatusMessage "Uninstallation completed with warnings" "Warning"
    exit 0
}
