<#
.SYNOPSIS
    Launches the ScreenTimeMonitor Service and WPF UI in separate PowerShell windows for local development/testing.

.USAGE
    .\run-dev.ps1
    .\run-dev.ps1 -Configuration Release

.NOTES
    - Run from the repository root or call the script directly from anywhere; it will resolve paths relative to the script.
    - Requires PowerShell and the .NET SDK installed.
#>

Param(
    [string]$Configuration = 'Debug'
)

# Resolve repository root (one level up from scripts folder)
$root = Resolve-Path "$PSScriptRoot\.."

$serviceProj = Join-Path $root "ScreenTimeMonitor.Service\ScreenTimeMonitor.Service.csproj"
$wpfProj = Join-Path $root "ScreenTimeMonitor.UI.WPF\ScreenTimeMonitor.UI.WPF.csproj"
$consoleProj = Join-Path $root "ScreenTimeMonitor.UI\ScreenTimeMonitor.UI.csproj"

Write-Host "Repository root: $root"
Write-Host "Launching projects with configuration: $Configuration"

function Launch-InNewWindow([string]$projectPath, [string]$title) {
    $projFull = (Resolve-Path $projectPath).Path
    $cmd = "dotnet run --project '$projFull' -c $Configuration"
    Start-Process -FilePath powershell -ArgumentList "-NoExit","-Command","Write-Host 'Starting $title'; $cmd" -WindowStyle Normal
}

# Launch the service host
Launch-InNewWindow $serviceProj "ScreenTimeMonitor.Service (host)"

# Small delay so service can start before UI connects
Start-Sleep -Seconds 1

# Launch the WPF UI
Launch-InNewWindow $wpfProj "ScreenTimeMonitor.UI.WPF"

# (optional) Launch the console UI as an alternative client
# Launch-InNewWindow $consoleProj "ScreenTimeMonitor.UI (console)"

Write-Host "Launched service and UI windows. Watch the service window for server logs and the UI window to connect."
