namespace ScreenTimeMonitor.Service.Utilities;

/// <summary>
/// Application constants
/// </summary>
public static class AppConstants
{
    /// <summary>
    /// Service name for Windows Service registration
    /// </summary>
    public const string ServiceName = "ScreenTimeMonitor";

    /// <summary>
    /// Service display name
    /// </summary>
    public const string ServiceDisplayName = "Screen Time Monitor Service";

    /// <summary>
    /// Named pipe name for IPC
    /// </summary>
    public const string NamedPipeName = "ScreenTimeMonitor.Pipe";

    /// <summary>
    /// Default data directory (now relative - will be resolved at runtime)
    /// </summary>
    public const string DefaultDataDirectory = "./data";

    /// <summary>
    /// Default log directory (now relative - will be resolved at runtime)
    /// </summary>
    public const string DefaultLogDirectory = "./logs";

    /// <summary>
    /// Default database directory (now relative - will be resolved at runtime)
    /// </summary>
    public const string DefaultDatabaseDirectory = "./data";

    /// <summary>
    /// SQLite database filename
    /// </summary>
    public const string SqliteDatabaseFileName = "screentime_monitor.db";

    /// <summary>
    /// Default metrics polling interval (seconds)
    /// </summary>
    public const int DefaultMetricsPollingInterval = 5;

    /// <summary>
    /// Default batch size for database writes
    /// </summary>
    public const int DefaultBatchSize = 100;

    /// <summary>
    /// Default maximum queue size
    /// </summary>
    public const int DefaultMaxQueueSize = 1000;

    /// <summary>
    /// Bootup phase duration (seconds)
    /// </summary>
    public const int BootupPhaseDuration = 60;

    /// <summary>
    /// Window hook event debounce time (milliseconds)
    /// </summary>
    public const int WindowHookDebounceMs = 100;

    /// <summary>
    /// Service shutdown timeout (milliseconds)
    /// </summary>
    public const int ServiceShutdownTimeoutMs = 30000;

    /// <summary>
    /// Database flush timeout (milliseconds)
    /// </summary>
    public const int DatabaseFlushTimeoutMs = 15000;
}

/// <summary>
/// Database-related constants
/// </summary>
public static class DatabaseConstants
{
    /// <summary>
    /// App sessions table name
    /// </summary>
    public const string AppSessionsTable = "app_sessions";

    /// <summary>
    /// System metrics table name
    /// </summary>
    public const string SystemMetricsTable = "system_metrics";

    /// <summary>
    /// Daily app summary table name
    /// </summary>
    public const string DailyAppSummaryTable = "daily_app_summary";

    /// <summary>
    /// Daily system summary table name
    /// </summary>
    public const string DailySystemSummaryTable = "daily_system_summary";
}

/// <summary>
/// Configuration keys
/// </summary>
public static class ConfigurationKeys
{
    /// <summary>
    /// Whether to use PostgreSQL (vs SQLite)
    /// </summary>
    public const string UsePostgresQL = "MonitoringSettings:UsePostgreSQL";

    /// <summary>
    /// PostgreSQL connection string
    /// </summary>
    public const string PostgresqlConnectionString = "ConnectionStrings:PostgreSQL";

    /// <summary>
    /// SQLite connection string
    /// </summary>
    public const string SqliteConnectionString = "ConnectionStrings:SQLite";

    /// <summary>
    /// Metrics polling interval
    /// </summary>
    public const string MetricsPollingInterval = "MonitoringSettings:MetricsPollingIntervalSeconds";

    /// <summary>
    /// Batch size for database writes
    /// </summary>
    public const string BatchSize = "MonitoringSettings:BatchSize";

    /// <summary>
    /// Maximum queue size
    /// </summary>
    public const string MaxQueueSize = "MonitoringSettings:MaxQueueSize";

    /// <summary>
    /// Database flush interval
    /// </summary>
    public const string DatabaseFlushInterval = "MonitoringSettings:DatabaseFlushIntervalSeconds";

    /// <summary>
    /// Data retention days
    /// </summary>
    public const string DataRetentionDays = "MonitoringSettings:DataRetentionDays";
}

/// <summary>
/// Exit codes for the application
/// </summary>
public static class ExitCodes
{
    /// <summary>
    /// Success
    /// </summary>
    public const int Success = 0;

    /// <summary>
    /// General failure
    /// </summary>
    public const int Failure = 1;

    /// <summary>
    /// Configuration error
    /// </summary>
    public const int ConfigurationError = 2;

    /// <summary>
    /// Database error
    /// </summary>
    public const int DatabaseError = 3;

    /// <summary>
    /// Service installation error
    /// </summary>
    public const int ServiceInstallError = 4;
}
