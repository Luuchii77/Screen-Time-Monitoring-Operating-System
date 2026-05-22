using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

#if NET8_0_OR_GREATER
using System.Data.SQLite;
#endif

namespace ScreenTimeMonitor.Service.Database
{
    /// <summary>
    /// Handles database initialization, schema creation, and directory setup.
    /// </summary>
    public class DatabaseInitializer
    {
        private readonly DatabaseContext _databaseContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseInitializer> _logger;

        /// <summary>
        /// Initializes a new instance of the DatabaseInitializer class.
        /// </summary>
        /// <param name="databaseContext">The database context for data access.</param>
        /// <param name="configuration">The application configuration for initialization settings.</param>
        /// <param name="logger">The logger instance for logging initialization operations.</param>
        public DatabaseInitializer(DatabaseContext databaseContext, IConfiguration configuration, ILogger<DatabaseInitializer> logger)
        {
            _databaseContext = databaseContext;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Initializes the database, creating necessary directories and schema.
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Starting database initialization...");

                // Ensure data directory exists
                EnsureDataDirectoryExists();

                // If using SQLite, ensure database file exists and enable WAL mode
                var usePostgreSQL = _configuration.GetValue<bool>("MonitoringSettings:UsePostgreSQL");
                if (!usePostgreSQL)
                {
                    await InitializeSQLiteAsync();
                }

                // Create schema
                await _databaseContext.InitializeDatabaseAsync();

                _logger.LogInformation("Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize database");
                throw;
            }
        }

        /// <summary>
        /// Ensures the required data directory exists.
        /// </summary>
        private void EnsureDataDirectoryExists()
        {
            try
            {
                var dataDirectory = _configuration["Paths:DatabaseDirectory"] 
                    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ScreenTimeMonitor", "Data");
                
                if (!Directory.Exists(dataDirectory))
                {
                    Directory.CreateDirectory(dataDirectory);
                    _logger.LogInformation($"Created data directory: {dataDirectory}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create data directory");
                throw;
            }
        }

        /// <summary>
        /// Initializes SQLite database with WAL mode for better concurrency.
        /// </summary>
        private async Task InitializeSQLiteAsync()
        {
#if NET8_0_OR_GREATER
            try
            {
                var connectionString = _configuration.GetConnectionString("SQLite");
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogWarning("SQLite connection string not configured");
                    return;
                }

                // Extract database file path
                var parts = connectionString.Split(';');
                var dataSourcePart = parts.FirstOrDefault(p => p.StartsWith("Data Source="));
                if (dataSourcePart == null)
                {
                    _logger.LogWarning("Could not extract database path from connection string");
                    return;
                }

                var dbPath = dataSourcePart.Replace("Data Source=", "").Trim();
                
                // Ensure directory exists
                var dbDirectory = Path.GetDirectoryName(dbPath);
                if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
                {
                    Directory.CreateDirectory(dbDirectory);
                    _logger.LogInformation($"Created database directory: {dbDirectory}");
                }

                // Enable WAL mode for better concurrency
                using (var connection = new SQLiteConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "PRAGMA journal_mode = WAL;";
                        await command.ExecuteNonQueryAsync();
                        _logger.LogInformation("SQLite WAL mode enabled");
                    }

                    // Enable foreign keys
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "PRAGMA foreign_keys = ON;";
                        await command.ExecuteNonQueryAsync();
                        _logger.LogInformation("SQLite foreign keys enabled");
                    }

                    // Set synchronous mode for performance
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "PRAGMA synchronous = NORMAL;";
                        await command.ExecuteNonQueryAsync();
                        _logger.LogInformation("SQLite synchronous mode set to NORMAL");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize SQLite database");
                throw;
            }
#endif
        }
        /// <summary>
        /// Asynchronously cleans up old data based on the configured retention period.
        /// Deletes records older than the specified number of days and optimizes the database.
        /// </summary>
        public async Task CleanupOldDataAsync()
        {
            try
            {
                var retentionDays = _configuration.GetValue("MonitoringSettings:DataRetentionDays", 90);
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

                _logger.LogInformation($"Cleaning up data older than {cutoffDate:yyyy-MM-dd}");

                var connection = _databaseContext.GetConnection();

                // Delete old sessions
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM app_usage_sessions WHERE created_at < @CutoffDate";
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@CutoffDate";
                    parameter.Value = cutoffDate;
                    command.Parameters.Add(parameter);
                    command.ExecuteNonQuery();
                }

                // Delete old metrics
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM system_metrics WHERE created_at < @CutoffDate";
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@CutoffDate";
                    parameter.Value = cutoffDate;
                    command.Parameters.Add(parameter);
                    command.ExecuteNonQuery();
                }

                _logger.LogInformation("Data cleanup completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup old data");
                // Don't throw - cleanup failure shouldn't stop the service
            }
        }
    }
}
