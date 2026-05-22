using ScreenTimeMonitor.Service.Models;

namespace ScreenTimeMonitor.Service.Services
{
    /// <summary>
    /// Service interface for monitoring active windows and capturing app usage data.
    /// </summary>
    public interface IWindowMonitoringService
    {
        /// <summary>
        /// Starts the window monitoring service (hooks into Windows API).
        /// </summary>
        Task StartMonitoringAsync();

        /// <summary>
        /// Stops the window monitoring service.
        /// </summary>
        Task StopMonitoringAsync();

        /// <summary>
        /// Gets the currently active application.
        /// </summary>
        AppUsageSession? GetCurrentlyActiveApp();

        /// <summary>
        /// Gets all sessions captured since the service started.
        /// </summary>
        List<AppUsageSession> GetCapturedSessions();

        /// <summary>
        /// Drains and returns captured sessions since last call.
        /// </summary>
        List<AppUsageSession> DrainCapturedSessions();

        /// <summary>
        /// Clears all cached sessions.
        /// </summary>
        void ClearCachedSessions();

        /// <summary>
        /// Gets the total screen time (sum of all sessions).
        /// </summary>
        TimeSpan GetTotalScreenTime();

        /// <summary>
        /// Gets a snapshot of the current active session for periodic saving.
        /// </summary>
        AppUsageSession? GetCurrentSessionSnapshot();
    }

    /// <summary>
    /// Service interface for collecting system metrics.
    /// </summary>
    public interface ISystemMetricsService
    {
        /// <summary>
        /// Starts periodic collection of system metrics.
        /// </summary>
        Task StartCollectionAsync();

        /// <summary>
        /// Stops the metrics collection.
        /// </summary>
        Task StopCollectionAsync();

        /// <summary>
        /// Gets the current system metrics snapshot.
        /// </summary>
        SystemMetric GetCurrentMetrics();

        /// <summary>
        /// Gets the average metrics for a given time period.
        /// </summary>
        SystemMetric GetAverageMetrics(DateTime startTime, DateTime endTime);
    }

    /// <summary>
    /// Service interface for managing data persistence and background collection.
    /// </summary>
    public interface IDataCollectionService
    {
        /// <summary>
        /// Starts the data collection service (enqueues and periodically flushes to database).
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// Stops the data collection service.
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Enqueues an app usage session for storage.
        /// </summary>
        void EnqueueAppUsageSession(AppUsageSession session);

        /// <summary>
        /// Enqueues system metrics for storage.
        /// </summary>
        void EnqueueSystemMetrics(SystemMetric metric);

        /// <summary>
        /// Gets the current queue size (for monitoring).
        /// </summary>
        int GetQueueSize();

        /// <summary>
        /// Forces immediate flush of queued data to database.
        /// </summary>
        Task FlushAsync();

        /// <summary>
        /// Gets collection statistics.
        /// </summary>
        (int TotalItemsProcessed, int TotalItemsFailed, DateTime LastFlush) GetStatistics();
    }

    /// <summary>
    /// Service interface for managing inter-process communication with the UI.
    /// </summary>
    public interface IIPCService
    {
        /// <summary>
        /// Starts the IPC server (listening for UI connections).
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// Stops the IPC server.
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Sends data to connected UI clients.
        /// </summary>
        Task BroadcastDataAsync(string data);

        /// <summary>
        /// Gets the number of connected clients.
        /// </summary>
        int GetConnectedClientCount();
    }

    /// <summary>
    /// Service interface for managing the service lifecycle and health.
    /// </summary>
    public interface IHealthCheckService
    {
        /// <summary>
        /// Checks if all critical services are running.
        /// </summary>
        Task<bool> IsHealthyAsync();

        /// <summary>
        /// Gets detailed health information.
        /// </summary>
        Task<ServiceHealthReport> GetHealthReportAsync();

        /// <summary>
        /// Attempts to recover from a service failure.
        /// </summary>
        Task<bool> RecoverAsync();
    }

    /// <summary>
    /// Data class for service health report.
    /// </summary>
    public class ServiceHealthReport
    {
        public bool IsOverallHealthy { get; set; }
        public bool IsWindowMonitoringHealthy { get; set; }
        public bool IsMetricsCollectionHealthy { get; set; }
        public bool IsDatabaseHealthy { get; set; }
        public bool IsIPCHealthy { get; set; }
        public string LastError { get; set; } = string.Empty;
        public DateTime LastCheckTime { get; set; }
    }
}
