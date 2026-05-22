# Implementation Analysis & Architecture Plan
## Screen Time & App Usage Monitoring System

**Status**: Analysis Phase (No coding yet)  
**Last Updated**: December 2, 2025  
**Project Type**: Windows Desktop Application (Console-based Backend + Simple UI)

---

## PART 1: PROJECT SCOPE CONFIRMATION

### Core Objective
Build a **Windows Desktop Application** that:
1. ✅ Monitors all app usage (screen time)
2. ✅ Tracks number of times apps are used per day
3. ✅ Logs total time spent on each app
4. ✅ Runs as Windows Service (auto-start with OS)
5. ✅ Logs all computer activities
6. ✅ Deployable via installer

### What This IS:
- Desktop/System application (NOT web-based)
- Windows Service for background monitoring
- Console-based backend with simple UI for testing
- Installer-ready at end of development

### What This IS NOT:
- Web application
- Cloud-based system
- Complex GUI (fancy UI comes LATER)
- Real-time streaming system

---

## PART 2: TECHNOLOGY STACK FINALIZATION

### Confirmed Stack (Based on all recommendations)

```
┌─────────────────────────────────────────────────┐
│          TECHNOLOGY STACK DECISION              │
├─────────────────────────────────────────────────┤
│                                                  │
│  Language:         C# / .NET 8.0                │
│  IDE:              Visual Studio Code           │
│  OS Target:        Windows 10 & 11              │
│  Execution Model:  Windows Service              │
│  Database:         PostgreSQL (Primary)         │
│                    SQLite (Alternative)         │
│  API/IPC:          Named Pipes                  │
│  UI:               Console App (Testing)        │
│  Final UI:         WPF or Windows Forms (later) │
│  Installer:        Inno Setup or WiX            │
│                                                  │
└─────────────────────────────────────────────────┘
```

### Why These Choices:
| Component | Choice | Reason |
|-----------|--------|--------|
| **Language** | C# / .NET 8.0 | Best for Windows API integration, excellent performance, async support |
| **Backend** | Windows Service | Runs in background, auto-starts with OS, SYSTEM privilege level |
| **Monitoring** | Windows API P/Invoke | Direct native access to window hooks, minimal overhead |
| **Database** | PostgreSQL | ACID compliant, handles concurrent writes, crash-safe. SQLite fallback if PostgreSQL unavailable |
| **UI** | Console (Testing) + WPF (Later) | Simple testing UI first, then professional WPF UI |
| **Installer** | Inno Setup | User-friendly, handles service installation, easy to create |

---

## PART 3: ARCHITECTURE DESIGN

### High-Level System Architecture

```
┌──────────────────────────────────────────────────────────┐
│                    USER MACHINE                           │
├──────────────────────────────────────────────────────────┤
│                                                            │
│  ┌────────────────────────────────────────────────────┐  │
│  │  Windows Service (Runs in background at SYSTEM)    │  │
│  │                                                     │  │
│  │  ┌──────────────────────────────────────────────┐  │  │
│  │  │  Monitoring Engines:                         │  │  │
│  │  │  • Window Event Hook (Active App)            │  │  │
│  │  │  • System Metrics (CPU/Mem/Disk)             │  │  │
│  │  │  • Process Information (PID, Name)           │  │  │
│  │  └──────────────────────────────────────────────┘  │  │
│  │                                                     │  │
│  │  ┌──────────────────────────────────────────────┐  │  │
│  │  │  Data Collection & Storage:                  │  │  │
│  │  │  • In-Memory Queues (BlockingCollection)     │  │  │
│  │  │  • Database Layer (PostgreSQL/SQLite)        │  │  │
│  │  │  • Batch Processing (100 records at a time)  │  │  │
│  │  │  • Transaction Management                    │  │  │
│  │  └──────────────────────────────────────────────┘  │  │
│  │                                                     │  │
│  │  ┌──────────────────────────────────────────────┐  │  │
│  │  │  IPC Communication:                          │  │  │
│  │  │  • Named Pipes Interface                     │  │  │
│  │  │  • UI <-> Service Communication              │  │  │
│  │  └──────────────────────────────────────────────┘  │  │
│  └────────────────────────────────────────────────────┘  │
│                                                            │
│  ┌────────────────────────────────────────────────────┐  │
│  │  Console UI App (Testing & Basic Viewing)         │  │
│  │  • Display current app usage                      │  │
│  │  • Show daily statistics                          │  │
│  │  • Test connectivity to service                   │  │
│  └────────────────────────────────────────────────────┘  │
│                                                            │
│  ┌────────────────────────────────────────────────────┐  │
│  │  Database (PostgreSQL or SQLite)                  │  │
│  │  • app_sessions table                             │  │
│  │  • system_metrics table                           │  │
│  │  • daily_app_summary table                        │  │
│  │  • daily_system_summary table                     │  │
│  └────────────────────────────────────────────────────┘  │
│                                                            │
└──────────────────────────────────────────────────────────┘
```

### Data Flow Diagram

```
User opens application
           ↓
Window Event Hook triggered
           ↓
App name & timestamp captured
           ↓
Added to BlockingCollection<AppUsageEvent>
           ↓
Background thread processes queue
           ↓
Batch 100 events together
           ↓
Database insert/update
           ↓
Daily summary aggregation
           ↓
Console UI queries database
           ↓
Display results to user
```

---

## PART 4: FOLDER & PROJECT STRUCTURE

### Physical Folder Layout

```
Operating System Project/
│
├── Documentation/
│   ├── PROJECT_ANALYSIS.md              (Already created)
│   ├── TECHNICAL_RECOMMENDATIONS.md     (Already created)
│   ├── VS_CODE_SETUP_GUIDE.md           (Already created)
│   └── IMPLEMENTATION_ANALYSIS.md       (This file)
│
├── ScreenTimeMonitor.sln                (Solution file)
│
├── ScreenTimeMonitor.Service/           (Windows Service Project)
│   ├── Program.cs                       (Service entry point)
│   ├── ServiceHost.cs                   (Service wrapper)
│   ├── ScreenTimeMonitor.Service.csproj
│   ├── appsettings.json                 (Configuration)
│   │
│   ├── Services/                        (Business Logic)
│   │   ├── WindowMonitoringService.cs   (App tracking via hooks)
│   │   ├── SystemMetricsService.cs      (CPU/Memory/Disk)
│   │   ├── DatabaseService.cs           (Data persistence)
│   │   ├── DataCollectionService.cs     (Queue management)
│   │   └── AggregationService.cs        (Daily summaries)
│   │
│   ├── Models/                          (Data Classes)
│   │   ├── AppUsageSession.cs           (App usage record)
│   │   ├── SystemMetric.cs              (System stat record)
│   │   ├── DailySummary.cs              (Daily aggregation)
│   │   └── EventModels.cs               (Event data types)
│   │
│   ├── Database/                        (Data Access Layer)
│   │   ├── DatabaseContext.cs           (Connection management)
│   │   ├── DatabaseInitializer.cs       (Schema creation)
│   │   ├── AppUsageRepository.cs        (CRUD for app usage)
│   │   ├── SystemMetricsRepository.cs   (CRUD for metrics)
│   │   └── SummaryRepository.cs         (CRUD for summaries)
│   │
│   ├── Utilities/                       (Helper Functions)
│   │   ├── PInvokeDeclarations.cs       (Windows API declarations)
│   │   ├── Logger.cs                    (Logging utility)
│   │   ├── ConfigurationManager.cs      (Settings management)
│   │   └── Constants.cs                 (App constants)
│   │
│   ├── IPC/                             (Inter-Process Communication)
│   │   ├── NamedPipeServer.cs           (Named pipe communication)
│   │   └── MessageProtocol.cs           (Message format)
│   │
│   └── bin/
│       └── Debug/Release/               (Compiled output)
│
├── ScreenTimeMonitor.UI/                (Console/WPF UI Project)
│   ├── Program.cs                       (UI entry point)
│   ├── ScreenTimeMonitor.UI.csproj
│   │
│   ├── Services/                        (UI Logic)
│   │   ├── ServiceCommunication.cs      (Connect to service)
│   │   ├── DataDisplayService.cs        (Format data for display)
│   │   └── ReportGenerator.cs           (Generate reports)
│   │
│   ├── Models/                          (UI Models)
│   │   └── UIDataModels.cs              (Models for UI display)
│   │
│   ├── Views/                           (UI Components)
│   │   ├── ConsoleMenus.cs              (Console menus)
│   │   └── MainWindow.xaml              (WPF - future)
│   │
│   └── bin/
│       └── Debug/Release/               (Compiled output)
│
├── ScreenTimeMonitor.Tests/             (Unit Tests - Optional)
│   ├── ScreenTimeMonitor.Tests.csproj
│   ├── Services/
│   │   └── WindowMonitoringServiceTests.cs
│   └── Database/
│       └── DatabaseServiceTests.cs
│
├── Database/                            (Database Scripts)
│   ├── schema-postgresql.sql            (PostgreSQL schema)
│   ├── schema-sqlite.sql                (SQLite schema)
│   ├── seed-data.sql                    (Test data)
│   └── migration-scripts.sql            (Version upgrades)
│
├── Installer/                           (Installation Files)
│   ├── ScreenTimeMonitor.iss            (Inno Setup script)
│   ├── install-service.bat              (Service installation batch)
│   └── uninstall-service.bat            (Service uninstallation batch)
│
└── Config/                              (Configuration Templates)
    └── appsettings.template.json        (Configuration template)
```

---

## PART 5: DETAILED PHASE BREAKDOWN

### Phase 1: Foundation & Setup (Week 1)
**Goal**: Establish project structure and environment

#### Tasks:
1. **Project Creation**
   - Create .NET 8.0 Console solution
   - Create Service project (ScreenTimeMonitor.Service)
   - Create UI project (ScreenTimeMonitor.UI)
   - Create Tests project (ScreenTimeMonitor.Tests)

2. **NuGet Dependencies**
   - Npgsql (PostgreSQL) or System.Data.SQLite
   - Dapper (ORM)
   - Microsoft.Extensions.* (logging, config)
   - Microsoft.Extensions.Hosting.WindowsServices (for service)

3. **Configuration Files**
   - appsettings.json for each project
   - Database connection strings
   - Service configuration

4. **Initial Documentation**
   - Code structure walkthrough
   - Setup instructions
   - Dependency list

#### Deliverable:
- Solution compiles
- All projects configured
- Ready for coding

---

### Phase 2: Core Monitoring Engine (Week 2-3)
**Goal**: Implement app tracking and system metrics collection

#### Tasks:
1. **Window Monitoring Service**
   - P/Invoke declarations for Windows API
   - SetWinEventHook for EVENT_SYSTEM_FOREGROUND
   - Active window change detection
   - Process information extraction
   - Error handling for edge cases

2. **System Metrics Service**
   - PerformanceCounter for CPU usage
   - WMI for memory information
   - Disk I/O metrics
   - Periodic collection (every 5 seconds)

3. **Data Collection Pipeline**
   - BlockingCollection queues
   - Producer threads (monitors)
   - Consumer threads (database writers)
   - Batch processing (100 records per batch)

#### Key Implementations:
- `WindowMonitoringService.cs` - App tracking
- `SystemMetricsService.cs` - System stats
- `DataCollectionService.cs` - Queue management
- Event handlers and callbacks

#### Deliverable:
- Monitors can detect app changes
- System metrics collect successfully
- Data flows to memory queues
- No database writes yet

---

### Phase 3: Database Layer (Week 3-4)
**Goal**: Persistent data storage with schema

#### Tasks:
1. **Database Schema**
   - Create `app_sessions` table
   - Create `system_metrics` table
   - Create `daily_app_summary` table
   - Create `daily_system_summary` table
   - Set up indexes for performance
   - WAL mode for SQLite or ACID for PostgreSQL

2. **Data Access Layer**
   - DatabaseContext class
   - Repository pattern implementation
   - Connection pooling
   - Transaction management

3. **Database Initialization**
   - Auto-create database on first run
   - Schema migrations
   - Data cleanup policies

#### Key Implementations:
- `DatabaseContext.cs` - Connection management
- Repository classes for CRUD operations
- Transaction handling
- Error recovery

#### Deliverable:
- Database creates successfully
- Data persists to database
- Batch inserts working
- Daily summaries generate

---

### Phase 4: Windows Service Integration (Week 4)
**Goal**: Service runs as Windows Service with auto-start

#### Tasks:
1. **Service Host Implementation**
   - ServiceBase wrapper
   - OnStart() method
   - OnStop() method
   - Error handling

2. **Service Installation**
   - sc.exe commands for registration
   - Service permissions setup
   - Auto-start configuration
   - Startup type = "Automatic"

3. **Service Reliability**
   - Crash recovery
   - Logging to file
   - Health checks
   - Restart on failure

#### Key Implementations:
- Service host configuration
- Logging infrastructure
- Error handling
- Recovery mechanisms

#### Deliverable:
- Service installs successfully
- Runs at Windows startup
- Monitors continuously
- Can be started/stopped

---

### Phase 5: IPC & Service Communication (Week 5)
**Goal**: UI can communicate with background service

#### Tasks:
1. **Named Pipe Communication**
   - Server-side named pipe listener
   - Client-side pipe connection
   - Message protocol definition
   - Request/response handling

2. **API Endpoints** (via named pipes)
   - GetCurrentAppUsage()
   - GetDailySummary()
   - GetSystemMetrics()
   - GetAppHistory()

#### Key Implementations:
- `NamedPipeServer.cs` in service
- `NamedPipeClient.cs` in UI
- Message serialization (JSON)

#### Deliverable:
- UI can query service
- Service returns data
- Real-time updates work
- Handles multiple connections

---

### Phase 6: Simple Console UI (Week 5-6)
**Goal**: Basic testing interface to verify functionality

#### Tasks:
1. **Console Menu System**
   - Menu-driven interface
   - Display current app usage
   - Show daily statistics
   - Test service connectivity

2. **Data Display**
   - Format for readability
   - Sort by time/frequency
   - Show CPU/Memory usage
   - Display top apps

3. **Test Utilities**
   - Verify service running
   - Check database connection
   - Test data collection
   - Generate test reports

#### Key Implementations:
- `ConsoleMenus.cs` - Menu logic
- `DataDisplayService.cs` - Formatting
- `ReportGenerator.cs` - Reports

#### Deliverable:
- Console menu works
- Data displays correctly
- All features testable
- Ready for function testing

---

### Phase 7: Testing & Debugging (Week 6-7)
**Goal**: Verify all components work correctly

#### Tasks:
1. **Functional Testing**
   - Test with single app running
   - Test with multiple apps (5+ simultaneously)
   - Test duration calculation accuracy
   - Test frequency counting

2. **Reliability Testing**
   - Run for 24 hours
   - Kill service and restart
   - Simulate app crashes
   - Database crash recovery

3. **Performance Testing**
   - Monitor memory usage
   - Monitor CPU usage
   - Check database query speed
   - Verify no memory leaks

4. **Edge Cases**
   - Rapid app switching
   - System sleep/wake
   - Windows restart
   - Database connection loss

#### Deliverable:
- All components working
- No memory leaks
- Handles edge cases
- Ready for installer

---

### Phase 8: Installer Creation (Week 8)
**Goal**: Distributable installation package

#### Tasks:
1. **Inno Setup Script**
   - Application files
   - Service installation
   - Database setup
   - Shortcuts creation
   - Uninstall routine

2. **Installation Steps**
   - Detect Windows version
   - Check .NET installation
   - Install service
   - Create database
   - Set permissions

3. **Deployment Files**
   - Service executable
   - UI executable
   - Database scripts
   - Configuration files
   - Documentation

#### Deliverable:
- .exe installer file
- User-friendly installation
- Service auto-installs
- Uninstall support
- Ready for distribution

---

## PART 6: DATA MODEL & SCHEMA

### Core Tables

#### `app_sessions` Table
```
Columns:
- id (BIGSERIAL PRIMARY KEY)
- process_id (INT)
- app_name (VARCHAR 255)
- window_title (TEXT)
- session_start (TIMESTAMP WITH TIME ZONE)
- session_end (TIMESTAMP WITH TIME ZONE)
- duration_ms (BIGINT)
- created_at (TIMESTAMP - index)

Indexes:
- ON (app_name)
- ON (session_start)
- ON (created_at)

Usage: Track individual app usage sessions
```

#### `system_metrics` Table
```
Columns:
- id (BIGSERIAL PRIMARY KEY)
- timestamp (TIMESTAMP - index)
- cpu_usage (DECIMAL 5,2)
- memory_usage_mb (BIGINT)
- memory_percent (DECIMAL 5,2)
- disk_read_bytes (BIGINT)
- disk_write_bytes (BIGINT)
- process_id (INT - index)

Indexes:
- ON (timestamp)
- ON (process_id)

Usage: Collect system resource usage over time
```

#### `daily_app_summary` Table
```
Columns:
- id (BIGSERIAL PRIMARY KEY)
- summary_date (DATE)
- app_name (VARCHAR 255)
- total_usage_ms (BIGINT)
- usage_count (INT)
- first_use (TIMESTAMP)
- last_use (TIMESTAMP)

UNIQUE(summary_date, app_name)
Indexes: ON (summary_date)

Usage: Aggregated daily app statistics
```

#### `daily_system_summary` Table
```
Columns:
- id (BIGSERIAL PRIMARY KEY)
- summary_date (DATE)
- avg_cpu_usage (DECIMAL 5,2)
- peak_cpu_usage (DECIMAL 5,2)
- avg_memory_mb (BIGINT)
- peak_memory_mb (BIGINT)
- total_disk_read_gb (DECIMAL 10,2)
- total_disk_write_gb (DECIMAL 10,2)

UNIQUE(summary_date)
Indexes: ON (summary_date)

Usage: Aggregated daily system statistics
```

---

## PART 7: KEY IMPLEMENTATION DECISIONS

### Decision 1: Database Choice
**Decision**: PostgreSQL (Primary) with SQLite (Fallback)
**Reasoning**: 
- PostgreSQL for ACID compliance and concurrent writes
- SQLite fallback for users without PostgreSQL
- App detects and adapts at startup

### Decision 2: Data Collection Pattern
**Decision**: Event-driven hooks + batched writes
**Reasoning**:
- Minimal overhead (event-driven, not polling)
- Database efficiency (batch 100 records)
- Accurate timestamps

### Decision 3: Service Architecture
**Decision**: Windows Service + Console UI + Future WPF
**Reasoning**:
- Service runs independently
- Console UI for testing/verification
- WPF UI added later without modifying service
- Clean separation of concerns

### Decision 4: IPC Mechanism
**Decision**: Named Pipes (Windows-specific)
**Reasoning**:
- Fast, local-only communication
- No network overhead
- Secure (Windows access control)
- Native Windows support

### Decision 5: Memory Management
**Decision**: BlockingCollection with batch processing
**Reasoning**:
- Thread-safe by design
- Bounded queue (prevents memory overflow)
- Batching reduces database hits
- Efficient CPU usage

---

## PART 8: ERROR HANDLING STRATEGY

### Critical Error Points

1. **Window Hook Failures**
   - Fallback to polling
   - Log error
   - Continue operation

2. **Database Connection Loss**
   - Retry logic with exponential backoff
   - Queue data in memory
   - Sync when reconnected

3. **Database Corruption**
   - Automatic backup
   - Recovery from backup
   - Notify user

4. **Service Crashes**
   - Windows restart on crash
   - Preserve queued data
   - Log crash dump

5. **IPC Disconnection**
   - UI reconnects automatically
   - Shows "Disconnected" status
   - Queues requests

### Logging Strategy
- **File Location**: `C:\ProgramData\ScreenTimeMonitor\Logs\`
- **Log Level**: Debug (development), Error (production)
- **Rotation**: Daily rotation, keep 30 days
- **Format**: JSON for easier parsing

---

## PART 9: SECURITY & PERMISSIONS

### Service Permissions
- Run as: **LOCAL SYSTEM** (for window hooks)
- Privilege Level: **Admin** (required for system metrics)
- Requires: UAC elevation during installation

### Data Protection
- Database file at: `C:\ProgramData\ScreenTimeMonitor\`
- NTFS permissions: Administrators only
- No sensitive data (just app names and metrics)
- Data retention: Configurable (default 90 days)

### Windows Compatibility
- Minimum: Windows 10 (Build 19041)
- Target: Windows 10 & 11 (all builds)
- Testing: Verify on both versions

---

## PART 10: INSTALLER STRATEGY

### Inno Setup Installation Flow

```
1. Welcome screen
   ↓
2. License agreement
   ↓
3. Select installation path (default: C:\Program Files\ScreenTimeMonitor\)
   ↓
4. Select components (Service, UI, Database)
   ↓
5. Ready to install
   ↓
6. Install files
   ↓
7. Create database (if PostgreSQL available)
   ↓
8. Install Windows Service
   ↓
9. Start service
   ↓
10. Completion (with "Launch UI" option)
```

### Post-Installation
- Service runs automatically
- UI shortcut on desktop
- Uninstall available via Control Panel
- Uninstall removes service and files

---

## PART 11: IMPLEMENTATION CHECKLIST

### Before Coding Starts
- [ ] Review this analysis document
- [ ] Confirm technology stack
- [ ] Decide PostgreSQL vs SQLite
- [ ] Review folder structure
- [ ] Understand data flow

### Setup Phase
- [ ] Create solution and projects
- [ ] Add NuGet packages
- [ ] Create folder structure
- [ ] Set up appsettings.json

### Development Phase
- [ ] Implement each phase sequentially
- [ ] Test after each phase
- [ ] Update documentation
- [ ] Create code comments

### Testing Phase
- [ ] Unit tests for services
- [ ] Integration tests for database
- [ ] System tests with multiple apps
- [ ] Performance profiling
- [ ] Edge case testing

### Deployment Phase
- [ ] Create Inno Setup script
- [ ] Test installer
- [ ] Create user documentation
- [ ] Prepare distribution

---

## PART 12: TECHNOLOGY STACK REFERENCE

### All Decisions Summarized

```
CORE TECHNOLOGIES:
• Language: C# 12 / .NET 8.0
• IDE: Visual Studio Code
• Build: dotnet CLI (command line)
• Runtime: .NET 8.0 Runtime

BACKEND SERVICE:
• Type: Windows Service (Console-based)
• Host: Microsoft.Extensions.Hosting
• API: Windows API via P/Invoke
• Hooks: SetWinEventHook, GetForegroundWindow

MONITORING:
• App Tracking: Windows Event Hooks
• Metrics: System.Diagnostics.PerformanceCounter
• Advanced: Windows Management Instrumentation (WMI)
• Polling: 5-second intervals for metrics

DATABASE:
• Primary: PostgreSQL 15+
  - Connector: Npgsql (PostgreSQL .NET driver)
  - ORM: Dapper (micro-ORM)
  - Connection Pooling: Built-in Npgsql pooling
  
• Fallback: SQLite 3
  - Connector: System.Data.SQLite
  - Mode: Write-Ahead Logging (WAL)
  
COMMUNICATION:
• Service ↔ UI: Named Pipes (IPC)
• Protocol: JSON serialization
• Data Format: UTF-8 text

UI (TESTING):
• Type: Console Application
• Framework: .NET Console
• Input: Keyboard menus
• Output: Formatted text
• Future: WPF (separate project, same backend)

INSTALLATION:
• Installer: Inno Setup (free, Windows-focused)
• Service Install: sc.exe commands
• Deployment: Single .exe installer

DEPENDENCIES (NuGet):
• Npgsql (PostgreSQL)
• System.Data.SQLite (SQLite fallback)
• Dapper (Data access)
• Microsoft.Extensions.Hosting
• Microsoft.Extensions.Configuration
• Microsoft.Extensions.Logging
• Microsoft.Extensions.DependencyInjection
• System.Management (WMI)

TESTING:
• Framework: xUnit or NUnit
• Mocking: Moq
• Coverage: Code coverage tool

DOCUMENTATION:
• Markdown files (already created)
• Inline code comments (will add)
• User guide (will create)
• Installation guide (will create)
```

---

## PART 13: NEXT STEPS (READY FOR CODING)

### Ready to Start Implementation:
Once you confirm this analysis is acceptable, I will:

1. **Create Project Structure** (Phase 1)
   - Solution file
   - Project files (Service, UI, Tests)
   - Folder structure
   - NuGet package configuration

2. **Implement Core Service** (Phase 2)
   - Window monitoring with P/Invoke
   - System metrics collection
   - Data queuing system
   - Service host wrapper

3. **Database Layer** (Phase 3)
   - Schema creation
   - Repository pattern
   - Connection management

4. **Integration & Testing** (Phase 4-5)
   - UI communication
   - Service testing
   - Database testing

5. **Installer Creation** (Phase 6)
   - Inno Setup script
   - Installation batch files

---

## SUMMARY OF CONFIRMED DECISIONS

✅ **Application Type**: Windows Desktop Application (NOT Web)  
✅ **Execution Model**: Windows Service + Console UI (testing) + WPF (later)  
✅ **Language**: C# / .NET 8.0  
✅ **IDE**: Visual Studio Code  
✅ **Database**: PostgreSQL (with SQLite fallback)  
✅ **Monitoring**: Windows API hooks + System.Diagnostics  
✅ **UI Approach**: Console first (testing), WPF later (fancy)  
✅ **Installer**: Inno Setup (distributable .exe)  
✅ **Development Phase**: Analysis ✓ COMPLETE → Ready for coding  

---

**Status**: ✅ Analysis Complete - Ready to Begin Implementation Phase 1
