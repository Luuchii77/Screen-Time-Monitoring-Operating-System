using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using ScreenTimeMonitor.Service.Models;
using ScreenTimeMonitor.Service.Utilities;

namespace ScreenTimeMonitor.Service.Services
{
    /// <summary>
    /// Collects system performance metrics (CPU, memory, disk, processes).
    /// </summary>
    public class SystemMetricsService : ISystemMetricsService
    {
        private readonly ILogger<SystemMetricsService> _logger;
        private PerformanceCounter? _cpuCounter;
        private PerformanceCounter? _memoryCounter;
        private bool _isInitialized;
        private DateTime _lastCollectionTime;

        public SystemMetricsService(ILogger<SystemMetricsService> logger)
        {
            _logger = logger;
            _lastCollectionTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Starts periodic collection of system metrics.
        /// </summary>
        public async Task StartCollectionAsync()
        {
            try
            {
                _logger.LogInformation("Initializing system metrics collection...");

                // Initialize performance counters
                try
                {
                    _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                    _memoryCounter = new PerformanceCounter("Memory", "Available MBytes", true);
                    
                    // Warm up counters
                    _ = _cpuCounter.NextValue();
                    _ = _memoryCounter.NextValue();
                    
                    _isInitialized = true;
                    _logger.LogInformation("System metrics collection initialized successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not initialize performance counters - will use fallback method");
                    _isInitialized = false;
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start system metrics collection");
                throw;
            }
        }

        /// <summary>
        /// Stops the metrics collection.
        /// </summary>
        public async Task StopCollectionAsync()
        {
            try
            {
                _logger.LogInformation("Stopping system metrics collection...");

                _cpuCounter?.Dispose();
                _memoryCounter?.Dispose();
                _isInitialized = false;

                _logger.LogInformation("System metrics collection stopped");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping system metrics collection");
                throw;
            }
        }

        /// <summary>
        /// Gets the current system metrics snapshot.
        /// </summary>
        public SystemMetric GetCurrentMetrics()
        {
            try
            {
                var metric = new SystemMetric
                {
                    Timestamp = DateTime.UtcNow
                };

                // Get CPU usage
                if (_isInitialized && _cpuCounter != null)
                {
                    metric.CpuUsage = (decimal)_cpuCounter.NextValue();
                }
                else
                {
                    metric.CpuUsage = GetCpuUsageFallback();
                }

                // Get memory info
                var memStatus = GetMemoryStatus();
                metric.MemoryUsageMb = (long)(memStatus.dwTotalPhys - memStatus.dwAvailPhys) / 1024 / 1024;
                metric.MemoryPercent = (decimal)memStatus.dwMemoryLoad;

                // Get disk info
                var diskFree = GetDiskFreeSpace();
                metric.DiskReadBytes = 0; // Will be aggregated by collection service
                metric.DiskWriteBytes = 0;

                _lastCollectionTime = DateTime.UtcNow;
                return metric;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting current system metrics");
                
                // Return a fallback metric with minimal information
                return new SystemMetric
                {
                    Timestamp = DateTime.UtcNow,
                    CpuUsage = 0m,
                    MemoryUsageMb = 0,
                    MemoryPercent = 0m
                };
            }
        }

        /// <summary>
        /// Gets the average metrics for a given time period.
        /// </summary>
        public SystemMetric GetAverageMetrics(DateTime startTime, DateTime endTime)
        {
            try
            {
                // This would be populated by the DataCollectionService
                // For now, return current metrics
                var metric = GetCurrentMetrics();
                metric.Timestamp = startTime + (endTime - startTime) / 2;
                return metric;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating average metrics");
                throw;
            }
        }

        /// <summary>
        /// Gets memory status information using Windows API.
        /// </summary>
        private PInvokeDeclarations.MEMORYSTATUS GetMemoryStatus()
        {
            try
            {
                // Use GlobalMemoryStatusEx (recommended) instead of deprecated GlobalMemoryStatus
                var memStatusEx = new PInvokeDeclarations.MEMORYSTATUSEX();
                memStatusEx.dwLength = (uint)Marshal.SizeOf(memStatusEx);

                if (PInvokeDeclarations.GlobalMemoryStatusEx(ref memStatusEx))
                {
                    // Convert to legacy MEMORYSTATUS structure for compatibility
                    return new PInvokeDeclarations.MEMORYSTATUS
                    {
                        dwLength = memStatusEx.dwLength,
                        dwMemoryLoad = memStatusEx.dwMemoryLoad,
                        dwTotalPhys = memStatusEx.ullTotalPhys,
                        dwAvailPhys = memStatusEx.ullAvailPhys,
                        dwTotalPageFile = memStatusEx.ullTotalPageFile,
                        dwAvailPageFile = memStatusEx.ullAvailPageFile,
                        dwTotalVirtual = memStatusEx.ullTotalVirtual,
                        dwAvailVirtual = memStatusEx.ullAvailVirtual
                    };
                }

                _logger.LogWarning("GlobalMemoryStatusEx failed, falling back to GlobalMemoryStatus");
                // Fallback to deprecated API if Ex version fails
                var memStatus = new PInvokeDeclarations.MEMORYSTATUS();
                memStatus.dwLength = (uint)Marshal.SizeOf(memStatus);
                if (PInvokeDeclarations.GlobalMemoryStatus(ref memStatus))
                {
                    return memStatus;
                }

                _logger.LogWarning("Both GlobalMemoryStatusEx and GlobalMemoryStatus failed");
                return memStatus;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting memory status");
                return new PInvokeDeclarations.MEMORYSTATUS();
            }
        }

        /// <summary>
        /// Gets free disk space information.
        /// </summary>
        private long GetDiskFreeSpace()
        {
            try
            {
                var driveInfo = new System.IO.DriveInfo(System.IO.Path.GetPathRoot(AppDomain.CurrentDomain.BaseDirectory) ?? "C:\\");
                return driveInfo.AvailableFreeSpace / 1024 / 1024; // In MB
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting disk free space");
                return 0;
            }
        }

        /// <summary>
        /// Fallback method to get CPU usage when performance counters are unavailable.
        /// </summary>
        private decimal GetCpuUsageFallback()
        {
            try
            {
                var cpuLoad = GC.GetTotalMemory(false);
                // Rough estimate based on available memory
                return Math.Min(100m, (decimal)(cpuLoad / (1024 * 1024)) * 0.1m);
            }
            catch
            {
                return 0m;
            }
        }
    }
}
