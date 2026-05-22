namespace ScreenTimeMonitor.Service.Models;

/// <summary>
/// Represents a single application usage session
/// </summary>
public class AppUsageSession
{
    /// <summary>
    /// Unique identifier for the session
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Process ID of the application
    /// </summary>
    public int ProcessId { get; set; }

    /// <summary>
    /// Name of the application (executable name)
    /// </summary>
    public required string AppName { get; set; }

    /// <summary>
    /// Window title of the application
    /// </summary>
    public string? WindowTitle { get; set; }

    /// <summary>
    /// When the application came into focus
    /// </summary>
    public DateTime SessionStart { get; set; }

    /// <summary>
    /// When the application lost focus
    /// </summary>
    public DateTime? SessionEnd { get; set; }

    /// <summary>
    /// Duration in milliseconds
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// When this record was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Get the duration as a TimeSpan
    /// </summary>
    public TimeSpan Duration => TimeSpan.FromMilliseconds(DurationMs);

    /// <summary>
    /// Calculate duration from start and end times
    /// </summary>
    public void CalculateDuration()
    {
        if (SessionEnd.HasValue)
        {
            DurationMs = (long)(SessionEnd.Value - SessionStart).TotalMilliseconds;
        }
    }
}

/// <summary>
/// Represents system metrics at a point in time
/// </summary>
public class SystemMetric
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// When this metric was recorded
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// CPU usage percentage (0-100)
    /// </summary>
    public decimal CpuUsage { get; set; }

    /// <summary>
    /// Available memory in MB
    /// </summary>
    public long MemoryUsageMb { get; set; }

    /// <summary>
    /// Memory usage percentage (0-100)
    /// </summary>
    public decimal MemoryPercent { get; set; }

    /// <summary>
    /// Disk read bytes in this interval
    /// </summary>
    public long DiskReadBytes { get; set; }

    /// <summary>
    /// Disk write bytes in this interval
    /// </summary>
    public long DiskWriteBytes { get; set; }

    /// <summary>
    /// Process ID if tracking specific process (optional)
    /// </summary>
    public int? ProcessId { get; set; }
}

/// <summary>
/// Daily aggregated application usage statistics
/// </summary>
public class DailyAppSummary
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Date of this summary
    /// </summary>
    public DateTime SummaryDate { get; set; }

    /// <summary>
    /// Application name
    /// </summary>
    public required string AppName { get; set; }

    /// <summary>
    /// Total time spent on app in milliseconds
    /// </summary>
    public long TotalUsageMs { get; set; }

    /// <summary>
    /// Number of times app was used
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// First time app was used on this day
    /// </summary>
    public DateTime FirstUse { get; set; }

    /// <summary>
    /// Last time app was used on this day
    /// </summary>
    public DateTime LastUse { get; set; }

    /// <summary>
    /// Get total usage as TimeSpan
    /// </summary>
    public TimeSpan TotalUsage => TimeSpan.FromMilliseconds(TotalUsageMs);
}

/// <summary>
/// Daily aggregated system statistics
/// </summary>
public class DailySystemSummary
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Date of this summary
    /// </summary>
    public DateTime SummaryDate { get; set; }

    /// <summary>
    /// Average CPU usage for the day
    /// </summary>
    public decimal AverageCpuUsage { get; set; }

    /// <summary>
    /// Peak CPU usage for the day
    /// </summary>
    public decimal PeakCpuUsage { get; set; }

    /// <summary>
    /// Average memory usage in MB
    /// </summary>
    public long AverageMemoryMb { get; set; }

    /// <summary>
    /// Peak memory usage in MB
    /// </summary>
    public long PeakMemoryMb { get; set; }

    /// <summary>
    /// Total disk read in GB
    /// </summary>
    public decimal TotalDiskReadGb { get; set; }

    /// <summary>
    /// Total disk write in GB
    /// </summary>
    public decimal TotalDiskWriteGb { get; set; }
}

/// <summary>
/// Event model for data collection
/// </summary>
public class AppUsageEvent
{
    /// <summary>
    /// Application name
    /// </summary>
    public required string AppName { get; set; }

    /// <summary>
    /// When the event occurred
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Process ID
    /// </summary>
    public int ProcessId { get; set; }

    /// <summary>
    /// Window title
    /// </summary>
    public string? WindowTitle { get; set; }

    /// <summary>
    /// Event type
    /// </summary>
    public AppEventType EventType { get; set; }

    /// <summary>
    /// Startup sequence order (if during boot)
    /// </summary>
    public int? BootSequenceOrder { get; set; }
}

/// <summary>
/// Types of app events
/// </summary>
public enum AppEventType
{
    FocusChange,
    SessionStart,
    SessionEnd
}
