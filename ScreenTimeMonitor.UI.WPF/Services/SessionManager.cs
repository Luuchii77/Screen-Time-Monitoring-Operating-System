using System;
using System.Collections.Generic;

namespace ScreenTimeMonitor.UI.WPF.Services
{
    /// <summary>
    /// Manages session tracking, including session ID, connection state, and timestamps.
    /// </summary>
    public class SessionManager
    {
        private static int _sessionCounter = 1;
        
        public int SessionID { get; private set; }
        public DateTime SessionStartTime { get; private set; }
        public bool IsConnected { get; set; }
        public string SessionName => $"ScreenMonitorActivity {SessionID}";
        public TimeSpan SessionDuration => DateTime.Now - SessionStartTime;

        public SessionManager()
        {
            SessionID = _sessionCounter++;
            SessionStartTime = DateTime.Now;
            IsConnected = false;
        }

        /// <summary>
        /// Resets the session counter (useful for testing or app restart)
        /// </summary>
        public static void ResetCounter()
        {
            _sessionCounter = 1;
        }

        /// <summary>
        /// Gets the session counter value
        /// </summary>
        public static int GetCurrentSessionCount()
        {
            return _sessionCounter;
        }

        /// <summary>
        /// Gets the connection status display string
        /// </summary>
        public string GetStatusDisplay()
        {
            return IsConnected ? "● Connected" : "○ Disconnected";
        }

        /// <summary>
        /// Gets the connection status color (for UI binding)
        /// </summary>
        public string GetStatusColor()
        {
            return IsConnected ? "#00FF00" : "#808080"; // Green for connected, Gray for disconnected
        }
    }
}
