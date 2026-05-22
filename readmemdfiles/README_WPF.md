# ScreenTimeMonitor.UI.WPF — Desktop Application

Modern WPF desktop application for Screen Time Monitor with rich UI, real-time activity streaming, and daily usage analytics.

## Quick Start

### Build
```powershell
cd "c:\Users\PC\Downloads\School Files\Operating System Project"
dotnet build ScreenTimeMonitor.UI.WPF -c Debug
```

### Run the WPF Application
```powershell
cd ScreenTimeMonitor.UI.WPF
dotnet run
```

## Features

### Live Activity Tab
- **Connect to Service**: Establish IPC connection to the monitoring service.
- **Disconnect**: Gracefully close the connection.
- **Live Activity Stream**: Real-time list of app usage events with timestamps.
- **Clear Activity**: Clear the activity list.

### App Usage History Tab
- **Daily Summary**: DataGrid showing all apps used today with metrics:
  - Application name
  - Total time used (hours)
  - Number of sessions
  - First use time
  - Last use time
- **Refresh Data**: Load latest history from the service/database.

### System Stats Tab
- **CPU Usage**: Current system CPU utilization.
- **Memory Usage**: Current system memory utilization.
- **Uptime**: System uptime since last boot.

## Architecture

- **Framework**: WPF (.NET 8.0-windows)
- **UI Pattern**: Event-driven with async/await
- **IPC**: Named Pipes client (`ScreenTimeMonitor.Pipe`)
- **Threading**: UI updates marshalled to Dispatcher thread

## Key Components

### MainWindow.xaml / MainWindow.xaml.cs
- Three-tab interface: Live Activity, History, System Stats
- Connect/Disconnect buttons with status display
- Activity ListBox with auto-scroll (max 100 items)
- History DataGrid with sortable columns

### IPCClient.cs
- Async Named Pipe client for service communication
- Event-driven message reception (`OnMessageReceived`)
- PING/PONG test support
- Graceful timeout and disconnection handling

### AppUsageHistoryItem
- ViewModel for history list
- Properties: AppName, TotalHours, SessionCount, FirstUse, LastUse

## Usage Example

1. Start the monitoring service:
   ```powershell
   cd ScreenTimeMonitor.Service
   dotnet run
   ```

2. Start the WPF UI in another terminal:
   ```powershell
   cd ScreenTimeMonitor.UI.WPF
   dotnet run
   ```

3. Click "Connect to Service" (or press Enter to use default pipe name).

4. Observe real-time activity in the "Live Activity" tab.

5. Switch to "App Usage History" tab to view today's usage summary.

## Future Enhancements

- Charts and graphs for usage trends
- Export daily reports (CSV, PDF)
- Settings dialog (log level, retention policy, etc.)
- Notifications for long app usage
- Multi-day history view
- Dark mode / theme selection

## Dependencies

- Microsoft.Extensions.Configuration / Logging
- Npgsql (for PostgreSQL access if needed)
- System.Data.SQLite (for SQLite access)
- Dapper (for ORM)

## Notes

- History data loading from database is a TODO (currently placeholder).
- System Stats (CPU/Memory/Uptime) are placeholders; integrate `System.Diagnostics` and `System.Management` to display real metrics.
- The WPF app expects the service to be running; if not, the "Connect" button will show an error.

---

Generated on: 2025-12-05  
Part of Screen Time Monitor Phase 6
