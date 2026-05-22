-- Screen Time Monitor - SQLite Database Schema
-- This script creates the complete database schema for the monitoring system using SQLite
-- The database file will be created automatically at: C:\ProgramData\ScreenTimeMonitor\screentime_monitor.db

-- Enable Write-Ahead Logging for better concurrency
PRAGMA journal_mode = WAL;

-- Enable foreign key constraints
PRAGMA foreign_keys = ON;

-- Set synchronous mode to NORMAL for better performance
PRAGMA synchronous = NORMAL;

-- Create AppUsageSessions table
-- Stores individual application usage sessions
CREATE TABLE IF NOT EXISTS AppUsageSessions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    AppName TEXT NOT NULL,
    SessionStart DATETIME NOT NULL,
    SessionEnd DATETIME,
    DurationMs INTEGER,
    WindowTitle TEXT,
    ProcessId INTEGER,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    CHECK (SessionEnd IS NULL OR SessionEnd > SessionStart),
    CHECK (DurationMs IS NULL OR DurationMs >= 0)
);

-- Create indices for AppUsageSessions
CREATE INDEX IF NOT EXISTS idx_app_usage_sessions_app_name ON AppUsageSessions(AppName);
CREATE INDEX IF NOT EXISTS idx_app_usage_sessions_start_time ON AppUsageSessions(SessionStart);
CREATE INDEX IF NOT EXISTS idx_app_usage_sessions_created_at ON AppUsageSessions(CreatedAt);

-- Create SystemMetrics table
-- Stores periodic system performance metrics
CREATE TABLE IF NOT EXISTS SystemMetrics (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Timestamp DATETIME NOT NULL,
    CpuUsagePercent REAL,
    MemoryUsageMb INTEGER,
    TotalMemoryMb INTEGER,
    DiskReadBytes INTEGER,
    DiskWriteBytes INTEGER,
    ProcessId INTEGER,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    CHECK (CpuUsagePercent IS NULL OR (CpuUsagePercent >= 0 AND CpuUsagePercent <= 100))
);

-- Create indices for SystemMetrics
CREATE INDEX IF NOT EXISTS idx_system_metrics_timestamp ON SystemMetrics(Timestamp);
CREATE INDEX IF NOT EXISTS idx_system_metrics_created_at ON SystemMetrics(CreatedAt);

-- Create DailyAppSummaries table
-- Stores aggregated daily usage statistics per application
CREATE TABLE IF NOT EXISTS DailyAppSummaries (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    AppName TEXT NOT NULL,
    SummaryDate DATE NOT NULL,
    TotalDurationMs INTEGER NOT NULL DEFAULT 0,
    SessionCount INTEGER NOT NULL DEFAULT 0,
    FirstUse DATETIME,
    LastUse DATETIME,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    UNIQUE(AppName, SummaryDate),
    CHECK (TotalDurationMs >= 0),
    CHECK (SessionCount >= 0)
);

-- Create indices for DailyAppSummaries
CREATE INDEX IF NOT EXISTS idx_daily_app_summaries_app_name ON DailyAppSummaries(AppName);
CREATE INDEX IF NOT EXISTS idx_daily_app_summaries_summary_date ON DailyAppSummaries(SummaryDate);
CREATE INDEX IF NOT EXISTS idx_daily_app_summaries_date_desc ON DailyAppSummaries(SummaryDate DESC);

-- Create DailySystemSummaries table
-- Stores aggregated daily system metrics
CREATE TABLE IF NOT EXISTS DailySystemSummaries (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SummaryDate DATE NOT NULL UNIQUE,
    AvgCpuUsagePercent REAL,
    PeakCpuUsagePercent REAL,
    AvgMemoryUsageMb INTEGER,
    PeakMemoryUsageMb INTEGER,
    TotalUptimeMinutes INTEGER,
    TotalSessionCount INTEGER,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    CHECK (AvgCpuUsagePercent IS NULL OR (AvgCpuUsagePercent >= 0 AND AvgCpuUsagePercent <= 100)),
    CHECK (PeakCpuUsagePercent IS NULL OR (PeakCpuUsagePercent >= 0 AND PeakCpuUsagePercent <= 100))
);

-- Create indices for DailySystemSummaries
CREATE INDEX IF NOT EXISTS idx_daily_system_summaries_date ON DailySystemSummaries(SummaryDate DESC);

-- Enable AUTOINCREMENT for better performance with deleted records
-- SQLite automatically handles this, but making it explicit

-- Optimize settings for monitoring workload
PRAGMA cache_size = 10000;        -- Use 10MB cache
PRAGMA temp_store = MEMORY;       -- Store temp data in memory
PRAGMA query_only = FALSE;        -- Allow writes

-- Log completion
SELECT 'Screen Time Monitor SQLite database schema created successfully!' as Result;
