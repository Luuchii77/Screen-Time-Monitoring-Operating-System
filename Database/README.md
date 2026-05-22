# Screen Time Monitor - Database Setup Guide

## Overview
This directory contains database schema and configuration files for the Screen Time Monitor application.

## Database Support

The application supports two database backends:

### 1. SQLite (Default - Recommended for Most Users)
- **File-based**: No server installation required
- **Location**: `C:\ProgramData\ScreenTimeMonitor\screentime_monitor.db`
- **Features**: 
  - Write-Ahead Logging (WAL) for better concurrency
  - Foreign key constraints enabled
  - Optimized for the monitoring workload
  - Zero configuration needed
- **Best for**: Offline use, simple deployments, school projects

### 2. PostgreSQL (Advanced)
- **Server-based**: Requires PostgreSQL server installation
- **Connection**: `Host=localhost;Port=5432;Username=postgres;Password=;Database=screentime_monitor`
- **Features**:
  - Full ACID compliance
  - Better concurrent write handling
  - More advanced features
  - Suitable for larger deployments
- **Best for**: Enterprise environments, high-volume monitoring

## Setup Instructions

### Using SQLite (Default)

**No additional setup required!** The application will:
1. Create the database file automatically at the configured location
2. Initialize the schema on first run
3. Enable WAL mode for optimal performance

The database is created on first service startup.

### Using PostgreSQL

#### 1. Install PostgreSQL
```powershell
# Download from https://www.postgresql.org/download/windows/
# Run installer and remember the postgres password
```

#### 2. Create Database
```powershell
# Open PowerShell and run:
psql -U postgres

# In psql prompt:
CREATE DATABASE screentime_monitor;
\q
```

#### 3. Initialize Schema
```powershell
# Option A: Using psql (Manual)
psql -U postgres -d screentime_monitor -f "Database\schema-postgresql.sql"

# Option B: Application will auto-initialize on first run (Recommended)
```

#### 4. Configure appsettings.json
```json
{
  "MonitoringSettings": {
    "UsePostgreSQL": true
  },
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Port=5432;Username=postgres;Password=YOUR_PASSWORD;Database=screentime_monitor;"
  }
}
```

## Database Tables

### 1. AppUsageSessions
Stores individual application usage sessions.

**Columns:**
- `id` - Primary key
- `app_name` - Application name
- `session_start` - When app gained focus
- `session_end` - When app lost focus
- `duration_ms` - Session duration in milliseconds
- `window_title` - Window title when active
- `process_id` - Windows process ID
- `created_at` - Timestamp when recorded

**Typical Volume:**
- ~50-200 sessions per day (depends on user behavior)
- ~2KB per session
- ~100KB-400KB per day

### 2. SystemMetrics
Stores periodic system performance metrics.

**Columns:**
- `id` - Primary key
- `timestamp` - When metric was captured
- `cpu_usage_percent` - CPU usage 0-100%
- `memory_usage_mb` - Used memory in MB
- `total_memory_mb` - Total system memory
- `disk_read_bytes` - Disk read bytes
- `disk_write_bytes` - Disk write bytes
- `process_id` - Process being monitored
- `created_at` - Timestamp when recorded

**Typical Volume:**
- ~720 metrics per day (one every ~2 minutes)
- ~100 bytes per metric
- ~72KB per day

### 3. DailyAppSummaries
Aggregated daily statistics per application.

**Columns:**
- `id` - Primary key
- `app_name` - Application name
- `summary_date` - The date of the summary
- `total_duration_ms` - Total time app was active (milliseconds)
- `session_count` - Number of times app was used
- `first_use` - Time of first use that day
- `last_use` - Time of last use that day
- `created_at` - When summary was created

**Typical Volume:**
- ~10-30 unique apps per day
- ~300 bytes per summary
- ~3KB-9KB per day

### 4. DailySystemSummaries
Aggregated daily system performance metrics.

**Columns:**
- `id` - Primary key
- `summary_date` - The date of the summary
- `avg_cpu_usage_percent` - Average CPU usage
- `peak_cpu_usage_percent` - Peak CPU usage
- `avg_memory_usage_mb` - Average memory usage
- `peak_memory_usage_mb` - Peak memory usage
- `total_uptime_minutes` - Total system uptime that day
- `total_session_count` - Total application sessions that day
- `created_at` - When summary was created

**Typical Volume:**
- 1 summary per day
- ~100 bytes per summary
- ~100 bytes per day

## Storage Estimates

### Daily Storage Growth

| Scenario | SQLite | PostgreSQL |
|----------|--------|-----------|
| Light Use (5 apps/day) | ~180 KB | ~220 KB |
| Medium Use (15 apps/day) | ~280 KB | ~340 KB |
| Heavy Use (30+ apps/day) | ~380 KB | ~460 KB |

### Yearly Storage (With Default 90-day Retention)

| Scenario | SQLite | PostgreSQL |
|----------|--------|-----------|
| Light Use | ~50 MB | ~65 MB |
| Medium Use | ~80 MB | ~100 MB |
| Heavy Use | ~110 MB | ~130 MB |

**Note:** Total storage includes database overhead, indices, and WAL mode overhead.

## Data Retention

By default, data older than 90 days is automatically cleaned up when the service stops.

To change retention period, modify `appsettings.json`:
```json
{
  "MonitoringSettings": {
    "DataRetentionDays": 180  // Keep 180 days of data
  }
}
```

## Performance Optimization

### For SQLite
- WAL mode enabled by default
- Foreign keys enabled
- Autovacuum enabled
- Indices on frequently queried columns

### For PostgreSQL
- Connection pooling
- Query optimization
- Autovacuum configured
- Indices on frequently queried columns
- Batch inserts for better performance

## Backup and Recovery

### SQLite Backup
```powershell
# Copy the database file to backup location
Copy-Item "C:\ProgramData\ScreenTimeMonitor\screentime_monitor.db" "D:\Backups\screentime_monitor.db"

# WAL files (optional but recommended)
Copy-Item "C:\ProgramData\ScreenTimeMonitor\screentime_monitor.db-wal" "D:\Backups\screentime_monitor.db-wal"
Copy-Item "C:\ProgramData\ScreenTimeMonitor\screentime_monitor.db-shm" "D:\Backups\screentime_monitor.db-shm"
```

### PostgreSQL Backup
```powershell
# Full database backup
pg_dump -U postgres -d screentime_monitor -f "D:\Backups\screentime_monitor_backup.sql"

# Compressed backup (recommended)
pg_dump -U postgres -d screentime_monitor -Fc -f "D:\Backups\screentime_monitor_backup.dump"
```

## Troubleshooting

### Database File Locked Error
**SQLite Only**

If you get "database is locked" errors:
1. Ensure WAL mode is enabled
2. Check that only one service instance is running
3. Close any other connections to the database

### Connection Issues (PostgreSQL)
```powershell
# Test connection
psql -U postgres -h localhost -d screentime_monitor

# Check if PostgreSQL is running
Get-Service postgresql-x64-15  # Adjust version number
```

### Corrupt Database
**SQLite**:
```powershell
# Check database integrity
sqlite3 "C:\ProgramData\ScreenTimeMonitor\screentime_monitor.db" "PRAGMA integrity_check;"
```

**PostgreSQL**:
```sql
REINDEX DATABASE screentime_monitor;
```

## Schema Migration

If updating the application with schema changes:

1. **Backup existing database** first
2. **Review migration scripts** in this directory
3. **Apply migrations** in order:
   ```powershell
   # For SQLite
   sqlite3 screentime_monitor.db < migration-001.sql
   
   # For PostgreSQL
   psql -U postgres -d screentime_monitor < migration-001.sql
   ```

## API Reference

See the repositories in `ScreenTimeMonitor.Service\Database\` for:
- `AppUsageRepository` - Query/insert app usage data
- `SystemMetricsRepository` - Query/insert system metrics
- `DailyAppSummaryRepository` - Query/insert daily app summaries
- `DailySystemSummaryRepository` - Query/insert daily system summaries

## Support

For issues or questions about the database setup, refer to the main README or documentation.
