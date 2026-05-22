using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using ScreenTimeMonitor.Service.Models;
using ScreenTimeMonitor.Service.Utilities;

namespace ScreenTimeMonitor.Service.Database
{
    /// <summary>
    /// Implementation of IAppUsageRepository using Dapper ORM.
    /// Handles CRUD operations for AppUsageSession entities.
    /// </summary>
    public class AppUsageRepository : IAppUsageRepository
    {
        private readonly DatabaseContext _databaseContext;
        private readonly ILogger<AppUsageRepository> _logger;

        public AppUsageRepository(DatabaseContext databaseContext, ILogger<AppUsageRepository> logger)
        {
            _databaseContext = databaseContext;
            _logger = logger;
        }

        public async Task<int> CreateSessionAsync(AppUsageSession session)
        {
            try
            {
                var connection = _databaseContext.GetConnection();
                const string sql = @"INSERT INTO app_usage_sessions 
                    (app_name, session_start, session_end, duration_ms, window_title, process_id, created_at)
                    VALUES (@AppName, @SessionStart, @SessionEnd, @DurationMs, @WindowTitle, @ProcessId, @CreatedAt)";

                var result = await connection.ExecuteAsync(sql, new
                {
                    session.AppName,
                    session.SessionStart,
                    session.SessionEnd,
                    session.DurationMs,
                    session.WindowTitle,
                    session.ProcessId,
                    session.CreatedAt
                });

                _logger.LogInformation($"Created app usage session for {session.AppName}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to create app usage session for {session.AppName}");
                throw;
            }
        }

        public async Task<AppUsageSession?> GetSessionByIdAsync(int id)
        {
            try
            {
                var connection = _databaseContext.GetConnection();
                const string sql = "SELECT * FROM app_usage_sessions WHERE id = @Id";
                var session = await connection.QueryFirstOrDefaultAsync<AppUsageSession>(sql, new { Id = id });
                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get app usage session with ID {id}");
                throw;
            }
        }

        public async Task<List<AppUsageSession>> GetSessionsByDateAsync(DateTime date)
        {
            try
            {
                var connection = _databaseContext.GetConnection();
                const string sql = @"SELECT * FROM app_usage_sessions 
                    WHERE DATE(session_start) = DATE(@Date) 
                    ORDER BY session_start";
                var sessions = (await connection.QueryAsync<AppUsageSession>(sql, new { Date = date })).ToList();
                return sessions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get app usage sessions for date {date:yyyy-MM-dd}");
                throw;
            }
        }

        public async Task<List<AppUsageSession>> GetSessionsByAppAsync(string appName, DateTime startDate, DateTime endDate)
        {
            try
            {
                var connection = _databaseContext.GetConnection();
                const string sql = @"SELECT * FROM app_usage_sessions 
                    WHERE app_name = @AppName 
                    AND session_start >= @StartDate 
                    AND session_start < @EndDate 
                    ORDER BY session_start";
                var sessions = (await connection.QueryAsync<AppUsageSession>(sql, new
                {
                    AppName = appName,
                    StartDate = startDate,
                    EndDate = endDate
                })).ToList();
                return sessions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get app usage sessions for {appName}");
                throw;
            }
        }

        public async Task<List<AppUsageSession>> GetActiveSessionsAsync()
        {
            try
            {
                var connection = _databaseContext.GetConnection();
                const string sql = "SELECT * FROM app_usage_sessions WHERE session_end IS NULL";
                var sessions = (await connection.QueryAsync<AppUsageSession>(sql)).ToList();
                return sessions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active app usage sessions");
                throw;
            }
        }

        public async Task UpdateSessionAsync(AppUsageSession session)
        {
            try
            {
                var connection = _databaseContext.GetConnection();
                const string sql = @"UPDATE app_usage_sessions 
                    SET app_name = @AppName, session_start = @SessionStart, session_end = @SessionEnd, 
                        duration_ms = @DurationMs, window_title = @WindowTitle, process_id = @ProcessId
                    WHERE id = @Id";
                await connection.ExecuteAsync(sql, new
                {
                    session.Id,
                    session.AppName,
                    session.SessionStart,
                    session.SessionEnd,
                    session.DurationMs,
                    session.WindowTitle,
                    session.ProcessId
                });
                _logger.LogInformation($"Updated app usage session with ID {session.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to update app usage session with ID {session.Id}");
                throw;
            }
        }

        public async Task DeleteSessionAsync(int id)
        {
            try
            {
                var connection = _databaseContext.GetConnection();
                const string sql = "DELETE FROM app_usage_sessions WHERE id = @Id";
                await connection.ExecuteAsync(sql, new { Id = id });
                _logger.LogInformation($"Deleted app usage session with ID {id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete app usage session with ID {id}");
                throw;
            }
        }

        public async Task<long> GetAppHistoricalTotalAsync(string appName)
        {
            try
            {
                var connection = _databaseContext.GetConnection();
                // Sum all completed sessions (SessionEnd IS NOT NULL) for this app
                const string sql = @"
                    SELECT COALESCE(SUM(DurationMs), 0) as TotalMs
                    FROM AppUsageSessions
                    WHERE AppName = @AppName
                    AND SessionEnd IS NOT NULL";
                
                var result = await connection.QueryFirstOrDefaultAsync<long>(sql, new { AppName = appName });
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get historical total for app {appName}");
                return 0;
            }
        }

        public async Task<List<AppUsageSession>> GetAppSessionHistoryAsync(string appName, DateTime beforeDate)
        {
            try
            {
                var connection = _databaseContext.GetConnection();
                // Get all completed sessions before the specified date
                const string sql = @"
                    SELECT *
                    FROM AppUsageSessions
                    WHERE AppName = @AppName
                    AND SessionEnd IS NOT NULL
                    AND SessionEnd < @BeforeDate
                    ORDER BY SessionStart DESC";
                
                var sessions = await connection.QueryAsync<AppUsageSession>(sql, new { AppName = appName, BeforeDate = beforeDate });
                return sessions.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get session history for app {appName}");
                return new List<AppUsageSession>();
            }
        }
    }

    /// <summary>
    /// Implementation of ISystemMetricsRepository using Dapper ORM.
    /// </summary>
    public class SystemMetricsRepository : ISystemMetricsRepository
    {
        private readonly DatabaseContext _databaseContext;
        private readonly ILogger<SystemMetricsRepository> _logger;

        public SystemMetricsRepository(DatabaseContext databaseContext, ILogger<SystemMetricsRepository> logger)
        {
            _databaseContext = databaseContext;
            _logger = logger;
        }

        public async Task<int> CreateMetricAsync(SystemMetric metric)
        {
            try
            {
                var connection = _databaseContext.GetConnection();
                const string sql = @"INSERT INTO system_metrics 
                    (timestamp, cpu_usage, memory_usage_mb, memory_percent, disk_read_bytes, disk_write_bytes, process_id)
                    VALUES (@Timestamp, @CpuUsage, @MemoryUsageMb, @MemoryPercent, @DiskReadBytes, @DiskWriteBytes, @ProcessId)";

                var result = await connection.ExecuteAsync(sql, new
                {
                    metric.Timestamp,
                    metric.CpuUsage,
                    metric.MemoryUsageMb,
                    metric.MemoryPercent,
                    metric.DiskReadBytes,
                    metric.DiskWriteBytes,
                    metric.ProcessId
                });
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create system metric");
                throw;
            }
        }

        public async Task<SystemMetric?> GetMetricByIdAsync(int id)
        {
            try
            {
                var connection = _databaseContext.GetConnection();
                const string sql = "SELECT * FROM system_metrics WHERE id = @Id";
                var metric = await connection.QueryFirstOrDefaultAsync<SystemMetric>(sql, new { Id = id });
                return metric;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get system metric with ID {id}");
                throw;
            }
        }

        public async Task<List<SystemMetric>> GetMetricsByDateAsync(DateTime date)
        {
            try
            {
                var connection = _databaseContext.GetConnection();
                const string sql = @"SELECT * FROM system_metrics 
                    WHERE DATE(timestamp) = DATE(@Date) 
                    ORDER BY timestamp";
                var metrics = (await connection.QueryAsync<SystemMetric>(sql, new { Date = date })).ToList();
                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get system metrics for date {date:yyyy-MM-dd}");
                throw;
            }
        }

        public async Task<List<SystemMetric>> GetMetricsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var connection = _databaseContext.GetConnection();
                const string sql = @"SELECT * FROM system_metrics 
                    WHERE timestamp >= @StartDate 
                    AND timestamp < @EndDate 
                    ORDER BY timestamp";
                var metrics = (await connection.QueryAsync<SystemMetric>(sql, new
                {
                    StartDate = startDate,
                    EndDate = endDate
                })).ToList();
                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get system metrics");
                throw;
            }
        }

        public async Task<List<SystemMetric>> GetLatestMetricsAsync(int count)
        {
            try
            {
                var connection = _databaseContext.GetConnection();
                const string sql = "SELECT * FROM system_metrics ORDER BY id DESC LIMIT @Count";
                var metrics = (await connection.QueryAsync<SystemMetric>(sql, new { Count = count })).ToList();
                metrics.Reverse();
                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get latest system metrics");
                throw;
            }
        }

        public async Task DeleteMetricsBeforeDateAsync(DateTime date)
        {
            try
            {
                var connection = _databaseContext.GetConnection();
                const string sql = "DELETE FROM system_metrics WHERE timestamp < @Date";
                await connection.ExecuteAsync(sql, new { Date = date });
                _logger.LogInformation($"Deleted system metrics before {date:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete system metrics before {date:yyyy-MM-dd}");
                throw;
            }
        }
    }

    /// <summary>
    /// Implementation of IDailyAppSummaryRepository using Dapper ORM.
    /// </summary>
    public class DailyAppSummaryRepository : IDailyAppSummaryRepository
    {
        private readonly DatabaseContext _databaseContext;
        private readonly ILogger<DailyAppSummaryRepository> _logger;

        public DailyAppSummaryRepository(DatabaseContext databaseContext, ILogger<DailyAppSummaryRepository> logger)
        {
            _databaseContext = databaseContext;
            _logger = logger;
        }

        public async Task<int> CreateSummaryAsync(DailyAppSummary summary)
        {
            try
            {
                var connection = _databaseContext.GetConnection();
                const string sql = @"INSERT INTO daily_app_summaries 
                    (app_name, summary_date, total_usage_ms, usage_count, first_use, last_use)
                    VALUES (@AppName, @SummaryDate, @TotalUsageMs, @UsageCount, @FirstUse, @LastUse)";

                var result = await connection.ExecuteAsync(sql, new
                {
                    summary.AppName,
                    summary.SummaryDate,
                    summary.TotalUsageMs,
                    summary.UsageCount,
                    summary.FirstUse,
                    summary.LastUse
                });
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to create daily app summary for {summary.AppName}");
                throw;
            }
        }

        public async Task<DailyAppSummary?> GetSummaryAsync(string appName, DateTime date)
        {
            try
            {
                var connection = _databaseContext.GetConnection();
                const string sql = @"SELECT * FROM daily_app_summaries 
                    WHERE app_name = @AppName AND summary_date = @Date";
                var summary = await connection.QueryFirstOrDefaultAsync<DailyAppSummary>(sql, new
                {
                    AppName = appName,
                    Date = date
                });
                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get daily app summary for {appName}");
                throw;
            }
        }

        public async Task<List<DailyAppSummary>> GetSummariesByDateAsync(DateTime date)
        {
            try
            {
                var connection = _databaseContext.GetConnection();
                const string sql = @"SELECT * FROM daily_app_summaries 
                    WHERE summary_date = @Date 
                    ORDER BY app_name";
                var summaries = (await connection.QueryAsync<DailyAppSummary>(sql, new { Date = date })).ToList();
                return summaries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get daily app summaries for {date:yyyy-MM-dd}");
                throw;
            }
        }

        public async Task<List<DailyAppSummary>> GetSummariesByAppAsync(string appName, int daysBack)
        {
            try
            {
                var connection = _databaseContext.GetConnection();
                var startDate = DateTime.Now.AddDays(-daysBack).Date;
                const string sql = @"SELECT * FROM daily_app_summaries 
                    WHERE app_name = @AppName 
                    AND summary_date >= @StartDate 
                    ORDER BY summary_date";
                var summaries = (await connection.QueryAsync<DailyAppSummary>(sql, new
                {
                    AppName = appName,
                    StartDate = startDate
                })).ToList();
                return summaries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get daily app summaries for {appName}");
                throw;
            }
        }

        public async Task<List<DailyAppSummary>> GetAllSummariesAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var connection = _databaseContext.GetConnection();
                const string sql = @"SELECT * FROM daily_app_summaries 
                    WHERE summary_date >= @StartDate 
                    AND summary_date < @EndDate 
                    ORDER BY summary_date";
                var summaries = (await connection.QueryAsync<DailyAppSummary>(sql, new
                {
                    StartDate = startDate,
                    EndDate = endDate
                })).ToList();
                return summaries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get daily app summaries");
                throw;
            }
        }

        public async Task UpdateSummaryAsync(DailyAppSummary summary)
        {
            try
            {
                var connection = _databaseContext.GetConnection();
                const string sql = @"UPDATE daily_app_summaries 
                    SET app_name = @AppName, summary_date = @SummaryDate, 
                        total_usage_ms = @TotalUsageMs, usage_count = @UsageCount, 
                        first_use = @FirstUse, last_use = @LastUse
                    WHERE id = @Id";
                await connection.ExecuteAsync(sql, new
                {
                    summary.Id,
                    summary.AppName,
                    summary.SummaryDate,
                    summary.TotalUsageMs,
                    summary.UsageCount,
                    summary.FirstUse,
                    summary.LastUse
                });
                _logger.LogInformation($"Updated daily app summary with ID {summary.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to update daily app summary with ID {summary.Id}");
                throw;
            }
        }

        public async Task DeleteSummaryAsync(int id)
        {
            try
            {
                var connection = _databaseContext.GetConnection();
                const string sql = "DELETE FROM daily_app_summaries WHERE id = @Id";
                await connection.ExecuteAsync(sql, new { Id = id });
                _logger.LogInformation($"Deleted daily app summary with ID {id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete daily app summary with ID {id}");
                throw;
            }
        }

        /// <summary>
        /// Aggregates app usage sessions for a date, deduplicating overlapping sessions.
        /// This prevents double-counting when the same app has multiple sessions on the same day.
        /// </summary>
        public async Task<List<DailyAppSummary>> AggregateDailyUsageAsync(DateTime date)
        {
            try
            {
                var connection = _databaseContext.GetConnection();
                var targetDate = date.Date;

                // Get all sessions for this date
                const string sessionSql = @"
                    SELECT app_name, session_start, session_end, duration_ms
                    FROM app_usage_sessions
                    WHERE DATE(session_start) = @TargetDate
                    ORDER BY app_name, session_start";

                var sessions = (await connection.QueryAsync<dynamic>(sessionSql, new { TargetDate = targetDate }))
                    .ToList();

                // Group by app and aggregate
                var appGroups = new Dictionary<string, List<(DateTime start, DateTime? end, long durationMs)>>(StringComparer.OrdinalIgnoreCase);

                foreach (var session in sessions)
                {
                    var appName = (string)session.app_name;
                    var sessionStart = (DateTime)session.session_start;
                    var sessionEnd = session.session_end != null ? (DateTime?)session.session_end : null;
                    var durationMs = (long)session.duration_ms;

                    if (!appGroups.ContainsKey(appName))
                    {
                        appGroups[appName] = new List<(DateTime, DateTime?, long)>();
                    }

                    appGroups[appName].Add((sessionStart, sessionEnd, durationMs));
                }

                // Convert grouped data to DailyAppSummary with deduplication
                var summaries = new List<DailyAppSummary>();

                foreach (var (appName, sessionList) in appGroups)
                {
                    if (sessionList.Count == 0) continue;

                    // Sort by start time
                    var sortedSessions = sessionList.OrderBy(s => s.start).ToList();

                    // Merge overlapping sessions
                    var mergedSessions = new List<(DateTime start, DateTime end, long durationMs)>();
                    var currentMerged = (start: sortedSessions[0].start, end: sortedSessions[0].end ?? DateTime.UtcNow, durationMs: sortedSessions[0].durationMs);

                    for (int i = 1; i < sortedSessions.Count; i++)
                    {
                        var nextSession = sortedSessions[i];
                        var nextStart = nextSession.start;
                        var nextEnd = nextSession.end ?? DateTime.UtcNow;

                        // Check if sessions overlap or are adjacent (within 1 second)
                        if (nextStart <= currentMerged.end.AddSeconds(1))
                        {
                            // Merge: extend end time if necessary
                            var mergedEnd = nextEnd > currentMerged.end ? nextEnd : currentMerged.end;
                            var mergedDuration = (long)(mergedEnd - currentMerged.start).TotalMilliseconds;
                            currentMerged = (start: currentMerged.start, end: mergedEnd, durationMs: mergedDuration);
                        }
                        else
                        {
                            // Gap detected - save current merged session and start a new one
                            mergedSessions.Add(currentMerged);
                            currentMerged = (start: nextStart, end: nextEnd, durationMs: nextSession.durationMs);
                        }
                    }

                    // Add the last merged session
                    mergedSessions.Add(currentMerged);

                    // Calculate totals
                    var totalDurationMs = mergedSessions.Sum(s => s.durationMs);
                    var firstUse = mergedSessions.First().start;
                    var lastUse = mergedSessions.Last().end;

                    var summary = new DailyAppSummary
                    {
                        AppName = appName,
                        SummaryDate = targetDate,
                        TotalUsageMs = totalDurationMs,
                        UsageCount = mergedSessions.Count,
                        FirstUse = firstUse,
                        LastUse = lastUse
                    };

                    summaries.Add(summary);

                    _logger.LogInformation(
                        $"Aggregated {appName}: {sessionList.Count} sessions merged to {mergedSessions.Count}, " +
                        $"total time: {TimeSpan.FromMilliseconds(totalDurationMs).TotalSeconds:F2}s"
                    );
                }

                return summaries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to aggregate daily usage for {date:yyyy-MM-dd}");
                throw;
            }
        }
    }

    /// <summary>
    /// Implementation of IDailySystemSummaryRepository using Dapper ORM.
    /// </summary>
    public class DailySystemSummaryRepository : IDailySystemSummaryRepository
    {
        private readonly DatabaseContext _databaseContext;
        private readonly ILogger<DailySystemSummaryRepository> _logger;

        public DailySystemSummaryRepository(DatabaseContext databaseContext, ILogger<DailySystemSummaryRepository> logger)
        {
            _databaseContext = databaseContext;
            _logger = logger;
        }

        public async Task<int> CreateSummaryAsync(DailySystemSummary summary)
        {
            try
            {
                var connection = _databaseContext.GetConnection();
                const string sql = @"INSERT INTO daily_system_summaries 
                    (summary_date, average_cpu_usage, peak_cpu_usage, average_memory_mb, peak_memory_mb, total_disk_read_gb, total_disk_write_gb)
                    VALUES (@SummaryDate, @AverageCpuUsage, @PeakCpuUsage, @AverageMemoryMb, @PeakMemoryMb, @TotalDiskReadGb, @TotalDiskWriteGb)";

                var result = await connection.ExecuteAsync(sql, new
                {
                    summary.SummaryDate,
                    summary.AverageCpuUsage,
                    summary.PeakCpuUsage,
                    summary.AverageMemoryMb,
                    summary.PeakMemoryMb,
                    summary.TotalDiskReadGb,
                    summary.TotalDiskWriteGb
                });
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create daily system summary");
                throw;
            }
        }

        public async Task<DailySystemSummary?> GetSummaryByDateAsync(DateTime date)
        {
            try
            {
                var connection = _databaseContext.GetConnection();
                const string sql = "SELECT * FROM daily_system_summaries WHERE summary_date = @Date";
                var summary = await connection.QueryFirstOrDefaultAsync<DailySystemSummary>(sql, new { Date = date });
                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get daily system summary for {date:yyyy-MM-dd}");
                throw;
            }
        }

        public async Task<List<DailySystemSummary>> GetSummariesAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var connection = _databaseContext.GetConnection();
                const string sql = @"SELECT * FROM daily_system_summaries 
                    WHERE summary_date >= @StartDate 
                    AND summary_date < @EndDate 
                    ORDER BY summary_date";
                var summaries = (await connection.QueryAsync<DailySystemSummary>(sql, new
                {
                    StartDate = startDate,
                    EndDate = endDate
                })).ToList();
                return summaries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get daily system summaries");
                throw;
            }
        }

        public async Task<DailySystemSummary?> GetLatestSummaryAsync()
        {
            try
            {
                var connection = _databaseContext.GetConnection();
                const string sql = "SELECT * FROM daily_system_summaries ORDER BY id DESC LIMIT 1";
                var summary = await connection.QueryFirstOrDefaultAsync<DailySystemSummary>(sql);
                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get latest daily system summary");
                throw;
            }
        }

        public async Task UpdateSummaryAsync(DailySystemSummary summary)
        {
            try
            {
                var connection = _databaseContext.GetConnection();
                const string sql = @"UPDATE daily_system_summaries 
                    SET summary_date = @SummaryDate, average_cpu_usage = @AverageCpuUsage, 
                        peak_cpu_usage = @PeakCpuUsage, average_memory_mb = @AverageMemoryMb, 
                        peak_memory_mb = @PeakMemoryMb, total_disk_read_gb = @TotalDiskReadGb, 
                        total_disk_write_gb = @TotalDiskWriteGb
                    WHERE id = @Id";
                await connection.ExecuteAsync(sql, new
                {
                    summary.Id,
                    summary.SummaryDate,
                    summary.AverageCpuUsage,
                    summary.PeakCpuUsage,
                    summary.AverageMemoryMb,
                    summary.PeakMemoryMb,
                    summary.TotalDiskReadGb,
                    summary.TotalDiskWriteGb
                });
                _logger.LogInformation($"Updated daily system summary with ID {summary.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to update daily system summary with ID {summary.Id}");
                throw;
            }
        }

        public async Task DeleteSummaryAsync(int id)
        {
            try
            {
                var connection = _databaseContext.GetConnection();
                const string sql = "DELETE FROM daily_system_summaries WHERE id = @Id";
                await connection.ExecuteAsync(sql, new { Id = id });
                _logger.LogInformation($"Deleted daily system summary with ID {id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete daily system summary with ID {id}");
                throw;
            }
        }
    }
}
