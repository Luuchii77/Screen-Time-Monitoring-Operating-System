using System.Diagnostics;

namespace ScreenTimeMonitor.Service.Utilities;

/// <summary>
/// Manages Windows Event Log setup and operations for the Screen Time Monitor service
/// </summary>
public static class EventLogSetup
{
    private const string EventSourceName = "ScreenTimeMonitor";
    private const string EventLogName = "Application";
    
    /// <summary>
    /// Ensures the event source is registered in Windows Event Log
    /// Must be called with administrator privileges
    /// </summary>
    public static void EnsureEventSourceExists()
    {
        try
        {
            if (!EventLog.SourceExists(EventSourceName))
            {
                EventLog.CreateEventSource(EventSourceName, EventLogName);
            }
        }
        catch (Exception ex)
        {
            // If we can't register event source, log to console instead
            Console.WriteLine($"Warning: Could not register event source: {ex.Message}");
        }
    }

    /// <summary>
    /// Writes an informational event to the Windows Event Log
    /// </summary>
    public static void WriteInformationEvent(string message)
    {
        WriteEvent(message, EventLogEntryType.Information);
    }

    /// <summary>
    /// Writes a warning event to the Windows Event Log
    /// </summary>
    public static void WriteWarningEvent(string message)
    {
        WriteEvent(message, EventLogEntryType.Warning);
    }

    /// <summary>
    /// Writes an error event to the Windows Event Log
    /// </summary>
    public static void WriteErrorEvent(string message, Exception? ex = null)
    {
        var fullMessage = ex != null 
            ? $"{message}\n\nException: {ex.GetType().Name}\nMessage: {ex.Message}\n\nStackTrace: {ex.StackTrace}"
            : message;
        
        WriteEvent(fullMessage, EventLogEntryType.Error);
    }

    /// <summary>
    /// Writes a generic event to the Windows Event Log
    /// </summary>
    private static void WriteEvent(string message, EventLogEntryType entryType)
    {
        try
        {
            if (!EventLog.SourceExists(EventSourceName))
            {
                return; // Event source not registered, skip silently
            }

            using (var eventLog = new EventLog(EventLogName))
            {
                eventLog.Source = EventSourceName;
                
                // Truncate message if too long (Windows Event Log has a 32KB limit per event)
                if (message.Length > 30000)
                {
                    message = message.Substring(0, 30000) + "\n... [truncated]";
                }

                eventLog.WriteEntry(message, entryType);
            }
        }
        catch
        {
            // Silently ignore event log write failures
        }
    }

    /// <summary>
    /// Removes the event source from Windows Event Log (requires admin privileges)
    /// Used for uninstallation
    /// </summary>
    public static void RemoveEventSource()
    {
        try
        {
            if (EventLog.SourceExists(EventSourceName))
            {
                EventLog.DeleteEventSource(EventSourceName);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not remove event source: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the event source name for use with EventLog operations
    /// </summary>
    public static string GetEventSourceName() => EventSourceName;
}
