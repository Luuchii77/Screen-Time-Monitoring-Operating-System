using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ScreenTimeMonitor.Service.Database;
using ScreenTimeMonitor.Service.Models;

namespace ScreenTimeMonitor.Service.Services
{
    /// <summary>
    /// Monitors background processes (Discord, Spotify, Slack, etc.) that run without focus.
    /// Periodically scans for running processes and tracks their usage time.
    /// </summary>
    public class BackgroundProcessMonitorService : IBackgroundProcessMonitorService
    {
        private readonly ILogger<BackgroundProcessMonitorService> _logger;
        private readonly IAppUsageRepository _appUsageRepository;
        private readonly ConcurrentDictionary<int, ProcessTracker> _trackedProcesses;
        private bool _isMonitoring;
        private Timer? _scanTimer;
        private DateTime _lastScanTime;

        // Apps commonly run in background without foreground window (USER-FACING ONLY)
        private readonly string[] _backgroundApps = new[]
        {
            "Discord",
            "Spotify",
            "SpotifyLauncher",
            "Slack",
            "Teams",
            "Skype",
            "WhatsApp",
            "Telegram",
            "Signal",
            "Steam",
            "Epic",
            "BattleNet",
            "Origin",
            "Ubisoft",
            "VLC",
            "iTunes",
            "Winamp",
            "MusicBee",
            "foobar2000",
            "OBS",
            "Streamlabs",
            "Twitch",
            "Zoom",
            "Anydesk",
            "TeamViewer",
            "Chrome",
            "Firefox",
            "Edge",
            "msedge",
            "Opera",
            "Vivaldi",
            "GoogleDrive",
            "OneDrive",
            "Dropbox",
            "Thunderbird",
            "Outlook",
            "Code",
            "DevEnv",
            "Visual Studio",
            "Notepad++",
            "VSCode",
            "sublime",
            "atom",
            "IntelliJ",
            "PyCharm",
            "WebStorm",
            "Photoshop",
            "Illustrator",
            "Premiere",
            "AfterEffects",
            "GIMP",
            "Blender",
            "Unreal",
            "Unity",
            "Godot",
            "Minecraft",
            "RobloxStudio",
            "Dota2",
            "CSGO",
            "Valorant",
            "Fortnite",
            "ApexLegends",
            "OverWatch",
            "Telegram",
            "WhatsApp",
            "Slack",
            "Zoom",
            "GoogleMeet",
            "Skype",
            "Teams",
            "Discord",
            "Twitch",
            "OBS",
            "StreamLabs",
            "Audacity",
            "Handbrake",
            "FFmpeg",
            "VLC",
            "MediaPlayer",
            "WinRAR",
            "7Zip",
            "WinZip",
            "Explorer",
            "TotalCommander",
            "Everything",
            "Clipboard",
            "Notion",
            "Obsidian",
            "OneNote",
            "Evernote",
            "Trello",
            "Asana",
            "Monday",
            "Jira",
            "Confluence",
            "GitHub",
            "GitLab",
            "BitBucket",
            "SourceTree",
            "GitKraken",
            "Putty",
            "MobaXterm",
            "WinSCP",
            "FileZilla",
            "Slack",
            "Telegram",
            "Discord",
            "IRC",
            "Element",
            "Blender",
            "Cinema4D",
            "Maya",
            "3dsMax",
            "Unity",
            "Unreal",
            "Godot",
            "Game",
            "App"
        };

        // System processes and services to EXCLUDE (blacklist)
        private readonly string[] _systemProcesses = new[]
        {
            // Windows System Core
            "svchost", "conhost", "dwm", "explorer", "winlogon", "lsass", "services", "spoolsv",
            "smss", "csrss", "wininit", "system", "registry", "idle", "Memory", "Interrupt", "dpc",
            
            // Windows Search
            "SearchIndexer", "SearchApp", "SearchProtocolHost", "SearchFilterHost", "SearchUI",
            
            // Windows UI/Shell
            "taskhostw", "taskhost", "backgroundTaskHost", "RuntimeBroker", "Windows.UI",
            "ApplicationFrameHost", "ShellExperienceHost", "PickerHost", "Cortana",
            "StartMenuExperienceHost", "MoUsoCoreWorker", "tiworker", "mrt", "cleanmgr", "defrag",
            
            // VS Code & Developer Tools (NOT user apps - these are background services)
            "CodeSetup", "code-cli", "code-server", "Microsoft.VisualStudio.Code.ServiceHost",
            "Microsoft.VisualStudio.Code.Server", "ServiceHub", "VsHub", "devenv", "msbuild",
            "MSBuildAllProjects", "ngentask", "ngen",
            
            // Audio/Display Drivers
            "audiodg", "fontdrvhost", "fontCache", "Crypto", "RtkAudUService64", "RtkAudUService",
            "promecefpluginhost", "RtkUWP", "RaZip", "RaUI", "RaScal", "RaProxy",
            
            // AMD/GPU Drivers  
            "amdfendsr", "amdfenorc", "AMDRSrExt", "atiesrxx", "atieclxx", "RadeonSoftware",
            "RadeonSettings", "AMD", "igfxEM", "igfxHK",
            
            // NVIDIA Drivers
            "NvBackend", "NvCplDaemon", "NvTelemetry",
            
            // Windows Security/Admin
            "SecurityHealthSystray", "SecurityHealthService", "MsSense", "MpCmdRun", "WinDefend",
            "mpssvc", "SecurityHealthUI", "AUEPMaster",
            
            // Antivirus Services
            "avgui", "avgsvc", "avgidsagent", "BitDefender", "Norton", "McAfee",
            
            // Browser Helpers
            "iexplore", "msiexec", "wps", "wpscenter", "wpsservice",
            
            // Game Services (background)
            "RiotClientServices", "RiotClientCrashHandler", "RiotClientUxRender",
            
            // Web/Database Services
            "w3wp", "iisexpress", "IISExpress", "aspnet_wp", "aspnet_state", "sqlservr",
            "MySql", "postgres", "mongodb", "redis", "mariadb",
            
            // Shell/Command Line Tools (not apps)
            "cmd", "powershell", "pwsh", "bash", "sh", "cscript", "wscript", "tasklist", "sc.exe",
            
            // Runtime/Platform Tools
            "java", "javaw", "node", "python", "dotnet", "dotnet-host", "SgrmBroker",
            
            // Telemetry/Monitoring
            "CompatTelRunner", "diagtrack", "TelemetryIt", "SIHClient", "SysMain",
            "UpdateOrchestrator", "InstallService", "vds", "wmiprvse", "wmiapsrv", "wsmprovhost",
            "WmiPrvSE", "dllhost", "taskmgr", "procexp", "ProcessExplorer"
        };



        private class ProcessTracker
        {
            public int ProcessId { get; set; }
            public string ProcessName { get; set; } = string.Empty;
            public DateTime FirstSeen { get; set; }
            public DateTime LastSeen { get; set; }
            public long TotalDurationMs { get; set; }
            public long SessionStartDurationMs { get; set; } // Duration at session start - for session-relative tracking
            public long HistoricalTotalMs { get; set; } // Total from all previous sessions
            public DateTime SessionStartTime { get; set; } // When this session started for saving
            public bool IsBackgroundApp { get; set; }
        }

        /// <summary>
        /// Initializes a new instance of BackgroundProcessMonitorService.
        /// </summary>
        public BackgroundProcessMonitorService(ILogger<BackgroundProcessMonitorService> logger, IAppUsageRepository appUsageRepository)
        {
            _logger = logger;
            _appUsageRepository = appUsageRepository;
            _trackedProcesses = new ConcurrentDictionary<int, ProcessTracker>();
            _lastScanTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Starts the background process monitor service.
        /// </summary>
        public async Task StartMonitoringAsync()
        {
            if (_isMonitoring)
            {
                _logger.LogWarning("Background process monitoring is already running");
                return;
            }

            try
            {
                _logger.LogInformation("Starting background process monitor service...");
                _isMonitoring = true;
                _lastScanTime = DateTime.UtcNow;

                // Scan every 3 seconds
                _scanTimer = new Timer(ScanBackgroundProcesses, null, TimeSpan.Zero, TimeSpan.FromSeconds(3));

                _logger.LogInformation("Background process monitor service started");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start background process monitoring");
                throw;
            }
        }

        /// <summary>
        /// Resets session tracking for all processes (called when UI connects to start a new session).
        /// Sets SessionStartDurationMs to current TotalDurationMs so Live Activity shows session-relative time.
        /// </summary>
        public void ResetSessionTracking()
        {
            try
            {
                int resetCount = 0;
                foreach (var tracker in _trackedProcesses.Values)
                {
                    tracker.SessionStartDurationMs = tracker.TotalDurationMs;
                    resetCount++;
                }
                _logger.LogInformation($"Session tracking reset for {resetCount} processes - Live Activity will show time since session start");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting session tracking");
            }
        }

        /// <summary>
        /// Stops the background process monitor service.
        /// </summary>
        public async Task StopMonitoringAsync()
        {
            if (!_isMonitoring)
            {
                return;
            }

            try
            {
                _logger.LogInformation("Stopping background process monitor...");
                _isMonitoring = false;

                _scanTimer?.Dispose();
                _scanTimer = null;

                _logger.LogInformation("Background process monitor stopped");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping background process monitor");
                throw;
            }
        }

        /// <summary>
        /// Periodic scan callback to detect running processes.
        /// </summary>
        private void ScanBackgroundProcesses(object? state)
        {
            // Fire and forget - don't block the timer
            _ = ScanBackgroundProcessesAsync();
        }

        /// <summary>
        /// Async implementation of the background process scan.
        /// </summary>
        private async Task ScanBackgroundProcessesAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var timeSinceLastScan = (now - _lastScanTime).TotalSeconds;
                _lastScanTime = now;

                // Get all currently running processes
                var currentProcesses = Process.GetProcesses()
                    .Where(p => !string.IsNullOrEmpty(p.ProcessName))
                    .ToList();

                // Update tracked processes
                var seenPids = new HashSet<int>();
                foreach (var process in currentProcesses)
                {
                    try
                    {
                        int pid = process.Id;
                        
                        string processName = process.ProcessName;
                        
                        // Check if this is a system process (skip if it is)
                        if (_systemProcesses.Any(sys => processName.Equals(sys, StringComparison.OrdinalIgnoreCase) ||
                                                       processName.StartsWith(sys, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue; // Skip system processes entirely
                        }
                        
                        // Only track processes that have a window (taskbar apps)
                        if (!HasVisibleWindow(process))
                        {
                            continue; // Skip processes without windows
                        }
                        
                        seenPids.Add(pid);
                        
                        bool isBackgroundApp = IsBackgroundApp(processName);

                        if (_trackedProcesses.TryGetValue(pid, out var tracker))
                        {
                            // Update existing tracker
                            tracker.LastSeen = now;
                            tracker.TotalDurationMs += (long)(timeSinceLastScan * 1000);

                        }
                        else
                        {
                            // Add new tracked process - load historical data
                            var historicalTotal = await _appUsageRepository.GetAppHistoricalTotalAsync(processName);
                            
                            _trackedProcesses.TryAdd(pid, new ProcessTracker
                            {
                                ProcessId = pid,
                                ProcessName = processName,
                                FirstSeen = now,
                                LastSeen = now,
                                TotalDurationMs = 0,
                                SessionStartDurationMs = 0,
                                HistoricalTotalMs = historicalTotal,
                                SessionStartTime = now,
                                IsBackgroundApp = isBackgroundApp
                            });

                            if (isBackgroundApp || historicalTotal > 0)
                            {
                                _logger.LogInformation($"Started tracking {processName} (PID: {pid}) - Historical total: {historicalTotal}ms");
                            }
                        }

                        process.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug($"Error processing PID: {ex.Message}");
                    }
                }

                // Remove processes that are no longer running
                var deadProcesses = _trackedProcesses.Keys
                    .Where(pid => !seenPids.Contains(pid))
                    .ToList();

                foreach (var deadPid in deadProcesses)
                {
                    if (_trackedProcesses.TryRemove(deadPid, out var tracker))
                    {
                        _logger.LogDebug($"Process ended: {tracker.ProcessName} (PID: {deadPid}) - Total time: {tracker.TotalDurationMs}ms");
                        
                        // Save the session to database for historical tracking
                        try
                        {
                            var sessionDuration = tracker.TotalDurationMs - tracker.SessionStartDurationMs;
                            if (sessionDuration > 0)
                            {
                                var session = new AppUsageSession
                                {
                                    AppName = tracker.ProcessName,
                                    ProcessId = tracker.ProcessId,
                                    SessionStart = tracker.SessionStartTime,
                                    SessionEnd = now,
                                    DurationMs = sessionDuration,
                                    CreatedAt = now
                                };
                                
                                await _appUsageRepository.CreateSessionAsync(session);
                                _logger.LogInformation($"Saved session for {tracker.ProcessName}: {sessionDuration}ms");
                            }
                        }
                        catch (Exception saveEx)
                        {
                            _logger.LogError(saveEx, $"Failed to save session for {tracker.ProcessName}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background process scan");
            }
        }

        /// <summary>
        /// Gets all currently tracked background applications.
        /// </summary>
        public List<(string AppName, long DurationMs, bool IsRunning)> GetBackgroundApps()
        {
            return _trackedProcesses
                .Values
                .Where(t => t.IsBackgroundApp)
                .Select(t => (t.ProcessName, t.TotalDurationMs, true))
                .ToList();
        }

        /// <summary>
        /// Gets all running processes with cumulative durations (session-relative + historical total).
        /// Duration = (TotalDurationMs - SessionStartDurationMs) + HistoricalTotalMs
        /// </summary>
        public List<(string AppName, long DurationMs)> GetAllRunningApps()
        {
            return _trackedProcesses
                .Values
                .Where(t => !_systemProcesses.Any(sys => t.ProcessName.Equals(sys, StringComparison.OrdinalIgnoreCase) ||
                                                         t.ProcessName.StartsWith(sys, StringComparison.OrdinalIgnoreCase)))
                .GroupBy(t => t.ProcessName, StringComparer.OrdinalIgnoreCase)
                .Select(g => (g.Key, Math.Max(0, g.Sum(t => (t.TotalDurationMs - t.SessionStartDurationMs) + t.HistoricalTotalMs))))
                .ToList();
        }

        /// <summary>
        /// Gets usage statistics for a specific app.
        /// </summary>
        public (long TotalMs, DateTime FirstSeen, DateTime LastSeen)? GetAppStats(string appName)
        {
            var trackers = _trackedProcesses
                .Values
                .Where(t => t.ProcessName.Equals(appName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!trackers.Any())
                return null;

            return (
                trackers.Sum(t => t.TotalDurationMs),
                trackers.Min(t => t.FirstSeen),
                trackers.Max(t => t.LastSeen)
            );
        }

        /// <summary>
        /// Determines if an app is commonly run in the background.
        /// </summary>
        private bool IsBackgroundApp(string processName)
        {
            // First check if it's a system process (blacklist - exclude these)
            if (_systemProcesses.Any(sys => processName.Equals(sys, StringComparison.OrdinalIgnoreCase) || 
                                            processName.StartsWith(sys, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            // Check if it's in the user-facing apps whitelist
            return _backgroundApps.Any(bg => processName.Contains(bg, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks if a process has a visible window (appears in taskbar).
        /// </summary>
        private bool HasVisibleWindow(Process process)
        {
            try
            {
                // A process has a window if it has a main window handle
                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    return true;
                }

                // For processes without a main window, check if they have ANY window owned by them
                // using Windows API EnumWindows
                var hasWindow = false;
                EnumWindows((hWnd, param) =>
                {
                    try
                    {
                        if (GetWindowThreadProcessId(hWnd, out int processId) && processId == process.Id)
                        {
                            // Found a window for this process
                            // Check if it's visible and not owned by another window
                            if (IsWindowVisible(hWnd) && GetWindow(hWnd, GetWindowCmd.GW_OWNER) == IntPtr.Zero)
                            {
                                hasWindow = true;
                                return false; // Stop enumeration
                            }
                        }
                    }
                    catch { }
                    return true; // Continue enumeration
                }, IntPtr.Zero);

                return hasWindow;
            }
            catch
            {
                return false;
            }
        }

        // Windows API declarations for proper window detection
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, GetWindowCmd uCmd);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private enum GetWindowCmd : uint
        {
            GW_OWNER = 4
        }

        /// <summary>
        /// Clears all tracking data.
        /// </summary>
        public void ClearTracking()
        {
            _trackedProcesses.Clear();
            _logger.LogInformation("Cleared process tracking data");
        }
    }

    /// <summary>
    /// Interface for background process monitoring.
    /// </summary>
    public interface IBackgroundProcessMonitorService
    {
        /// <summary>
        /// Starts the background process monitor service.
        /// </summary>
        Task StartMonitoringAsync();

        /// <summary>
        /// Stops the background process monitor service.
        /// </summary>
        Task StopMonitoringAsync();

        /// <summary>
        /// Resets session tracking for all processes (called when UI connects).
        /// </summary>
        void ResetSessionTracking();

        /// <summary>
        /// Gets all currently tracked background applications.
        /// </summary>
        List<(string AppName, long DurationMs, bool IsRunning)> GetBackgroundApps();

        /// <summary>
        /// Gets all running processes (foreground and background).
        /// </summary>
        List<(string AppName, long DurationMs)> GetAllRunningApps();

        /// <summary>
        /// Gets usage statistics for a specific app.
        /// </summary>
        (long TotalMs, DateTime FirstSeen, DateTime LastSeen)? GetAppStats(string appName);

        /// <summary>
        /// Clears all tracking data.
        /// </summary>
        void ClearTracking();
    }
}
