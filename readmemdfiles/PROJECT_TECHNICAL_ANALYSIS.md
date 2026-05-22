# ScreenTimeMonitor - Comprehensive Technical Analysis

## 📋 Executive Summary

**ScreenTimeMonitor** is a comprehensive Windows-based application monitoring system designed to track screen time, application usage, and system metrics. It consists of a background Windows Service that monitors activities and collects data, coupled with UI frontends (console and WPF) for viewing analytics and reports.

**Status**: Production-ready with comprehensive database integration, IPC communication, and health monitoring  
**Technology Stack**: .NET 8.0, C#, Windows API (P/Invoke), EF Core, SQLite/PostgreSQL  
**Target Platform**: Windows 10/11 (all builds)

---

## 🏗️ Architecture Overview

### Multi-Tier Architecture

```
┌─────────────────────────────────────────────────┐
│             UI Layer (Presentation)              │
├──────────────────┬──────────────────────────────┤
│  Console UI      │  WPF Desktop UI              │
│  (ScreenTimeMonitor.UI)  │  (ScreenTimeMonitor.UI.WPF)   │
└──────────────────┴──────────────────────────────┘
                        ↓
           ┌────────────────────────┐
           │   IPC Communication     │
           │  (Named Pipes)          │
           └────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────┐
│      Background Service Layer                    │
│    (ScreenTimeMonitor.Service)                  │
├─────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────┐   │
│  │ Windows API Monitoring Services         │   │
│  ├─────────────────────────────────────────┤   │
│  │ • WindowMonitoringService               │   │
│  │   - SetWinEventHook for foreground      │   │
│  │   - GetForegroundWindow() polling       │   │
│  │   - Process information capture         │   │
│  │                                          │   │
│  │ • BackgroundProcessMonitorService       │   │
│  │   - Enumerate running processes         │   │
│  │   - Track background activities         │   │
│  │                                          │   │
│  │ • SystemMetricsService                  │   │
│  │   - CPU usage                           │   │
│  │   - Memory utilization                  │   │
│  │   - System performance metrics          │   │
│  │                                          │   │
│  │ • DataCollectionService                 │   │
│  │   - Aggregate collected data            │   │
│  │   - Manage session lifecycle            │   │
│  │   - Batch processing                    │   │
│  │                                          │   │
│  │ • IPCService (IPC Communication)        │   │
│  │   - Named pipe server                   │   │
│  │   - Message broadcasting                │   │
│  │   - Client connection management        │   │
│  │                                          │   │
│  │ • HealthCheckService                    │   │
│  │   - Monitor component health            │   │
│  │   - Error recovery                      │   │
│  └─────────────────────────────────────────┘   │
│                                                  │
│  ┌─────────────────────────────────────────┐   │
│  │ Data Layer                              │   │
│  ├─────────────────────────────────────────┤   │
│  │ • DatabaseContext (EF Core)             │   │
│  │ • Repositories (Data Access Pattern)    │   │
│  │ • DatabaseInitializer                   │   │
│  └─────────────────────────────────────────┘   │
└─────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────┐
│           Data Persistence Layer                │
├─────────────────────────────────────────────────┤
│  ┌──────────────────┐    ┌──────────────────┐  │
│  │ SQLite Database  │    │ PostgreSQL DB    │  │
│  │ (Local/Embedded) │    │ (Remote/Server)  │  │
│  └──────────────────┘    └──────────────────┘  │
│                                                  │
│  ┌──────────────────────────────────────────┐  │
│  │ Data Directory (C:\ProgramData\...)      │  │
│  │ • screentime_monitor.db                  │  │
│  │ • Log files                              │  │
│  └──────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
```

---

## 🔧 Core Components Analysis

### 1. **WindowMonitoringService** - Activity Detection
**Purpose**: Hook into Windows API to detect window/application focus changes

**Key Methods**:
- `StartMonitoringAsync()` - Initialize Windows event hooks
- `GetCurrentlyActiveApp()` - Return current foreground application
- `DrainCapturedSessions()` - Retrieve and clear session buffer
- `GetCurrentSessionSnapshot()` - Create snapshot of active session

**Technical Implementation**:
- Uses P/Invoke to call Windows API functions:
  - `SetWinEventHook()` - Register for foreground window change events
  - `GetForegroundWindow()` - Poll current active window
  - `GetWindowThreadProcessId()` - Extract process ID from window handle
  - `Process.GetProcessById()` - Get process information (name, memory, etc.)
  
**Challenge Addressed**: Fallback polling mechanism when event hooks miss rapid window changes

**Execution Context**: Runs at SYSTEM/Administrator privilege level (required for service)

---

### 2. **BackgroundProcessMonitorService** - Process Enumeration
**Purpose**: Enumerate and track background processes running on system

**Functionality**:
- Scan all running processes periodically
- Extract process metadata (ID, name, memory, CPU usage)
- Identify zombie/orphaned processes
- Build process inventory for analysis

**Performance Consideration**: Lightweight polling (configurable interval) to minimize CPU impact

---

### 3. **SystemMetricsService** - Performance Monitoring
**Purpose**: Collect system-wide performance metrics

**Metrics Collected**:
- CPU usage percentage
- Memory utilization (physical & virtual)
- Disk I/O activity
- System load average
- Network statistics (if configured)

**Technical Approach**:
- Uses `System.Diagnostics.PerformanceCounter` for Windows counters
- Windows API calls via P/Invoke for detailed memory stats
- Performance: Cached values with configurable refresh interval

---

### 4. **DataCollectionService** - Session Management
**Purpose**: Aggregate raw activity data and manage session lifecycle

**Responsibilities**:
- Convert raw window monitoring events → structured sessions
- Manage session boundaries (start → end)
- Calculate session duration
- Handle concurrent sessions (multiple app windows)
- Batch sessions for database persistence

**Session Lifecycle**:
```
1. Window Focus → SessionStart (timestamp)
2. App Name + Window Title captured
3. ProcessId extracted from active window
4. On focus loss → SessionEnd (timestamp)
5. Duration calculated (SessionEnd - SessionStart)
6. Queued for database persistence
```

**Queue Management**:
- Configurable batch size (default: 100 sessions)
- Max queue size: 1000 sessions
- Database flush interval: 30 seconds

---

### 5. **IPCService** - Inter-Process Communication
**Purpose**: Enable UI clients to communicate with service

**Communication Protocol**: Named Pipes (Windows-specific)
- Pipe name: `ScreenTimeMonitor.Pipe` (configurable in appsettings.json)

**Supported Operations**:
- `PING` / `PONG` - Connection health check
- `BROADCAST` - Service → UI event broadcasting
- Query operations - Retrieve sessions, metrics, summaries
- Configuration operations - Adjust monitoring settings

**Design Pattern**: 
- Event-driven architecture for real-time updates
- Asynchronous message handling
- Graceful client disconnection handling
- Support for multiple simultaneous clients

---

### 6. **HealthCheckService** - Reliability & Monitoring
**Purpose**: Monitor health of all system components

**Health Indicators Tracked**:
- Window monitoring service status
- Database connectivity
- Data collection queue status
- IPC service availability
- System metrics collection success rate

**Recovery Actions**:
- Automatic service restart on failure
- Error logging with detailed diagnostics
- Graceful degradation (continue operation if non-critical service fails)

---

### 7. **DatabaseContext** - Data Access Layer
**Framework**: Entity Framework Core (EF Core)

**Database Support**:
- **SQLite** (default, embedded) - Lightweight, file-based
- **PostgreSQL** (optional) - For server-based deployments

**Entities Managed**:
- `AppUsageSession` - Individual app usage records
- `SystemMetric` - System performance data points
- `DailyAppSummary` - Aggregated daily app statistics
- `DailySystemSummary` - Aggregated daily system statistics

**Configuration**:
```json
"ConnectionStrings": {
  "SQLite": "Data Source=./data/screentime_monitor.db",
  "PostgreSQL": "Host=localhost;Port=5432;Database=screentime_monitor"
}
```

---

## 📊 Data Model

### AppUsageSession (Core Activity Record)
```csharp
public class AppUsageSession
{
    public long Id { get; set; }                  // Primary Key
    public int ProcessId { get; set; }            // Process ID
    public required string AppName { get; set; } // Executable name
    public string? WindowTitle { get; set; }     // Window title
    public DateTime SessionStart { get; set; }   // When app came to focus
    public DateTime? SessionEnd { get; set; }    // When app lost focus
    public long DurationMs { get; set; }         // Duration in milliseconds
    public DateTime CreatedAt { get; set; }      // Record creation time
}
```

**Purpose**: Atomic record of a single app usage period

---

### SystemMetric (Performance Data)
```csharp
public class SystemMetric
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public float CpuUsagePercentage { get; set; }
    public long MemoryUsageBytes { get; set; }
    public long AvailableMemoryBytes { get; set; }
    public float DiskUsagePercentage { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**Purpose**: Track system performance over time

---

### DailyAppSummary (Aggregated Statistics)
```csharp
public class DailyAppSummary
{
    public long Id { get; set; }
    public DateTime Date { get; set; }
    public required string AppName { get; set; }
    public long TotalUsageMs { get; set; }
    public int UsageCount { get; set; }
    public DateTime FirstUseTime { get; set; }
    public DateTime LastUseTime { get; set; }
    public int AverageSessionDurationMs { get; set; }
}
```

**Purpose**: Daily rollup for analytics and reporting

---

### DailySystemSummary (System-Wide Daily Stats)
```csharp
public class DailySystemSummary
{
    public long Id { get; set; }
    public DateTime Date { get; set; }
    public float AverageCpuUsage { get; set; }
    public long AverageMemoryUsage { get; set; }
    public float PeakCpuUsage { get; set; }
    public long PeakMemoryUsage { get; set; }
    public int ActiveAppCount { get; set; }
}
```

**Purpose**: System-wide performance analysis

---

## 🗄️ Database Schema Highlights

### Key Tables
1. **AppUsageSessions** - Raw activity records
2. **SystemMetrics** - Performance timestamped data
3. **DailyAppSummaries** - Pre-aggregated daily app stats
4. **DailySystemSummaries** - Pre-aggregated daily system stats

### Indexes (for Performance)
- AppName + Date (for daily summaries)
- SessionStart (for time-range queries)
- ProcessId (for process-specific tracking)
- CreatedAt (for cleanup and archival)

### Data Retention Policy
- Configurable retention: Default 90 days
- Automatic cleanup job removes old records
- Preserves aggregated summaries longer than raw data

---

## 🔄 Data Flow & Processing Pipeline

### Real-Time Event Flow
```
Windows Event (Window Focus Change)
    ↓
WindowMonitoringService (P/Invoke Hook)
    ↓
DataCollectionService (Transform to Session)
    ↓
In-Memory Queue (Batching)
    ↓
[Periodic Flush - Every 30 seconds OR Queue Full]
    ↓
DatabaseContext.SaveChangesAsync()
    ↓
SQLite/PostgreSQL Database
    ↓
[Daily Aggregation Job (Background)]
    ↓
DailyAppSummary / DailySystemSummary
```

---

## 🔐 Security & Permissions

### Privilege Levels Required
- **Windows Service**: SYSTEM or Administrator
- **Console/UI Apps**: Standard user (connects via IPC)
- **Database**: Local file (SQLite) or server credentials (PostgreSQL)

### Security Considerations
- Service must run elevated to hook into Windows API
- IPC named pipes have OS-level access control
- Database stored in `C:\ProgramData\` (protected system directory)
- Log files include security events

---

## ⚡ Performance Characteristics

### Design for Minimal Overhead
- **Event-Driven**: Window hooks (vs constant polling)
- **Batching**: Accumulate 100 sessions before flush
- **Async/Await**: Non-blocking I/O operations
- **Memory Efficient**: Stream processing, not load-all-in-memory
- **Configurable Intervals**: 
  - Window hook polling: 1000ms fallback
  - Metrics polling: 5 seconds (configurable)
  - Database flush: 30 seconds (configurable)

### Estimated Resource Usage
- **Memory**: ~50-100 MB resident
- **CPU**: <2% under normal operation
- **Disk I/O**: ~500KB/day activity data, ~50KB system metrics

---

## 🚀 Service Lifecycle & Integration

### Windows Service Integration
```powershell
# Installation
ScreenTimeMonitor.Service\Installer\install-service.ps1

# Service Configuration
- Service Name: ScreenTimeMonitor
- Display Name: Screen Time Monitor Service
- Startup Type: Automatic
- Recovery: Restart service on failure
```

### Startup Sequence
1. Host.CreateDefaultBuilder() - DI container setup
2. DatabaseContext initialization
3. DatabaseInitializer - Schema validation/creation
4. Service layer instantiation (Singletons)
5. MonitoringHostedService startup
   - WindowMonitoringService.StartMonitoringAsync()
   - IPCService initialization
   - HealthCheckService activation
6. Ready to accept client connections

### Graceful Shutdown
- 30-second shutdown timeout
- Flush all pending data to database
- Close all client connections
- Stop all monitoring hooks

---

## 📡 IPC Communication Protocol

### Named Pipe Transport
- **Pipe Name**: `\\.\pipe\ScreenTimeMonitor.Pipe`
- **Message Format**: JSON-RPC (Request/Response)

### Example Operations

**Connection Health Check**:
```json
Request:  { "command": "PING" }
Response: { "status": "PONG", "timestamp": "2025-12-12T10:30:00Z" }
```

**Live Activity Subscription**:
```json
Request:  { "command": "SUBSCRIBE", "event_type": "BROADCAST" }
Response: [Continuous stream of app activity updates]
```

**Query Daily Summary**:
```json
Request:  { "command": "GET_DAILY_SUMMARY", "date": "2025-12-12" }
Response: { "summaries": [...], "count": 15 }
```

---

## 🧪 Testing Infrastructure

### Test Projects
- **ScreenTimeMonitor.Tests** - Unit and integration tests
- **Testing Frameworks**: xUnit, Moq (mocking)

### Test Coverage Areas
- ServiceDatabaseIntegrationTests - Database operations
- ServiceDatabaseEndToEndTests - Full pipeline
- IPCClientTests - Inter-process communication
- LiveActivityIntegrationTests - Real-time event streaming

---

## 📦 Project Structure

```
ScreenTimeMonitor.Service/          [Windows Service - Background daemon]
├── Program.cs                      [Entry point, DI configuration]
├── appsettings.json               [Service configuration]
├── Services/                       [Business logic layer]
│   ├── WindowMonitoringService    [Windows API hooks]
│   ├── BackgroundProcessMonitorService
│   ├── SystemMetricsService       [Performance metrics]
│   ├── DataCollectionService      [Session aggregation]
│   ├── IPCService                 [Named pipe server]
│   ├── HealthCheckService         [Health monitoring]
│   └── MonitoringHostedService    [Lifetime management]
├── Database/                       [Data access layer]
│   ├── DatabaseContext.cs         [EF Core DbContext]
│   ├── Repositories.cs            [Data operations]
│   └── DatabaseInitializer.cs     [Schema setup]
├── Models/                         [Domain models]
│   └── DomainModels.cs
├── Utilities/                      [Helper functions]
│   ├── PInvokeDeclarations.cs     [Windows API P/Invoke]
│   └── Constants.cs
└── Installer/                      [Service installation scripts]

ScreenTimeMonitor.UI/               [Console UI for service interaction]
├── Program.cs
├── appsettings.json
├── Services/                       [UI logic]
├── Views/                          [Console views]
└── Models/                         [UI-specific models]

ScreenTimeMonitor.UI.WPF/           [Desktop UI (advanced)]
├── App.xaml
├── ViewModels/
├── Views/
└── Services/

ScreenTimeMonitor.Tests/            [Automated test suite]
├── ServiceDatabaseIntegrationTests.cs
├── ServiceDatabaseEndToEndTests.cs
├── IPCClientTests.cs
└── LiveActivityIntegrationTests.cs
```

---

## 🔍 Key Design Patterns Used

1. **Dependency Injection** - .NET Core DI container (constructor injection)
2. **Repository Pattern** - Abstraction over database access
3. **Observer Pattern** - Event hooks for window monitoring
4. **Producer-Consumer** - Queue-based data collection
5. **Health Check Pattern** - Monitoring component status
6. **Hosted Service Pattern** - ASP.NET Core/Worker Service pattern

---

## ⚠️ Current Known Issues & Warnings

### Build Warnings
1. **Npgsql Vulnerability** (High Severity)
   - Package: Npgsql 8.0.0
   - Advisory: GHSA-x9vc-6hfv-hg8c
   - Action: Consider upgrading to patched version

2. **Obsolete API Usage**
   - `GlobalMemoryStatus()` - Should use `GlobalMemoryStatusEx()`
   - Location: SystemMetricsService.cs:187

3. **Missing XML Documentation**
   - 100+ public members lack XML doc comments
   - Severity: Warning (documentation only)

### Design Considerations
- Service requires elevated privileges (admin/system)
- P/Invoke calls are Windows-only (not cross-platform)
- Named pipes IPC is Windows-specific (not Linux/macOS compatible)

---

## 🎯 Strengths of the Design

✅ **Modular Architecture** - Clear separation of concerns (monitoring, collection, storage, UI)  
✅ **Scalable Data Model** - Pre-aggregated summaries for fast reporting  
✅ **Health Monitoring** - Built-in reliability checks and recovery  
✅ **Flexible Storage** - Support for both SQLite (embedded) and PostgreSQL (server)  
✅ **Rich Telemetry** - Captures apps, window titles, system metrics, usage counts  
✅ **Real-Time Updates** - IPC-based event streaming to UI  
✅ **Production Ready** - Windows Service integration, installer scripts, logging  
✅ **Test Coverage** - Comprehensive integration and end-to-end tests  

---

## 🚧 Potential Enhancements

1. **Cross-Platform Support** - Port monitoring to Linux/macOS using platform-specific APIs
2. **Advanced Analytics** - ML-based anomaly detection, usage patterns
3. **Data Export** - CSV/JSON reports, cloud sync
4. **Performance Tuning** - Profile and optimize hot paths
5. **UI Polish** - Enhanced WPF UI with visualizations (charts, graphs)
6. **API Layer** - RESTful API for programmatic access
7. **Configuration UI** - GUI for service settings (instead of JSON editing)

---

## 📚 Configuration & Deployment

### appsettings.json Configuration
```json
{
  "MonitoringSettings": {
    "MetricsPollingIntervalSeconds": 5,      // How often to sample system metrics
    "BatchSize": 100,                         // Sessions per database flush
    "DatabaseFlushIntervalSeconds": 30,      // How often to persist data
    "DataRetentionDays": 90,                 // How long to keep raw data
    "WindowHookPollbackIntervalMs": 1000    // Fallback polling interval
  },
  "ConnectionStrings": {
    "SQLite": "Data Source=./data/screentime_monitor.db",
    "PostgreSQL": "Host=localhost;..."  // For server deployments
  }
}
```

### Database Setup
- **SQLite**: Auto-created on first run at `./data/screentime_monitor.db`
- **PostgreSQL**: Requires manual schema import (`schema-postgresql.sql`)

---

## 📖 Usage Examples

### Install and Run as Windows Service
```powershell
cd ScreenTimeMonitor.Service\Installer
.\install-service.ps1 -ServicePath "C:\path\to\service"
# Service will auto-start with Windows
```

### Run Locally for Development
```powershell
cd ScreenTimeMonitor.Service
dotnet run
# Logs to console, can attach debugger
```

### Use Console UI
```powershell
cd ScreenTimeMonitor.UI
dotnet run
# Interactive menu to connect, ping, view live activity
```

---

## 🎓 Educational Value

This project demonstrates:
- **Windows API Integration** via P/Invoke for system-level monitoring
- **Service Architecture** using .NET Worker Services/Windows Services
- **Database Design** with EF Core, SQLite, PostgreSQL
- **IPC Patterns** using named pipes for inter-process communication
- **Async/Await** patterns for responsive, non-blocking operations
- **Dependency Injection** using .NET Core DI container
- **Repository Pattern** for data access abstraction
- **Testing** with xUnit and mocking frameworks

Perfect for an **Operating Systems** course project or **System Administration** coursework.

---

**Last Updated**: December 12, 2025  
**Analysis Version**: 2.0
