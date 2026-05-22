using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ScreenTimeMonitor.Service.Database;
using ScreenTimeMonitor.Service.Models;
using ScreenTimeMonitor.Service.Utilities;

namespace ScreenTimeMonitor.Service.Services
{
    /// <summary>
    /// Manages data collection queues and periodic persistence to database.
    /// Acts as a buffer between monitoring services and database layer.
    /// </summary>
    public class DataCollectionService : IDataCollectionService, IDisposable
    {
        private readonly ILogger<DataCollectionService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IAppUsageRepository _appUsageRepository;
        private readonly ISystemMetricsRepository _metricsRepository;
        private readonly IDailyAppSummaryRepository _appSummaryRepository;
        private readonly IDailySystemSummaryRepository _systemSummaryRepository;

        private readonly BlockingCollection<AppUsageSession> _appUsageQueue;
        private readonly BlockingCollection<SystemMetric> _metricsQueue;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _flushTask;
        private bool _isRunning;

        // Statistics
        private long _totalItemsProcessed;
        private long _totalItemsFailed;
        private DateTime _lastFlushTime;

        public DataCollectionService(
            ILogger<DataCollectionService> logger,
            IConfiguration configuration,
            IAppUsageRepository appUsageRepository,
            ISystemMetricsRepository metricsRepository,
            IDailyAppSummaryRepository appSummaryRepository,
            IDailySystemSummaryRepository systemSummaryRepository)
        {
            _logger = logger;
            _configuration = configuration;
            _appUsageRepository = appUsageRepository;
            _metricsRepository = metricsRepository;
            _appSummaryRepository = appSummaryRepository;
            _systemSummaryRepository = systemSummaryRepository;

            var maxQueueSize = configuration.GetValue("MonitoringSettings:MaxQueueSize", 1000);
            _appUsageQueue = new BlockingCollection<AppUsageSession>(Math.Max(maxQueueSize, 10));
            _metricsQueue = new BlockingCollection<SystemMetric>(Math.Max(maxQueueSize, 10));

            _totalItemsProcessed = 0;
            _totalItemsFailed = 0;
            _lastFlushTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Starts the data collection service.
        /// </summary>
        public async Task StartAsync()
        {
            if (_isRunning)
            {
                _logger.LogWarning("Data collection service is already running");
                return;
            }

            try
            {
                _logger.LogInformation("Starting data collection service...");

                _cancellationTokenSource = new CancellationTokenSource();
                
                // Start the background flush task
                _flushTask = RunFlushLoopAsync(_cancellationTokenSource.Token);

                _isRunning = true;
                _logger.LogInformation("Data collection service started successfully");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start data collection service");
                throw;
            }
        }

        /// <summary>
        /// Stops the data collection service.
        /// </summary>
        public async Task StopAsync()
        {
            if (!_isRunning)
            {
                return;
            }

            try
            {
                _logger.LogInformation("Stopping data collection service...");

                // Signal cancellation
                _cancellationTokenSource?.Cancel();

                // Flush remaining items
                await FlushAsync();

                // Wait for flush task to complete
                if (_flushTask != null)
                {
                    await _flushTask;
                }

                _isRunning = false;
                _logger.LogInformation("Data collection service stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping data collection service");
                throw;
            }
        }

        /// <summary>
        /// Enqueues an app usage session for storage.
        /// </summary>
        public void EnqueueAppUsageSession(AppUsageSession session)
        {
            try
            {
                if (!_appUsageQueue.TryAdd(session, TimeSpan.FromSeconds(5)))
                {
                    _logger.LogWarning("Failed to enqueue app usage session - queue full");
                    _totalItemsFailed++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enqueuing app usage session");
                _totalItemsFailed++;
            }
        }

        /// <summary>
        /// Enqueues system metrics for storage.
        /// </summary>
        public void EnqueueSystemMetrics(SystemMetric metric)
        {
            try
            {
                if (!_metricsQueue.TryAdd(metric, TimeSpan.FromSeconds(5)))
                {
                    _logger.LogWarning("Failed to enqueue system metrics - queue full");
                    _totalItemsFailed++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enqueuing system metrics");
                _totalItemsFailed++;
            }
        }

        /// <summary>
        /// Gets the current queue size (for monitoring).
        /// </summary>
        public int GetQueueSize()
        {
            return _appUsageQueue.Count + _metricsQueue.Count;
        }

        /// <summary>
        /// Forces immediate flush of queued data to database.
        /// </summary>
        public async Task FlushAsync()
        {
            try
            {
                var appSessions = new List<AppUsageSession>();
                var metrics = new List<SystemMetric>();

                // Drain queues
                while (_appUsageQueue.TryTake(out var session, TimeSpan.FromMilliseconds(100)))
                {
                    appSessions.Add(session);
                }

                while (_metricsQueue.TryTake(out var metric, TimeSpan.FromMilliseconds(100)))
                {
                    metrics.Add(metric);
                }

                if (appSessions.Count == 0 && metrics.Count == 0)
                {
                    return;
                }

                _logger.LogInformation($"Flushing {appSessions.Count} app sessions and {metrics.Count} metrics to database");

                // Write app sessions
                foreach (var session in appSessions)
                {
                    try
                    {
                        await _appUsageRepository.CreateSessionAsync(session);
                        _totalItemsProcessed++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to save app usage session for {session.AppName}");
                        _totalItemsFailed++;
                    }
                }

                // Write metrics
                foreach (var metric in metrics)
                {
                    try
                    {
                        await _metricsRepository.CreateMetricAsync(metric);
                        _totalItemsProcessed++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save system metric");
                        _totalItemsFailed++;
                    }
                }

                // Clean up old data based on retention policy
                var retentionDays = _configuration.GetValue("MonitoringSettings:DataRetentionDays", 90);
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

                try
                {
                    await _metricsRepository.DeleteMetricsBeforeDateAsync(cutoffDate);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clean up old metrics");
                }

                _lastFlushTime = DateTime.UtcNow;
                _logger.LogInformation($"Flush completed. Total processed: {_totalItemsProcessed}, Failed: {_totalItemsFailed}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during flush operation");
                throw;
            }
        }

        /// <summary>
        /// Gets collection statistics.
        /// </summary>
        public (int TotalItemsProcessed, int TotalItemsFailed, DateTime LastFlush) GetStatistics()
        {
            return ((int)_totalItemsProcessed, (int)_totalItemsFailed, _lastFlushTime);
        }

        /// <summary>
        /// Background task that periodically flushes queued data.
        /// </summary>
        private async Task RunFlushLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                var flushIntervalSeconds = _configuration.GetValue("MonitoringSettings:DatabaseFlushIntervalSeconds", 30);
                var flushInterval = TimeSpan.FromSeconds(flushIntervalSeconds);

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(flushInterval, cancellationToken);
                        
                        if (!cancellationToken.IsCancellationRequested && _isRunning)
                        {
                            await FlushAsync();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in flush loop");
                        // Continue running despite errors
                    }
                }

                _logger.LogInformation("Flush loop ended");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in flush loop");
            }
        }

        public void Dispose()
        {
            _appUsageQueue?.Dispose();
            _metricsQueue?.Dispose();
            _cancellationTokenSource?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
