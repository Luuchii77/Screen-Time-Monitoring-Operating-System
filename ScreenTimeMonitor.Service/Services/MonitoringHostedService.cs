using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScreenTimeMonitor.Service.Database;
using ScreenTimeMonitor.Service.Models;
using ScreenTimeMonitor.Service.Services;

namespace ScreenTimeMonitor.Service.Services
{
    /// <summary>
    /// Hosted service that manages the lifecycle of all monitoring services.
    /// </summary>
    public class MonitoringHostedService : BackgroundService
    {
        private readonly ILogger<MonitoringHostedService> _logger;
        private readonly DatabaseInitializer _databaseInitializer;
        private readonly IWindowMonitoringService _windowMonitoringService;
        private readonly IBackgroundProcessMonitorService _backgroundProcessMonitorService;
        private readonly ISystemMetricsService _metricsService;
        private readonly IDataCollectionService _dataCollectionService;
        private readonly IIPCService _ipcService;
        private readonly IHealthCheckService _healthCheckService;
        private Task? _metricsCollectionTask;
        private Task? _healthCheckTask;
        private CancellationTokenSource? _cancellationTokenSource;

        public MonitoringHostedService(
            ILogger<MonitoringHostedService> logger,
            DatabaseInitializer databaseInitializer,
            IWindowMonitoringService windowMonitoringService,
            IBackgroundProcessMonitorService backgroundProcessMonitorService,
            ISystemMetricsService metricsService,
            IDataCollectionService dataCollectionService,
            IIPCService ipcService,
            IHealthCheckService healthCheckService)
        {
            _logger = logger;
            _databaseInitializer = databaseInitializer;
            _windowMonitoringService = windowMonitoringService;
            _backgroundProcessMonitorService = backgroundProcessMonitorService;
            _metricsService = metricsService;
            _dataCollectionService = dataCollectionService;
            _ipcService = ipcService;
            _healthCheckService = healthCheckService;
        }

        /// <summary>
        /// Called when the service starts.
        /// </summary>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting monitoring hosted service...");

            try
            {
                // Initialize database first
                await _databaseInitializer.InitializeAsync();

                // Start database-related services
                await _dataCollectionService.StartAsync();

                // Start monitoring services
                await _windowMonitoringService.StartMonitoringAsync();
                await _backgroundProcessMonitorService.StartMonitoringAsync();
                await _metricsService.StartCollectionAsync();

                // Start IPC service
                await _ipcService.StartAsync();

                // Start background tasks
                _cancellationTokenSource = new CancellationTokenSource();
                _metricsCollectionTask = RunMetricsCollectionAsync(_cancellationTokenSource.Token);
                _healthCheckTask = RunHealthCheckAsync(_cancellationTokenSource.Token);

                _logger.LogInformation("All monitoring services started successfully");
                await base.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start monitoring services");
                throw;
            }
        }

        /// <summary>
        /// Called when the service stops.
        /// </summary>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping monitoring hosted service...");

            try
            {
                // Signal cancellation to background tasks
                _cancellationTokenSource?.Cancel();

                // Wait for background tasks to complete
                if (_metricsCollectionTask != null)
                {
                    try
                    {
                        await _metricsCollectionTask;
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected
                    }
                }

                if (_healthCheckTask != null)
                {
                    try
                    {
                        await _healthCheckTask;
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected
                    }
                }

                // Stop services in reverse order
                await _ipcService.StopAsync();
                await _dataCollectionService.StopAsync();
                await _metricsService.StopCollectionAsync();
                await _backgroundProcessMonitorService.StopMonitoringAsync();
                await _windowMonitoringService.StopMonitoringAsync();

                // Cleanup old data
                await _databaseInitializer.CleanupOldDataAsync();

                _logger.LogInformation("All monitoring services stopped");
                await base.StopAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping monitoring services");
                throw;
            }
        }

        /// <summary>
        /// Executes the main service logic (background thread).
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Monitoring hosted service is running");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        // Main monitoring loop - just keep the service alive
                        await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in monitoring hosted service");
            }

            _logger.LogInformation("Monitoring hosted service has stopped");
        }

        /// <summary>
        /// Background task that periodically collects metrics.
        /// </summary>
        private async Task RunMetricsCollectionAsync(CancellationToken cancellationToken)
        {
            const int metricsIntervalSeconds = 5;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(metricsIntervalSeconds), cancellationToken);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            // Collect current metrics
                            var metrics = _metricsService.GetCurrentMetrics();

                            // Enqueue for storage
                            _dataCollectionService.EnqueueSystemMetrics(metrics);

                            // Drain any completed app sessions captured since last cycle
                            var sessions = _windowMonitoringService.DrainCapturedSessions();
                            foreach (var session in sessions)
                            {
                                _dataCollectionService.EnqueueAppUsageSession(session);
                            }

                            // NOTE: Removed periodic snapshots to prevent duplicate session counting.
                            // Sessions are now only closed when user actually switches to a different app,
                            // and debouncing prevents brief focus changes (notifications, etc.) from 
                            // fragmenting sessions.

                            // Capture background app data (Discord, Spotify, etc.)
                            var backgroundApps = _backgroundProcessMonitorService.GetBackgroundApps();
                            foreach (var (appName, durationMs, isRunning) in backgroundApps)
                            {
                                var backgroundSession = new AppUsageSession
                                {
                                    AppName = appName,
                                    WindowTitle = $"[Background] {appName}",
                                    ProcessId = 0,
                                    SessionStart = DateTime.UtcNow.AddMilliseconds(-durationMs),
                                    SessionEnd = DateTime.UtcNow,
                                    CreatedAt = DateTime.UtcNow,
                                    DurationMs = (long)durationMs
                                };
                                _dataCollectionService.EnqueueAppUsageSession(backgroundSession);
                            }

                            // Log metrics periodically
                            _logger.LogDebug(
                                $"Metrics: CPU={metrics.CpuUsage:F1}%, " +
                                $"Memory={metrics.MemoryUsageMb}MB, " +
                                $"Sessions drained: {sessions.Count}, " +
                                $"Background apps: {backgroundApps.Count}"
                            );
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in metrics collection loop");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in metrics collection");
            }
        }

        /// <summary>
        /// Background task that periodically checks service health.
        /// </summary>
        private async Task RunHealthCheckAsync(CancellationToken cancellationToken)
        {
            const int healthCheckIntervalSeconds = 30;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(healthCheckIntervalSeconds), cancellationToken);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            var isHealthy = await _healthCheckService.IsHealthyAsync();

                            if (!isHealthy)
                            {
                                _logger.LogWarning("Service health check failed - attempting recovery");
                                var recovered = await _healthCheckService.RecoverAsync();

                                if (recovered)
                                {
                                    _logger.LogInformation("Service successfully recovered");
                                }
                                else
                                {
                                    _logger.LogError("Service recovery failed");
                                }
                            }
                            else
                            {
                                _logger.LogDebug("Service health check passed");
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in health check loop");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in health check");
            }
        }
    }
}
