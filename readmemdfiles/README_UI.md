# ScreenTimeMonitor — UI Quick Examples

This short document shows example interactions you can expect when running the console UI (`ScreenTimeMonitor.UI`).

## Example: Connect + Ping

1. Start the service (or run `ScreenTimeMonitor.Service` with `dotnet run`).
2. Run the UI:

```powershell
cd .\ScreenTimeMonitor.UI
dotnet run
```

3. Menu actions:

- Select `Connect to Service` (enter the pipe name or press Enter to use `ScreenTimeMonitor.Pipe`).
- Select `Send Ping`.

Expected output:

```
Connecting to pipe 'ScreenTimeMonitor.Pipe'... Connected
Sending PING... Received: PONG
```

## Example: Live Activity View

1. From the UI main menu select `Live Activity`.
2. The UI subscribes to broadcasts and prints messages as they arrive.

Example output:

```
[2025-12-05T14:23:10] BROADCAST: AppStarted - process=chrome.exe title='Stack Overflow - Google Chrome'
[2025-12-05T14:25:01] BROADCAST: AppStopped - process=chrome.exe duration_ms=120000
Press 'q' to quit live view.
```

## Notes
- If the UI fails to connect, check that the service is running and the pipe name matches.
- Live activity requires the service to actively broadcast events; run the service in debug mode to generate test messages.
