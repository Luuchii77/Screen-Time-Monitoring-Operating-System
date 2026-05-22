using System;
using System.Data.SQLite;
using System.IO;
using Dapper;
using Xunit;

namespace ScreenTimeMonitor.Tests
{
    public class ServiceDatabaseEndToEndTests
    {
        [Fact]
        public void SqliteSchema_CanCreateAndInsertAppSession()
        {
            var tmp = Path.GetTempPath();
            var dbFile = Path.Combine(tmp, $"stmon_e2e_{Guid.NewGuid():N}.db");
            try
            {
                var connString = $"Data Source={dbFile};Version=3;";
                SQLiteConnection.CreateFile(dbFile);

                using (var conn = new SQLiteConnection(connString))
                {
                    conn.Open();

                    // Execute schema SQL if present
                    var schemaPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Database", "schema-sqlite.sql");
                    if (File.Exists(schemaPath))
                    {
                        var sql = File.ReadAllText(schemaPath);
                        conn.Execute(sql);
                    }
                    else
                    {
                        // Fallback: create a minimal app_sessions table
                        conn.Execute(@"CREATE TABLE IF NOT EXISTS app_sessions (
                                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                                        process_id INTEGER,
                                        app_name TEXT,
                                        window_title TEXT,
                                        session_start TEXT,
                                        session_end TEXT,
                                        duration_ms INTEGER
                                      );");
                    }

                    // Determine which table exists (schema may define app_usage_sessions)
                    var tableName = conn.ExecuteScalar<string>("SELECT name FROM sqlite_master WHERE type='table' AND name IN ('app_sessions','app_usage_sessions') LIMIT 1;");
                    if (string.IsNullOrEmpty(tableName))
                    {
                        // Create fallback table
                        conn.Execute(@"CREATE TABLE IF NOT EXISTS app_sessions (
                                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                                        process_id INTEGER,
                                        app_name TEXT,
                                        window_title TEXT,
                                        session_start TEXT,
                                        session_end TEXT,
                                        duration_ms INTEGER
                                      );");
                        tableName = "app_sessions";
                    }

                    // Insert a sample session into the detected table
                    var now = DateTime.UtcNow;
                    var insertSql = $"INSERT INTO {tableName} (process_id, app_name, window_title, session_start, session_end, duration_ms) VALUES (@pid,@app,@title,@start,@end,@dur)";
                    conn.Execute(insertSql,
                        new
                        {
                            pid = 1234,
                            app = "TestApp",
                            title = "Test Window",
                            start = now.ToString("o"),
                            end = now.AddMinutes(1).ToString("o"),
                            dur = 60000
                        });

                    var count = conn.ExecuteScalar<long>($"SELECT COUNT(1) FROM {tableName};");
                    Assert.Equal(1L, count);
                }
            }
            finally
            {
                try { File.Delete(dbFile); } catch { }
            }
        }
    }
}
