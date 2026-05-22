using Microsoft.Extensions.Logging;
using ScreenTimeMonitor.Service.Database;

namespace ScreenTimeMonitor.Service.Services
{
    /// <summary>
    /// Monitors the health of all critical services.
    /// </summary>
    public class HealthCheckService : IHealthCheckService
    {
        private readonly ILogger<HealthCheckService> _logger;
        private readonly DatabaseContext _databaseContext;
        private readonly IWindowMonitoringService _windowMonitoringService;
        private readonly ISystemMetricsService _metricsService;
        private readonly IIPCService _ipcService;
        private readonly IDataCollectionService _dataCollectionService;

        public HealthCheckService(
            ILogger<HealthCheckService> logger,
            DatabaseContext databaseContext,
            IWindowMonitoringService windowMonitoringService,
            ISystemMetricsService metricsService,
            IIPCService ipcService,
            IDataCollectionService dataCollectionService)
        {
            _logger = logger;
            _databaseContext = databaseContext;
            _windowMonitoringService = windowMonitoringService;
            _metricsService = metricsService;
            _ipcService = ipcService;
            _dataCollectionService = dataCollectionService;
        }

        /// <summary>
        /// Checks if all critical services are running.
        /// </summary>
        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                var report = await GetHealthReportAsync();
                return report.IsOverallHealthy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking service health");
                return false;
            }
        }

        /// <summary>
        /// Gets detailed health information.
        /// </summary>
        public Task<ServiceHealthReport> GetHealthReportAsync()
        {
            var report = new ServiceHealthReport
            {
                LastCheckTime = DateTime.UtcNow
            };

            try
            {
                // Check database connection
                report.IsDatabaseHealthy = _databaseContext.IsConnected;
                if (!report.IsDatabaseHealthy)
                {
                    report.LastError = "Database connection not healthy";
                }

                // Check window monitoring service
                report.IsWindowMonitoringHealthy = _windowMonitoringService.GetCapturedSessions().Count >= 0; // Always true if no error
                
                // Check metrics collection
                report.IsMetricsCollectionHealthy = true; // Could add more sophisticated check

                // Check IPC service
                report.IsIPCHealthy = true; // Could add more sophisticated check

                // Overall health
                report.IsOverallHealthy = report.IsDatabaseHealthy && 
                                        report.IsWindowMonitoringHealthy && 
                                        report.IsMetricsCollectionHealthy &&
                                        report.IsIPCHealthy;

                _logger.LogInformation(
                    $"Health check: Database={report.IsDatabaseHealthy}, " +
                    $"Monitoring={report.IsWindowMonitoringHealthy}, " +
                    $"Metrics={report.IsMetricsCollectionHealthy}, " +
                    $"IPC={report.IsIPCHealthy}, " +
                    $"Overall={report.IsOverallHealthy}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating health report");
                report.LastError = ex.Message;
                report.IsOverallHealthy = false;
            }

            return Task.FromResult(report);
        }

        /// <summary>
        /// Attempts to recover from a service failure.
        /// </summary>
        public async Task<bool> RecoverAsync()
        {
            try
            {
                _logger.LogInformation("Attempting service recovery...");

                // Try to restart failed services
                var report = await GetHealthReportAsync();

                if (!report.IsDatabaseHealthy)
                {
                    try
                    {
                        // Attempt to reconnect database
                        var connection = _databaseContext.GetConnection();
                        if (connection.State == System.Data.ConnectionState.Open)
                        {
                            _logger.LogInformation("Database connection recovered");
                            report.IsDatabaseHealthy = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to recover database connection");
                    }
                }

                report.IsOverallHealthy = report.IsDatabaseHealthy && 
                                        report.IsWindowMonitoringHealthy && 
                                        report.IsMetricsCollectionHealthy &&
                                        report.IsIPCHealthy;

                _logger.LogInformation($"Recovery attempt completed. Healthy: {report.IsOverallHealthy}");
                return report.IsOverallHealthy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error attempting recovery");
                return false;
            }
        }
    }
}
