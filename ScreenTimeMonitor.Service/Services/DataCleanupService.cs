using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScreenTimeMonitor.Service.Database;

namespace ScreenTimeMonitor.Service.Services
{
    /// <summary>
    /// Handles periodic cleanup of old monitoring data based on retention policy.
    /// Runs as a background service to delete data older than DataRetentionDays.
    /// </summary>
    public class DataCleanupService : BackgroundService
    {
        private readonly ILogger<DataCleanupService> _logger;
        private readonly IConfiguration _configuration;
        private readonly DatabaseContext _databaseContext;
        private readonly IAppUsageRepository _appUsageRepository;
        private readonly ISystemMetricsRepository _systemMetricsRepository;
        
        private int _dataRetentionDays = 90;  // Default: keep 90 days
        private int _cleanupIntervalHours = 24; // Run cleanup once per day

        /// <summary>
        /// Initializes a new instance of the DataCleanupService.
        /// </summary>
        public DataCleanupService(
            ILogger<DataCleanupService> logger,
            IConfiguration configuration,
            DatabaseContext databaseContext,
            IAppUsageRepository appUsageRepository,
            ISystemMetricsRepository systemMetricsRepository)
        {
            _logger = logger;
            _configuration = configuration;
            _databaseContext = databaseContext;
            _appUsageRepository = appUsageRepository;
            _systemMetricsRepository = systemMetricsRepository;

            // Load configuration
            _dataRetentionDays = _configuration.GetValue("MonitoringSettings:DataRetentionDays", 90);
            _cleanupIntervalHours = _configuration.GetValue("MonitoringSettings:CleanupIntervalHours", 24);
        }

        /// <summary>
        /// Executes the cleanup service background task.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Data Cleanup Service started. Retention: {_dataRetentionDays} days, Interval: {_cleanupIntervalHours} hours");

            // Wait a bit before first cleanup (give app time to start)
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupOldDataAsync(stoppingToken);

                    // Wait for next cleanup cycle
                    await Task.Delay(TimeSpan.FromHours(_cleanupIntervalHours), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when service is stopping
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during data cleanup");
                    // Continue running on error, try again next cycle
                }
            }

            _logger.LogInformation("Data Cleanup Service stopped");
        }

        /// <summary>
        /// Deletes app usage sessions older than the retention period.
        /// </summary>
        private async Task CleanupOldDataAsync(CancellationToken cancellationToken)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-_dataRetentionDays);

                _logger.LogInformation($"Starting data cleanup. Removing records older than {cutoffDate:yyyy-MM-dd HH:mm:ss}");

                var connection = _databaseContext.GetConnection();

                // Delete old app usage sessions
                var deleteSessionsSql = @"
                    DELETE FROM AppUsageSessions 
                    WHERE SessionStart < @CutoffDate;
                ";

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = deleteSessionsSql;
                    var param = command.CreateParameter();
                    param.ParameterName = "@CutoffDate";
                    param.Value = cutoffDate;
                    command.Parameters.Add(param);

                    var sessionsDeleted = await (command is System.Data.SQLite.SQLiteCommand 
                        ? ((System.Data.SQLite.SQLiteCommand)command).ExecuteNonQueryAsync()
                        : Task.FromResult(command.ExecuteNonQuery()));

                    _logger.LogInformation($"Deleted {sessionsDeleted} old app usage sessions");
                }

                // Delete old system metrics
                var deleteMetricsSql = @"
                    DELETE FROM SystemMetrics 
                    WHERE Timestamp < @CutoffDate;
                ";

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = deleteMetricsSql;
                    var param = command.CreateParameter();
                    param.ParameterName = "@CutoffDate";
                    param.Value = cutoffDate;
                    command.Parameters.Add(param);

                    var metricsDeleted = await (command is System.Data.SQLite.SQLiteCommand 
                        ? ((System.Data.SQLite.SQLiteCommand)command).ExecuteNonQueryAsync()
                        : Task.FromResult(command.ExecuteNonQuery()));

                    _logger.LogInformation($"Deleted {metricsDeleted} old system metrics records");
                }

                // Optimize database (SQLite specific)
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "VACUUM;";
                    if (command is System.Data.SQLite.SQLiteCommand)
                    {
                        await ((System.Data.SQLite.SQLiteCommand)command).ExecuteNonQueryAsync();
                        _logger.LogInformation("Database optimized");
                    }
                }

                _logger.LogInformation("Data cleanup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup old data");
                throw;
            }
        }
    }
}
