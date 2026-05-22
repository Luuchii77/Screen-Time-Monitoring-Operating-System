# ============================================================================
# Screen Time Monitor Service - Management Script
# ============================================================================
# This script provides management commands for the Screen Time Monitor service
# 
# Requirements:
# - Administrator privileges (for start/stop/restart)
# - PowerShell 5.0 or later
#
# Usage: 
#   powershell -ExecutionPolicy Bypass -File manage-service.ps1 -Action "start|stop|restart|status"
# ============================================================================

param(
    [Parameter(Mandatory=$true, HelpMessage="Action to perform: start, stop, restart, status, enable, disable")]
    [ValidateSet("start", "stop", "restart", "status", "enable", "disable", "logs", "health")]
    [string]$Action,
    
    [Parameter(Mandatory=$false, HelpMessage="Service name (default: ScreenTimeMonitor)")]
    [string]$ServiceName = "ScreenTimeMonitor"
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

function Write-ServiceStatus {
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    
    if ($null -eq $service) {
        Write-StatusMessage "Service '$ServiceName' not found" "Error"
        return
    }
    
    Write-StatusMessage "Service Details:" "Info"
    Write-Host "  Name: $($service.Name)"
    Write-Host "  Display Name: $($service.DisplayName)"
    Write-Host "  Status: $($service.Status)"
    Write-Host "  Start Type: $(if($service.StartType) { $service.StartType } else { 'Not determined' })"
    Write-Host "  Can Stop: $($service.CanStop)"
    Write-Host "  Can Pause Resume: $($service.CanPauseAndContinue)"
}

function Show-RecentLogs {
    Write-StatusMessage "Recent service logs from Event Log:" "Info"
    Write-StatusMessage "============================================================" "Info"
    
    try {
        $events = Get-EventLog -LogName Application -Source "ScreenTimeMonitor" -Newest 20 -ErrorAction Stop
        
        if ($events.Count -eq 0) {
            Write-StatusMessage "No log entries found" "Warning"
        } else {
            $events | Format-Table -AutoSize -Property TimeGenerated, EntryType, Message
        }
    }
    catch {
        Write-StatusMessage "Could not retrieve logs: $_" "Warning"
    }
}

function Check-ServiceHealth {
    Write-StatusMessage "Service Health Check:" "Info"
    Write-StatusMessage "============================================================" "Info"
    
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    
    if ($null -eq $service) {
        Write-StatusMessage "Service '$ServiceName' not found - CRITICAL" "Error"
        return
    }
    
    # Check service status
    if ($service.Status -eq "Running") {
        Write-StatusMessage "Service Status: Running - OK" "Success"
    } else {
        Write-StatusMessage "Service Status: $($service.Status) - WARNING" "Warning"
    }
    
    # Check startup type
    $startType = if($service.StartType) { $service.StartType } else { "Unknown" }
    if ($startType -eq "Automatic") {
        Write-StatusMessage "Startup Type: Automatic - OK" "Success"
    } else {
        Write-StatusMessage "Startup Type: $startType - INFO" "Info"
    }
    
    # Check data directory
    $dataDir = "C:\ProgramData\ScreenTimeMonitor"
    if (Test-Path $dataDir) {
        Write-StatusMessage "Data Directory: Found - OK" "Success"
        $dbFiles = @(Get-ChildItem $dataDir -Filter "*.db" -ErrorAction SilentlyContinue)
        Write-StatusMessage "Database Files: $($dbFiles.Count) found" "Info"
    } else {
        Write-StatusMessage "Data Directory: Not found - WARNING" "Warning"
    }
    
    # Check log directory
    $logDir = "C:\ProgramData\ScreenTimeMonitor\Logs"
    if (Test-Path $logDir) {
        $logFiles = @(Get-ChildItem $logDir -Filter "*.log" -ErrorAction SilentlyContinue)
        Write-StatusMessage "Log Directory: Found ($($logFiles.Count) files) - OK" "Success"
    } else {
        Write-StatusMessage "Log Directory: Not found - WARNING" "Warning"
    }
}

# ============================================================================
# Main Execution
# ============================================================================

Write-StatusMessage "Screen Time Monitor Service Management Tool" "Info"
Write-StatusMessage "============================================================" "Info"

# Handle status action (doesn't require admin)
if ($Action -eq "status") {
    Write-StatusMessage "" "Info"
    Write-ServiceStatus
    exit 0
}

if ($Action -eq "logs") {
    Write-StatusMessage "" "Info"
    Show-RecentLogs
    exit 0
}

if ($Action -eq "health") {
    Write-StatusMessage "" "Info"
    Check-ServiceHealth
    exit 0
}

# All other actions require administrator privileges
if (-not (Test-Administrator)) {
    Write-StatusMessage "Administrator privileges required for this action" "Error"
    exit 1
}

# ============================================================================
# Service Management Actions
# ============================================================================

Write-StatusMessage "" "Info"

switch ($Action) {
    "start" {
        Write-StatusMessage "Starting service..." "Info"
        try {
            Start-Service -Name $ServiceName -ErrorAction Stop
            Start-Sleep -Seconds 2
            Write-ServiceStatus
            Write-StatusMessage "Service started successfully" "Success"
        }
        catch {
            Write-StatusMessage "Failed to start service: $_" "Error"
            exit 1
        }
    }
    
    "stop" {
        Write-StatusMessage "Stopping service..." "Info"
        try {
            Stop-Service -Name $ServiceName -Force -ErrorAction Stop
            Start-Sleep -Seconds 2
            Write-ServiceStatus
            Write-StatusMessage "Service stopped successfully" "Success"
        }
        catch {
            Write-StatusMessage "Failed to stop service: $_" "Error"
            exit 1
        }
    }
    
    "restart" {
        Write-StatusMessage "Restarting service..." "Info"
        try {
            Restart-Service -Name $ServiceName -Force -ErrorAction Stop
            Start-Sleep -Seconds 3
            Write-ServiceStatus
            Write-StatusMessage "Service restarted successfully" "Success"
        }
        catch {
            Write-StatusMessage "Failed to restart service: $_" "Error"
            exit 1
        }
    }
    
    "enable" {
        Write-StatusMessage "Setting startup type to Automatic..." "Info"
        try {
            Set-Service -Name $ServiceName -StartupType Automatic -ErrorAction Stop
            Write-ServiceStatus
            Write-StatusMessage "Service set to automatic startup" "Success"
        }
        catch {
            Write-StatusMessage "Failed to enable auto-startup: $_" "Error"
            exit 1
        }
    }
    
    "disable" {
        Write-StatusMessage "Setting startup type to Manual..." "Info"
        try {
            Set-Service -Name $ServiceName -StartupType Manual -ErrorAction Stop
            Write-ServiceStatus
            Write-StatusMessage "Service set to manual startup" "Success"
        }
        catch {
            Write-StatusMessage "Failed to disable auto-startup: $_" "Error"
            exit 1
        }
    }
}

Write-StatusMessage "" "Info"
exit 0
