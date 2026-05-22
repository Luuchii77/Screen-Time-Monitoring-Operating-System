# Screen Time & App Usage Monitoring System - Project Analysis

## Project Overview
Build a system that monitors and logs all computer activities including screen time, app usage frequency, and time spent per application.

---

## 1. Core Requirements Analysis

### Functional Requirements
- **Activity Monitoring**: Track all applications opened/closed on the system
- **Time Tracking**: Measure the duration each application is in focus/active
- **Daily Logging**: Record usage statistics per day
- **Usage Frequency**: Count how many times each app is used daily
- **OS Integration**: System must integrate as part of the OS startup process

### Non-Functional Requirements
- Minimal system performance impact
- Background execution without user interference
- Data persistence (logging to database/file)
- System-level installation capability
- Reliable startup with OS boot

### Additional Requirements (NEW)
- **Offline Operation**: System operates completely offline without internet connectivity
- **Lightweight Footprint**: Minimal memory and CPU usage to avoid performance degradation
- **Multi-Machine Capability**: Data can be transferred/synchronized across multiple computers
- **Windows Version Support**: Compatible with Windows 10 and Windows 11 (all build versions)
- **Cross-Build Compatibility**: Handle differences between Windows 10/11 builds gracefully

---

## 2. Technical Architecture Components

### 2.1 System Monitoring Layer
**What**: Hook into OS to detect window/application focus changes
- **On Windows**: Use Windows API (SetWinEventHook, GetForegroundWindow)
- **C#/.NET Implementation**: 
  - Use `SetWinEventHook()` via P/Invoke for window focus events
  - Monitor EVENT_SYSTEM_FOREGROUND for active window changes
  - Use `GetForegroundWindow()` to retrieve current active window
  - Use `GetWindowThreadProcessId()` to get process information
  - Use `System.Diagnostics.Process` for process details (name, memory, etc.)
  - Timer-based polling as fallback if event hooks miss changes
- **Detect**: Active window changes, process information, application name
- **Challenge**: Requires elevated privileges (admin/system level)
- **Offline**: All monitoring happens locally without network calls

### 2.2 Data Collection Layer
**What**: Gather raw activity data
- Track active application name/process ID
- Track timestamp of activity
- Track window title (if needed)
- Calculate active duration between focus changes

### 2.3 Logging & Storage Layer
**What**: Persist collected data locally (offline-capable)
- **Database**: SQLite (embedded, no server required, lightweight)
- **File-based logging**: JSON for easy data transfer between machines
- **Storage location**: System-protected directory (e.g., `C:\ProgramData\ScreenTimeMonitor\`)
- **Data schema**: For app statistics with export capability
- **Multi-Machine Support**: Data stored in portable format for migration
- **Lightweight Design**: Efficient queries, indexed database for quick lookups

### 2.4 Service/Daemon Layer
**What**: Ensure continuous background operation
- **Windows**: Windows Service (recommended for reliability)
  - Runs at SYSTEM privilege level
  - Auto-starts with OS boot
  - Compatible with Windows 10 and Windows 11
  - Handles service recovery if it crashes
- **Lightweight Approach**: Minimal memory resident, event-driven design
- **Linux**: Systemd service or daemon
- **macOS**: Launch daemon

### 2.5 Analysis & Reporting Layer
**What**: Generate insights from collected data
- Daily usage reports
- App usage frequency counts
- Time spent per application
- Visualizations (optional)

---

## 3. Data Model & Schema

### Core Data Structures

```
Activity Log Entry:
- timestamp (datetime)
- app_name (string)
- process_id (int)
- window_title (string)
- session_start (datetime)
- session_end (datetime)
- duration_minutes (int)

Daily Summary:
- date (date)
- app_name (string)
- total_usage_time (minutes)
- usage_count (int)
- first_use (datetime)
- last_use (datetime)

System Configuration:
- monitoring_enabled (bool)
- start_with_os (bool)
- data_retention_days (int)
```

---

## 4. Implementation Tasks Checklist

### Phase 1: Foundation (Setup & Architecture)
- [ ] Choose programming language (C#/.NET for Windows, Python, C++)
- [ ] Select OS platform (Windows, Linux, macOS, or cross-platform)
- [ ] Design database schema
- [ ] Set up development environment
- [ ] Create project structure

### Phase 2: Core Monitoring (Activity Detection)
- [ ] Implement OS-level window monitoring hook
- [ ] Implement process detection (get active application)
- [ ] Implement timestamp recording
- [ ] Handle window focus change events
- [ ] Test activity detection accuracy

### Phase 3: Data Collection & Storage
- [ ] Implement logging mechanism
- [ ] Create database/file storage system
- [ ] Implement data persistence
- [ ] Create data cleanup/archival mechanism
- [ ] Test data integrity

### Phase 4: Service Integration
- [ ] Create service installer for OS integration
- [ ] Configure auto-start on OS boot
- [ ] Implement service startup/shutdown logic
- [ ] Handle service permissions (admin/system)
- [ ] Test service reliability

### Phase 5: Analysis & Reporting
- [ ] Create data aggregation functions (daily summaries)
- [ ] Calculate usage statistics (frequency, duration)
- [ ] Create reporting interface
- [ ] Generate usage reports
- [ ] Test report accuracy

### Phase 6: UI & User Interface (Optional)
- [ ] Create configuration UI
- [ ] Create dashboard/viewer for statistics
- [ ] Add settings management
- [ ] Implement data export features

### Phase 7: Testing & Optimization
- [ ] Unit testing
- [ ] Integration testing
- [ ] Performance testing
- [ ] Memory leak detection
- [ ] Security testing (ensure admin-only access)

### Phase 8: Documentation & Deployment
- [ ] Write installation guide
- [ ] Create user documentation
- [ ] Write technical documentation
- [ ] Create deployment script
- [ ] Package for distribution

---

## 5. Platform-Specific Considerations

### Windows Implementation
**Advantages**: Clear API, enterprise-level tools available
- Use `SetWinEventHook()` for window events
- Use `GetForegroundWindow()` to get active window
- Windows Service for background execution
- Requires admin privileges

**Tools**: C#/.NET, Windows API, Task Scheduler

### Linux Implementation
**Advantages**: Open system, access to process information
- Monitor `/proc` filesystem
- Use X11 or Wayland event system
- Systemd service for background execution

**Tools**: Python, C, systemd

### Cross-Platform Implementation
- Use abstraction layers for OS-specific code
- Separate business logic from OS hooks

---

## 6. Security & Privacy Considerations

- [ ] Ensure admin-only access to logs
- [ ] Secure database with encryption (optional)
- [ ] Implement user consent mechanisms
- [ ] Comply with data privacy regulations
- [ ] Secure service startup (prevent tampering)
- [ ] Validate and sanitize all inputs
- [ ] Implement proper error handling
- [ ] Data portability: Encrypt sensitive data when exporting for multi-machine use
- [ ] No external network calls (offline-only operation)
- [ ] Secure database credentials if password-protected

---

## 7. Performance Considerations

- **Minimal Resource Usage**: Event-driven, not polling
- **Memory Efficiency**: Use circular buffers, limit in-memory cache
- **Disk I/O**: Batch writes, use efficient data formats
- **CPU Impact**: Lightweight event processing
- **Database Optimization**: Indexing, query optimization

---

## 8. Key Technical Challenges

1. **Elevated Privileges**: Need admin/system access for OS hooks
2. **Multithreading**: Handle concurrent activity logging safely
3. **System Integration**: Making service reliable and persistent
4. **Data Volume**: Managing large amounts of logged data
5. **OS Compatibility**: Different APIs for different operating systems
6. **Performance Impact**: Ensuring minimal slowdown

---

## 9. Technology Stack Recommendation

### Option A: Windows (.NET/C#) - RECOMMENDED FOR YOUR PROJECT
- **Language**: C# / .NET Framework (6.0 or 8.0)
- **Monitoring**: Windows API via P/Invoke for window hooks
  - SetWinEventHook for EVENT_SYSTEM_FOREGROUND
  - GetForegroundWindow for active window detection
  - System.Diagnostics.Process for application info
- **Database**: SQLite (lightweight, embedded, offline-capable)
- **Service**: Windows Service (auto-start, SYSTEM privilege)
- **UI**: WPF or Console App (for dashboard/reporting)
- **Windows Compatibility**: 
  - Windows 10 (all recent builds) and Windows 11
  - Uses stable Windows APIs supported across versions
  - Version detection to handle OS-specific behaviors
- **Lightweight**: Minimal dependencies, efficient memory usage
- **Offline**: Completely self-contained, no internet required
- **Multi-Machine**: Export/import data as JSON for portability

### Option B: Cross-Platform (Python)
- **Language**: Python 3.x
- **Monitoring**: pygetwindow, psutil, pynput
- **Database**: SQLite
- **Service**: systemd (Linux), LaunchAgent (macOS), Windows Service wrapper
- **UI**: tkinter or web-based (Flask/FastAPI)

### Option C: Low-Level (C/C++)
- **Language**: C/C++
- **Monitoring**: Native OS APIs
- **Database**: SQLite with C bindings
- **Service**: Native implementation
- **Advantages**: Best performance, smallest footprint

---

## 10. Monitoring Implementation Details (C#/.NET)

### Screen Time Tracking Approach
To measure time spent on each application:

1. **Event Hook Method** (Recommended)
   - Set up `SetWinEventHook()` to monitor `EVENT_SYSTEM_FOREGROUND`
   - When window focus changes, record the previous app's end time
   - Calculate duration = (end_time - start_time)
   - Store session record in SQLite

2. **Implementation Steps**
   - P/Invoke declarations for Windows API functions
   - Create WindowEventHook callback method
   - On each window change event:
     - Get current foreground window using `GetForegroundWindow()`
     - Extract process name and ID using `GetWindowThreadProcessId()`
     - Log session end for previous app (with duration)
     - Log session start for new app (with timestamp)
   - Store all data in local SQLite database

3. **Data Collection**
   - Timestamp when app gains focus (start)
   - Timestamp when app loses focus (end)
   - Calculate duration in milliseconds
   - Store process ID, window title, application name
   - No network calls or external dependencies

4. **Offline Data Storage**
   - All data stored locally in SQLite
   - No internet connectivity required
   - Data can be exported as JSON for multi-machine transfer
   - Efficient database indexes for quick queries

### Windows 10/11 Compatibility
- Use common Windows API calls available in both Windows 10 and Windows 11
- APIs like `SetWinEventHook()` and `GetForegroundWindow()` are stable across versions
- Test on Windows 10 Build 19041+ and Windows 11 (all builds)
- Handle minor API differences with OS version detection if needed
- Graceful fallback mechanisms for edge cases

---

## 11. Project Timeline Estimate

- **Phase 1-2**: 1-2 weeks (Core monitoring setup with P/Invoke)
- **Phase 3-4**: 1-2 weeks (SQLite storage & Windows Service integration)
- **Phase 5-6**: 1 week (Analysis, reporting & data export for multi-machine)
- **Phase 7-8**: 1 week (Testing on Windows 10/11 & deployment)

**Total**: 4-6 weeks for full implementation

---

## Next Steps

1. **Decide on platform**: Windows, Linux, or cross-platform?
2. **Choose technology stack**: C#, Python, or C++?
3. **Set specific requirements**: What data to track, retention period, target users?
4. **Create project skeleton**: Set up repository structure
5. **Implement Phase 1-2**: Start with core monitoring
