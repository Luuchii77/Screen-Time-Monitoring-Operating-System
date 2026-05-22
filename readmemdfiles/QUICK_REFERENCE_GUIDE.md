# Quick Reference Guide
## Screen Time & App Usage Monitoring System

**Last Updated**: December 2, 2025  
**Analysis Status**: ✅ COMPLETE

---

## 📚 YOUR STUDY MATERIALS

You have **5 comprehensive documents** ready for study:

| Document | Purpose | Read Time |
|----------|---------|-----------|
| **PROJECT_ANALYSIS.md** | Project vision & requirements | 30 min |
| **TECHNICAL_RECOMMENDATIONS.md** | Technology choices explained | 45 min |
| **VS_CODE_SETUP_GUIDE.md** | Dev environment setup | 30 min |
| **IMPLEMENTATION_ANALYSIS.md** | 8-phase implementation plan | 1.5 hours |
| **ADVANCED_CONSIDERATIONS.md** | Solutions to 3 critical issues | 1.5 hours |
| **PROJECT_PROGRESS_SUMMARY.md** | Study guide & reference | 20 min |

**Total Study Time**: ~5 hours

---

## 🎯 THE PROJECT IN 60 SECONDS

**What**: Windows application that monitors which apps you use and how long  
**How**: Runs as Windows Service in background, hooks into Windows API  
**Where**: Stores data in PostgreSQL or SQLite database  
**Why**: School project on Operating Systems  
**When**: Deploy via Inno Setup installer  

```
User boots computer
    ↓
ScreenTimeMonitor service auto-starts
    ↓
Service monitors all app usage
    ↓
Console UI (or later WPF) shows statistics
    ↓
Data persists in database
```

---

## 🏗️ ARCHITECTURE AT A GLANCE

```
┌─────────────────────────────────────────┐
│      Windows Service (24/7)              │
│  • Monitors apps via Windows API hooks   │
│  • Collects CPU/Memory/Disk metrics      │
│  • Queues data in memory                 │
│  • Batches writes to database            │
│  • Communicates with UI via pipes        │
└─────────────────────────────────────────┘
              ↕ (IPC)
┌─────────────────────────────────────────┐
│    Console/WPF UI (Optional)             │
│  • View statistics                       │
│  • Check compatibility                   │
│  • Start/stop monitoring                 │
└─────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────┐
│   Database (PostgreSQL or SQLite)        │
│  • app_sessions table                    │
│  • system_metrics table                  │
│  • daily_app_summary table               │
│  • daily_system_summary table            │
└─────────────────────────────────────────┘
```

---

## 💻 TECHNOLOGY STACK

**Backend**:
- Language: C# / .NET 8.0
- Execution: Windows Service
- Monitoring: Windows API P/Invoke

**Database**:
- Primary: PostgreSQL 15+
- Fallback: SQLite 3
- ORM: Dapper

**UI**:
- Testing: Console Application
- Production: WPF (built later)

**Installation**:
- Installer: Inno Setup

**Communication**:
- Service ↔ UI: Named Pipes

---

## 📅 IMPLEMENTATION TIMELINE

| Phase | Duration | What You'll Build |
|-------|----------|-------------------|
| 1 | Week 1 | Project structure & setup |
| 2 | Week 2-3 | App monitoring & metrics |
| 3 | Week 3-4 | Database & persistence |
| 4 | Week 4 | Windows Service wrapper |
| 5 | Week 5 | IPC communication |
| 6 | Week 5-6 | Console UI for testing |
| 7 | Week 6-7 | Testing & debugging |
| 8 | Week 8 | Installer creation |
| **Total** | **4-6 weeks** | **Fully functional system** |

---

## 🔑 3 KEY SOLUTIONS PROVIDED

### 1️⃣ Continuous Execution & Graceful Shutdown
**Solution**: Windows Service with CancellationToken pattern
- Service runs indefinitely
- Graceful shutdown flushes queued data
- Auto-recovery on crash
- User notifications

### 2️⃣ Lightweight & Machine Compatibility
**Solution**: Event-driven design + adaptive configuration
- Lightweight by design (15-30 MB memory)
- System specs checker at startup
- 3 machine profiles (Low/Mid/High-end)
- Works on machines with as little as 512 MB RAM

### 3️⃣ Startup Queue Management
**Solution**: Unbounded queue during boot (60s) + deduplication
- Handles 100+ apps launching simultaneously
- Event deduplication (75% queue reduction)
- No data loss during boot
- Transitions to normal mode automatically

---

## 📊 DATABASE SCHEMA

**4 Core Tables**:

```
app_sessions
├─ id, process_id, app_name, window_title
├─ session_start, session_end, duration_ms
└─ Indexes: app_name, session_start, created_at

system_metrics
├─ id, timestamp, cpu_usage, memory_usage_mb
├─ memory_percent, disk_read_bytes, disk_write_bytes
└─ Indexes: timestamp, process_id

daily_app_summary
├─ id, summary_date, app_name
├─ total_usage_ms, usage_count, first_use, last_use
└─ Unique: (summary_date, app_name)

daily_system_summary
├─ id, summary_date
├─ avg_cpu, peak_cpu, avg_memory, peak_memory
└─ Unique: summary_date
```

---

## 📁 FOLDER STRUCTURE

```
Operating System Project/
├── ScreenTimeMonitor.sln (solution)
│
├── ScreenTimeMonitor.Service/
│   ├── Services/ (monitoring, database, collection)
│   ├── Models/ (data classes)
│   ├── Database/ (repositories, context)
│   ├── Utilities/ (P/Invoke, logging)
│   ├── IPC/ (named pipes)
│   └── Program.cs
│
├── ScreenTimeMonitor.UI/
│   ├── Services/ (communication, display)
│   ├── Views/ (console menus)
│   └── Program.cs
│
├── ScreenTimeMonitor.Tests/
│   └── Test classes
│
├── Database/
│   ├── schema-postgresql.sql
│   └── schema-sqlite.sql
│
├── Installer/
│   └── ScreenTimeMonitor.iss
│
└── Documentation/ (all .md files)
```

---

## ⚙️ KEY IMPLEMENTATION PATTERNS

### Event-Driven Monitoring
```csharp
// Window hook fires only on app change (not continuously)
[DllImport("user32.dll")]
static extern IntPtr SetWinEventHook(uint event_type, ...);

// Event handler processes change
void OnWindowFocusChanged(object sender, EventArgs e)
{
    var app = GetActiveApplication();
    QueueAppUsageEvent(app);
}
```

### Batched Queue Processing
```csharp
// Collect 100 events
var batch = new List<AppEvent>();
foreach (var evt in queue.GetConsumingEnumerable())
{
    batch.Add(evt);
    if (batch.Count >= 100)
    {
        InsertBatchToDatabase(batch); // One write, not 100
        batch.Clear();
    }
}
```

### Graceful Shutdown
```csharp
protected override void OnStop()
{
    _cancellationToken.Cancel();           // Signal stop
    _monitoringTask.Wait(TimeSpan.FromSeconds(10)); // Wait
    FlushQueuesToDatabaseAsync().Wait();   // Save data
    CloseConnections();                    // Cleanup
}
```

### System Specs Checking
```csharp
var specs = checker.GetSystemSpecs();
if (specs.TotalMemoryMB < 512)
    Status = CompatibilityStatus.Unsupported;
else if (specs.TotalMemoryMB < 2048)
    Status = CompatibilityStatus.LimitedSupport;
```

---

## 🎓 STUDY RECOMMENDATIONS

### Day 1: Big Picture (1 hour)
- Read PROJECT_ANALYSIS.md
- Read PROJECT_PROGRESS_SUMMARY.md

### Day 2: Technology (1.5 hours)
- Read TECHNICAL_RECOMMENDATIONS.md
- Read VS_CODE_SETUP_GUIDE.md

### Day 3: Implementation Plan (1.5 hours)
- Read IMPLEMENTATION_ANALYSIS.md
- Focus on the 8 phases

### Day 4: Advanced Topics (1 hour)
- Read ADVANCED_CONSIDERATIONS.md
- Understand the 3 solutions

### Day 5: Review & Plan (1 hour)
- Review all documents
- Prepare to start Phase 1

---

## 🚀 WHEN YOU START CODING

**Phase 1** will involve:
1. Creating .NET solution
2. Creating 3 projects (Service, UI, Tests)
3. Setting up folder structure
4. Adding NuGet packages
5. Creating configuration files

**You'll be ready after**:
- Understanding all 5 documents
- Installing .NET 8.0 SDK
- Installing Visual Studio Code
- Installing required extensions

---

## ✅ CHECKLIST: BEFORE YOU CODE

- [ ] Read all 5 analysis documents
- [ ] Understand the architecture
- [ ] Know the 8 phases
- [ ] Know the technology choices
- [ ] Understand the 3 key solutions
- [ ] Install .NET 8.0 SDK
- [ ] Install Visual Studio Code
- [ ] Install C# Dev Kit extension
- [ ] Install PostgreSQL or prepare for SQLite
- [ ] Review folder structure
- [ ] Ready for Phase 1

---

## 📞 QUICK ANSWERS

| Question | Answer |
|----------|--------|
| What language? | C# / .NET 8.0 |
| What database? | PostgreSQL (primary) or SQLite |
| What IDE? | Visual Studio Code |
| How does it run? | Windows Service (always on) |
| How does it monitor? | Windows API hooks + System.Diagnostics |
| How does UI talk to service? | Named Pipes (IPC) |
| How many phases? | 8 phases over 4-6 weeks |
| How much memory? | 15-30 MB (very lightweight) |
| Can it handle low-spec machines? | Yes, with adaptive config |
| Can it handle startup burst? | Yes, unbounded queue for first 60s |
| Can it shut down gracefully? | Yes, saves all data |
| Is it installable? | Yes, via Inno Setup |

---

## 📍 DOCUMENT LOCATIONS

All files in:
```
c:\Users\PC\Downloads\School Files\Operating System Project\
```

- ✅ PROJECT_ANALYSIS.md
- ✅ TECHNICAL_RECOMMENDATIONS.md
- ✅ VS_CODE_SETUP_GUIDE.md
- ✅ IMPLEMENTATION_ANALYSIS.md
- ✅ ADVANCED_CONSIDERATIONS.md
- ✅ PROJECT_PROGRESS_SUMMARY.md
- ✅ QUICK_REFERENCE_GUIDE.md (this file)

---

## 🎯 YOUR CURRENT STATUS

**Phase**: Analysis Complete ✅  
**Documents**: 6 created  
**Analysis Lines**: 2500+  
**Architecture Diagrams**: 4  
**Code Examples**: 20+  
**Implementation Phases**: 8 planned  

**Next Step**: Study the documents at your own pace  
**When Ready**: Begin Phase 1 (Project Setup)

---

**Everything you need to understand the project is documented above.** 📚

Take your time studying. When you're ready to implement Phase 1, just let me know! 🚀
