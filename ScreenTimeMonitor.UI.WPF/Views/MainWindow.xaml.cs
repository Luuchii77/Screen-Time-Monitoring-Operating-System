using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using System.Management;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Drawing;
using ScreenTimeMonitor.UI.WPF.Services;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace ScreenTimeMonitor.UI.WPF.Views
{
    public partial class MainWindow : Window
    {
        private IPCClient? _ipcClient;
        private SessionManager? _currentSession;
        private readonly ObservableCollection<string> _activityItems = new();
        private const string DefaultPipeName = "ScreenTimeMonitor.Pipe";
        
        // Refresh timer for live data updates (3-second interval)
        private System.Timers.Timer? _liveDataRefreshTimer;
        
        // System processes to filter out (loaded from config)
        private string[] _systemProcessesToExclude = Array.Empty<string>();
        
private System.Windows.Controls.Button? _connectButtonRef;
        private System.Windows.Controls.Button? _disconnectButtonRef;
        private System.Windows.Controls.ListBox? _activityListBoxRef;
        private System.Windows.Controls.TextBlock? _statusTextBlockRef;
        private System.Windows.Controls.TextBlock? _footerTextBlockRef;
        private System.Windows.Controls.TextBlock? _lastRefreshTextBlockRef;
        private System.Windows.Controls.DatePicker? _historyDatePickerRef;
        private System.Windows.Controls.DataGrid? _historyDataGridRef;
        private readonly System.Collections.ObjectModel.ObservableCollection<AppUsageHistoryItem> _historyItems = new();
        private System.Windows.Controls.CheckBox? _autostartCheckBoxRef;
        private readonly HashSet<string> _previouslyRunningApps = new(StringComparer.OrdinalIgnoreCase);
        private System.Windows.Controls.CheckBox? _minimizeToTrayCheckBoxRef;
        private System.Windows.Controls.TextBlock? _gpuStatsTextBlockRef;
        private System.Windows.Controls.DataGrid? _runningAppsDataGridRef;
        private readonly System.Timers.Timer _historyAutoRefreshTimer = new System.Timers.Timer(5000);

        // Tray icon
        private NotifyIcon? _trayIcon;
        
        // Reset button reference
        private System.Windows.Controls.Button? _resetDataButtonRef;
        
        // Daily Summary sorting state
        private string _currentDailySortColumn = "AppName";
        private bool _dailySortAscending = true;
        
        // Current Session tracking
        private DateTime _currentSessionStartTime = DateTime.Now;
        private DateTime? _sessionDisconnectTime = null; // Track when user disconnected
        private DateTime? _uiSessionStartTime = null; // Track when UI connected to service - filters Daily Summary
        private System.Windows.Controls.TextBlock? _currentSessionTimeTextBlockRef;
        private System.Windows.Controls.TextBlock? _currentSessionAppsTextBlockRef;
        private System.Windows.Controls.TextBlock? _currentSessionDurationTextBlockRef;
        private System.Windows.Controls.TextBlock? _currentSessionStatusTextBlockRef;
        private System.Windows.Controls.TextBlock? _trackingSinceTextBlockRef;

        // Stats
        private System.Timers.Timer? _statsTimer;
        private System.Diagnostics.PerformanceCounter? _cpuCounter;
        // Graph history (simple fixed-length queues)
        private readonly System.Collections.Generic.Queue<double> _cpuHistory = new(Enumerable.Repeat(0.0, 40));
        private readonly System.Collections.Generic.Queue<double> _memHistory = new(Enumerable.Repeat(0.0, 40));
        private readonly System.Collections.Generic.Queue<double> _gpuHistory = new(Enumerable.Repeat(0.0, 40));
        private readonly System.Collections.Generic.Queue<double> _diskHistory = new(Enumerable.Repeat(0.0, 40));
        
        // GPU selection
        private System.Windows.Controls.ComboBox? _gpuSelectionComboBoxRef;
        private System.Windows.Controls.ComboBox? _diskSelectionComboBoxRef;
        private System.Windows.Controls.TextBlock? _diskStatsTextBlockRef;
        private System.Windows.Controls.TextBlock? _diskResponseTextBlockRef;
        private System.Windows.Controls.TextBlock? _diskNameTextBlockRef;
        private System.Windows.Controls.TextBlock? _diskUsageTextBlockRef;
        private System.Windows.Controls.TextBlock? _uptimeTextBlockRef;
        private System.Windows.Controls.DatePicker? _exportDatePickerRef;
        private System.Diagnostics.PerformanceCounter? _diskCounter;
        private int _selectedGpuIndex = 0;
        private int _selectedDiskIndex = 0;
        private List<string> _detectedGpus = new();
        private List<System.IO.DriveInfo> _detectedDrives = new();
        private DateTime _systemBootTime = DateTime.Now;
        
        // Activity session tracking
        private List<ActivitySession> _activitySessions = new();
        private int _sessionCounter = 0;
        private Dictionary<string, List<(string AppName, long DurationMs)>> _sessionAppsCache = new();

        // Chart references
        private System.Windows.Controls.Canvas? _appUsageChartCanvasRef;
        private System.Windows.Controls.TextBlock? _highestUsageAppNameRef;
        private System.Windows.Controls.TextBlock? _highestUsageTimeRef;
        private System.Windows.Controls.TextBlock? _lowestUsageAppNameRef;
        private System.Windows.Controls.TextBlock? _lowestUsageTimeRef;
        private System.Windows.Controls.TextBlock? _totalAppsCountRef;
        private System.Windows.Controls.TextBlock? _totalScreenTimeRef;
        private System.Windows.Controls.TextBlock? _chartRefreshTextBlockRef;

        // Sorting state
        private string _currentSortColumn = ""; // Track which column is sorted
        private bool _sortAscending = true;     // Track sort direction

        public MainWindow()
        {
            // Load XAML content explicitly to avoid editor diagnostics that don't include generated files
            System.Windows.Application.LoadComponent(this, new System.Uri("/ScreenTimeMonitor.UI.WPF;component/Views/MainWindow.xaml", System.UriKind.Relative));
            // Cache commonly used controls by name (robust against generated-file indexing issues)
            _activityListBoxRef = this.FindName("ActivityListBox") as System.Windows.Controls.ListBox;
            _connectButtonRef = this.FindName("ConnectButton") as System.Windows.Controls.Button;
            _disconnectButtonRef = this.FindName("DisconnectButton") as System.Windows.Controls.Button;
            _statusTextBlockRef = this.FindName("StatusTextBlock") as System.Windows.Controls.TextBlock;
            _footerTextBlockRef = this.FindName("FooterTextBlock") as System.Windows.Controls.TextBlock;
            _lastRefreshTextBlockRef = this.FindName("LastRefreshTextBlock") as System.Windows.Controls.TextBlock;
            _historyDatePickerRef = this.FindName("HistoryDatePicker") as System.Windows.Controls.DatePicker;
            _historyDataGridRef = this.FindName("HistoryDataGrid") as System.Windows.Controls.DataGrid;
            _autostartCheckBoxRef = this.FindName("AutostartCheckBox") as System.Windows.Controls.CheckBox;
            _minimizeToTrayCheckBoxRef = this.FindName("MinimizeToTrayCheckBox") as System.Windows.Controls.CheckBox;
            _gpuSelectionComboBoxRef = this.FindName("GpuSelectionComboBox") as System.Windows.Controls.ComboBox;
            _diskSelectionComboBoxRef = this.FindName("DiskSelectionComboBox") as System.Windows.Controls.ComboBox;
            _diskStatsTextBlockRef = this.FindName("DiskStatsTextBlock") as System.Windows.Controls.TextBlock;
            _diskResponseTextBlockRef = this.FindName("DiskResponseTextBlock") as System.Windows.Controls.TextBlock;
            _diskNameTextBlockRef = this.FindName("DiskNameTextBlock") as System.Windows.Controls.TextBlock;
            _diskUsageTextBlockRef = this.FindName("DiskUsageTextBlock") as System.Windows.Controls.TextBlock;
            _uptimeTextBlockRef = this.FindName("UptimeTextBlock") as System.Windows.Controls.TextBlock;
            _exportDatePickerRef = this.FindName("ExportDatePicker") as System.Windows.Controls.DatePicker;
            _gpuStatsTextBlockRef = this.FindName("GpuStatsTextBlock") as System.Windows.Controls.TextBlock;
            _runningAppsDataGridRef = this.FindName("RunningAppsDataGrid") as System.Windows.Controls.DataGrid;
            _resetDataButtonRef = this.FindName("ResetDataButton") as System.Windows.Controls.Button;
            
            // Suppress DataGrid selection highlighting
            if (_runningAppsDataGridRef != null)
            {
                _runningAppsDataGridRef.SelectionChanged += (s, e) =>
                {
                    // Clear selection immediately to prevent visual highlighting
                    _runningAppsDataGridRef.SelectedIndex = -1;
                };
            }
            
            // Chart references
            _appUsageChartCanvasRef = this.FindName("AppUsageChartCanvas") as System.Windows.Controls.Canvas;
            _highestUsageAppNameRef = this.FindName("HighestUsageAppName") as System.Windows.Controls.TextBlock;
            _highestUsageTimeRef = this.FindName("HighestUsageTime") as System.Windows.Controls.TextBlock;
            _lowestUsageAppNameRef = this.FindName("LowestUsageAppName") as System.Windows.Controls.TextBlock;
            _lowestUsageTimeRef = this.FindName("LowestUsageTime") as System.Windows.Controls.TextBlock;
            _totalAppsCountRef = this.FindName("TotalAppsCount") as System.Windows.Controls.TextBlock;
            _totalScreenTimeRef = this.FindName("TotalScreenTime") as System.Windows.Controls.TextBlock;
            _chartRefreshTextBlockRef = this.FindName("ChartRefreshTextBlock") as System.Windows.Controls.TextBlock;
            
            // Current Session panel references
            _currentSessionTimeTextBlockRef = this.FindName("CurrentSessionTimeTextBlock") as System.Windows.Controls.TextBlock;
            _currentSessionAppsTextBlockRef = this.FindName("CurrentSessionAppsTextBlock") as System.Windows.Controls.TextBlock;
            _currentSessionDurationTextBlockRef = this.FindName("CurrentSessionDurationTextBlock") as System.Windows.Controls.TextBlock;
            _currentSessionStatusTextBlockRef = this.FindName("CurrentSessionStatusTextBlock") as System.Windows.Controls.TextBlock;
            _trackingSinceTextBlockRef = this.FindName("TrackingSinceTextBlock") as System.Windows.Controls.TextBlock;
            
            // Wire up reset button event handler
            if (_resetDataButtonRef != null)
                _resetDataButtonRef.Click += ResetDataButton_Click;
            
            // Track session start time when window loaded
            _currentSessionStartTime = DateTime.Now;

            if (_activityListBoxRef != null)
                _activityListBoxRef.ItemsSource = _activityItems;

            if (_runningAppsDataGridRef != null)
                _runningAppsDataGridRef.ItemsSource = _historyItems;

            if (_historyDataGridRef != null)
            {
                _historyDataGridRef.ItemsSource = _historyItems;
            }
            if (_historyDatePickerRef != null)
            {
                _historyDatePickerRef.SelectedDate = DateTime.Today;
                _historyDatePickerRef.SelectedDateChanged += HistoryDatePicker_SelectedDateChanged;
            }

            // Load settings and initialize UI
            try
            {
                SettingsManager.Load();
                if (_autostartCheckBoxRef != null)
                    _autostartCheckBoxRef.IsChecked = SettingsManager.Settings.StartWithWindows;
                if (_minimizeToTrayCheckBoxRef != null)
                    _minimizeToTrayCheckBoxRef.IsChecked = SettingsManager.Settings.MinimizeToTray;

                if (_autostartCheckBoxRef != null)
                {
                    _autostartCheckBoxRef.Checked += AutostartCheckBox_CheckedChanged;
                    _autostartCheckBoxRef.Unchecked += AutostartCheckBox_CheckedChanged;
                }

                if (_minimizeToTrayCheckBoxRef != null)
                {
                    _minimizeToTrayCheckBoxRef.Checked += MinimizeToTrayCheckBox_CheckedChanged;
                    _minimizeToTrayCheckBoxRef.Unchecked += MinimizeToTrayCheckBox_CheckedChanged;
                }

                // Initialize tray if enabled
                if (SettingsManager.Settings.MinimizeToTray)
                {
                    InitializeTray();
                }
                // Load hardware names (best-effort)
                EnsureHardwareNamesLoaded();
                LoadGpuList();
                LoadDiskList();
                GetSystemBootTime();
                LoadDiskInfo();
                // Redraw graphs when window size changes so canvases scale responsively
                this.SizeChanged += MainWindow_SizeChanged;
                // Initialize stats timer - sample every 1 second to match system clock
                try
                {
                    _cpuCounter = new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total", readOnly: true);
                    _statsTimer = new System.Timers.Timer(1000);
                    _statsTimer.Elapsed += StatsTimer_Elapsed;
                    _statsTimer.AutoReset = true;
                    _statsTimer.Start();
                }
                catch { }
            }
            catch { }

            this.Loaded += MainWindow_Loaded;

            // Load system process exclusion list from config
            try
            {
                var config = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                    .Build();
                var exclusionList = config.GetSection("SystemProcessesToExclude").Get<string[]>();
                if (exclusionList != null && exclusionList.Length > 0)
                {
                    _systemProcessesToExclude = exclusionList;
                }
            }
            catch
            {
                // If config loading fails, use fallback empty array (will load all processes)
            }

            // Reset session counter on app startup (so first session is always 1)
            SessionManager.ResetCounter();

            // Initialize live data refresh timer (1-second interval for real-time updates)
            _liveDataRefreshTimer = new System.Timers.Timer(1000);
            _liveDataRefreshTimer.Elapsed += LiveDataRefreshTimer_Elapsed;
            _liveDataRefreshTimer.AutoReset = true;

            // Auto-refresh history
            _historyAutoRefreshTimer.Elapsed += HistoryAutoRefreshTimer_Elapsed;
            _historyAutoRefreshTimer.AutoReset = true;
            _historyAutoRefreshTimer.Enabled = true;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Attempt automatic connect on startup
            if (_connectButtonRef != null)
            {
                _connectButtonRef.IsEnabled = false;
            }
            await ConnectToServiceAsync(showStatus: false);
        }

        private void AutostartCheckBox_CheckedChanged(object? sender, RoutedEventArgs e)
        {
            try
            {
                var enabled = _autostartCheckBoxRef?.IsChecked == true;
                SettingsManager.Settings.StartWithWindows = enabled;
                SettingsManager.Save();
                SetAutostart("ScreenTimeMonitor", System.Reflection.Assembly.GetEntryAssembly()?.Location ?? AppDomain.CurrentDomain.BaseDirectory, enabled);
                if (_footerTextBlockRef != null) _footerTextBlockRef.Text = enabled ? "Autostart enabled" : "Autostart disabled";
            }
            catch (Exception ex)
            {
                if (_footerTextBlockRef != null) _footerTextBlockRef.Text = $"Error updating autostart: {ex.Message}";
            }
        }

        private void MinimizeToTrayCheckBox_CheckedChanged(object? sender, RoutedEventArgs e)
        {
            var enabled = _minimizeToTrayCheckBoxRef?.IsChecked == true;
            SettingsManager.Settings.MinimizeToTray = enabled;
            SettingsManager.Save();
            if (enabled && _trayIcon == null)
                InitializeTray();
            else if (!enabled && _trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
        }

        private void SetAutostart(string appName, string exePath, bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (key == null) return;
                if (enable)
                {
                    // Ensure exePath is quoted
                    var value = exePath.Contains(" ") ? $"\"{exePath}\"" : exePath;
                    key.SetValue(appName, value);
                }
                else
                {
                    key.DeleteValue(appName, false);
                }
            }
            catch { }
        }

        private void InitializeTray()
        {
            try
            {
                if (_trayIcon != null) return;
                _trayIcon = new NotifyIcon();
                _trayIcon.Icon = SystemIcons.Application;
                _trayIcon.Text = "Screen Time Monitor";

                var menu = new ContextMenuStrip();
                var openItem = new ToolStripMenuItem("Open");
                openItem.Click += (s, e) => Dispatcher.Invoke(ShowMainWindow);
                var exitItem = new ToolStripMenuItem("Exit");
                exitItem.Click += (s, e) => Dispatcher.Invoke(() => { _trayIcon.Visible = false; _trayIcon.Dispose(); _trayIcon = null; System.Windows.Application.Current.Shutdown(); });
                menu.Items.Add(openItem);
                menu.Items.Add(exitItem);

                _trayIcon.ContextMenuStrip = menu;
                _trayIcon.DoubleClick += (s, e) => Dispatcher.Invoke(ShowMainWindow);
                _trayIcon.Visible = true;
            }
            catch { }
        }

        private void ShowMainWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private async Task ConnectToServiceAsync(bool showStatus = true)
        {
            const int maxRetries = 3;
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    if (_ipcClient != null)
                    {
                        try { await _ipcClient.DisconnectAsync(); } catch { }
                        _ipcClient.Dispose();
                        _ipcClient = null;
                    }

                    if (showStatus && _connectButtonRef != null) _connectButtonRef.IsEnabled = false;
                    if (showStatus && _statusTextBlockRef != null) _statusTextBlockRef.Text = "Connecting...";
                    if (showStatus && _footerTextBlockRef != null) _footerTextBlockRef.Text = "Connecting to service...";

                    _ipcClient = new IPCClient(DefaultPipeName);
                    _ipcClient.OnMessageReceived += OnMessageReceived;
                    // Use 30 second timeout for more reliable reconnects
                    await _ipcClient.ConnectAsync(30000);

                    // Session connected - update status and start refresh timer
                    _currentSession = new SessionManager();
                    _currentSession.IsConnected = true;
                    // Reset session start time when reconnecting to live activity
                    _currentSessionStartTime = DateTime.Now;
                    // Clear disconnect time so duration keeps counting
                    _sessionDisconnectTime = null;
                    // Set UI session start time - this filters Daily Summary to only show current session data
                    _uiSessionStartTime = DateTime.Now;
                    
                    // Notify service that UI connected - this resets session tracking in BackgroundProcessMonitor
                    // so that Live Activity shows time since this moment (0:00:00 for all apps)
                    try
                    {
                        await _ipcClient.SendAsync("UI_CONNECTED");
                    }
                    catch (Exception ex)
                    {
                        if (_footerTextBlockRef != null) _footerTextBlockRef.Text = $"Warning: Could not notify service of connection: {ex.Message}";
                    }
                    
                    if (_statusTextBlockRef != null) 
                        _statusTextBlockRef.Text = $"{_currentSession.SessionName} - Connected to {DefaultPipeName}";
                    if (_connectButtonRef != null) _connectButtonRef.IsEnabled = false;
                    if (_disconnectButtonRef != null) _disconnectButtonRef.IsEnabled = true;
                    if (_footerTextBlockRef != null) _footerTextBlockRef.Text = "Connected successfully";

                    _activityItems.Add($"[{DateTime.Now:HH:mm:ss}] {_currentSession.SessionName} - {_currentSession.GetStatusDisplay()}");
                    
                    // Start live data refresh timer
                    if (_liveDataRefreshTimer != null)
                    {
                        _liveDataRefreshTimer.Stop();
                        _liveDataRefreshTimer.Start();
                    }
                    
                    return; // Successfully connected
                }
                catch (Exception ex)
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        // Final attempt failed - show error
                        if (_statusTextBlockRef != null) _statusTextBlockRef.Text = $"Connection failed: {ex.Message}";
                        if (_footerTextBlockRef != null) _footerTextBlockRef.Text = "Service not available. Click 'Connect to Service' to try again.";
                        if (_connectButtonRef != null) _connectButtonRef.IsEnabled = true;
                        _ipcClient = null;
                        return;
                    }
                    else
                    {
                        // Retry after delay
                        if (showStatus && _statusTextBlockRef != null) 
                            _statusTextBlockRef.Text = $"Retrying connection ({retryCount}/{maxRetries})...";
                        await Task.Delay(1000 * retryCount); // Exponential backoff
                    }
                }
            }
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            await ConnectToServiceAsync();
        }

        private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Stop refresh timer before disconnecting
                if (_liveDataRefreshTimer != null)
                {
                    _liveDataRefreshTimer.Stop();
                }

                if (_ipcClient != null)
                {
                    await _ipcClient.DisconnectAsync();
                    _ipcClient.Dispose();
                    _ipcClient = null;
                }

                // Clear UI session start time on disconnect so next session starts fresh
                _uiSessionStartTime = null;

                if (_currentSession != null)
                {
                    _currentSession.IsConnected = false;
                    _activityItems.Add($"[{DateTime.Now:HH:mm:ss}] {_currentSession.SessionName} - {_currentSession.GetStatusDisplay()}");
                }

                // Freeze the session duration at disconnect time
                _sessionDisconnectTime = DateTime.Now;

                if (_statusTextBlockRef != null) _statusTextBlockRef.Text = "Disconnected";
                if (_connectButtonRef != null) _connectButtonRef.IsEnabled = true;
                if (_disconnectButtonRef != null) _disconnectButtonRef.IsEnabled = false;
                if (_footerTextBlockRef != null) _footerTextBlockRef.Text = "Disconnected from service";
            }
            catch (Exception ex)
            {
                if (_footerTextBlockRef != null) _footerTextBlockRef.Text = $"Error: {ex.Message}";
            }
        }

        private void OnMessageReceived(string message)
        {
            Dispatcher.Invoke(() =>
            {
                // Ignore raw query responses (pipe-delimited app data)
                // Only show meaningful activity messages
                if (string.IsNullOrWhiteSpace(message) || message.Contains("|") && message.Length > 100)
                {
                    return; // Skip displaying raw data responses
                }

                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                _activityItems.Add($"[{timestamp}] {message}");

                // Auto-scroll to latest
                if (_activityListBoxRef != null && _activityItems.Count > 0)
                {
                    _activityListBoxRef.ScrollIntoView(_activityItems[_activityItems.Count - 1]);
                }

                // Keep only last 100 items to avoid memory issues
                while (_activityItems.Count > 100)
                {
                    _activityItems.RemoveAt(0);
                }
            });
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            // Show warning dialog before clearing
            var result = System.Windows.MessageBox.Show(
                "Are you sure you want to clear all activity logs?\n\nThis action cannot be undone.",
                "Clear Activity Confirmation",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning,
                System.Windows.MessageBoxResult.No
            );

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                _activityItems.Clear();
                if (_footerTextBlockRef != null) _footerTextBlockRef.Text = "Activity cleared";
            }
        }

        private void ResetDataButton_Click(object sender, RoutedEventArgs e)
        {
            // Check if live activity is connected
            bool isConnected = _ipcClient != null && _ipcClient.IsConnected;
            
            string dialogMessage;
            string dialogTitle;
            
            if (isConnected)
            {
                // Live activity is connected - show auto-disconnect warning
                dialogMessage = "⚠️ LIVE ACTIVITY CONNECTED\n\n" +
                    "The monitoring service is currently tracking your app usage.\n\n" +
                    "To safely reset data, this will:\n" +
                    "• Automatically disconnect from live activity\n" +
                    "• Delete the entire database\n" +
                    "• Clear all app usage history\n" +
                    "• Reset the session counter to 1\n" +
                    "• Erase all exported CSV files\n\n" +
                    "After reset, you can reconnect to live activity.\n\n" +
                    "This CANNOT be undone!\n\n" +
                    "Are you sure you want to proceed?";
                dialogTitle = "Reset All Data - Requires Disconnect";
            }
            else
            {
                // Service not connected - show standard warning
                dialogMessage = "⚠️ WARNING: This will permanently delete ALL monitoring data!\n\n" +
                    "This action will:\n" +
                    "• Delete the entire database\n" +
                    "• Clear all app usage history\n" +
                    "• Reset the session counter to 1\n" +
                    "• Erase all exported CSV files\n\n" +
                    "This CANNOT be undone!\n\n" +
                    "Are you absolutely sure?";
                dialogTitle = "Reset All Data - Permanent Action";
            }
            
            var result = System.Windows.MessageBox.Show(
                dialogMessage,
                dialogTitle,
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning,
                System.Windows.MessageBoxResult.No
            );

            if (result != System.Windows.MessageBoxResult.Yes)
                return; // User cancelled

            // If connected, auto-disconnect before reset
            if (isConnected && _ipcClient != null && _currentSession != null)
            {
                try
                {
                    Task.Run(async () => await _ipcClient.DisconnectAsync()).Wait(1000);
                    _currentSession.IsConnected = false;
                    
                    System.Windows.MessageBox.Show(
                        "✓ Live activity disconnected successfully.\n\n" +
                        "Proceeding with data reset...",
                        "Service Disconnected",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information
                    );
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"Error disconnecting from service: {ex.Message}\n\n" +
                        "Please try disconnecting manually before resetting.",
                        "Disconnect Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error
                    );
                    return;
                }
            }

            // Proceed with actual data reset
            try
            {
                // Delete database files
                var dbPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..\\..\\..\\ScreenTimeMonitor.Service\\data\\screentime_monitor.db"
                );
                var dbDirectory = System.IO.Path.GetDirectoryName(dbPath);
                
                if (System.IO.Directory.Exists(dbDirectory))
                {
                    var dbFiles = System.IO.Directory.GetFiles(dbDirectory, "screentime_monitor.db*");
                    foreach (var file in dbFiles)
                    {
                        try { System.IO.File.Delete(file); } catch { }
                    }
                }

                // Clear all collections in UI
                _historyItems.Clear();
                _activityItems.Clear();
                _previouslyRunningApps.Clear();
                _activitySessions.Clear();
                _sessionAppsCache.Clear();

                // Reset session counter
                SessionManager.ResetCounter();
                _currentSession = null;

                // Reset session start time
                _currentSessionStartTime = DateTime.Now;

                // Update UI
                if (_statusTextBlockRef != null) _statusTextBlockRef.Text = "All data reset - Ready to connect";
                if (_footerTextBlockRef != null) _footerTextBlockRef.Text = "✓ All data cleared successfully. You can now connect to the service.";
                if (_lastRefreshTextBlockRef != null) _lastRefreshTextBlockRef.Text = "Data reset at " + DateTime.Now.ToString("HH:mm:ss");
                
                System.Windows.MessageBox.Show(
                    "✓ All data has been reset successfully!\n\n" +
                    "You can now reconnect to the service to start fresh monitoring.",
                    "Reset Complete",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                if (_footerTextBlockRef != null) _footerTextBlockRef.Text = $"Reset failed: {ex.Message}";
                System.Windows.MessageBox.Show(
                    $"Error during reset: {ex.Message}",
                    "Reset Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
            }
        }

        private void CustomizeMessages_Click(object sender, RoutedEventArgs e)
        {
            // Create a dialog to manage activity sessions
            var dialog = new Window
            {
                Title = "Manage Activity Sessions",
                Width = 600,
                Height = 400,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = System.Windows.Media.Brushes.White
            };

            var mainGrid = new System.Windows.Controls.Grid();
            mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
            mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

            // Title
            var title = new System.Windows.Controls.TextBlock
            {
                Text = "Screen Monitor Activity Sessions",
                FontSize = 14,
                FontWeight = System.Windows.FontWeights.Bold,
                Margin = new System.Windows.Thickness(15),
                Foreground = System.Windows.Media.Brushes.Black
            };
            System.Windows.Controls.Grid.SetRow(title, 0);
            mainGrid.Children.Add(title);

            // Session list
            var sessionListBox = new System.Windows.Controls.ListBox
            {
                Margin = new System.Windows.Thickness(15, 0, 15, 15),
                BorderThickness = new System.Windows.Thickness(1),
                BorderBrush = System.Windows.Media.Brushes.LightGray
            };

            // Populate with existing sessions
            foreach (var session in _activitySessions)
            {
                var sessionItem = new System.Windows.Controls.TextBlock
                {
                    Text = $"{session.Name} - {session.StartTime:MM/dd HH:mm:ss} ({session.AppCount} apps, {session.TotalDurationSeconds}s)",
                    Padding = new System.Windows.Thickness(8),
                    Tag = session
                };
                sessionListBox.Items.Add(sessionItem);
            }

            System.Windows.Controls.Grid.SetRow(sessionListBox, 1);
            mainGrid.Children.Add(sessionListBox);

            // Buttons
            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Margin = new System.Windows.Thickness(15)
            };

            var newSessionBtn = new System.Windows.Controls.Button
            {
                Content = "➕ New Session",
                Width = 120,
                Padding = new System.Windows.Thickness(10, 8, 10, 8),
                Margin = new System.Windows.Thickness(0, 0, 10, 0),
                Background = System.Windows.Media.Brushes.SkyBlue,
                Foreground = System.Windows.Media.Brushes.White
            };
            newSessionBtn.Click += (s, e) =>
            {
                var sessionName = $"ScreenMonitorActivity{(_sessionCounter > 0 ? _sessionCounter : "")}";
                _sessionCounter++;
                var session = new ActivitySession
                {
                    Name = sessionName,
                    StartTime = DateTime.Now,
                    AppCount = 0,
                    TotalDurationSeconds = 0
                };
                _activitySessions.Add(session);
                sessionListBox.Items.Add(new System.Windows.Controls.TextBlock
                {
                    Text = $"{session.Name} - {session.StartTime:MM/dd HH:mm:ss} (0 apps, 0s)",
                    Padding = new System.Windows.Thickness(8),
                    Tag = session
                });
                if (_footerTextBlockRef != null) _footerTextBlockRef.Text = $"Created new session: {sessionName}";
            };
            buttonPanel.Children.Add(newSessionBtn);

            var exportBtn = new System.Windows.Controls.Button
            {
                Content = "📊 Export Session",
                Width = 140,
                Padding = new System.Windows.Thickness(10, 8, 10, 8),
                Margin = new System.Windows.Thickness(0, 0, 10, 0),
                Background = System.Windows.Media.Brushes.Orange,
                Foreground = System.Windows.Media.Brushes.White
            };
            exportBtn.Click += (s, e) =>
            {
                if (sessionListBox.SelectedItem is System.Windows.Controls.TextBlock selectedBlock && selectedBlock.Tag is ActivitySession session)
                {
                    ExportActivitySession(session);
                }
                else
                {
                    if (_footerTextBlockRef != null) _footerTextBlockRef.Text = "Please select a session to export";
                }
            };
            buttonPanel.Children.Add(exportBtn);

            var closeBtn = new System.Windows.Controls.Button
            {
                Content = "❌ Close",
                Width = 100,
                Padding = new System.Windows.Thickness(10, 8, 10, 8),
                Background = System.Windows.Media.Brushes.LightGray,
                Foreground = System.Windows.Media.Brushes.Black
            };
            closeBtn.Click += (s, e) => dialog.Close();
            buttonPanel.Children.Add(closeBtn);

            System.Windows.Controls.Grid.SetRow(buttonPanel, 2);
            mainGrid.Children.Add(buttonPanel);

            dialog.Content = mainGrid;
            dialog.ShowDialog();
        }

        private void ExportActivitySession(ActivitySession session)
        {
            try
            {
                if (!_sessionAppsCache.ContainsKey(session.Name) || _sessionAppsCache[session.Name].Count == 0)
                {
                    if (_footerTextBlockRef != null) _footerTextBlockRef.Text = "No app data for this session";
                    return;
                }

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    FileName = $"ScreenTimeMonitor_{session.Name}_{session.StartTime:yyyyMMdd_HHmmss}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var csv = new System.Text.StringBuilder();
                    csv.AppendLine("Application,Time Spent (seconds),Time Spent (formatted)");

                    foreach (var (appName, durationMs) in _sessionAppsCache[session.Name])
                    {
                        var seconds = durationMs / 1000;
                        var formatted = TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss");
                        csv.AppendLine($"{appName},{seconds},{formatted}");
                    }

                    System.IO.File.WriteAllText(saveDialog.FileName, csv.ToString());
                    if (_footerTextBlockRef != null) _footerTextBlockRef.Text = $"Exported session to {saveDialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                if (_footerTextBlockRef != null) _footerTextBlockRef.Text = $"Export failed: {ex.Message}";
            }
        }

        private void ClearSelected_Click(object sender, RoutedEventArgs e)
        {
            if (_activityListBoxRef?.SelectedItem is string selectedItem)
            {
                _activityItems.Remove(selectedItem);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshHistoryData();
        }

        private void LoadHistoryFromDatabase(DateTime date)
        {
            // Get database path from configuration
            var dbPath = SettingsManager.GetDatabasePath();
            if (!File.Exists(dbPath))
                throw new FileNotFoundException("Database not found", dbPath);

            // Use System.Data.SQLite to query tables
            try
            {
                using var conn = new System.Data.SQLite.SQLiteConnection($"Data Source={dbPath};Version=3;Pooling=true;");
                conn.Open();

                // Determine which table exists - try multiple possible names
                string? tableName = null;
                var possibleTableNames = new[] { "AppUsageSessions", "app_usage_sessions", "app_sessions" };
                
                foreach (var possibleName in possibleTableNames)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        // Use parameterized query to avoid SQL injection
                        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name = @TableName LIMIT 1;";
                        cmd.Parameters.AddWithValue("@TableName", possibleName);
                        var res = cmd.ExecuteScalar();
                        if (res != null)
                        {
                            tableName = res.ToString();
                            if (tableName != null) break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(tableName))
                {
                    // List all available tables for debugging
                    var availableTables = new List<string>();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";
                        using var tableReader = cmd.ExecuteReader();
                        while (tableReader.Read())
                        {
                            availableTables.Add(tableReader["name"]?.ToString() ?? "");
                        }
                    }
                    throw new InvalidOperationException($"No recognized app sessions table found. Available tables: {string.Join(", ", availableTables)}");
                }

                // Detect actual column names from the table schema
                string? appNameCol = null;
                string? sessionStartCol = null;
                string? sessionEndCol = null;
                var allColumns = new List<string>();
                
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"PRAGMA table_info(\"{tableName}\");";
                    using var schemaReader = cmd.ExecuteReader();
                    while (schemaReader.Read())
                    {
                        var colName = schemaReader["name"]?.ToString() ?? "";
                        allColumns.Add(colName);
                        
                        // Match column names (case-insensitive)
                        if (string.IsNullOrEmpty(appNameCol) && 
                            (colName.Equals("AppName", StringComparison.OrdinalIgnoreCase) || 
                             colName.Equals("app_name", StringComparison.OrdinalIgnoreCase)))
                            appNameCol = colName;
                        else if (string.IsNullOrEmpty(sessionStartCol) && 
                                 (colName.Equals("SessionStart", StringComparison.OrdinalIgnoreCase) || 
                                  colName.Equals("session_start", StringComparison.OrdinalIgnoreCase) ||
                                  colName.Equals("StartTime", StringComparison.OrdinalIgnoreCase) ||
                                  colName.Equals("start_time", StringComparison.OrdinalIgnoreCase)))
                            sessionStartCol = colName;
                        else if (string.IsNullOrEmpty(sessionEndCol) && 
                                 (colName.Equals("SessionEnd", StringComparison.OrdinalIgnoreCase) || 
                                  colName.Equals("session_end", StringComparison.OrdinalIgnoreCase) ||
                                  colName.Equals("EndTime", StringComparison.OrdinalIgnoreCase) ||
                                  colName.Equals("end_time", StringComparison.OrdinalIgnoreCase)))
                            sessionEndCol = colName;
                    }
                }

                // Validate that we found all required columns
                if (string.IsNullOrEmpty(appNameCol))
                    throw new InvalidOperationException($"Could not find app name column. Available columns: {string.Join(", ", allColumns)}");
                if (string.IsNullOrEmpty(sessionStartCol))
                    throw new InvalidOperationException($"Could not find session start column. Available columns: {string.Join(", ", allColumns)}");
                if (string.IsNullOrEmpty(sessionEndCol))
                    throw new InvalidOperationException($"Could not find session end column. Available columns: {string.Join(", ", allColumns)}");

                // Build query using detected column names (use quoted identifiers for safety)
                // Use DurationMs column if available, otherwise fall back to date calculation
                string durationCol = "DurationMs";
                
                // Filter by session start time if connected (only show data from current monitoring session)
                string sessionFilter = _uiSessionStartTime.HasValue 
                    ? "AND \"" + sessionStartCol + "\" >= @SessionStartTime" 
                    : "";
                
                string sql = $@"
                                 SELECT ""{appNameCol}"" as AppName, 
                                        (SUM(COALESCE(""{durationCol}"", 0)) / 3600000.0) AS TotalHours, 
                                        COUNT(*) as SessionCount, 
                                        MIN(""{sessionStartCol}"") as FirstUse, 
                                        MAX(COALESCE(""{sessionEndCol}"", ""{sessionStartCol}"")) as LastUse,
                                        MAX(COALESCE(""{sessionEndCol}"", ""{sessionStartCol}"")) as LastSeen
                                 FROM ""{tableName}"" 
                                 WHERE (date(""{sessionStartCol}"") = date(@SelectedDate) 
                                    OR date(datetime(""{sessionStartCol}"", 'localtime')) = date(@SelectedDate))
                                 {sessionFilter}
                                 GROUP BY ""{appNameCol}"" 
                                 ORDER BY TotalHours DESC;";

                using var cmd2 = conn.CreateCommand();
                cmd2.CommandText = sql;
                cmd2.Parameters.AddWithValue("@SelectedDate", date.ToString("yyyy-MM-dd"));
                
                // Add session start time parameter if filtering by current session
                if (_uiSessionStartTime.HasValue)
                {
                    cmd2.Parameters.AddWithValue("@SessionStartTime", _uiSessionStartTime.Value);
                }
                using var reader = cmd2.ExecuteReader();

                _historyItems.Clear();
                var now = DateTime.UtcNow;
                while (reader.Read())
                {
                    var appName = reader["AppName"]?.ToString() ?? string.Empty;
                    
                    // Filter out system processes
                    if (_systemProcessesToExclude.Any(sys => appName.Equals(sys, StringComparison.OrdinalIgnoreCase) ||
                                                              appName.StartsWith(sys, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue; // Skip this system process
                    }
                    
                    var first = reader["FirstUse"] != DBNull.Value ? (DateTime?)DateTime.Parse(reader["FirstUse"].ToString()!) : null;
                    var last = reader["LastUse"] != DBNull.Value ? (DateTime?)DateTime.Parse(reader["LastUse"].ToString()!) : null;
                    var totalHours = reader["TotalHours"] != DBNull.Value ? Convert.ToDouble(reader["TotalHours"]) : 0.0;
                    var sessions = reader["SessionCount"] != DBNull.Value ? Convert.ToInt32(reader["SessionCount"]) : 0;
                    var lastSeenRaw = reader["LastSeen"] != DBNull.Value ? (DateTime?)DateTime.Parse(reader["LastSeen"].ToString()!) : null;

                    var status = "Idle";
                    if (lastSeenRaw.HasValue)
                    {
                        var diff = now - lastSeenRaw.Value.ToUniversalTime();
                        if (diff.TotalSeconds <= 5)
                            status = "Running";
                        else if (diff.TotalSeconds <= 60)
                            status = "Background";
                        else
                            status = "Stopped";
                    }

                    var item = new AppUsageHistoryItem
                    {
                        AppName = appName,
                        TotalTime = TimeSpan.FromHours(totalHours).ToString(@"hh\:mm\:ss"),
                        SessionCount = sessions,
                        FirstUse = first,
                        LastUse = last,
                        Status = status
                    };
                    _historyItems.Add(item);
                }
            }
            catch (System.Data.SQLite.SQLiteException ex)
            {
                throw new InvalidOperationException($"SQLite error: {ex.Message}", ex);
            }
        }

        private void HistoryDatePicker_SelectedDateChanged(object? sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Auto-refresh when date changes
            RefreshHistoryData();
        }

        private async void RefreshHistoryData()
        {
            if (_lastRefreshTextBlockRef != null) _lastRefreshTextBlockRef.Text = $"Last refreshed: {DateTime.Now:HH:mm:ss}";
            
            // Load currently running apps from service via IPC
            try
            {
                if (_ipcClient == null || !_ipcClient.IsConnected)
                {
                    if (_footerTextBlockRef != null) _footerTextBlockRef.Text = "Not connected to service - showing history";
                    // Fallback to history from database
                    var selectedDate = _historyDatePickerRef?.SelectedDate ?? DateTime.Today;
                    LoadHistoryFromDatabase(selectedDate);
                    return;
                }

                // Request currently running apps from service
                var responseReceived = false;
                string? responseData = null;
                
                void MessageHandler(string msg)
                {
                    responseData = msg;
                    responseReceived = true;
                }
                
                _ipcClient.OnMessageReceived += MessageHandler;
                
                try
                {
                    await _ipcClient.SendAsync("GET_RUNNING_APPS");
                    
                    // Wait for response (max 2 seconds)
                    var timeout = DateTime.Now.AddSeconds(2);
                    while (!responseReceived && DateTime.Now < timeout)
                    {
                        await Task.Delay(50);
                    }
                }
                finally
                {
                    _ipcClient.OnMessageReceived -= MessageHandler;
                }
                
                if (!responseReceived || string.IsNullOrEmpty(responseData))
                {
                    if (_footerTextBlockRef != null) _footerTextBlockRef.Text = "No response from service";
                    return;
                }

                // Parse response: "APP_NAME|DURATION_MS|APP_NAME|DURATION_MS|..."
                var apps = responseData.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                
                // Group by app name (case-insensitive) to aggregate duplicates
                var appDict = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
                
                for (int i = 0; i < apps.Length; i += 2)
                {
                    if (i + 1 < apps.Length)
                    {
                        var appName = apps[i];
                        if (long.TryParse(apps[i + 1], out var durationMs))
                        {
                            // Aggregate by app name
                            if (appDict.ContainsKey(appName))
                                appDict[appName] += durationMs;
                            else
                                appDict[appName] = durationMs;
                        }
                    }
                }

                // Get current running apps
                var currentRunningApps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var (appName, totalDurationMs) in appDict)
                {
                    currentRunningApps.Add(appName);
                }

                // Mark previously running apps that are no longer running as "Closed"
                // IMPORTANT: Do this BEFORE clearing _previouslyRunningApps
                var appsToMarkClosed = _previouslyRunningApps.Where(app => !currentRunningApps.Contains(app)).ToList();
                
                // Update existing items or mark as closed - don't clear all items
                foreach (var closedApp in appsToMarkClosed)
                {
                    var existingItem = _historyItems.FirstOrDefault(x => x.AppName.Equals(closedApp, StringComparison.OrdinalIgnoreCase));
                    if (existingItem != null)
                    {
                        existingItem.Status = "Closed";
                        existingItem.LastUse = DateTime.Now;
                    }
                    else
                    {
                        // Add if not already there
                        _historyItems.Add(new AppUsageHistoryItem
                        {
                            AppName = closedApp,
                            TotalTime = "00:00:00",
                            SessionCount = 0,
                            FirstUse = null,
                            LastUse = DateTime.Now,
                            Status = "Closed",
                            TimesOpened = 0
                        });
                    }
                }

                // NOW update the previously running apps cache AFTER we've checked for closes
                _previouslyRunningApps.Clear();
                foreach (var app in currentRunningApps)
                {
                    _previouslyRunningApps.Add(app);
                }

                if (_activitySessions.Count > 0)
                {
                    var lastSession = _activitySessions[_activitySessions.Count - 1];
                    if (!_sessionAppsCache.ContainsKey(lastSession.Name))
                    {
                        _sessionAppsCache[lastSession.Name] = new List<(string, long)>();
                    }
                    _sessionAppsCache[lastSession.Name].Clear();
                    foreach (var (appName, duration) in appDict)
                    {
                        _sessionAppsCache[lastSession.Name].Add((appName, duration));
                    }
                    lastSession.AppCount = appDict.Count;
                    lastSession.TotalDurationSeconds = (int)(appDict.Values.Sum() / 1000);
                }

                // Update or add running apps
                foreach (var (appName, totalDurationMs) in appDict.OrderByDescending(x => x.Value))
                {
                    // Convert milliseconds to HH:MM:SS format
                    var totalSeconds = totalDurationMs / 1000;
                    var hours = totalSeconds / 3600;
                    var minutes = (totalSeconds % 3600) / 60;
                    var seconds = totalSeconds % 60;
                    var timeStr = $"{hours:D2}:{minutes:D2}:{seconds:D2}";

                    var existingItem = _historyItems.FirstOrDefault(x => x.AppName.Equals(appName, StringComparison.OrdinalIgnoreCase));
                    
                    if (existingItem != null)
                    {
                        // Update existing item
                        existingItem.TotalTime = timeStr;
                        existingItem.Status = "Running";
                        existingItem.LastUse = DateTime.Now;
                    }
                    else
                    {
                        // Add new item
                        _historyItems.Add(new AppUsageHistoryItem
                        {
                            AppName = appName,
                            TotalTime = timeStr,
                            SessionCount = 1,
                            FirstUse = DateTime.Now,
                            LastUse = DateTime.Now,
                            Status = "Running",
                            TimesOpened = 1
                        });
                    }
                }

                if (_footerTextBlockRef != null) _footerTextBlockRef.Text = $"Currently running - {_historyItems.Count} unique apps";
                
                // Update charts
                DrawAppUsageChart();
                UpdateChartStatistics();
            }
            catch (Exception ex)
            {
                if (_footerTextBlockRef != null) _footerTextBlockRef.Text = $"Error: {ex.Message}";
            }
        }

        private void HistoryAutoRefreshTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.Invoke(RefreshHistoryData);
        }

        /// <summary>
        /// Live data refresh timer - updates live activity and running apps every 3 seconds
        /// </summary>
        private void LiveDataRefreshTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (_ipcClient != null && _ipcClient.IsConnected)
            {
                Dispatcher.Invoke(RefreshHistoryData);
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_historyItems.Count == 0)
                {
                    if (_footerTextBlockRef != null) _footerTextBlockRef.Text = "Nothing to export";
                    return;
                }

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                    FileName = $"AppUsage_{(_historyDatePickerRef?.SelectedDate ?? DateTime.Today):yyyyMMdd}.csv"
                };

                var result = dialog.ShowDialog();
                if (result != true) return;

                var sb = new StringBuilder();
                sb.AppendLine("Application,TotalTime,Sessions,FirstUse,LastUse,Status");
                foreach (var item in _historyItems)
                {
                    sb.AppendLine($"{item.AppName},{item.TotalTime},{item.SessionCount},{item.FirstUse},{item.LastUse},{item.Status}");
                }
                File.WriteAllText(dialog.FileName, sb.ToString());
                if (_footerTextBlockRef != null) _footerTextBlockRef.Text = $"Exported to {dialog.FileName}";
            }
            catch (Exception ex)
            {
                if (_footerTextBlockRef != null) _footerTextBlockRef.Text = $"Export failed: {ex.Message}";
            }
        }

        /// <summary>
        /// Exports the current session's activity to a CSV file
        /// </summary>
        private void ExportSessionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentSession == null || !_currentSession.IsConnected)
                {
                    if (_footerTextBlockRef != null) 
                        _footerTextBlockRef.Text = "No active session to export. Please connect to the service first.";
                    return;
                }

                if (_historyItems.Count == 0)
                {
                    if (_footerTextBlockRef != null) 
                        _footerTextBlockRef.Text = "No app data in current session to export";
                    return;
                }

                // Create filename with session ID and timestamp
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var filename = $"ScreenMonitorActivity_{_currentSession.SessionID}_{timestamp}.csv";

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                    FileName = filename
                };

                var result = dialog.ShowDialog();
                if (result != true) return;

                var sb = new StringBuilder();
                // Header with session info
                sb.AppendLine($"Session,{_currentSession.SessionName}");
                sb.AppendLine($"StartTime,{_currentSession.SessionStartTime:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Duration,{_currentSession.SessionDuration.Hours:D2}:{_currentSession.SessionDuration.Minutes:D2}:{_currentSession.SessionDuration.Seconds:D2}");
                sb.AppendLine($"ExportTime,{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine();
                sb.AppendLine("Application,TotalTime,Sessions,FirstUse,LastUse,Status");

                foreach (var item in _historyItems)
                {
                    sb.AppendLine($"{item.AppName},{item.TotalTime},{item.SessionCount},{item.FirstUse},{item.LastUse},{item.Status}");
                }

                File.WriteAllText(dialog.FileName, sb.ToString());
                if (_footerTextBlockRef != null) 
                    _footerTextBlockRef.Text = $"Session exported to {dialog.FileName}";
            }
            catch (Exception ex)
            {
                if (_footerTextBlockRef != null) 
                    _footerTextBlockRef.Text = $"Session export failed: {ex.Message}";
            }
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            if (this.WindowState == WindowState.Minimized && SettingsManager.Settings.MinimizeToTray)
            {
                this.Hide();
                if (_trayIcon != null)
                {
                    _trayIcon.ShowBalloonTip(1000, "Screen Time Monitor", "Running in background. Double-click the tray icon to open.", ToolTipIcon.Info);
                }
            }
        }

        private void StatsTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    // CPU
                    try
                    {
                        if (_cpuCounter != null)
                        {
                            var cpu = Math.Round(_cpuCounter.NextValue(), 1);
                            var txt = $"{cpu}%";
                            var cpuTb = this.FindName("CpuStatsTextBlock") as System.Windows.Controls.TextBlock;
                            if (cpuTb != null) cpuTb.Text = txt;
                            UpdateHistoryQueue(_cpuHistory, cpu);
                        }
                    }
                    catch { }

                    // Memory
                    try
                    {
                        var mem = GetMemoryUsagePercent();
                        var memTb = this.FindName("MemoryStatsTextBlock") as System.Windows.Controls.TextBlock;
                        if (memTb != null) memTb.Text = $"{mem}%";
                        UpdateHistoryQueue(_memHistory, mem);
                    }
                    catch { }

                    // GPU (use real metric if available, otherwise placeholder)
                    try
                    {
                        var gpuVal = GetGpuPlaceholderValue();
                        if (_gpuStatsTextBlockRef != null) _gpuStatsTextBlockRef.Text = $"{gpuVal:0}%";
                        UpdateHistoryQueue(_gpuHistory, gpuVal);
                    }
                    catch { }

                    // Disk metrics
                    try
                    {
                        var diskInfo = GetDiskMetrics();
                        if (_diskStatsTextBlockRef != null) _diskStatsTextBlockRef.Text = $"Active: {diskInfo.ActiveTimePercent:F1}%";
                        if (_diskResponseTextBlockRef != null) _diskResponseTextBlockRef.Text = $"Response: {diskInfo.ResponseTimeMs:F1}ms";
                        if (_diskUsageTextBlockRef != null)
                        {
                            if (_selectedDiskIndex < _detectedDrives.Count)
                            {
                                var selectedDrive = _detectedDrives[_selectedDiskIndex];
                                var usedSpace = selectedDrive.TotalSize - selectedDrive.AvailableFreeSpace;
                                var usagePercent = (usedSpace / (double)selectedDrive.TotalSize) * 100;
                                _diskUsageTextBlockRef.Text = $"{usagePercent:F1}%";
                            }
                        }
                        UpdateHistoryQueue(_diskHistory, diskInfo.ActiveTimePercent);
                    }
                    catch { }

                    // System uptime
                    try
                    {
                        if (_uptimeTextBlockRef != null)
                        {
                            _uptimeTextBlockRef.Text = GetFormattedUptime();
                        }
                    }
                    catch { }

                    // Redraw graphs
                    try
                    {
                        DrawGraph("CpuGraphPolyline", "CpuGraphCanvas", _cpuHistory);
                        DrawGraph("MemoryGraphPolyline", "MemoryGraphCanvas", _memHistory);
                        DrawGraph("GpuGraphPolyline", "GpuGraphCanvas", _gpuHistory);
                        DrawGraph("DiskGraphPolyline", "DiskGraphCanvas", _diskHistory);
                    }
                    catch { }
                    
                    // Update Current Session display
                    try
                    {
                        UpdateCurrentSessionDisplay();
                    }
                    catch { }
                });
            }
            catch { }
        }

        private static int GetMemoryUsagePercent()
        {
            try
            {
                var memStatus = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(memStatus))
                {
                    var used = memStatus.ullTotalPhys - memStatus.ullAvailPhys;
                    var pct = (int)Math.Round((used / (double)memStatus.ullTotalPhys) * 100);
                    return pct;
                }
            }
            catch { }
            return 0;
        }
        
        /// <summary>
        /// Update the Current Session display panel with live stats
        /// </summary>
        private void UpdateCurrentSessionDisplay()
        {
            try
            {
                // Calculate time since session start (frozen if disconnected)
                DateTime endTime = _sessionDisconnectTime ?? DateTime.Now;
                var sessionDuration = endTime - _currentSessionStartTime;
                
                // Update "Tracking Since" timestamp
                if (_trackingSinceTextBlockRef != null)
                {
                    if (_uiSessionStartTime.HasValue)
                    {
                        _trackingSinceTextBlockRef.Text = $"⏲️ Tracking since: {_uiSessionStartTime.Value:yyyy-MM-dd hh:mm:ss tt}";
                        _trackingSinceTextBlockRef.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 183, 0)); // Orange
                    }
                    else
                    {
                        _trackingSinceTextBlockRef.Text = "⏲️ Tracking since: [Not connected]";
                        _trackingSinceTextBlockRef.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(150, 150, 150)); // Gray
                    }
                }
                
                // Current App Time - Show the monitoring session start timestamp
                if (_currentSessionTimeTextBlockRef != null)
                {
                    if (_uiSessionStartTime.HasValue)
                    {
                        _currentSessionTimeTextBlockRef.Text = _uiSessionStartTime.Value.ToString("hh:mm:ss tt");
                    }
                    else
                    {
                        _currentSessionTimeTextBlockRef.Text = "Not connected";
                    }
                }
                
                // Apps This Session - Count unique running apps from history items
                if (_currentSessionAppsTextBlockRef != null)
                {
                    _currentSessionAppsTextBlockRef.Text = _historyItems.Count.ToString();
                }
                
                if (_currentSessionDurationTextBlockRef != null)
                    _currentSessionDurationTextBlockRef.Text = $"{sessionDuration.Hours:00}:{sessionDuration.Minutes:00}:{sessionDuration.Seconds:00}";
                
                if (_currentSessionStatusTextBlockRef != null)
                {
                    if (_ipcClient != null && _ipcClient.IsConnected)
                    {
                        _currentSessionStatusTextBlockRef.Text = "Connected";
                        _currentSessionStatusTextBlockRef.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 217, 255)); // Cyan
                    }
                    else
                    {
                        _currentSessionStatusTextBlockRef.Text = "Disconnected";
                        _currentSessionStatusTextBlockRef.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 107, 107)); // Red
                    }
                }
            }
            catch { }
        }

        // Helper: update a fixed-length history queue
        private static void UpdateHistoryQueue(System.Collections.Generic.Queue<double> q, double value, int maxLen = 40)
        {
            if (q.Count >= maxLen)
                q.Dequeue();
            q.Enqueue(value);
        }

        // Draw polyline into named canvas/polyline
        private void DrawGraph(string polylineName, string canvasName, System.Collections.Generic.Queue<double> history)
        {
            try
            {
                var poly = this.FindName(polylineName) as System.Windows.Shapes.Polyline;
                var canvas = this.FindName(canvasName) as System.Windows.Controls.Canvas;
                if (poly == null || canvas == null) return;

                var pts = new System.Windows.Media.PointCollection();
                var arr = history.ToArray();
                var w = Math.Max(1, canvas.ActualWidth > 0 ? canvas.ActualWidth : canvas.Width);
                var h = Math.Max(1, canvas.ActualHeight > 0 ? canvas.ActualHeight : canvas.Height);
                var span = arr.Length;
                for (int i = 0; i < arr.Length; i++)
                {
                    double x = (i / (double)Math.Max(1, span - 1)) * (w - 4) + 2;
                    // scale value (assume 0-100)
                    double v = Math.Max(0, Math.Min(100, arr[i]));
                    double y = h - (v / 100.0) * (h - 4) - 2;
                    pts.Add(new System.Windows.Point(x, y));
                }
                poly.Points = pts;
            }
            catch { }
        }

        /// <summary>
        /// Draws a column chart of app usage in the AppUsageChartCanvas
        /// </summary>
        private void DrawAppUsageChart()
        {
            try
            {
                if (_appUsageChartCanvasRef == null || _historyItems.Count == 0) 
                    return;

                var canvas = _appUsageChartCanvasRef;
                canvas.Children.Clear();

                // Get top 10 apps by usage time
                var topApps = _historyItems
                    .OrderByDescending(x =>
                    {
                        // Parse time string "HH:mm:ss"
                        var parts = x.TotalTime.Split(':');
                        if (parts.Length >= 3 &&
                            long.TryParse(parts[0], out var hours) &&
                            long.TryParse(parts[1], out var minutes) &&
                            long.TryParse(parts[2], out var seconds))
                        {
                            return hours * 3600 + minutes * 60 + seconds;
                        }
                        return 0;
                    })
                    .Take(10)
                    .ToList();

                if (topApps.Count == 0) return;

                var canvasWidth = canvas.ActualWidth > 0 ? canvas.ActualWidth : canvas.Width;
                var canvasHeight = canvas.ActualHeight > 0 ? canvas.ActualHeight : canvas.Height;
                var chartMargin = 40;
                var chartWidth = canvasWidth - (chartMargin * 2);
                var chartHeight = canvasHeight - (chartMargin * 2);

                // Draw background
                var bg = new System.Windows.Shapes.Rectangle
                {
                    Width = canvasWidth,
                    Height = canvasHeight,
                    Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 20, 25))
                };
                canvas.Children.Add(bg);

                // Draw chart axes
                var axisX = new System.Windows.Shapes.Line
                {
                    X1 = chartMargin,
                    Y1 = canvasHeight - chartMargin,
                    X2 = canvasWidth - chartMargin,
                    Y2 = canvasHeight - chartMargin,
                    Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 217, 255)),
                    StrokeThickness = 2
                };
                canvas.Children.Add(axisX);

                var axisY = new System.Windows.Shapes.Line
                {
                    X1 = chartMargin,
                    Y1 = chartMargin,
                    X2 = chartMargin,
                    Y2 = canvasHeight - chartMargin,
                    Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 217, 255)),
                    StrokeThickness = 2
                };
                canvas.Children.Add(axisY);

                // Calculate max duration for scaling
                var maxSeconds = topApps.Max(x =>
                {
                    var parts = x.TotalTime.Split(':');
                    if (parts.Length >= 3 &&
                        long.TryParse(parts[0], out var hours) &&
                        long.TryParse(parts[1], out var minutes) &&
                        long.TryParse(parts[2], out var seconds))
                    {
                        return hours * 3600 + minutes * 60 + seconds;
                    }
                    return 0;
                });

                if (maxSeconds == 0) maxSeconds = 1;

                // Draw columns
                var columnCount = topApps.Count;
                var columnWidth = chartWidth / (columnCount * 1.5);
                var columnSpacing = columnWidth * 0.5;

                for (int i = 0; i < topApps.Count; i++)
                {
                    var app = topApps[i];
                    var appSeconds = 0L;
                    
                    var parts = app.TotalTime.Split(':');
                    if (parts.Length >= 3 &&
                        long.TryParse(parts[0], out var hours) &&
                        long.TryParse(parts[1], out var minutes) &&
                        long.TryParse(parts[2], out var seconds))
                    {
                        appSeconds = hours * 3600 + minutes * 60 + seconds;
                    }

                    var columnHeightRatio = (double)appSeconds / maxSeconds;
                    var columnHeight = columnHeightRatio * chartHeight;
                    var columnX = chartMargin + (i * (columnWidth + columnSpacing));
                    var columnY = canvasHeight - chartMargin - columnHeight;

                    // Color: gradient from cyan for highest to orange for lowest
                    // FIX: Prevent division by zero when there's only 1 app
                    var hueRatio = columnCount > 1 ? 1.0 - (double)i / (columnCount - 1) : 1.0;
                    System.Windows.Media.Color columnColor;
                    
                    if (hueRatio > 0.5)
                    {
                        // Cyan to green
                        columnColor = System.Windows.Media.Color.FromRgb(
                            (byte)(0),
                            (byte)(217 + (119 - 217) * (1 - hueRatio) * 2),
                            (byte)(255)
                        );
                    }
                    else
                    {
                        // Green to orange
                        columnColor = System.Windows.Media.Color.FromRgb(
                            (byte)(255 * hueRatio * 2),
                            (byte)(187 - (187 * hueRatio * 2)),
                            (byte)(66 - (66 * hueRatio * 2))
                        );
                    }

                    var column = new System.Windows.Shapes.Rectangle
                    {
                        Width = columnWidth,
                        Height = columnHeight,
                        Fill = new System.Windows.Media.SolidColorBrush(columnColor)
                    };
                    System.Windows.Controls.Canvas.SetLeft(column, columnX);
                    System.Windows.Controls.Canvas.SetTop(column, columnY);
                    canvas.Children.Add(column);

                    // Add app name label
                    var label = new System.Windows.Controls.TextBlock
                    {
                        Text = app.AppName.Length > 12 ? app.AppName.Substring(0, 12) + "..." : app.AppName,
                        FontSize = 10,
                        Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(160, 160, 160)),
                        TextAlignment = System.Windows.TextAlignment.Center,
                        Width = columnWidth
                    };
                    System.Windows.Controls.Canvas.SetLeft(label, columnX);
                    System.Windows.Controls.Canvas.SetTop(label, canvasHeight - chartMargin + 5);
                    canvas.Children.Add(label);

                    // Add duration label on top of column
                    var durationLabel = new System.Windows.Controls.TextBlock
                    {
                        Text = app.TotalTime,
                        FontSize = 9,
                        Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 234, 237)),
                        TextAlignment = System.Windows.TextAlignment.Center,
                        Width = columnWidth,
                        FontWeight = System.Windows.FontWeights.Bold
                    };
                    System.Windows.Controls.Canvas.SetLeft(durationLabel, columnX);
                    System.Windows.Controls.Canvas.SetTop(durationLabel, columnY - 20);
                    canvas.Children.Add(durationLabel);
                }
            }
            catch (Exception ex)
            {
                if (_chartRefreshTextBlockRef != null)
                    _chartRefreshTextBlockRef.Text = $"Chart error: {ex.Message}";
            }
        }

        /// <summary>
        /// Updates the chart statistics (highest, lowest, totals)
        /// </summary>
        private void UpdateChartStatistics()
        {
            try
            {
                if (_historyItems.Count == 0)
                {
                    if (_highestUsageAppNameRef != null) _highestUsageAppNameRef.Text = "N/A";
                    if (_highestUsageTimeRef != null) _highestUsageTimeRef.Text = "0h 0m 0s";
                    if (_lowestUsageAppNameRef != null) _lowestUsageAppNameRef.Text = "N/A";
                    if (_lowestUsageTimeRef != null) _lowestUsageTimeRef.Text = "0h 0m 0s";
                    if (_totalAppsCountRef != null) _totalAppsCountRef.Text = "0";
                    if (_totalScreenTimeRef != null) _totalScreenTimeRef.Text = "0h 0m 0s";
                    return;
                }

                // Find highest and lowest usage apps
                var sorted = _historyItems.OrderByDescending(x =>
                {
                    var parts = x.TotalTime.Split(':');
                    if (parts.Length >= 3 &&
                        long.TryParse(parts[0], out var hours) &&
                        long.TryParse(parts[1], out var minutes) &&
                        long.TryParse(parts[2], out var seconds))
                    {
                        return hours * 3600 + minutes * 60 + seconds;
                    }
                    return 0;
                }).ToList();

                if (sorted.Count > 0)
                {
                    // Highest
                    var highest = sorted.First();
                    if (_highestUsageAppNameRef != null) _highestUsageAppNameRef.Text = highest.AppName;
                    if (_highestUsageTimeRef != null) _highestUsageTimeRef.Text = highest.TotalTime;

                    // Lowest
                    var lowest = sorted.Last();
                    if (_lowestUsageAppNameRef != null) _lowestUsageAppNameRef.Text = lowest.AppName;
                    if (_lowestUsageTimeRef != null) _lowestUsageTimeRef.Text = lowest.TotalTime;
                }

                // Total apps
                if (_totalAppsCountRef != null)
                    _totalAppsCountRef.Text = _historyItems.Count.ToString();

                // Total screen time
                var totalSeconds = 0L;
                foreach (var item in _historyItems)
                {
                    var parts = item.TotalTime.Split(':');
                    if (parts.Length >= 3 &&
                        long.TryParse(parts[0], out var hours) &&
                        long.TryParse(parts[1], out var minutes) &&
                        long.TryParse(parts[2], out var seconds))
                    {
                        totalSeconds += hours * 3600 + minutes * 60 + seconds;
                    }
                }

                var totalHours = totalSeconds / 3600;
                var totalMinutes = (totalSeconds % 3600) / 60;
                var totalSecs = totalSeconds % 60;
                if (_totalScreenTimeRef != null)
                    _totalScreenTimeRef.Text = $"{totalHours}h {totalMinutes}m {totalSecs}s";

                if (_chartRefreshTextBlockRef != null)
                    _chartRefreshTextBlockRef.Text = $"Last updated: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                if (_chartRefreshTextBlockRef != null)
                    _chartRefreshTextBlockRef.Text = $"Stats error: {ex.Message}";
            }
        }

        private void RefreshChartButton_Click(object sender, RoutedEventArgs e)
        {
            DrawAppUsageChart();
            UpdateChartStatistics();
        }

        /// <summary>
        /// Sort app usage history by the specified column
        /// </summary>
        private void SortHistoryItems(string sortColumn)
        {
            try
            {
                List<AppUsageHistoryItem> sortedList;

                // Toggle direction if clicking same column again
                if (_currentSortColumn == sortColumn)
                {
                    _sortAscending = !_sortAscending;
                }
                else
                {
                    _sortAscending = true;
                    _currentSortColumn = sortColumn;
                }

                // Sort based on column
                switch (sortColumn)
                {
                    case "AppName":
                        sortedList = _sortAscending
                            ? _historyItems.OrderBy(x => x.AppName).ToList()
                            : _historyItems.OrderByDescending(x => x.AppName).ToList();
                        break;

                    case "TotalTime":
                        sortedList = _sortAscending
                            ? _historyItems.OrderBy(x => TimeSpanFromString(x.TotalTime)).ToList()
                            : _historyItems.OrderByDescending(x => TimeSpanFromString(x.TotalTime)).ToList();
                        break;

                    case "SessionCount":
                        sortedList = _sortAscending
                            ? _historyItems.OrderBy(x => x.SessionCount).ToList()
                            : _historyItems.OrderByDescending(x => x.SessionCount).ToList();
                        break;

                    case "TimesOpened":
                        sortedList = _sortAscending
                            ? _historyItems.OrderBy(x => x.TimesOpened).ToList()
                            : _historyItems.OrderByDescending(x => x.TimesOpened).ToList();
                        break;

                    case "Status":
                        sortedList = _sortAscending
                            ? _historyItems.OrderBy(x => x.Status).ToList()
                            : _historyItems.OrderByDescending(x => x.Status).ToList();
                        break;

                    default:
                        return;
                }

                // Rebuild collection with sorted items
                _historyItems.Clear();
                foreach (var item in sortedList)
                {
                    _historyItems.Add(item);
                }

                // Update UI feedback
                var direction = _sortAscending ? "↑" : "↓";
                if (_chartRefreshTextBlockRef != null)
                    _chartRefreshTextBlockRef.Text = $"Sorted by {sortColumn} {direction}";
            }
            catch (Exception ex)
            {
                if (_chartRefreshTextBlockRef != null)
                    _chartRefreshTextBlockRef.Text = $"Sort error: {ex.Message}";
            }
        }

        /// <summary>
        /// Convert time string "HH:mm:ss" to TimeSpan for sorting
        /// </summary>
        private TimeSpan TimeSpanFromString(string timeStr)
        {
            try
            {
                var parts = timeStr.Split(':');
                if (parts.Length >= 3 &&
                    int.TryParse(parts[0], out var hours) &&
                    int.TryParse(parts[1], out var minutes) &&
                    int.TryParse(parts[2], out var seconds))
                {
                    return new TimeSpan(hours, minutes, seconds);
                }
            }
            catch { }
            return TimeSpan.Zero;
        }

        /// <summary>
        /// Handle DataGrid header clicks for sorting
        /// </summary>
        private void HistoryDataGrid_ColumnHeaderMouseClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.OriginalSource is System.Windows.Controls.Primitives.DataGridColumnHeader header && header.Content is string columnName)
            {
                SortHistoryItems(columnName);
            }
        }

        private void MainWindow_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            try
            {
                // Redraw graphs to fit new sizes
                DrawGraph("CpuGraphPolyline", "CpuGraphCanvas", _cpuHistory);
                DrawGraph("MemoryGraphPolyline", "MemoryGraphCanvas", _memHistory);
                DrawGraph("GpuGraphPolyline", "GpuGraphCanvas", _gpuHistory);
                DrawGraph("DiskGraphPolyline", "DiskGraphCanvas", _diskHistory);
            }
            catch { }
        }

        // Format uptime as Days:HH:MM:SS
        private static string FormatUptime(TimeSpan uptime)
        {
            return $"{uptime.Days}:{uptime.Hours:00}:{uptime.Minutes:00}:{uptime.Seconds:00}";
        }

        // Retrieve CPU/GPU/RAM names (safe best-effort)
        private void EnsureHardwareNamesLoaded()
        {
            try
            {
                var cpuName = "CPU: Unknown";
                var ramName = "RAM: Unknown";
                var gpuName = "GPU: Unknown";

                // CPU via WMI
                try
                {
                    var searcher = new System.Management.ManagementObjectSearcher("select Name from Win32_Processor");
                    foreach (var item in searcher.Get())
                    {
                        cpuName = "CPU: " + (item["Name"]?.ToString() ?? "Unknown");
                        break;
                    }
                }
                catch { }

                // RAM: show total physical memory in GB
                try
                {
                    var memStatus = new MEMORYSTATUSEX();
                    if (GlobalMemoryStatusEx(memStatus))
                    {
                        ramName = $"RAM: {Math.Round(memStatus.ullTotalPhys / 1024.0 / 1024.0 / 1024.0, 1)} GB";
                    }
                }
                catch { }

                // GPU via WMI
                try
                {
                    var searcher = new System.Management.ManagementObjectSearcher("select Name from Win32_VideoController");
                    foreach (var item in searcher.Get())
                    {
                        gpuName = "GPU: " + (item["Name"]?.ToString() ?? "Unknown");
                        break;
                    }
                }
                catch { }

                var cpuTb = this.FindName("CpuNameTextBlock") as System.Windows.Controls.TextBlock;
                var memTb = this.FindName("MemoryNameTextBlock") as System.Windows.Controls.TextBlock;
                var gpuTb = this.FindName("GpuNameTextBlock") as System.Windows.Controls.TextBlock;
                if (cpuTb != null) cpuTb.Text = cpuName;
                if (memTb != null) memTb.Text = ramName;
                if (gpuTb != null) gpuTb.Text = gpuName;
            }
            catch { }
        }

        // Simple GPU placeholder value when no real GPU metric is collected
        private static double GetGpuPlaceholderValue()
        {
            // return a small varying value to make the graph look alive (could be replaced with real GPU metric)
            return (DateTime.Now.Second % 30) * (100.0 / 30.0);
        }

        // Load GPU list for selection
        private void LoadGpuList()
        {
            try
            {
                _detectedGpus.Clear();
                var searcher = new System.Management.ManagementObjectSearcher("select Name from Win32_VideoController");
                foreach (var item in searcher.Get())
                {
                    var gpuName = item["Name"]?.ToString() ?? "Unknown GPU";
                    _detectedGpus.Add(gpuName);
                }

                if (_gpuSelectionComboBoxRef != null)
                {
                    _gpuSelectionComboBoxRef.ItemsSource = _detectedGpus;
                    if (_detectedGpus.Count > 0)
                    {
                        _gpuSelectionComboBoxRef.SelectedIndex = _selectedGpuIndex;
                    }
                }
            }
            catch { }
        }

        private void GpuSelectionComboBox_SelectionChanged(object? sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_gpuSelectionComboBoxRef != null && _gpuSelectionComboBoxRef.SelectedIndex >= 0)
            {
                _selectedGpuIndex = _gpuSelectionComboBoxRef.SelectedIndex;
                if (_selectedGpuIndex < _detectedGpus.Count)
                {
                    var gpuTb = this.FindName("GpuNameTextBlock") as System.Windows.Controls.TextBlock;
                    if (gpuTb != null) gpuTb.Text = $"GPU: {_detectedGpus[_selectedGpuIndex]}";
                }
            }
        }

        // Load available disk drives
        private void LoadDiskList()
        {
            try
            {
                _detectedDrives.Clear();
                var drives = System.IO.DriveInfo.GetDrives().Where(d => d.IsReady).ToList();
                foreach (var drive in drives)
                {
                    _detectedDrives.Add(drive);
                }

                if (_diskSelectionComboBoxRef != null)
                {
                    _diskSelectionComboBoxRef.ItemsSource = _detectedDrives.Select(d => d.Name).ToList();
                    if (_detectedDrives.Count > 0)
                    {
                        _diskSelectionComboBoxRef.SelectedIndex = _selectedDiskIndex;
                    }
                }
            }
            catch { }
        }

        private void DiskSelectionComboBox_SelectionChanged(object? sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_diskSelectionComboBoxRef != null && _diskSelectionComboBoxRef.SelectedIndex >= 0)
            {
                _selectedDiskIndex = _diskSelectionComboBoxRef.SelectedIndex;
                if (_selectedDiskIndex < _detectedDrives.Count)
                {
                    var selectedDrive = _detectedDrives[_selectedDiskIndex];
                    if (_diskNameTextBlockRef != null)
                    {
                        var totalGb = selectedDrive.TotalSize / (1024.0 * 1024.0 * 1024.0);
                        var freeGb = selectedDrive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                        _diskNameTextBlockRef.Text = $"Storage: {selectedDrive.Name} ({totalGb:F1} GB total, {freeGb:F1} GB free)";
                    }
                }
            }
        }

        // Get system boot time for uptime calculation
        private void GetSystemBootTime()
        {
            try
            {
                var uptime = System.Diagnostics.Process.GetCurrentProcess().StartTime;
                _systemBootTime = DateTime.Now.AddMilliseconds(-System.Environment.TickCount64);
            }
            catch
            {
                _systemBootTime = DateTime.Now;
            }
        }

        // Calculate and display system uptime
        private string GetFormattedUptime()
        {
            try
            {
                var tickCount = System.Environment.TickCount64;
                var uptime = TimeSpan.FromMilliseconds(tickCount);
                var days = uptime.Days;
                var hours = uptime.Hours;
                var minutes = uptime.Minutes;
                
                if (days > 0)
                    return $"{days}d {hours}h {minutes}m";
                else if (hours > 0)
                    return $"{hours}h {minutes}m {uptime.Seconds}s";
                else
                    return $"{minutes}m {uptime.Seconds}s";
            }
            catch
            {
                return "N/A";
            }
        }

        // Disk metrics structure
        private struct DiskMetrics
                {
            public double ActiveTimePercent;
            public double ResponseTimeMs;
        }

        private DiskMetrics GetDiskMetrics()
        {
            try
            {
                if (_diskCounter == null)
                {
                    _diskCounter = new System.Diagnostics.PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total", true);
                    _ = _diskCounter.NextValue(); // Warm up
                }

                var diskTime = _diskCounter.NextValue();
                // Response time is approximated (real implementation would use disk counters)
                var responseTime = diskTime > 0 ? (100.0 / diskTime) * 10 : 0;

                return new DiskMetrics
                {
                    ActiveTimePercent = Math.Min(100, diskTime),
                    ResponseTimeMs = Math.Max(0, responseTime)
                };
            }
            catch
            {
                return new DiskMetrics { ActiveTimePercent = 0, ResponseTimeMs = 0 };
            }
        }

        private void LoadDiskInfo()
        {
            try
            {
                var driveInfo = new System.IO.DriveInfo("C:\\");
                var driveType = driveInfo.DriveType == System.IO.DriveType.Fixed ? "Fixed" : driveInfo.DriveType.ToString();
                var totalSize = driveInfo.TotalSize / (1024.0 * 1024.0 * 1024.0);
                var availableSpace = driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                
                // Try to detect HDD vs SSD via WMI
                var driveTypeStr = "HDD"; // Default
                try
                {
                    var searcher = new System.Management.ManagementObjectSearcher(
                        $"SELECT MediaType FROM Win32_DiskDrive WHERE DeviceID LIKE '%{driveInfo.Name.Replace("\\", "").Replace(":", "")}%'");
                    foreach (var item in searcher.Get())
                    {
                        var mediaType = item["MediaType"]?.ToString() ?? "";
                        if (mediaType.Contains("SSD") || mediaType.Contains("Solid State"))
                            driveTypeStr = "SSD";
                    }
                }
                catch { }

                if (_diskNameTextBlockRef != null)
                    _diskNameTextBlockRef.Text = $"Storage: {driveTypeStr} ({totalSize:F1} GB total, {availableSpace:F1} GB free)";
            }
            catch { }
        }

        private void ExportActivityButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedDate = _exportDatePickerRef?.SelectedDate ?? DateTime.Today;
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    FileName = $"ScreenTimeMonitor_Export_{selectedDate:yyyy-MM-dd}.csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    var csv = new System.Text.StringBuilder();
                    csv.AppendLine("Application,Total Hours,Sessions,First Use,Last Use");

                    // Load data for selected date
                    LoadHistoryFromDatabase(selectedDate);

                    foreach (var item in _historyItems)
                    {
                        csv.AppendLine($"{item.AppName},{item.TotalTime},{item.SessionCount}," +
                                    $"{(item.FirstUse?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A")}," +
                                    $"{(item.LastUse?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A")}");
                    }

                    System.IO.File.WriteAllText(dialog.FileName, csv.ToString());
                    if (_footerTextBlockRef != null) _footerTextBlockRef.Text = $"Exported to {dialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                if (_footerTextBlockRef != null) _footerTextBlockRef.Text = $"Export failed: {ex.Message}";
            }
        }

        private void GraphCanvas_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            try
            {
                // Redraw all graphs when any canvas size changes
                DrawGraph("CpuGraphPolyline", "CpuGraphCanvas", _cpuHistory);
                DrawGraph("MemoryGraphPolyline", "MemoryGraphCanvas", _memHistory);
                DrawGraph("GpuGraphPolyline", "GpuGraphCanvas", _gpuHistory);
                DrawGraph("DiskGraphPolyline", "DiskGraphCanvas", _diskHistory);
            }
            catch { }
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([System.Runtime.InteropServices.In] MEMORYSTATUSEX lpBuffer);

        protected override void OnClosed(EventArgs e)
        {
            if (_ipcClient != null)
            {
                _ipcClient.OnMessageReceived -= OnMessageReceived;
                _ipcClient.Dispose();
                _ipcClient = null;
            }
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
            base.OnClosed(e);
        }
    }

    /// <summary>
    /// View model for history items
    /// </summary>
    public class AppUsageHistoryItem : System.ComponentModel.INotifyPropertyChanged
    {
        private string _appName = string.Empty;
        private string _totalTime = "00:00:00";
        private int _sessionCount;
        private DateTime? _firstUse;
        private DateTime? _lastUse;
        private string _status = "Idle";
        private int _timesOpened;

        public string AppName
        {
            get => _appName;
            set { if (_appName != value) { _appName = value; OnPropertyChanged(nameof(AppName)); } }
        }

        public string TotalTime
        {
            get => _totalTime;
            set { if (_totalTime != value) { _totalTime = value; OnPropertyChanged(nameof(TotalTime)); } }
        }

        public int SessionCount
        {
            get => _sessionCount;
            set { if (_sessionCount != value) { _sessionCount = value; OnPropertyChanged(nameof(SessionCount)); } }
        }

        public DateTime? FirstUse
        {
            get => _firstUse;
            set { if (_firstUse != value) { _firstUse = value; OnPropertyChanged(nameof(FirstUse)); } }
        }

        public DateTime? LastUse
        {
            get => _lastUse;
            set { if (_lastUse != value) { _lastUse = value; OnPropertyChanged(nameof(LastUse)); } }
        }

        public string Status
        {
            get => _status;
            set { if (_status != value) { _status = value; OnPropertyChanged(nameof(Status)); } }
        }

        public int TimesOpened
        {
            get => _timesOpened;
            set { if (_timesOpened != value) { _timesOpened = value; OnPropertyChanged(nameof(TimesOpened)); } }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }

    public class ActivitySession
    {
        public string Name { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public int AppCount { get; set; }
        public int TotalDurationSeconds { get; set; }
    }
}
