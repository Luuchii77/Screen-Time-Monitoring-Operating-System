namespace ScreenTimeMonitor.Service.Utilities;

using System.Runtime.InteropServices;

/// <summary>
/// P/Invoke declarations for Windows API calls
/// </summary>
public static class PInvokeDeclarations
{
    #region Window Management

    /// <summary>
    /// Gets the window handle of the foreground window
    /// </summary>
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    /// <summary>
    /// Gets the window text (title) for a given window handle
    /// </summary>
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, [Out] System.Text.StringBuilder lpString, int nMaxCount);

    /// <summary>
    /// Gets the process ID for a given window handle
    /// </summary>
    [DllImport("user32.dll")]
    public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);

    #endregion

    #region Window Hooks

    /// <summary>
    /// Sets a window event hook
    /// </summary>
    [DllImport("user32.dll")]
    public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventHook,
        WinEventDelegate lpfnWinEventHook, uint idProcess, uint idThread, uint dwFlags);

    /// <summary>
    /// Unhooks a window event
    /// </summary>
    [DllImport("user32.dll")]
    public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    /// <summary>
    /// Delegate for window event hook callback
    /// </summary>
    public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject,
        int idChild, uint dwEventThread, uint dwmsEventTime);

    // Event types
    public const uint EVENT_SYSTEM_FOREGROUND = 3;

    // Hook flags
    public const uint EVENT_OBJECT_NAMECHANGE = 11001;

    // Hook installation flags
    public const uint WINEVENT_OUTOFCONTEXT = 0;

    #endregion

    #region Memory Information

    /// <summary>
    /// Gets memory status information (deprecated, use GlobalMemoryStatusEx instead)
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    [Obsolete("Use GlobalMemoryStatusEx instead")]
    public static extern bool GlobalMemoryStatus(ref MEMORYSTATUS lpBuffer);

    /// <summary>
    /// Gets extended memory status information (recommended)
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    /// <summary>
    /// Memory status structure (deprecated)
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORYSTATUS
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong dwTotalPhys;
        public ulong dwAvailPhys;
        public ulong dwTotalPageFile;
        public ulong dwAvailPageFile;
        public ulong dwTotalVirtual;
        public ulong dwAvailVirtual;
    }

    /// <summary>
    /// Extended memory status structure (recommended)
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    #endregion

    #region Process Information

    /// <summary>
    /// Gets the module file name for a given process ID
    /// </summary>
    [DllImport("kernel32.dll")]
    public static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule,
        [Out] System.Text.StringBuilder lpBaseName, [In][MarshalAs(UnmanagedType.U4)] int nSize);

    /// <summary>
    /// Opens a process with specified access rights
    /// </summary>
    [DllImport("kernel32.dll")]
    public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

    /// <summary>
    /// Closes a process handle
    /// </summary>
    [DllImport("kernel32.dll")]
    public static extern bool CloseHandle(IntPtr hHandle);

    // Process access flags
    public const uint PROCESS_QUERY_INFORMATION = 0x0400;
    public const uint PROCESS_VM_READ = 0x0010;

    #endregion

    #region Drive Information

    /// <summary>
    /// Gets drive information
    /// </summary>
    [DllImport("kernel32.dll")]
    public static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
        out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

    #endregion
}

/// <summary>
/// Utility class for Windows API operations
/// </summary>
public static class WindowsApiHelper
{
    /// <summary>
    /// Gets the name of the currently active application
    /// </summary>
    public static string GetActiveApplicationName()
    {
        try
        {
            IntPtr hwnd = PInvokeDeclarations.GetForegroundWindow();
            PInvokeDeclarations.GetWindowThreadProcessId(hwnd, out int processId);

            if (processId == 0)
                return "Unknown";

            try
            {
                var process = System.Diagnostics.Process.GetProcessById(processId);
                return process.ProcessName;
            }
            catch
            {
                return "Unknown";
            }
        }
        catch
        {
            return "Unknown";
        }
    }

    /// <summary>
    /// Gets the window title of the currently active window
    /// </summary>
    public static string GetActiveWindowTitle()
    {
        try
        {
            IntPtr hwnd = PInvokeDeclarations.GetForegroundWindow();
            var sb = new System.Text.StringBuilder(256);
            PInvokeDeclarations.GetWindowText(hwnd, sb, 256);
            return sb.ToString();
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Gets the process ID of the active window
    /// </summary>
    public static int GetActiveProcessId()
    {
        try
        {
            IntPtr hwnd = PInvokeDeclarations.GetForegroundWindow();
            PInvokeDeclarations.GetWindowThreadProcessId(hwnd, out int processId);
            return processId;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Gets free disk space in GB
    /// </summary>
    public static decimal GetFreeDiskSpaceGb(string drivePath)
    {
        try
        {
            if (PInvokeDeclarations.GetDiskFreeSpaceEx(drivePath, out ulong freeBytesAvailable,
                out ulong totalBytes, out ulong totalFreeBytes))
            {
                return (decimal)totalFreeBytes / (1024 * 1024 * 1024);
            }
        }
        catch
        { }

        return 0;
    }

    /// <summary>
    /// Gets total disk space in GB
    /// </summary>
    public static decimal GetTotalDiskSpaceGb(string drivePath)
    {
        try
        {
            if (PInvokeDeclarations.GetDiskFreeSpaceEx(drivePath, out ulong freeBytesAvailable,
                out ulong totalBytes, out ulong totalFreeBytes))
            {
                return (decimal)totalBytes / (1024 * 1024 * 1024);
            }
        }
        catch
        { }

        return 0;
    }
}
