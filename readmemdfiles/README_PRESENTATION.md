# Screen Time Monitor - Presentation Guide

## 📊 Project Overview (For PowerPoint Slides)

This guide is organized for creating presentation slides. Each section below can be a slide or group of slides.

---

## Slide 1: Project Title & Objective

### Screen Time Monitor
**A Personal Application to Track Computer Usage**

**Objective:**
Monitor which applications are running on a Windows PC and track how long each application is being used in real-time.

---

## Slide 2: The Problem

**Why This Project?**
- 🎯 No built-in Windows tool shows detailed app-by-app screen time
- 📊 Need to understand personal productivity and time management
- ⏱️ Want to see real-time usage statistics
- 💾 Require persistent tracking across sessions

---

## Slide 3: Solution Overview

**What Does Screen Time Monitor Do?**

✅ **Monitors Running Applications**
- Detects all active programs on Windows
- Identifies which application has focus
- Tracks process information in real-time

✅ **Tracks Usage Duration**
- Records how long each app runs
- Measures time in real-time
- Maintains historical data across sessions

✅ **Displays Statistics**
- Shows all currently running apps
- Displays duration for each app
- Calculates total screen time

---

## Slide 4: Key Features

| Feature | Description |
|---------|-------------|
| 🔍 **Real-Time Monitoring** | Tracks applications as they run |
| 💾 **Persistent Storage** | Saves usage history in local database |
| 📱 **Desktop UI** | User-friendly Windows interface |
| ⚙️ **Background Service** | Runs silently in the background |
| 📊 **Usage Statistics** | Shows total time per application |
| 🔄 **Session Management** | Tracks sessions across restarts |
| 🚀 **Fast & Efficient** | Minimal CPU/memory impact |

---

## Slide 5: Architecture - High Level

**How It Works - System Overview:**

```
┌─────────────────────────────────────┐
│   Windows Operating System          │
│  (Running Applications & Processes) │
└────────────┬────────────────────────┘
             │
    ┌────────▼─────────┐
    │  Background      │
    │  Service         │  ← Monitors processes
    │  (Windows        │     24/7
    │   Service)       │
    └────────┬─────────┘
             │
    ┌────────▼──────────────┐
    │  Named Pipe IPC       │  ← Inter-process communication
    │  (Data Transfer)      │
    └────────┬──────────────┘
             │
    ┌────────▼─────────┐
    │  Desktop UI      │     ← Shows usage
    │  (WPF)           │        statistics
    └──────────────────┘
             │
    ┌────────▼──────────────┐
    │  SQLite Database      │  ← Stores history
    │  (Usage Records)      │
    └───────────────────────┘
```

---

## Slide 6: Technology Stack

**Languages & Frameworks:**
- **Language:** C# (.NET 8.0)
- **Service:** Windows Service (.NET)
- **UI:** WPF (Windows Presentation Foundation) with XAML
- **Database:** SQLite
- **Communication:** Named Pipes (IPC)

**Key Libraries:**
- Entity Framework Core (Database)
- Dependency Injection
- Windows API P/Invoke (for process detection)

---

## Slide 7: How It Works - User Perspective

**User Experience Flow:**

1. **Launch Application**
   - User opens the application
   - Service starts monitoring in background

2. **View Running Apps**
   - UI displays all currently active applications
   - Shows duration for each app

3. **Real-Time Updates**
   - Timer updates every second
   - Durations increase in real-time

4. **Close Application**
   - Usage data is saved to database
   - History persists for next session

5. **Later Session**
   - Reopen application
   - Previous durations are loaded
   - Tracking continues from where it left off

---

## Slide 8: Technical Deep Dive - Process Detection

**How Does It Find Running Apps?**

```
Step 1: Enumerate Processes
└─ Uses Windows API to get all running processes

Step 2: Filter Visible Windows
└─ Checks which processes have visible windows
└─ Ignores background/system processes

Step 3: Identify Main Application
└─ Matches process to actual user-facing app
└─ Excludes helper processes

Step 4: Track Duration
└─ Records start time
└─ Calculates elapsed time
```

**Windows API Used:**
- `EnumWindows()` - List all windows
- `GetWindowThreadProcessId()` - Match window to process
- `IsWindowVisible()` - Confirm window is visible

---

## Slide 9: Technical Deep Dive - Data Storage

**How Is Usage Data Saved?**

**Database Table: AppUsageSessions**
```
Column Name       | Type     | Purpose
─────────────────────────────────────
SessionId         | Integer  | Unique identifier
AppName           | Text     | Application name
ProcessId         | Integer  | Windows process ID
SessionStart      | DateTime | When app started
SessionEnd        | DateTime | When app closed
DurationMs        | Long     | Total milliseconds used
```

**Example Record:**
```
AppName: "Code"
SessionStart: 2025-01-10 09:00:00
SessionEnd: 2025-01-10 09:30:00
DurationMs: 1800000  (30 minutes)
```

---

## Slide 10: Technical Deep Dive - Communication

**How Do Service & UI Talk?**

**Named Pipe IPC Protocol:**

```
UI                          Service
│                             │
├──── "GET_RUNNING_APPS" ────→│
│                             │
│                        (Queries running
│                         processes & DB)
│                             │
│←─ "[JSON data]" ───────────┤
│                             │
│  (Displays in UI)           │
└─────────────────────────────┘
```

**Why Named Pipes?**
- ✅ Process isolation (security)
- ✅ Asynchronous communication
- ✅ Lightweight and fast
- ✅ Built into Windows

---

## Slide 11: File Structure

**Project Organization:**

```
ScreenTimeMonitor/
├── ScreenTimeMonitor.Service/      ← Background service
│   ├── Services/                   ← Business logic
│   ├── Database/                   ← Data access
│   └── Models/                     ← Data models
│
├── ScreenTimeMonitor.UI.WPF/       ← Desktop application
│   ├── Views/                      ← UI screens (XAML)
│   ├── ViewModels/                 ← UI logic
│   └── Services/                   ← IPC communication
│
├── ScreenTimeMonitor.Tests/        ← Unit tests
│
└── Database/
    ├── schema-sqlite.sql           ← Database structure
    └── schema-postgresql.sql       ← Alternative schema
```

---

## Slide 12: Key Components Explained

### 1. **BackgroundProcessMonitorService**
- Runs continuously in the background
- Scans for running processes every second
- Detects when apps start/close
- Saves to database

### 2. **IPC Communication Service**
- Receives requests from UI
- Queries process data
- Sends responses via named pipes

### 3. **Database Repository**
- Stores application usage sessions
- Retrieves historical data
- Calculates cumulative usage

### 4. **WPF UI (MainWindow)**
- Displays running applications
- Shows duration for each app
- Updates in real-time
- Communicates via named pipes

---

## Slide 13: Features & Capabilities

**What Can It Track?**

✅ **All Applications**
- Office applications (Word, Excel)
- Development tools (VS Code, Visual Studio)
- Browsers (Chrome, Edge, Firefox)
- Games and entertainment
- Any Windows application

**What Does It Show?**
- Application name
- Current session duration
- Total accumulated duration (current + historical)
- Real-time updates every second

---

## Slide 14: Demo Scenario

**Example: Tracking Developer's Day**

**Morning Session (9:00 AM - 12:30 PM):**
- VS Code: 2h 15m
- Chrome: 1h 00m
- Discord: 0h 15m
- Notepad: 0h 10m

**After Lunch (1:30 PM - 3:00 PM):**
- VS Code: 1h 30m (total now: 3h 45m)
- Excel: 0h 30m
- Teams: 0h 10m

---

## Slide 15: Performance & Efficiency

**Resource Usage:**
- 💾 **Memory:** ~50-100 MB (light)
- ⚡ **CPU:** <5% (minimal)
- 🔋 **Battery:** Negligible impact
- 📊 **Database:** <1 MB per month of data

**Why It's Efficient:**
- Service uses Windows API efficiently
- Named pipes are lightweight
- Database queries are optimized
- UI updates are minimal

---

## Slide 16: Portability & Distribution

**Google Drive Compatible:**
- 📁 All paths are relative (`./data/`, `./logs/`)
- ☁️ No machine-specific configuration
- 🚀 Works on any Windows machine with .NET 8.0
- 🔄 Setup takes <5 minutes with provided script

**Multi-Device Workflow:**
1. Upload to Google Drive
2. Download on another PC
3. Run `setup.ps1`
4. Application ready to use

---

## Slide 17: Error Handling & Robustness

**How It Handles Problems:**

✅ **Process Crashes**
- Service automatically restarts monitoring
- Lost sessions are saved to database

✅ **Database Issues**
- Automatic retry logic
- Fallback to in-memory tracking

✅ **UI Disconnects**
- Service continues tracking
- UI reconnects automatically

✅ **Windows API Failures**
- Graceful degradation
- Logs errors for debugging

---

## Slide 18: Testing & Verification

**Quality Assurance:**

**Unit Tests**
- Test database operations
- Test IPC communication
- Test data calculations

**Integration Tests**
- Service + Database
- Service + UI communication
- Full application workflow

**Manual Testing**
- Real-world app tracking
- Long-term stability
- Multi-session persistence

---

## Slide 19: Challenges & Solutions

| Challenge | Solution |
|-----------|----------|
| Tracking background processes | Window visibility filtering |
| Process name matching | Case-insensitive comparison |
| Data persistence across restarts | SQLite database with sessions |
| UI freezing during monitoring | Async operations & threading |
| Multi-device setup | Relative paths + setup scripts |

---

## Slide 20: Future Enhancements

**Possible Improvements:**
- 📈 Advanced analytics & charts
- ⏰ Usage alerts & reminders
- 🎯 Productivity goals & tracking
- 📱 Mobile app companion
- ☁️ Cloud sync across devices
- 🔔 Notifications for excessive usage
- 🎨 Custom themes

---

## Slide 21: Conclusion

**What We Built:**
A fully functional, portable Windows application that monitors and tracks application usage in real-time with persistent storage.

**Key Achievements:**
✅ Real-time process monitoring
✅ Persistent data storage
✅ User-friendly interface
✅ Efficient resource usage
✅ Multi-device portability
✅ Robust error handling

**Why It Matters:**
Provides personal productivity insights and time management awareness on Windows systems.

---

## Quick Reference for Presenters

**Key Numbers:**
- 🔧 Built in: C# .NET 8.0
- 📊 Database: SQLite (single file)
- ⚡ Update interval: 1 second
- 💾 Memory usage: 50-100 MB
- 🚀 Setup time: <5 minutes

**Key Points:**
- Service + UI architecture with IPC
- Real-time monitoring with historical tracking
- Fully portable via Google Drive
- Efficient and lightweight

**Demo Focus:**
- Show application running
- Add/close applications to show real-time tracking
- Show data persists across restarts
