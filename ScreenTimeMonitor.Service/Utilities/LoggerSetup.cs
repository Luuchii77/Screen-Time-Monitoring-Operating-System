namespace ScreenTimeMonitor.Service.Utilities;

using Microsoft.Extensions.Logging;
using System.IO;

/// <summary>
/// Logging utility for the service
/// </summary>
public class LoggerSetup
{
    /// <summary>
    /// Configures file logging
    /// </summary>
    public static void ConfigureFileLogging(string logDirectory)
    {
        try
        {
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            // Cleanup old log files (keep last 30 days)
            CleanupOldLogs(logDirectory);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to configure logging: {ex.Message}");
        }
    }

    /// <summary>
    /// Removes log files older than 30 days
    /// </summary>
    private static void CleanupOldLogs(string logDirectory)
    {
        try
        {
            var di = new DirectoryInfo(logDirectory);
            var oldFiles = di.GetFiles("*.log")
                .Where(f => DateTime.Now - f.CreationTime > TimeSpan.FromDays(30))
                .ToList();

            foreach (var file in oldFiles)
            {
                try
                {
                    file.Delete();
                }
                catch
                {
                    // Ignore errors during cleanup
                }
            }
        }
        catch
        {
            // Ignore errors during cleanup
        }
    }

    /// <summary>
    /// Gets the path for today's log file
    /// </summary>
    public static string GetLogFilePath(string logDirectory)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd");
        return Path.Combine(logDirectory, $"ScreenTimeMonitor_{timestamp}.log");
    }
}

/// <summary>
/// Extension methods for ILogger
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Logs startup initialization
    /// </summary>
    public static void LogStartup(this ILogger logger, string message)
    {
        logger.LogInformation($"[STARTUP] {message}");
    }

    /// <summary>
    /// Logs shutdown information
    /// </summary>
    public static void LogShutdown(this ILogger logger, string message)
    {
        logger.LogWarning($"[SHUTDOWN] {message}");
    }

    /// <summary>
    /// Logs monitoring information
    /// </summary>
    public static void LogMonitoring(this ILogger logger, string message)
    {
        logger.LogDebug($"[MONITORING] {message}");
    }

    /// <summary>
    /// Logs database operations
    /// </summary>
    public static void LogDatabase(this ILogger logger, string message)
    {
        logger.LogDebug($"[DATABASE] {message}");
    }

    /// <summary>
    /// Logs queue operations
    /// </summary>
    public static void LogQueue(this ILogger logger, string message)
    {
        logger.LogDebug($"[QUEUE] {message}");
    }
}
