using System;
using System.IO;
using System.Text.Json;

namespace ScreenTimeMonitor.UI.WPF.Services
{
    public class AppSettings
    {
        public bool StartWithWindows { get; set; } = false;
        public bool MinimizeToTray { get; set; } = true;
    }

    public class AppConfig
    {
        public string AppMode { get; set; } = "Personal";
        public PathsConfig Paths { get; set; } = new();
        public UISettingsConfig UISettings { get; set; } = new();
        public MonitoringSettingsConfig MonitoringSettings { get; set; } = new();
    }

    public class PathsConfig
    {
        public string DatabasePath { get; set; } = "./data/screentime_monitor.db";
        public string LogDirectory { get; set; } = "./logs";
        public string DataDirectory { get; set; } = "./data";
    }

    public class UISettingsConfig
    {
        public string ServicePipeName { get; set; } = "ScreenTimeMonitor.Pipe";
        public int ConnectionTimeoutMs { get; set; } = 5000;
        public int RefreshIntervalSeconds { get; set; } = 2;
        public bool StartWithWindows { get; set; } = false;
        public bool MinimizeToTray { get; set; } = true;
    }

    public class MonitoringSettingsConfig
    {
        public int ProcessScanIntervalMs { get; set; } = 3000;
        public int MetricsPollingIntervalSeconds { get; set; } = 5;
        public int BatchSize { get; set; } = 100;
        public int MaxQueueSize { get; set; } = 1000;
    }

    public static class SettingsManager
    {
        private static readonly string _appFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenTimeMonitor");
        private static readonly string _settingsPath = Path.Combine(_appFolder, "config.json");
        private static readonly string _globalConfigPath = "appsettings.json";

        public static AppSettings Settings { get; private set; } = new AppSettings();
        public static AppConfig Config { get; private set; } = new AppConfig();

        public static void Load()
        {
            try
            {
                // Load global appsettings.json from project root
                if (File.Exists(_globalConfigPath))
                {
                    var json = File.ReadAllText(_globalConfigPath);
                    Config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }

                // Create necessary directories
                var dataDir = ResolvePath(Config.Paths.DataDirectory);
                var logDir = ResolvePath(Config.Paths.LogDirectory);
                
                if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
                if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);

                // Load legacy UI settings if they exist
                if (!Directory.Exists(_appFolder)) Directory.CreateDirectory(_appFolder);
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                else
                {
                    Settings = new AppSettings
                    {
                        StartWithWindows = Config.UISettings.StartWithWindows,
                        MinimizeToTray = Config.UISettings.MinimizeToTray
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Settings load error: {ex.Message}");
                Settings = new AppSettings();
                Config = new AppConfig();
            }
        }

        public static void Save()
        {
            try
            {
                if (!Directory.Exists(_appFolder)) Directory.CreateDirectory(_appFolder);
                var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
            }
            catch { }
        }

        /// <summary>
        /// Resolves a path that may be relative or absolute.
        /// Relative paths are resolved from the application base directory.
        /// </summary>
        public static string ResolvePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");

            if (Path.IsPathRooted(path))
                return path;

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
        }

        /// <summary>
        /// Gets the resolved database path
        /// </summary>
        public static string GetDatabasePath()
        {
            return ResolvePath(Config.Paths.DatabasePath);
        }
    }
}
