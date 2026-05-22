-- Screen Time Monitor - PostgreSQL Database Schema
-- This script creates the complete database schema for the monitoring system
-- Run this script after creating the database: createdb screentime_monitor

-- Drop existing tables if they exist (for fresh install)
DROP TABLE IF EXISTS daily_system_summaries CASCADE;
DROP TABLE IF EXISTS daily_app_summaries CASCADE;
DROP TABLE IF EXISTS system_metrics CASCADE;
DROP TABLE IF EXISTS app_usage_sessions CASCADE;

-- Create AppUsageSessions table
-- Stores individual application usage sessions
CREATE TABLE app_usage_sessions (
    id SERIAL PRIMARY KEY,
    app_name VARCHAR(255) NOT NULL,
    session_start TIMESTAMP WITH TIME ZONE NOT NULL,
    session_end TIMESTAMP WITH TIME ZONE,
    duration_ms BIGINT,
    window_title VARCHAR(512),
    process_id INT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    
    -- Constraints
    CHECK (session_end IS NULL OR session_end > session_start)
);

-- Create indices for app_usage_sessions
CREATE INDEX idx_app_usage_sessions_app_name ON app_usage_sessions(app_name);
CREATE INDEX idx_app_usage_sessions_start_time ON app_usage_sessions(session_start);
CREATE INDEX idx_app_usage_sessions_created_at ON app_usage_sessions(created_at);
CREATE INDEX idx_app_usage_sessions_date_app ON app_usage_sessions(DATE(session_start), app_name);

-- Create SystemMetrics table
-- Stores periodic system performance metrics
CREATE TABLE system_metrics (
    id SERIAL PRIMARY KEY,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL,
    cpu_usage_percent NUMERIC(5,2),
    memory_usage_mb BIGINT,
    total_memory_mb BIGINT,
    disk_read_bytes BIGINT,
    disk_write_bytes BIGINT,
    process_id INT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    
    -- Constraints
    CHECK (cpu_usage_percent >= 0 AND cpu_usage_percent <= 100)
);

-- Create indices for system_metrics
CREATE INDEX idx_system_metrics_timestamp ON system_metrics(timestamp);
CREATE INDEX idx_system_metrics_created_at ON system_metrics(created_at);
CREATE INDEX idx_system_metrics_date ON system_metrics(DATE(timestamp));

-- Create DailyAppSummaries table
-- Stores aggregated daily usage statistics per application
CREATE TABLE daily_app_summaries (
    id SERIAL PRIMARY KEY,
    app_name VARCHAR(255) NOT NULL,
    summary_date DATE NOT NULL,
    total_duration_ms BIGINT NOT NULL DEFAULT 0,
    session_count INT NOT NULL DEFAULT 0,
    first_use TIMESTAMP WITH TIME ZONE,
    last_use TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    
    -- Ensure one record per app per day
    UNIQUE(app_name, summary_date),
    
    -- Constraints
    CHECK (total_duration_ms >= 0),
    CHECK (session_count >= 0)
);

-- Create indices for daily_app_summaries
CREATE INDEX idx_daily_app_summaries_app_name ON daily_app_summaries(app_name);
CREATE INDEX idx_daily_app_summaries_summary_date ON daily_app_summaries(summary_date);
CREATE INDEX idx_daily_app_summaries_date_range ON daily_app_summaries(summary_date DESC, total_duration_ms DESC);

-- Create DailySystemSummaries table
-- Stores aggregated daily system metrics
CREATE TABLE daily_system_summaries (
    id SERIAL PRIMARY KEY,
    summary_date DATE NOT NULL UNIQUE,
    avg_cpu_usage_percent NUMERIC(5,2),
    peak_cpu_usage_percent NUMERIC(5,2),
    avg_memory_usage_mb BIGINT,
    peak_memory_usage_mb BIGINT,
    total_uptime_minutes INT,
    total_session_count INT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    
    -- Constraints
    CHECK (avg_cpu_usage_percent >= 0 AND avg_cpu_usage_percent <= 100),
    CHECK (peak_cpu_usage_percent >= 0 AND peak_cpu_usage_percent <= 100)
);

-- Create indices for daily_system_summaries
CREATE INDEX idx_daily_system_summaries_date ON daily_system_summaries(summary_date DESC);

-- Create a view for recent activity (last 30 days)
CREATE VIEW v_recent_activity AS
SELECT 
    app_name,
    summary_date,
    total_duration_ms,
    session_count,
    ROUND((total_duration_ms::NUMERIC / 1000 / 60), 2) as total_duration_minutes,
    first_use,
    last_use
FROM daily_app_summaries
WHERE summary_date >= CURRENT_DATE - INTERVAL '30 days'
ORDER BY summary_date DESC, total_duration_ms DESC;

-- Create a view for top apps (all time)
CREATE VIEW v_top_apps AS
SELECT 
    app_name,
    COUNT(DISTINCT summary_date) as days_used,
    SUM(session_count) as total_sessions,
    SUM(total_duration_ms) as total_duration_ms,
    ROUND((SUM(total_duration_ms)::NUMERIC / 1000 / 60), 2) as total_duration_minutes,
    MAX(last_use) as last_used,
    ROUND((AVG(total_duration_ms)::NUMERIC / 1000 / 60), 2) as avg_session_minutes
FROM daily_app_summaries
GROUP BY app_name
ORDER BY total_duration_ms DESC;

-- Create a view for daily system health
CREATE VIEW v_daily_system_health AS
SELECT 
    summary_date,
    avg_cpu_usage_percent,
    peak_cpu_usage_percent,
    avg_memory_usage_mb,
    peak_memory_usage_mb,
    total_uptime_minutes,
    total_session_count,
    ROUND((total_uptime_minutes::NUMERIC / 1440), 2) as uptime_hours
FROM daily_system_summaries
ORDER BY summary_date DESC;

-- Grant appropriate permissions (if using specific roles)
-- GRANT SELECT, INSERT, UPDATE, DELETE ON app_usage_sessions TO screentime_user;
-- GRANT SELECT, INSERT, UPDATE, DELETE ON system_metrics TO screentime_user;
-- GRANT SELECT, INSERT, UPDATE, DELETE ON daily_app_summaries TO screentime_user;
-- GRANT SELECT, INSERT, UPDATE, DELETE ON daily_system_summaries TO screentime_user;
-- GRANT SELECT ON v_recent_activity TO screentime_user;
-- GRANT SELECT ON v_top_apps TO screentime_user;
-- GRANT SELECT ON v_daily_system_health TO screentime_user;

-- Enable auto-vacuuming for better performance
ALTER TABLE app_usage_sessions SET (autovacuum_vacuum_scale_factor = 0.01);
ALTER TABLE system_metrics SET (autovacuum_vacuum_scale_factor = 0.01);
ALTER TABLE daily_app_summaries SET (autovacuum_vacuum_scale_factor = 0.05);
ALTER TABLE daily_system_summaries SET (autovacuum_vacuum_scale_factor = 0.05);

-- Log completion
\echo 'Screen Time Monitor PostgreSQL database schema created successfully!'
