using ScreenTimeMonitor.Service.Models;

namespace ScreenTimeMonitor.Service.Database
{
    /// <summary>
    /// Repository interface for AppUsageSession entities.
    /// </summary>
    public interface IAppUsageRepository
    {
        /// <summary>Creates a new app usage session.</summary>
        /// <param name="session">The session to create.</param>
        Task<int> CreateSessionAsync(AppUsageSession session);
        
        /// <summary>Gets a session by ID.</summary>
        /// <param name="id">The session ID.</param>
        Task<AppUsageSession?> GetSessionByIdAsync(int id);
        
        /// <summary>Gets all sessions for a specific date.</summary>
        /// <param name="date">The date to query.</param>
        Task<List<AppUsageSession>> GetSessionsByDateAsync(DateTime date);
        
        /// <summary>Gets sessions for a specific app within a date range.</summary>
        /// <param name="appName">The application name.</param>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        Task<List<AppUsageSession>> GetSessionsByAppAsync(string appName, DateTime startDate, DateTime endDate);
        
        /// <summary>Gets all currently active sessions.</summary>
        Task<List<AppUsageSession>> GetActiveSessionsAsync();
        
        /// <summary>Updates an existing session.</summary>
        /// <param name="session">The session to update.</param>
        Task UpdateSessionAsync(AppUsageSession session);
        
        /// <summary>Deletes a session by ID.</summary>
        /// <param name="id">The session ID.</param>
        Task DeleteSessionAsync(int id);
        
        /// <summary>
        /// Gets the total accumulated time for an app across all historical sessions (excluding current session).
        /// </summary>
        Task<long> GetAppHistoricalTotalAsync(string appName);
        
        /// <summary>
        /// Gets all past sessions for an app.
        /// </summary>
        Task<List<AppUsageSession>> GetAppSessionHistoryAsync(string appName, DateTime beforeDate);
    }

    /// <summary>
    /// Repository interface for SystemMetric entities.
    /// </summary>
    public interface ISystemMetricsRepository
    {
        Task<int> CreateMetricAsync(SystemMetric metric);
        Task<SystemMetric?> GetMetricByIdAsync(int id);
        Task<List<SystemMetric>> GetMetricsByDateAsync(DateTime date);
        Task<List<SystemMetric>> GetMetricsAsync(DateTime startDate, DateTime endDate);
        Task<List<SystemMetric>> GetLatestMetricsAsync(int count);
        Task DeleteMetricsBeforeDateAsync(DateTime date);
    }

    /// <summary>
    /// Repository interface for DailyAppSummary entities.
    /// </summary>
    public interface IDailyAppSummaryRepository
    {
        Task<int> CreateSummaryAsync(DailyAppSummary summary);
        Task<DailyAppSummary?> GetSummaryAsync(string appName, DateTime date);
        Task<List<DailyAppSummary>> GetSummariesByDateAsync(DateTime date);
        Task<List<DailyAppSummary>> GetSummariesByAppAsync(string appName, int daysBack);
        Task<List<DailyAppSummary>> GetAllSummariesAsync(DateTime startDate, DateTime endDate);
        Task UpdateSummaryAsync(DailyAppSummary summary);
        Task DeleteSummaryAsync(int id);
        
        /// <summary>
        /// Aggregates app usage sessions for a date, deduplicating overlapping sessions.
        /// Merges sessions for the same app to prevent double-counting time.
        /// </summary>
        Task<List<DailyAppSummary>> AggregateDailyUsageAsync(DateTime date);
    }

    /// <summary>
    /// Repository interface for DailySystemSummary entities.
    /// </summary>
    public interface IDailySystemSummaryRepository
    {
        Task<int> CreateSummaryAsync(DailySystemSummary summary);
        Task<DailySystemSummary?> GetSummaryByDateAsync(DateTime date);
        Task<List<DailySystemSummary>> GetSummariesAsync(DateTime startDate, DateTime endDate);
        Task<DailySystemSummary?> GetLatestSummaryAsync();
        Task UpdateSummaryAsync(DailySystemSummary summary);
        Task DeleteSummaryAsync(int id);
    }
}
