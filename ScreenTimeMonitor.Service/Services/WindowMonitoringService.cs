using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using ScreenTimeMonitor.Service.Models;
using ScreenTimeMonitor.Service.Utilities;

namespace ScreenTimeMonitor.Service.Services
{
    /// <summary>
    /// Monitors active windows and captures application usage sessions.
    /// Uses Windows API hooks to detect window focus changes.
    /// </summary>
    public class WindowMonitoringService : IWindowMonitoringService
    {
        private readonly ILogger<WindowMonitoringService> _logger;
        private readonly ConcurrentQueue<AppUsageSession> _capturedSessions;
        private IntPtr _hookHandle = IntPtr.Zero;
        private PInvokeDeclarations.WinEventDelegate? _delegate;
        private AppUsageSession? _currentSession;
        private bool _isMonitoring;
        private DateTime _monitoringStartTime;
        private readonly object _currentSessionLock = new object();
        
        // Debouncing for brief focus changes (e.g., notifications, tooltips)
        private AppUsageSession? _pendingWindowChange;
        private DateTime _lastWindowChangeTime = DateTime.MinValue;
        private const int DEBOUNCE_MILLISECONDS = 500; // 500ms threshold for brief changes

        public WindowMonitoringService(ILogger<WindowMonitoringService> logger)
        {
            _logger = logger;
            _capturedSessions = new ConcurrentQueue<AppUsageSession>();
            _monitoringStartTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Starts the window monitoring service by hooking into Windows foreground window events.
        /// </summary>
        public async Task StartMonitoringAsync()
        {
            if (_isMonitoring)
            {
                _logger.LogWarning("Window monitoring is already running");
                return;
            }

            try
            {
                _logger.LogInformation("Starting window monitoring service...");

                // Create the event hook delegate
                _delegate = new PInvokeDeclarations.WinEventDelegate(WinEventHook);

                // Set up the hook for foreground window changes
                _hookHandle = PInvokeDeclarations.SetWinEventHook(
                    PInvokeDeclarations.EVENT_SYSTEM_FOREGROUND,
                    PInvokeDeclarations.EVENT_OBJECT_NAMECHANGE,
                    IntPtr.Zero,
                    _delegate,
                    0,
                    0,
                    PInvokeDeclarations.WINEVENT_OUTOFCONTEXT
                );

                if (_hookHandle == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Failed to set up Windows event hook");
                }

                _isMonitoring = true;

                // Get initial foreground window
                var initialWindow = GetCurrentForegroundWindow();
                if (initialWindow != null)
                {
                    _currentSession = initialWindow;
                }

                _logger.LogInformation("Window monitoring service started successfully");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start window monitoring service");
                throw;
            }
        }

        /// <summary>
        /// Stops the window monitoring service and unhooks from Windows events.
        /// </summary>
        public async Task StopMonitoringAsync()
        {
            if (!_isMonitoring)
            {
                return;
            }

            try
            {
                _logger.LogInformation("Stopping window monitoring service...");

                // Close the current session
                lock (_currentSessionLock)
                {
                    if (_currentSession != null)
                    {
                        _currentSession.SessionEnd = DateTime.UtcNow;
                        _currentSession.CalculateDuration();
                        _capturedSessions.Enqueue(_currentSession);
                        _currentSession = null;
                    }
                }

                // Unhook the event
                if (_hookHandle != IntPtr.Zero)
                {
                    PInvokeDeclarations.UnhookWinEvent(_hookHandle);
                    _hookHandle = IntPtr.Zero;
                }

                _isMonitoring = false;
                _logger.LogInformation("Window monitoring service stopped");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping window monitoring service");
                throw;
            }
        }

        /// <summary>
        /// Gets the currently active application session.
        /// </summary>
        public AppUsageSession? GetCurrentlyActiveApp()
        {
            return _currentSession;
        }

        /// <summary>
        /// Gets all sessions captured since the service started.
        /// </summary>
        public List<AppUsageSession> GetCapturedSessions()
        {
            return _capturedSessions.ToList();
        }

        /// <summary>
        /// Clears all cached sessions.
        /// </summary>
        public void ClearCachedSessions()
        {
            while (_capturedSessions.TryDequeue(out _))
            {
                // Clear all items
            }
            _logger.LogInformation("Cleared captured sessions cache");
        }

        /// <summary>
        /// Gets the total screen time (sum of all completed sessions).
        /// </summary>
        public TimeSpan GetTotalScreenTime()
        {
            var total = _capturedSessions.Sum(s => s.DurationMs);
            return TimeSpan.FromMilliseconds(total);
        }

        /// <summary>
        /// Returns and clears captured sessions since last drain.
        /// </summary>
        public List<AppUsageSession> DrainCapturedSessions()
        {
            var sessions = new List<AppUsageSession>();
            while (_capturedSessions.TryDequeue(out var session))
            {
                sessions.Add(session);
            }
            return sessions;
        }

        /// <summary>
        /// Gets a snapshot of the current active session for periodic saving.
        /// DEPRECATED - Use DrainCapturedSessions() instead.
        /// This method now returns null to prevent duplicate session creation.
        /// </summary>
        public AppUsageSession? GetCurrentSessionSnapshot()
        {
            // Snapshots are no longer created periodically to avoid duplicate time counting.
            // Sessions are only closed when user actually switches to a different app.
            // This method is kept for interface compatibility but always returns null.
            return null;
        }

        /// <summary>
        /// Windows event hook callback - called when foreground window changes.
        /// Now includes debouncing to ignore brief focus changes (e.g., notifications).
        /// </summary>
        private void WinEventHook(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            try
            {
                var newWindow = GetWindowInfo(hwnd);
                if (newWindow == null)
                {
                    return;
                }

                lock (_currentSessionLock)
                {
                    // If switching to a different app
                    if (_currentSession != null && _currentSession.AppName != newWindow.AppName)
                    {
                        var timeSinceLast = DateTime.UtcNow - _lastWindowChangeTime;
                        
                        // If previous session was very brief (< 500ms), it was likely a popup/notification
                        // Merge it with the current session instead of closing it
                        if (timeSinceLast.TotalMilliseconds < DEBOUNCE_MILLISECONDS && _pendingWindowChange != null)
                        {
                            // Previous window was brief - this is likely a return to the original app
                            // Extend the original session instead of closing/reopening
                            _logger.LogInformation(
                                $"Brief window change detected ({timeSinceLast.TotalMilliseconds:F0}ms) - " +
                                $"treating as continuation of {_currentSession.AppName}"
                            );
                            
                            // Don't close the session, just ignore this brief change
                            return;
                        }

                        // This is a significant focus change - close the previous session
                        _currentSession.SessionEnd = DateTime.UtcNow;
                        _currentSession.CalculateDuration();
                        _capturedSessions.Enqueue(_currentSession);

                        _logger.LogInformation(
                            $"Session ended for {_currentSession.AppName} - Duration: {TimeSpan.FromMilliseconds(_currentSession.DurationMs).TotalSeconds:F2}s"
                        );
                    }

                    // Start new session
                    _pendingWindowChange = _currentSession;
                    _currentSession = newWindow;
                    _lastWindowChangeTime = DateTime.UtcNow;
                }
                
                _logger.LogInformation($"Window changed to: {newWindow.AppName} - {newWindow.WindowTitle}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WinEventHook callback");
            }
        }

        /// <summary>
        /// Gets the currently active foreground window information.
        /// </summary>
        private AppUsageSession? GetCurrentForegroundWindow()
        {
            try
            {
                IntPtr hwnd = PInvokeDeclarations.GetForegroundWindow();
                if (hwnd == IntPtr.Zero)
                {
                    return null;
                }

                return GetWindowInfo(hwnd);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting foreground window");
                return null;
            }
        }

        /// <summary>
        /// Extracts window information from a window handle.
        /// </summary>
        private AppUsageSession? GetWindowInfo(IntPtr hwnd)
        {
            try
            {
                if (hwnd == IntPtr.Zero)
                {
                    return null;
                }

                // Get process ID
                PInvokeDeclarations.GetWindowThreadProcessId(hwnd, out int processId);
                if (processId == 0)
                {
                    return null;
                }

                // Get window title
                const int titleMaxLength = 512;
                System.Text.StringBuilder titleBuilder = new System.Text.StringBuilder(titleMaxLength);
                PInvokeDeclarations.GetWindowText(hwnd, titleBuilder, titleMaxLength);
                string windowTitle = titleBuilder.ToString();

                // Get application name from process
                string appName;
                try
                {
                    var process = System.Diagnostics.Process.GetProcessById(processId);
                    appName = System.IO.Path.GetFileNameWithoutExtension(process.ProcessName);
                    process.Dispose();
                }
                catch
                {
                    appName = $"Process_{processId}";
                }

                // Filter out system windows
                if (IsSystemWindow(appName, windowTitle))
                {
                    return null;
                }

                return new AppUsageSession
                {
                    AppName = appName,
                    WindowTitle = windowTitle,
                    ProcessId = processId,
                    SessionStart = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    DurationMs = 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting window information");
                return null;
            }
        }

        /// <summary>
        /// Checks if a window is a system window that should be ignored.
        /// </summary>
        private bool IsSystemWindow(string appName, string windowTitle)
        {
            // Loosen filtering to capture more apps; still ignore empty titles and core system hosts.
            var systemApps = new[] { "dwm", "searchindexer", "taskhostw", "svchost" };

            if (systemApps.Any(s => appName.Contains(s, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // Ignore windows with empty titles
            if (string.IsNullOrWhiteSpace(windowTitle))
            {
                return true;
            }

            return false;
        }
    }
}
