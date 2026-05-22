# ============================================================================
# ScreenTimeMonitor - VS Code Extensions Installer (PowerShell)
# ============================================================================
# This script automatically installs all required VS Code extensions
# for the ScreenTimeMonitor project development
# ============================================================================

param(
    [switch]$IncludeOptional = $false,
    [switch]$Verbose = $false
)

# Colors for output
$colors = @{
    Success = "Green"
    Warning = "Yellow"
    Error   = "Red"
    Info    = "Cyan"
}

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Write-Header {
    Write-Host ""
    Write-Host "============================================================================" -ForegroundColor Cyan
    Write-Host "  ScreenTimeMonitor - VS Code Extensions Installer (PowerShell)" -ForegroundColor Cyan
    Write-Host "============================================================================" -ForegroundColor Cyan
    Write-Host ""
}

function Test-VSCode {
    Write-ColorOutput "[CHECK] Verifying VS Code installation..." $colors.Info
    
    $vscodeInstalled = $null
    try {
        $vscodeInstalled = Get-Command code -ErrorAction Stop
    }
    catch {
        Write-ColorOutput "[ERROR] VS Code is not installed or not in PATH" $colors.Error
        Write-ColorOutput "Please install VS Code from: https://code.visualstudio.com/" $colors.Error
        Write-ColorOutput "Or add VS Code to your PATH environment variable" $colors.Error
        Write-Host ""
        return $false
    }
    
    Write-ColorOutput "[SUCCESS] VS Code found at: $($vscodeInstalled.Source)" $colors.Success
    Write-Host ""
    return $true
}

function Install-Extensions {
    param(
        [array]$Extensions,
        [string]$Category
    )
    
    Write-ColorOutput "Installing $Category extensions..." $colors.Info
    Write-Host ""
    
    $successCount = 0
    $failureCount = 0
    
    foreach ($extension in $Extensions) {
        $publisherId = $extension.id
        $name = $extension.name
        
        Write-ColorOutput "  Installing: $name ($publisherId)" $colors.Info
        
        try {
            & code --install-extension $publisherId --force 2>&1 | Out-Null
            Write-ColorOutput "    ✓ Success" $colors.Success
            $successCount++
        }
        catch {
            Write-ColorOutput "    ✗ Failed to install $publisherId" $colors.Warning
            $failureCount++
        }
    }
    
    Write-Host ""
    Write-ColorOutput "Results: $successCount installed, $failureCount failed" $colors.Info
    Write-Host ""
    
    return @{ Success = $successCount; Failure = $failureCount }
}

# Main script
Clear-Host
Write-Header

# Required extensions
$requiredExtensions = @(
    @{ id = "ms-dotnettools.csharp"; name = "C# Dev Kit (Official)" },
    @{ id = "kreativ-software.csharp-extensions"; name = "C# Extensions" },
    @{ id = "jmrog.vscode-nuget-package-manager"; name = "NuGet Package Manager" },
    @{ id = "mtxr.sqltools"; name = "SQLTools - Database UI" },
    @{ id = "mtxr.sqltools-driver-sqlite"; name = "SQLTools SQLite Driver" },
    @{ id = "humao.rest-client"; name = "REST Client" },
    @{ id = "rangav.vscode-thunder-client"; name = "Thunder Client (API Testing)" },
    @{ id = "ms-vscode.powershell"; name = "PowerShell Support" }
)

# Optional extensions
$optionalExtensions = @(
    @{ id = "eamodio.gitlens"; name = "GitLens - Git Integration" },
    @{ id = "hediet.vscode-drawio"; name = "Draw.io Integration" },
    @{ id = "aaron-bond.better-comments"; name = "Better Comments" },
    @{ id = "streetsidesoftware.code-spell-checker"; name = "Code Spell Checker" },
    @{ id = "ms-python.vscode-pylance"; name = "Pylance (Python)" }
)

# Check VS Code installation
if (-not (Test-VSCode)) {
    exit 1
}

# Install required extensions
$requiredResults = Install-Extensions -Extensions $requiredExtensions -Category "Required"

# Install optional extensions if requested
if ($IncludeOptional) {
    Write-Host ""
    Write-ColorOutput "Installing optional extensions..." $colors.Info
    Write-Host ""
    $optionalResults = Install-Extensions -Extensions $optionalExtensions -Category "Optional"
} else {
    Write-ColorOutput "Skipping optional extensions (use -IncludeOptional flag to install)" $colors.Info
    Write-Host ""
}

# Summary
Write-Host "============================================================================" -ForegroundColor Cyan
Write-ColorOutput "Installation Summary" $colors.Info
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host ""

if ($requiredResults.Success -eq @($requiredExtensions).Count) {
    Write-ColorOutput "[✓] All required extensions installed successfully!" $colors.Success
} else {
    Write-ColorOutput "[!] Some extensions may not have installed (see above for details)" $colors.Warning
}

Write-Host ""
Write-ColorOutput "Next Steps:" $colors.Info
Write-ColorOutput "  1. Close and restart VS Code completely" $colors.Info
Write-ColorOutput "  2. Open your ScreenTimeMonitor project folder" $colors.Info
Write-ColorOutput "  3. VS Code will finish setting up C# IntelliSense and debugging" $colors.Info
Write-Host ""

Write-ColorOutput "To view installed extensions:" $colors.Info
Write-ColorOutput "  - Press Ctrl+Shift+X in VS Code to open Extensions panel" $colors.Info
Write-ColorOutput "  - Check the 'Installed' section to see all active extensions" $colors.Info
Write-Host ""

Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host ""

Write-ColorOutput "Optional Extensions (Manual Installation):" $colors.Warning
Write-Host ""
Write-ColorOutput "  eamodio.gitlens" -ForegroundColor Gray
Write-Host "    GitLens - Better Git integration and blame annotations" -ForegroundColor Gray
Write-Host ""
Write-ColorOutput "  hediet.vscode-drawio" -ForegroundColor Gray
Write-Host "    Draw.io Integration - Diagrams and flowcharts" -ForegroundColor Gray
Write-Host ""
Write-ColorOutput "  aaron-bond.better-comments" -ForegroundColor Gray
Write-Host "    Better Comments - Improved comment highlighting" -ForegroundColor Gray
Write-Host ""

Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host ""
Write-ColorOutput "Installation Complete!" $colors.Success
Write-Host ""

# Offer to install optional extensions
if (-not $IncludeOptional) {
    $response = Read-Host "Would you like to install optional extensions now? (Y/N)"
    if ($response -eq 'Y' -or $response -eq 'y') {
        Write-Host ""
        $optionalResults = Install-Extensions -Extensions $optionalExtensions -Category "Optional"
    }
}

pause
