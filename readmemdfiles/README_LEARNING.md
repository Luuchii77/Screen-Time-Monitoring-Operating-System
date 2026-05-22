# Screen Time Monitor - Technical Learning & Implementation Guide

## 📚 Complete Technical Documentation for Group Study

This document provides comprehensive technical details, implementation explanations, and code walkthroughs for understanding and learning from the Screen Time Monitor project.

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Project Structure](#project-structure)
3. [Core Components](#core-components)
4. [Data Flow](#data-flow)
5. [Implementation Details](#implementation-details)
6. [Database Design](#database-design)
7. [IPC Communication](#ipc-communication)
8. [Key Algorithms](#key-algorithms)
9. [Code Walkthrough](#code-walkthrough)
10. [Common Patterns Used](#common-patterns-used)
11. [Error Handling](#error-handling)
12. [Testing Strategy](#testing-strategy)

---

## Architecture Overview

### System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                 Windows Operating System                     │
│  (Kernel, Process Management, Window Management)             │
└──────────┬──────────────────────────────────────┬────────────┘
           │                                       │
     ┌─────▼──────────┐                   ┌────────▼────────┐
     │  Background    │                   │  Running Apps   │
     │  Service       │◄──Windows API────►│  (Code, Edge,   │
     │  .NET 8.0      │  EnumWindows()    │   Excel, etc)   │
     │                │  GetWindowId()    │                 │
     └─────┬──────────┘  IsWindowVisible()└────────────────┘
           │
     ┌─────▼────────────────┐
     │   In-Memory Tracking │
     │   ProcessTracker<>   │  ← Stores: AppName, PID, Duration,
     │   Dictionary         │            SessionStart, Historical
     └─────┬────────────────┘
           │
     ┌─────▼──────────────────┐
     │  Named Pipe IPC        │  ← Request/Response Protocol
     │  (LocalHost\pipe\...)  │
     └─────┬──────────────────┘
           │
     ┌─────▼────────────┐
     │  WPF UI          │     ← Displays apps, durations,
     │  MainWindow      │        real-time updates
     └─────┬────────────┘
           │
     ┌─────▼──────────────────┐
     │  SQLite Database       │  ← Persistent storage
     │  (AppUsageSessions)    │     (historical data)
     └────────────────────────┘
```

### Architecture Pattern: Service + UI Separation

**Why Separate?**
- ✅ Service runs as background process (survives UI crashes)
- ✅ UI can be restarted without losing tracking data
- ✅ Service can run independently (automated environments)
- ✅ IPC allows inter-process communication securely

---

## Project Structure

### Directory Layout

```
ScreenTimeMonitor/
│
├── ScreenTimeMonitor.Service/                 [Background Service]
│   ├── Program.cs                             ← Entry point, DI setup
│   ├── appsettings.json                       ← Configuration
│   │
│   ├── Services/
│   │   ├── BackgroundProcessMonitorService.cs ← Core monitoring logic
│   │   ├── IpcService.cs                      ← Named pipe communication
│   │   └── ... other services
│   │
│   ├── Database/
│   │   ├── DatabaseContext.cs                 ← EF Core DbContext
│   │   ├── DatabaseInitializer.cs             ← Schema creation
│   │   ├── IRepositories.cs                   ← Data interfaces
│   │   └── Repositories.cs                    ← Data implementation
│   │
│   ├── Models/
│   │   ├── DomainModels.cs                    ← AppUsageSession, etc
│   │   └── ViewModels.cs                      ← DTO models
│   │
│   └── IPC/
│       └── ... IPC protocol handlers
│
├── ScreenTimeMonitor.UI.WPF/                  [Desktop Application]
│   ├── Program.cs                             ← Entry point, DI setup
│   ├── App.xaml / App.xaml.cs                 ← Application startup
│   ├── appsettings.json                       ← Configuration
│   │
│   ├── Views/
│   │   ├── MainWindow.xaml                    ← UI layout (XML-based)
│   │   └── MainWindow.xaml.cs                 ← UI code-behind (logic)
│   │
│   ├── ViewModels/
│   │   └── MainWindowViewModel.cs             ← UI state & commands
│   │
│   └── Services/
│       ├── IpcClientService.cs                ← Communicates with service
│       └── ... other services
│
├── ScreenTimeMonitor.Tests/                   [Unit & Integration Tests]
│   ├── IPCClientTests.cs
│   ├── ServiceDatabaseIntegrationTests.cs
│   └── ... other tests
│
├── Database/
│   ├── schema-sqlite.sql                      ← Table definitions
│   └── schema-postgresql.sql                  ← Alternative DB schema
│
└── Configuration Files
    ├── ScreenTimeMonitor.sln                  ← Solution file
    ├── .gitignore                             ← Git configuration
    ├── setup.ps1 & setup.bat                  ← Automated setup
    └── appsettings.json                       ← Root configuration
```

### File Size Reference

| File | Lines | Purpose |
|------|-------|---------|
| BackgroundProcessMonitorService.cs | ~400 | Core monitoring logic |
| IRepositories.cs + Repositories.cs | ~300 | Database operations |
| MainWindow.xaml + code-behind | ~200 | UI definition |
| DatabaseContext.cs | ~100 | EF Core configuration |
| IpcService.cs | ~150 | Inter-process communication |
| **Total (essential files)** | **~1200** | Core application |

---

## Core Components

### 1. BackgroundProcessMonitorService

**Purpose:** Continuously monitors running Windows processes and tracks their usage.

**Key Class: ProcessTracker**

```csharp
public class ProcessTracker
{
    public string AppName { get; set; }              // e.g., "Code.exe"
    public int ProcessId { get; set; }               // Windows PID
    public DateTime SessionStartTime { get; set; }   // When session started
    public long TotalDurationMs { get; set; }        // Cumulative uptime
    public long SessionStartDurationMs { get; set; } // Duration at session reset
    public long HistoricalTotalMs { get; set; }      // Past sessions total
}
```

**Key Methods:**

```csharp
// Main monitoring method (runs every ~1 second)
private async Task ScanBackgroundProcessesAsync()
{
    var processes = Process.GetProcesses();
    
    foreach (var process in processes)
    {
        if (HasVisibleWindow(process))
        {
            TrackProcess(process);
        }
    }
    
    RemoveClosedProcesses();
}

// Proper window detection using Windows API
private bool HasVisibleWindow(Process process)
{
    bool found = false;
    EnumWindows((hwnd, lParam) =>
    {
        GetWindowThreadProcessId(hwnd, out uint pid);
        if (pid == process.Id)
        {
            if (IsWindowVisible(hwnd) && GetWindow(hwnd, 4) == IntPtr.Zero)
            {
                found = true;
                return false;
            }
        }
        return true;
    }, IntPtr.Zero);
    return found;
}

// Calculate total duration including historical data
public long GetTotalDuration()
{
    return (TotalDurationMs - SessionStartDurationMs) + HistoricalTotalMs;
}
```

**Why This Approach?**
- Proper Windows API calls ensure accuracy
- Filters background processes (helpers, workers)
- Only tracks visible, user-facing windows
- Efficient enumeration of window handles

---

### 2. IpcService & IpcClientService

**Purpose:** Enable communication between Service (backend) and UI (frontend) using Named Pipes.

**Protocol Format:**

```
Request:  "COMMAND\n"
Response: "[JSON data]\n"

Example Requests:
- "GET_RUNNING_APPS\n"
- "UI_CONNECTED\n"
- "RESET_SESSION\n"

Example Response:
[
  { "AppName": "Code", "Duration": 3600000 },
  { "AppName": "Edge", "Duration": 1800000 },
  { "AppName": "Excel", "Duration": 900000 }
]
```

**Named Pipe Characteristics:**
- **Name:** `\\.\pipe\ScreenTimeMonitor_{VERSION}`
- **Type:** Byte stream (asynchronous)
- **Access:** Local machine only (security)
- **Latency:** <1ms typical

---

### 3. DatabaseContext & Repositories

**Purpose:** Data persistence layer using Entity Framework Core and SQLite.

**Main Entity: AppUsageSession**

```csharp
public class AppUsageSession
{
    public int SessionId { get; set; }
    public string AppName { get; set; }
    public int ProcessId { get; set; }
    public DateTime SessionStart { get; set; }
    public DateTime SessionEnd { get; set; }
    public long DurationMs { get; set; }
}
```

**Repository Methods:**

```csharp
// Save a completed session to database
public async Task CreateSessionAsync(AppUsageSession session)
{
    await _dbContext.AppUsageSessions.AddAsync(session);
    await _dbContext.SaveChangesAsync();
}

// Load historical total for an app
public async Task<long> GetAppHistoricalTotalAsync(string appName)
{
    return await _dbContext.AppUsageSessions
        .Where(s => s.AppName == appName && s.SessionEnd != null)
        .SumAsync(s => s.DurationMs);
}

// Retrieve all sessions for an app before a certain date
public async Task<List<AppUsageSession>> GetAppSessionHistoryAsync(
    string appName, DateTime beforeDate)
{
    return await _dbContext.AppUsageSessions
        .Where(s => s.AppName == appName && s.SessionStart < beforeDate)
        .OrderByDescending(s => s.SessionStart)
        .ToListAsync();
}
```

**Why EF Core?**
- Abstraction over SQL
- Type-safe queries (LINQ)
- Automatic schema migration
- Cross-platform database support

---

### 4. WPF UI (MainWindow)

**Purpose:** Display application usage statistics with real-time updates.

**XAML Structure:**

```xaml
<Window ...>
    <Grid>
        <DataGrid Name="RunningAppsDataGrid"
                  ItemsSource="{Binding RunningApps}"
                  SelectionMode="Single">
            <DataGrid.Columns>
                <DataGridTextColumn Header="Application"
                                    Binding="{Binding AppName}" />
                <DataGridTextColumn Header="Duration"
                                    Binding="{Binding DurationFormatted}" />
            </DataGrid.Columns>
        </DataGrid>
    </Grid>
</Window>
```

**Code-Behind Logic:**

```csharp
public partial class MainWindow : Window
{
    private IpcClientService _ipcClient;
    private DispatcherTimer _updateTimer;
    private MainWindowViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        
        // Initialize IPC communication
        _ipcClient = new IpcClientService();
        
        // Setup real-time update timer (1 second interval)
        _updateTimer = new DispatcherTimer();
        _updateTimer.Interval = TimeSpan.FromSeconds(1);
        _updateTimer.Tick += async (s, e) => await UpdateRunningApps();
        _updateTimer.Start();
    }

    private async Task UpdateRunningApps()
    {
        var apps = await _ipcClient.GetRunningAppsAsync();
        _viewModel.RunningApps = new ObservableCollection<AppViewModel>(apps);
    }
}
```

**Data Binding:**
- Uses WPF's **ObservableCollection** for real-time updates
- XAML bindings automatically refresh UI when collection changes
- DispatcherTimer triggers updates on UI thread

---

## Data Flow

### Complete Data Flow from Process to Display

```
1. PROCESS STARTS
   │
   └─► Windows OS creates new process
       └─► AssignProcessID (PID)

2. SERVICE DETECTS
   │
   ├─► ScanBackgroundProcessesAsync() runs
   │
   └─► HasVisibleWindow(Process) called
       ├─► EnumWindows() to find all windows
       ├─► GetWindowThreadProcessId() matches to PID
       ├─► IsWindowVisible() confirms visibility
       └─► Result: TRUE if visible window found

3. SERVICE TRACKS
   │
   └─► ProcessTracker added to Dictionary
       ├─► AppName: "Code"
       ├─► ProcessId: 12345
       ├─► SessionStartTime: 2025-01-10 09:00:00
       ├─► TotalDurationMs: 0 (increments per scan)
       └─► HistoricalTotalMs: 900000 (loaded from DB)

4. UI REQUESTS DATA
   │
   ├─► Timer triggers every 1 second
   │
   └─► IpcClient sends "GET_RUNNING_APPS\n"

5. SERVICE RESPONDS
   │
   ├─► IpcService handles request
   │
   ├─► GetAllRunningApps() collects ProcessTrackers
   │
   └─► Calculates: (TotalDurationMs - SessionStartDurationMs) + HistoricalTotalMs
       └─► JSON response sent back

6. UI UPDATES
   │
   ├─► DataGrid ItemsSource updated
   │
   ├─► XAML bindings refresh
   │
   └─► User sees:
       AppName: "Code"
       Duration: "00:15:30"  ← Human-readable format

7. PROCESS CLOSES
   │
   └─► Service detects process exit
       ├─► Calculates final duration
       ├─► Creates AppUsageSession record
       └─► Saves to database (SQLite)
           └─► SessionEnd: 2025-01-10 09:15:30
               DurationMs: 930000

8. NEXT SESSION
   │
   └─► User reopens same app
       ├─► Service loads from DB
       ├─► HistoricalTotalMs: 930000 (from closed session)
       ├─► New tracking begins
       └─► Total shown: new time + 930000ms
```

---

## Implementation Details

### How Window Detection Works (Critical Algorithm)

**Problem:** Track only visible user-facing windows, not background/helper processes.

**Solution:** Windows API approach

```csharp
[DllImport("user32.dll")]
private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

[DllImport("user32.dll", SetLastError = true)]
private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

[DllImport("user32.dll")]
private static extern bool IsWindowVisible(IntPtr hWnd);

[DllImport("user32.dll", ExactSpelling = true)]
private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
private const uint GW_OWNER = 4; // Get owner window

public bool HasVisibleWindow(Process process)
{
    bool found = false;
    
    // Enumerate all windows on desktop
    EnumWindows((hwnd, lParam) =>
    {
        // Match window to process PID
        GetWindowThreadProcessId(hwnd, out uint pid);
        
        if (pid == process.Id)
        {
            // Check if window is visible AND not a child window
            if (IsWindowVisible(hwnd) && GetWindow(hwnd, GW_OWNER) == IntPtr.Zero)
            {
                found = true;
                return false; // Stop enumeration
            }
        }
        return true; // Continue enumeration
    }, IntPtr.Zero);
    
    return found;
}
```

**Why This Works:**
1. **EnumWindows()** - Gets ALL window handles
2. **GetWindowThreadProcessId()** - Maps window to process PID
3. **IsWindowVisible()** - Filters out hidden windows
4. **GetWindow(hwnd, GW_OWNER)** - Excludes child windows (owned by other windows)
5. Returns **true only if** process has visible, top-level window

**Example: VS Code**
```
Process: Code.exe (PID 12345)
├─ Window 1: Main window (visible, owner=NULL) ✓ TRACKED
│   Matched by IsWindowVisible() && GetWindow()=NULL
│
├─ Helper 1: Internal helper (hidden) ✗ IGNORED
│   Failed IsWindowVisible() check
│
├─ Helper 2: Extension worker (owner=Main window) ✗ IGNORED
│   Failed GetWindow()=NULL check (it has owner)
```

---

### Session-Relative Time Tracking

**Problem:** When UI connects, we want to show only the time elapsed since connection, not total service uptime.

**Solution:** SessionStartDurationMs baseline approach

```csharp
// When UI connects (receives UI_CONNECTED message)
public void ResetSessionTracking()
{
    foreach (var tracker in _processTrackers.Values)
    {
        // Lock current duration as baseline
        tracker.SessionStartDurationMs = tracker.TotalDurationMs;
        
        // Time displayed = Current - Baseline
        // So if tracker has been running 1 hour and we reset:
        // Display = 1:00:00 - 1:00:00 = 0:00:00 ✓
    }
}

// When calculating duration for display
public long GetTotalDuration()
{
    // Current duration in this session
    long currentSession = TotalDurationMs - SessionStartDurationMs;
    
    // Add all historical sessions from database
    long historical = HistoricalTotalMs;
    
    // Total = Current session time + All past sessions
    return currentSession + historical;
    
    // Example:
    // TotalDurationMs: 5400000 (1.5 hours since service start)
    // SessionStartDurationMs: 3600000 (locked when UI connected)
    // HistoricalTotalMs: 7200000 (2 hours from past sessions in DB)
    // Display: (5400000 - 3600000) + 7200000 = 9000000ms = 2.5 hours ✓
}
```

**Why Session Relative?**
- Service runs 24/7, but we want UI session time
- When UI reconnects, show time since last connection
- Separate from total accumulated time
- Cleaner user experience

---

### Cumulative Time Calculation

**Problem:** After closing and reopening an app, show total time (past + current).

**Solution:** Historical tracking in database

```csharp
// When process CLOSES
private async Task SaveSessionAsync(ProcessTracker tracker)
{
    var session = new AppUsageSession
    {
        AppName = tracker.AppName,
        ProcessId = tracker.ProcessId,
        SessionStart = tracker.SessionStartTime,
        SessionEnd = DateTime.Now,
        DurationMs = tracker.TotalDurationMs - tracker.SessionStartDurationMs
    };
    
    await _appUsageRepository.CreateSessionAsync(session);
    // Example: Code closed after 30 minutes
    // Record saved to DB with DurationMs = 1800000ms
}

// When process STARTS (new or reopened)
private async Task TrackProcessAsync(Process process)
{
    var tracker = new ProcessTracker
    {
        AppName = process.ProcessName,
        ProcessId = process.Id,
        SessionStartTime = DateTime.Now,
        TotalDurationMs = 0,
        SessionStartDurationMs = 0,
        
        // Load historical total from all past sessions
        HistoricalTotalMs = await _appUsageRepository
            .GetAppHistoricalTotalAsync(process.ProcessName)
    };
    
    _processTrackers[process.Id] = tracker;
    // Example: Code reopened after 2 hours
    // HistoricalTotalMs loaded = 1800000ms (from previous session)
}

// When DISPLAYING duration
public Dictionary<string, long> GetAllRunningApps()
{
    var result = new Dictionary<string, long>();
    
    foreach (var tracker in _processTrackers.Values)
    {
        // Group by app name (case-insensitive)
        string key = tracker.AppName.ToLower();
        
        long currentDuration = 
            (tracker.TotalDurationMs - tracker.SessionStartDurationMs);
        long totalDuration = 
            currentDuration + tracker.HistoricalTotalMs;
        
        if (!result.ContainsKey(key))
            result[key] = 0;
        
        result[key] += totalDuration;
    }
    
    return result;
    // Example output:
    // { "code": 7200000 }  // 2 hours: 30min current + 1h30min historical
}
```

---

### Real-Time UI Updates

**Problem:** Keep UI synchronized with service in real-time.

**Solution:** Timer + IPC polling

```csharp
// In MainWindow.cs
public MainWindow()
{
    InitializeComponent();
    
    // Create timer that fires every 1000ms (1 second)
    var updateTimer = new DispatcherTimer
    {
        Interval = TimeSpan.FromSeconds(1)
    };
    
    updateTimer.Tick += async (s, e) =>
    {
        // Send request to service for current app list
        var runningApps = await _ipcClient.GetRunningAppsAsync();
        
        // Update UI with new data
        // This triggers WPF's data binding refresh
        UpdateDataGrid(runningApps);
    };
    
    updateTimer.Start();
}

// In DataGrid, each cell binds to model properties
// When model changes, binding automatically refreshes display
<DataGrid ItemsSource="{Binding RunningApps}">
    <DataGridTextColumn Binding="{Binding DurationFormatted}" />
</DataGrid>
```

**Why 1 Second Interval?**
- Provides smooth visible updates (human perception)
- Balances accuracy vs system load
- Matches typical stopwatch/timer expectations
- Low CPU impact (~1% usage)

---

## Database Design

### Schema: AppUsageSessions Table

```sql
CREATE TABLE AppUsageSessions (
    SessionId      INTEGER PRIMARY KEY AUTOINCREMENT,
    AppName        TEXT NOT NULL,
    ProcessId      INTEGER,
    SessionStart   DATETIME NOT NULL,
    SessionEnd     DATETIME,
    DurationMs     INTEGER NOT NULL
);
```

### Sample Data

```sql
-- Session 1: Code used for 30 minutes
INSERT INTO AppUsageSessions VALUES
(1, 'Code', 12345, '2025-01-10 09:00:00', '2025-01-10 09:30:00', 1800000);

-- Session 2: Code used again for 45 minutes  
INSERT INTO AppUsageSessions VALUES
(2, 'Code', 12350, '2025-01-10 14:00:00', '2025-01-10 14:45:00', 2700000);

-- Session 3: Chrome used for 20 minutes
INSERT INTO AppUsageSessions VALUES
(3, 'Chrome', 23456, '2025-01-10 09:35:00', '2025-01-10 09:55:00', 1200000);
```

### Queries Used in Application

```csharp
// Get historical total for an app
var historicalTotal = await db.AppUsageSessions
    .Where(s => s.AppName == "Code" && s.SessionEnd != null)
    .SumAsync(s => s.DurationMs);
// Returns: 1800000 + 2700000 = 4500000ms (1.25 hours)

// Get all sessions for an app (for analytics)
var sessions = await db.AppUsageSessions
    .Where(s => s.AppName == "Code")
    .OrderByDescending(s => s.SessionStart)
    .ToListAsync();
// Returns: [Session 2, Session 1]

// Get sessions from a specific date
var todaySessions = await db.AppUsageSessions
    .Where(s => s.SessionStart >= DateTime.Today)
    .ToListAsync();
```

### Database Growth

```
Typical Usage: 8 hours/day, 50 apps tracked
Average session: 10 minutes

Sessions per day:
50 apps × (8 hours × 60 minutes / 10 minutes) = 50 × 48 = 2400 sessions/day

Database size growth:
- Per session: ~200 bytes
- Per day: 2400 × 200 = 480 KB
- Per month: 480 KB × 30 = 14.4 MB
- Per year: 14.4 MB × 12 = 172.8 MB

Typical: <1 MB per month, <15 MB per year
```

---

## IPC Communication

### Named Pipe Protocol

**Connection Flow:**

```
┌─────────────────────────────────────────┐
│      Client (UI)          Server (SVC)  │
├─────────────────────────────────────────┤
│                                         │
│  1. Connect to pipe                     │
│  └─────────────────────────────────────→│
│                                         │
│                       Accept connection │
│  2. Send "UI_CONNECTED\n"               │
│  ├────────────────────────────────────→│
│                                         │
│                 Receive "UI_CONNECTED"  │
│                 Save baseline duration  │
│  3. Wait for response                   │
│  │←────────────────────────────────────┤
│  │   (acknowledgement or data)          │
│  │                                      │
```

### Request-Response Examples

**Request 1: Get Running Apps**
```
REQUEST:
GET_RUNNING_APPS\n

RESPONSE:
[{"AppName":"Code","DurationMs":3600000},{"AppName":"Edge","DurationMs":1800000}]\n
```

**Request 2: UI Connection**
```
REQUEST:
UI_CONNECTED\n

RESPONSE:
{}\n
(Empty response, service resets session baseline)
```

### Async Named Pipe Implementation

```csharp
public class IpcService
{
    private NamedPipeServerStream _pipeServer;
    
    public async Task StartAsync()
    {
        _pipeServer = new NamedPipeServerStream(
            "ScreenTimeMonitor_v1",
            PipeDirection.InOut,
            NamedPipeServerStream.MaxAllowedServerInstances);
        
        while (true)
        {
            // Wait for client connection
            await _pipeServer.WaitForConnectionAsync();
            
            // Handle client in background
            _ = HandleClientAsync(_pipeServer);
            
            // Create new pipe for next client
            _pipeServer = new NamedPipeServerStream(
                "ScreenTimeMonitor_v1",
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances);
        }
    }
    
    private async Task HandleClientAsync(NamedPipeServerStream pipe)
    {
        using (var reader = new StreamReader(pipe))
        using (var writer = new StreamWriter(pipe))
        {
            try
            {
                // Read request
                string request = await reader.ReadLineAsync();
                
                // Process request
                string response = ProcessRequest(request);
                
                // Send response
                await writer.WriteLineAsync(response);
                await writer.FlushAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"IPC error: {ex.Message}");
            }
        }
    }
    
    private string ProcessRequest(string request)
    {
        if (request == "GET_RUNNING_APPS")
        {
            var apps = _monitorService.GetAllRunningApps();
            return JsonConvert.SerializeObject(apps);
        }
        else if (request == "UI_CONNECTED")
        {
            _monitorService.ResetSessionTracking();
            return "{}";
        }
        return "{}";
    }
}
```

---

## Key Algorithms

### Algorithm 1: Process Detection Loop

```
Algorithm: ScanBackgroundProcessesAsync()

Input: None (runs in loop)
Output: Updated _processTrackers dictionary

1. Get all processes from OS
   processes = Process.GetProcesses()

2. For each process:
   a. Check if it has visible window
      if HasVisibleWindow(process):
         - Add to tracking if not already tracked
         - Increment TotalDurationMs by 1 second

3. Check for closed processes:
   a. Get list of PIDs still running
   b. For each tracked process:
      if not in running PIDs:
         - Save session to database
         - Remove from tracking

4. Schedule next scan in 1 second
```

### Algorithm 2: Window Visibility Detection

```
Algorithm: HasVisibleWindow(process)

Input: Process object
Output: boolean (true if visible window found)

1. Initialize: found = false

2. Enumerate all desktop windows:
   for each window handle (hwnd):
      a. Get PID of this window
         pid = GetWindowThreadProcessId(hwnd)
      
      b. Is this window owned by our process?
         if pid == process.Id:
            i. Check visibility
               if IsWindowVisible(hwnd):
                  - Check if top-level window (no owner)
                  if GetWindow(hwnd, GW_OWNER) == NULL:
                     * Mark found = true
                     * Stop enumeration
      
      c. Continue to next window if not match

3. Return found
```

### Algorithm 3: Duration Calculation

```
Algorithm: CalculateTotalDuration(tracker)

Input: ProcessTracker object
Output: long (duration in milliseconds)

Step 1: Get current session duration
   currentSession = tracker.TotalDurationMs - tracker.SessionStartDurationMs

Step 2: Get historical total
   historical = tracker.HistoricalTotalMs

Step 3: Combine
   total = currentSession + historical

Step 4: Return
   return total

Example:
   TotalDurationMs: 7200000 (2 hours total uptime)
   SessionStartDurationMs: 3600000 (baseline when UI connected)
   HistoricalTotalMs: 5400000 (from past sessions in DB)
   
   currentSession = 7200000 - 3600000 = 3600000 (1 hour in this session)
   total = 3600000 + 5400000 = 9000000 (2.5 hours total)
```

---

## Code Walkthrough

### Walkthrough 1: Application Startup

```csharp
// ScreenTimeMonitor.Service/Program.cs

// Step 1: Setup dependency injection
var services = new ServiceCollection();

// Register logger
services.AddLogging(config =>
    config.AddConsole().SetMinimumLevel(LogLevel.Information));

// Register database
services.AddDbContext<ScreenTimeMonitorContext>(options =>
    options.UseSqlite("Data Source=./data/screentime_monitor.db"));

// Register repositories
services.AddScoped<IAppUsageRepository, AppUsageRepository>();

// Register core services
services.AddSingleton<IBackgroundProcessMonitorService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<BackgroundProcessMonitorService>>();
    var appUsageRepo = provider.CreateScope().ServiceProvider
        .GetRequiredService<IAppUsageRepository>();
    
    return new BackgroundProcessMonitorService(logger, appUsageRepo);
});

// Step 2: Build service provider
var serviceProvider = services.BuildServiceProvider();

// Step 3: Initialize database
var dbInitializer = new DatabaseInitializer(serviceProvider);
await dbInitializer.InitializeAsync();

// Step 4: Start background monitoring service
var monitorService = serviceProvider.GetRequiredService<IBackgroundProcessMonitorService>();
await monitorService.StartAsync();

// Step 5: Start IPC server
var ipcService = new IpcService(monitorService, logger);
await ipcService.StartAsync();

// Keep running indefinitely
await Task.Delay(Timeout.Infinite);
```

**What Happens:**
1. Service starts as Windows Service
2. Database initialized (creates tables if needed)
3. Background monitoring begins (scans processes every 1 second)
4. IPC server listens for UI connections
5. Service runs forever until stopped

---

### Walkthrough 2: UI Connection & First Display

```csharp
// ScreenTimeMonitor.UI.WPF/MainWindow.xaml.cs

public partial class MainWindow : Window
{
    private IpcClientService _ipcClient;
    private DispatcherTimer _updateTimer;
    
    public MainWindow()
    {
        InitializeComponent();
        
        // Step 1: Create IPC client
        _ipcClient = new IpcClientService();
        
        // Step 2: Load initial data
        _ = LoadInitialDataAsync();
        
        // Step 3: Setup update timer
        _updateTimer = new DispatcherTimer();
        _updateTimer.Interval = TimeSpan.FromSeconds(1);
        _updateTimer.Tick += async (s, e) => await UpdateRunningAppsAsync();
        _updateTimer.Start();
    }
    
    private async Task LoadInitialDataAsync()
    {
        try
        {
            // Step 1: Connect to service
            await _ipcClient.ConnectAsync();
            
            // Step 2: Tell service "UI connected" 
            // (This causes service to reset SessionStartDurationMs)
            await _ipcClient.SendCommandAsync("UI_CONNECTED");
            
            // Step 3: Get first list of running apps
            await UpdateRunningAppsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to connect to service: {ex.Message}");
        }
    }
    
    private async Task UpdateRunningAppsAsync()
    {
        try
        {
            // Step 1: Request current running apps from service
            var appsJson = await _ipcClient.SendCommandAsync("GET_RUNNING_APPS");
            
            // Step 2: Deserialize JSON response
            var apps = JsonConvert.DeserializeObject<List<AppViewModel>>(appsJson);
            
            // Step 3: Update UI (on UI thread)
            Dispatcher.Invoke(() =>
            {
                // This triggers WPF binding updates
                RunningAppsDataGrid.ItemsSource = apps;
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Update error: {ex.Message}");
        }
    }
}
```

**Timeline:**
```
T=0:00  UI launches
        └─ Connects to service via named pipe
        └─ Sends "UI_CONNECTED"
        
T=0:01  Service resets baseline durations
        └ Sends ack back
        
T=0:02  UI requests "GET_RUNNING_APPS"
        └─ Service returns [Code:0s, Edge:0s, ...]
        └─ UI displays in grid
        
T=0:03  Timer fires (every 1 second)
        └─ Request again
        └─ Service returns [Code:1s, Edge:2s, ...]
        └─ UI updates display
        
T=0:04  User closes Code application
        └─ Service detects exit
        └─ Saves session to DB
        
T=0:05  Timer fires
        └─ Request again
        └─ Service returns [Edge:3s] (Code gone)
        └─ UI removes Code from grid
```

---

### Walkthrough 3: Process Closure & Database Save

```csharp
// BackgroundProcessMonitorService.cs

private async Task ScanBackgroundProcessesAsync()
{
    var currentPids = new HashSet<int>();
    var processes = Process.GetProcesses();
    
    // Phase 1: Track new/existing processes
    foreach (var process in processes)
    {
        try
        {
            if (HasVisibleWindow(process))
            {
                currentPids.Add(process.Id);
                
                if (!_processTrackers.ContainsKey(process.Id))
                {
                    // NEW PROCESS: Load historical data
                    var historicalTotal = await _appUsageRepository
                        .GetAppHistoricalTotalAsync(process.ProcessName);
                    
                    _processTrackers[process.Id] = new ProcessTracker
                    {
                        AppName = process.ProcessName,
                        ProcessId = process.Id,
                        SessionStartTime = DateTime.Now,
                        TotalDurationMs = 0,
                        SessionStartDurationMs = 0,
                        HistoricalTotalMs = historicalTotal
                    };
                    
                    _logger.LogInformation($"Started tracking: {process.ProcessName}");
                }
                else
                {
                    // EXISTING PROCESS: Increment duration
                    _processTrackers[process.Id].TotalDurationMs += 1000;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error tracking process: {ex.Message}");
        }
    }
    
    // Phase 2: Detect closed processes
    var closedPids = _processTrackers.Keys
        .Where(pid => !currentPids.Contains(pid))
        .ToList();
    
    foreach (var pid in closedPids)
    {
        var tracker = _processTrackers[pid];
        
        // SAVE TO DATABASE before removing
        var session = new AppUsageSession
        {
            AppName = tracker.AppName,
            ProcessId = tracker.ProcessId,
            SessionStart = tracker.SessionStartTime,
            SessionEnd = DateTime.Now,
            DurationMs = tracker.TotalDurationMs - tracker.SessionStartDurationMs
        };
        
        try
        {
            await _appUsageRepository.CreateSessionAsync(session);
            _logger.LogInformation(
                $"Saved session: {tracker.AppName} for {session.DurationMs}ms");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to save session: {ex.Message}");
        }
        
        // Remove from tracking
        _processTrackers.Remove(pid);
    }
}
```

**Example Scenario:**
```
T=0:00  Code starts (PID 12345)
        └─ Tracker created
        └─ HistoricalTotalMs = 1800000 (from DB)
        └─ TotalDurationMs = 0
        
T=0:01  Service scans
        └─ PID 12345 still running
        └─ TotalDurationMs += 1000 (now 1000)
        
T=0:02  Service scans
        └─ TotalDurationMs += 1000 (now 2000)
        
T=0:03  Code closes (process exit detected)
        └─ Calculate duration: 2000 - 0 = 2000ms
        └─ Create AppUsageSession:
           - AppName: "Code"
           - SessionStart: T=0:00
           - SessionEnd: T=0:03
           - DurationMs: 2000
        └─ Save to database
        └─ Remove from tracking
        
Database state:
├─ Old session: Code 1800000ms
└─ New session: Code 2000ms

T=0:05  Code starts again (PID 12350, different)
        └─ Load historical: 1800000 + 2000 = 1802000ms
        └─ New tracker created
        └─ Display shows: 0ms current + 1802000ms historical = 1802000ms
```

---

## Common Patterns Used

### Pattern 1: Dependency Injection

**Problem:** Components need each other (Service needs Logger, Repository needs DbContext).

**Solution:** Constructor injection with service container.

```csharp
// Define interfaces
public interface IAppUsageRepository { ... }
public interface IBackgroundProcessMonitorService { ... }

// Register in DI container
services.AddScoped<IAppUsageRepository, AppUsageRepository>();
services.AddSingleton<IBackgroundProcessMonitorService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<...>>();
    var repo = provider.CreateScope().ServiceProvider
        .GetRequiredService<IAppUsageRepository>();
    return new BackgroundProcessMonitorService(logger, repo);
});

// Use in constructors
public class BackgroundProcessMonitorService
{
    private readonly ILogger<BackgroundProcessMonitorService> _logger;
    private readonly IAppUsageRepository _repository;
    
    public BackgroundProcessMonitorService(
        ILogger<BackgroundProcessMonitorService> logger,
        IAppUsageRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }
}
```

**Benefits:**
- ✅ Loose coupling (depend on interfaces, not concrete classes)
- ✅ Easy to test (inject mock implementations)
- ✅ Centralized configuration
- ✅ Lifecycle management (Singleton vs Scoped vs Transient)

---

### Pattern 2: Repository Pattern

**Problem:** Data access logic scattered throughout application.

**Solution:** Centralize in repository classes.

```csharp
// Define interface (contract)
public interface IAppUsageRepository
{
    Task<long> GetAppHistoricalTotalAsync(string appName);
    Task CreateSessionAsync(AppUsageSession session);
}

// Implement (concrete implementation)
public class AppUsageRepository : IAppUsageRepository
{
    private readonly ScreenTimeMonitorContext _dbContext;
    
    public async Task<long> GetAppHistoricalTotalAsync(string appName)
    {
        return await _dbContext.AppUsageSessions
            .Where(s => s.AppName == appName && s.SessionEnd != null)
            .SumAsync(s => s.DurationMs);
    }
    
    public async Task CreateSessionAsync(AppUsageSession session)
    {
        await _dbContext.AppUsageSessions.AddAsync(session);
        await _dbContext.SaveChangesAsync();
    }
}

// Use in service (depends on interface)
public class BackgroundProcessMonitorService
{
    private readonly IAppUsageRepository _repository;
    
    public BackgroundProcessMonitorService(IAppUsageRepository repository)
    {
        _repository = repository;
    }
    
    public async Task SaveSessionAsync(...)
    {
        await _repository.CreateSessionAsync(session);
    }
}
```

**Benefits:**
- ✅ Database access abstracted (could swap SQLite for PostgreSQL)
- ✅ Easier to test (mock repository)
- ✅ CRUD operations centralized
- ✅ Query logic reusable

---

### Pattern 3: Entity Framework Core

**Problem:** Writing SQL queries is error-prone and verbose.

**Solution:** ORM (Object-Relational Mapping) using LINQ.

```csharp
// Define entity (maps to database table)
public class AppUsageSession
{
    public int SessionId { get; set; }
    public string AppName { get; set; }
    public DateTime SessionStart { get; set; }
    public DateTime SessionEnd { get; set; }
    public long DurationMs { get; set; }
}

// Create DbContext
public class ScreenTimeMonitorContext : DbContext
{
    public DbSet<AppUsageSession> AppUsageSessions { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=./data/screentime_monitor.db");
    }
}

// Use with LINQ (strongly-typed, IntelliSense)
var repository = new AppUsageRepository(dbContext);

// This LINQ query:
var total = await dbContext.AppUsageSessions
    .Where(s => s.AppName == "Code")
    .SumAsync(s => s.DurationMs);

// Gets translated to SQL:
// SELECT SUM(DurationMs) FROM AppUsageSessions WHERE AppName = 'Code'
```

**Benefits:**
- ✅ Type-safe queries (compile-time checking)
- ✅ Automatic SQL generation
- ✅ Works with multiple databases (SQLite, PostgreSQL, SQL Server)
- ✅ Easier to maintain

---

### Pattern 4: Observer Pattern (Data Binding)

**Problem:** UI needs to stay synchronized with data model.

**Solution:** WPF ObservableCollection with data binding.

```csharp
// ViewModel exposes observable collection
public class MainWindowViewModel : INotifyPropertyChanged
{
    private ObservableCollection<AppViewModel> _runningApps;
    
    public ObservableCollection<AppViewModel> RunningApps
    {
        get => _runningApps;
        set
        {
            _runningApps = value;
            OnPropertyChanged(nameof(RunningApps));
        }
    }
    
    // When we update this collection:
    RunningApps = new ObservableCollection<AppViewModel>(newData);
    // Automatically notifies UI to refresh display
}

// XAML binds to ViewModel
<DataGrid ItemsSource="{Binding RunningApps}">
    <DataGridTextColumn Binding="{Binding AppName}" />
</DataGrid>

// When RunningApps collection changes:
// │
// ├─ ObservableCollection fires CollectionChanged event
// │
// ├─ Data binding system receives event
// │
// ├─ WPF renders new data to DataGrid
// │
// └─ User sees updated list
```

**Benefits:**
- ✅ Automatic UI synchronization
- ✅ Separation of concerns (logic vs presentation)
- ✅ Reactive updates (no manual UI.Refresh() calls)
- ✅ Declarative (XAML describes relationships)

---

### Pattern 5: Async/Await

**Problem:** Long-running operations block UI thread.

**Solution:** Async operations with Task-based API.

```csharp
// Blocking (BAD) - freezes UI
public void GetDataBlocking()
{
    var data = Database.Query("SELECT ..."); // Blocks 5 seconds
    UpdateUI(data);                          // UI frozen for 5 seconds!
}

// Async (GOOD) - UI stays responsive
public async Task GetDataAsync()
{
    // Query runs on background thread
    var data = await Database.QueryAsync("SELECT ...");
    // Resumes on UI thread when query completes
    UpdateUI(data);
}

// Usage
// Without await (don't do this):
GetDataAsync(); // Fire and forget (bad)

// With await (correct):
await GetDataAsync(); // Wait for completion
```

**In Screen Time Monitor:**
```csharp
// Service methods are async
public async Task ScanBackgroundProcessesAsync()
{
    // Periodically runs on thread pool
}

// Repository methods are async
public async Task<long> GetAppHistoricalTotalAsync(string appName)
{
    // Database query runs asynchronously
}

// UI calls are awaited
var result = await _ipcClient.GetRunningAppsAsync();
// Keeps UI responsive while fetching from service
```

**Benefits:**
- ✅ UI remains responsive during long operations
- ✅ Scales better (can handle more concurrent operations)
- ✅ Cleaner code than threading/callbacks
- ✅ Exception handling is straightforward

---

## Error Handling

### Strategy 1: Graceful Degradation

```csharp
// If database fails, continue monitoring in memory
public async Task SaveSessionAsync(ProcessTracker tracker)
{
    try
    {
        await _appUsageRepository.CreateSessionAsync(session);
        _logger.LogInformation("Session saved to database");
    }
    catch (SqliteException ex)
    {
        _logger.LogError($"Database error: {ex.Message}");
        // Continue anyway - session data might still be in memory
        // Next restart, data will be lost, but monitoring continues
    }
}
```

### Strategy 2: Logging Errors

```csharp
// Log detailed error information
public bool HasVisibleWindow(Process process)
{
    try
    {
        bool found = false;
        EnumWindows((hwnd, lParam) =>
        {
            GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid == process.Id)
            {
                if (IsWindowVisible(hwnd) && GetWindow(hwnd, 4) == IntPtr.Zero)
                {
                    found = true;
                    return false;
                }
            }
            return true;
        }, IntPtr.Zero);
        return found;
    }
    catch (Exception ex)
    {
        _logger.LogError($"Window detection failed for PID {process.Id}: {ex.Message}");
        return false; // Assume no visible window
    }
}
```

### Strategy 3: Connection Resilience

```csharp
// Retry logic for IPC connections
public async Task<string> SendCommandAsync(string command, int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            return await SendCommandInternalAsync(command);
        }
        catch (IOException) when (attempt < maxRetries)
        {
            _logger.LogWarning($"IPC attempt {attempt} failed, retrying...");
            await Task.Delay(100 * attempt);
        }
    }
    
    throw new IOException("Service unavailable after 3 attempts");
}
```

---

## Testing Strategy

### Unit Tests: Database Operations

```csharp
[TestClass]
public class RepositoryTests
{
    private AppUsageRepository _repository;
    private ScreenTimeMonitorContext _dbContext;
    
    [TestInitialize]
    public void Setup()
    {
        // Use in-memory database for tests
        var options = new DbContextOptionsBuilder<ScreenTimeMonitorContext>()
            .UseInMemoryDatabase("test-db")
            .Options;
        
        _dbContext = new ScreenTimeMonitorContext(options);
        _repository = new AppUsageRepository(_dbContext);
    }
    
    [TestMethod]
    public async Task GetHistoricalTotal_WithMultipleSessions_ReturnsSumOfAll()
    {
        // Arrange
        _dbContext.AppUsageSessions.Add(new AppUsageSession
        {
            AppName = "Code",
            DurationMs = 1800000,
            SessionEnd = DateTime.Now
        });
        _dbContext.AppUsageSessions.Add(new AppUsageSession
        {
            AppName = "Code",
            DurationMs = 2700000,
            SessionEnd = DateTime.Now
        });
        _dbContext.SaveChanges();
        
        // Act
        var total = await _repository.GetAppHistoricalTotalAsync("Code");
        
        // Assert
        Assert.AreEqual(4500000, total);
    }
}
```

### Integration Tests: Service + Database

```csharp
[TestClass]
public class ServiceDatabaseIntegrationTests
{
    private IBackgroundProcessMonitorService _service;
    private IAppUsageRepository _repository;
    
    [TestInitialize]
    public void Setup()
    {
        // Setup real database and service
        var dbContext = CreateTestDatabase();
        _repository = new AppUsageRepository(dbContext);
        _service = new BackgroundProcessMonitorService(
            new MockLogger(),
            _repository);
    }
    
    [TestMethod]
    public async Task ProcessClose_SavesSessionToDatabase()
    {
        // Arrange
        var tracker = new ProcessTracker
        {
            AppName = "Code",
            ProcessId = 12345,
            TotalDurationMs = 3600000,
            SessionStartDurationMs = 0,
            SessionStartTime = DateTime.Now.AddMinutes(-60)
        };
        
        // Act
        await _service.SaveSessionAsync(tracker);
        
        // Assert
        var saved = await _repository
            .GetAppSessionHistoryAsync("Code", DateTime.MaxValue);
        Assert.AreEqual(1, saved.Count);
        Assert.AreEqual(3600000, saved[0].DurationMs);
    }
}
```

---

## Summary

**Key Learning Points:**

1. **Service + UI Architecture** - Separation of concerns with IPC communication
2. **Process Monitoring** - Windows API usage for accurate window detection  
3. **Session Tracking** - Session-relative time with historical persistence
4. **Data Persistence** - EF Core + SQLite for lightweight database
5. **Real-Time UI** - WPF data binding with DispatcherTimer updates
6. **Design Patterns** - DI, Repository, Observer, Async/Await
7. **Error Handling** - Graceful degradation and logging
8. **Testing** - Unit and integration tests for quality assurance

**This project demonstrates:**
- ✅ Full-stack .NET application development
- ✅ Windows system integration via P/Invoke
- ✅ Multi-threaded asynchronous programming
- ✅ Database design and ORM usage
- ✅ Desktop UI development with WPF
- ✅ Inter-process communication
- ✅ Production-quality error handling
- ✅ Professional project structure and documentation

Great learning resource for understanding complete application architecture!
