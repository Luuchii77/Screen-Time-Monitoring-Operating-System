using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

#if NET8_0_OR_GREATER
using System.Data.SQLite;
#endif

namespace ScreenTimeMonitor.Service.Database
{
    /// <summary>
    /// Manages database connections and provides access to the underlying database.
    /// Supports both PostgreSQL (primary) and SQLite (fallback).
    /// </summary>
    public class DatabaseContext : IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseContext> _logger;
        private readonly bool _usePostgreSQL;
        private IDbConnection? _connection;
        private bool _isConnected;

        /// <summary>
        /// Initializes a new instance of the DatabaseContext class.
        /// </summary>
        /// <param name="configuration">The application configuration for database settings.</param>
        /// <param name="logger">The logger instance for logging database operations.</param>
        public DatabaseContext(IConfiguration configuration, ILogger<DatabaseContext> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _usePostgreSQL = configuration.GetValue<bool>("MonitoringSettings:UsePostgreSQL");
        }

        /// <summary>
        /// Gets or creates a database connection.
        /// </summary>
        public IDbConnection GetConnection()
        {
            if (_connection == null || _connection.State == ConnectionState.Closed)
            {
                _connection = CreateConnection();
                _connection.Open();
                _isConnected = true;
                var provider = _usePostgreSQL ? "PostgreSQL" : "SQLite";
                _logger.LogInformation($"Database connection opened. Provider: {provider}");
            }

            return _connection;
        }

        /// <summary>
        /// Creates a new database connection instance based on configuration.
        /// </summary>
        private IDbConnection CreateConnection()
        {
            try
            {
                if (_usePostgreSQL)
                {
                    var connectionString = _configuration.GetConnectionString("PostgreSQL");
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        throw new InvalidOperationException("PostgreSQL connection string not configured");
                    }

                    _logger.LogInformation("Creating PostgreSQL connection...");
                    return new NpgsqlConnection(connectionString);
                }
                else
                {
#if NET8_0_OR_GREATER
                    var connectionString = _configuration.GetConnectionString("SQLite");
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        throw new InvalidOperationException("SQLite connection string not configured");
                    }

                    _logger.LogInformation("Creating SQLite connection...");
                    return new SQLiteConnection(connectionString);
#else
                    throw new PlatformNotSupportedException("SQLite support requires .NET 8.0 or later");
#endif
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create database connection");
                throw;
            }
        }

        /// <summary>
        /// Initializes the database schema if it doesn't exist.
        /// </summary>
        public async Task InitializeDatabaseAsync()
        {
            try
            {
                var connection = GetConnection();

                _logger.LogInformation("Initializing database schema...");

                // Create tables based on domain models
                if (_usePostgreSQL)
                {
                    await ExecutePostgreSQLSchema(connection);
                }
                else
                {
                    await ExecuteSQLiteSchema(connection);
                }

                _logger.LogInformation("Database schema initialization completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize database schema");
                throw;
            }
        }

        /// <summary>
        /// Executes PostgreSQL schema creation commands.
        /// </summary>
        private async Task ExecutePostgreSQLSchema(IDbConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    -- Create AppUsageSessions table (aligned to DomainModels/AppUsageSession)
                    CREATE TABLE IF NOT EXISTS app_usage_sessions (
                        id SERIAL PRIMARY KEY,
                        app_name VARCHAR(255) NOT NULL,
                        session_start TIMESTAMP NOT NULL,
                        session_end TIMESTAMP,
                        duration_ms BIGINT DEFAULT 0,
                        window_title VARCHAR(512),
                        process_id INT,
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    );

                    -- Create SystemMetrics table (aligned to DomainModels/SystemMetric)
                    CREATE TABLE IF NOT EXISTS system_metrics (
                        id SERIAL PRIMARY KEY,
                        timestamp TIMESTAMP NOT NULL,
                        cpu_usage DECIMAL(8,2),
                        memory_usage_mb BIGINT,
                        memory_percent DECIMAL(8,2),
                        disk_read_bytes BIGINT,
                        disk_write_bytes BIGINT,
                        process_id INT,
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    );

                    -- Create DailyAppSummaries table (aligned to DailyAppSummary model)
                    CREATE TABLE IF NOT EXISTS daily_app_summaries (
                        id SERIAL PRIMARY KEY,
                        app_name VARCHAR(255) NOT NULL,
                        summary_date DATE NOT NULL,
                        total_usage_ms BIGINT NOT NULL,
                        usage_count INT NOT NULL,
                        first_use TIMESTAMP,
                        last_use TIMESTAMP,
                        UNIQUE(app_name, summary_date),
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    );

                    -- Create DailySystemSummaries table (aligned to DailySystemSummary model)
                    CREATE TABLE IF NOT EXISTS daily_system_summaries (
                        id SERIAL PRIMARY KEY,
                        summary_date DATE NOT NULL UNIQUE,
                        average_cpu_usage DECIMAL(8,2),
                        peak_cpu_usage DECIMAL(8,2),
                        average_memory_mb BIGINT,
                        peak_memory_mb BIGINT,
                        total_disk_read_gb DECIMAL(12,2),
                        total_disk_write_gb DECIMAL(12,2),
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    );

                    -- Create indices for performance
                    CREATE INDEX IF NOT EXISTS idx_app_usage_sessions_app_name 
                        ON app_usage_sessions(app_name);
                    CREATE INDEX IF NOT EXISTS idx_app_usage_sessions_start_time 
                        ON app_usage_sessions(session_start);
                    CREATE INDEX IF NOT EXISTS idx_system_metrics_timestamp 
                        ON system_metrics(timestamp);
                    CREATE INDEX IF NOT EXISTS idx_daily_app_summaries_app_name 
                        ON daily_app_summaries(app_name);
                    CREATE INDEX IF NOT EXISTS idx_daily_app_summaries_summary_date 
                        ON daily_app_summaries(summary_date);
                ";

                if (command is NpgsqlCommand npgsqlCmd)
                {
                    await npgsqlCmd.ExecuteNonQueryAsync();
                }
                else
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Executes SQLite schema creation commands.
        /// </summary>
        private async Task ExecuteSQLiteSchema(IDbConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    -- Create AppUsageSessions table (aligned to DomainModels/AppUsageSession)
                    CREATE TABLE IF NOT EXISTS app_usage_sessions (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        app_name TEXT NOT NULL,
                        session_start DATETIME NOT NULL,
                        session_end DATETIME,
                        duration_ms INTEGER DEFAULT 0,
                        window_title TEXT,
                        process_id INTEGER,
                        created_at DATETIME DEFAULT CURRENT_TIMESTAMP
                    );

                    -- Create SystemMetrics table (aligned to DomainModels/SystemMetric)
                    CREATE TABLE IF NOT EXISTS system_metrics (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        timestamp DATETIME NOT NULL,
                        cpu_usage REAL,
                        memory_usage_mb INTEGER,
                        memory_percent REAL,
                        disk_read_bytes INTEGER,
                        disk_write_bytes INTEGER,
                        process_id INTEGER,
                        created_at DATETIME DEFAULT CURRENT_TIMESTAMP
                    );

                    -- Create DailyAppSummaries table (aligned to DailyAppSummary model)
                    CREATE TABLE IF NOT EXISTS daily_app_summaries (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        app_name TEXT NOT NULL,
                        summary_date DATE NOT NULL,
                        total_usage_ms INTEGER NOT NULL,
                        usage_count INTEGER NOT NULL,
                        first_use DATETIME,
                        last_use DATETIME,
                        created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                        UNIQUE(app_name, summary_date)
                    );

                    -- Create DailySystemSummaries table (aligned to DailySystemSummary model)
                    CREATE TABLE IF NOT EXISTS daily_system_summaries (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        summary_date DATE NOT NULL UNIQUE,
                        average_cpu_usage REAL,
                        peak_cpu_usage REAL,
                        average_memory_mb INTEGER,
                        peak_memory_mb INTEGER,
                        total_disk_read_gb REAL,
                        total_disk_write_gb REAL,
                        created_at DATETIME DEFAULT CURRENT_TIMESTAMP
                    );

                    -- Create indices for performance
                    CREATE INDEX IF NOT EXISTS idx_app_usage_sessions_app_name 
                        ON app_usage_sessions(app_name);
                    CREATE INDEX IF NOT EXISTS idx_app_usage_sessions_start_time 
                        ON app_usage_sessions(session_start);
                    CREATE INDEX IF NOT EXISTS idx_system_metrics_timestamp 
                        ON system_metrics(timestamp);
                    CREATE INDEX IF NOT EXISTS idx_daily_app_summaries_app_name 
                        ON daily_app_summaries(app_name);
                    CREATE INDEX IF NOT EXISTS idx_daily_app_summaries_summary_date 
                        ON daily_app_summaries(summary_date);
                ";

                if (command is SQLiteCommand sqliteCmd)
                {
                    await sqliteCmd.ExecuteNonQueryAsync();
                }
                else
                {
                    await Task.FromResult(command.ExecuteNonQuery());
                }
            }
        }

        /// <summary>
        /// Checks if the database connection is alive.
        /// </summary>
        public bool IsConnected => _isConnected && _connection?.State == ConnectionState.Open;

        /// <summary>
        /// Disposes the database connection.
        /// </summary>
        public void Dispose()
        {
            if (_connection != null)
            {
                if (_connection.State == ConnectionState.Open)
                {
                    _connection.Close();
                    _logger.LogInformation("Database connection closed");
                }
                _connection.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }
}
