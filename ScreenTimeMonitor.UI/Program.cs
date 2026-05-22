using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScreenTimeMonitor.Service.Utilities;

// Set up configuration
var configBuilder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

IConfiguration configuration = configBuilder.Build();

// Set up dependency injection
var services = new ServiceCollection();
services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

services.AddSingleton(configuration);

var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

// --- IPC client support (declare early so local functions can capture) ---
ScreenTimeMonitor.UI.Services.IPCClient? _ipcClient = null;
string _pipeName = "ScreenTimeMonitor.Pipe";

try
{
    logger.LogStartup("Screen Time Monitor UI starting...");

    // Display welcome message
    Console.Clear();
    Console.WriteLine("╔════════════════════════════════════════════════════╗");
    Console.WriteLine("║     Screen Time Monitor - Console UI v1.0          ║");
    Console.WriteLine("║              Application Usage Tracker              ║");
    Console.WriteLine("╚════════════════════════════════════════════════════╝\n");

    Console.WriteLine("Welcome to Screen Time Monitor!");
    Console.WriteLine("This application monitors your screen time and app usage.\n");

    // Simple menu (will be expanded in Phase 6)
    await DisplayMainMenuAsync();

    logger.LogStartup("Screen Time Monitor UI closed normally");
}
catch (Exception ex)
{
    logger.LogError(ex, "Screen Time Monitor UI crashed");
    Console.WriteLine($"\nError: {ex.Message}");
    Environment.Exit(1);
}

async Task DisplayMainMenuAsync()
{
    bool running = true;
    while (running)
    {
        Console.WriteLine("\n────────────────────────────────────────────────────");
        Console.WriteLine("Main Menu:");
        Console.WriteLine("  1. Check Service Status");
        Console.WriteLine("  2. View System Compatibility Report");
        Console.WriteLine("  3. Live Activity (subscribe to service broadcasts)");
        Console.WriteLine("  4. View Daily Statistics (Not yet implemented)");
        Console.WriteLine("  5. View System Metrics (Not yet implemented)");
        Console.WriteLine("  6. IPC: Connect to Service");
        Console.WriteLine("  7. IPC: Send PING to Service");
        Console.WriteLine("  0. Exit");
        Console.WriteLine("────────────────────────────────────────────────────");
        Console.Write("Select an option: ");

        var choice = Console.ReadLine();
        Console.WriteLine();

        switch (choice)
        {
            case "1":
                CheckServiceStatus();
                break;
            case "2":
                ShowCompatibilityReport();
                break;
            case "3":
                await RunLiveActivityViewAsync();
                break;
            case "4":
                Console.WriteLine("Not yet implemented - will be available in Phase 6");
                break;
            case "5":
                Console.WriteLine("Not yet implemented - will be available in Phase 6");
                break;
            case "6":
                await EnsureIpcClientConnectedAsync();
                break;
            case "7":
                await SendPingAsync();
                break;
            case "0":
                running = false;
                break;
            default:
                Console.WriteLine("Invalid option. Please try again.");
                break;
        }
    }
}

void CheckServiceStatus()
{
    Console.WriteLine("Checking ScreenTimeMonitor service status...\n");
    
    try
    {
        var serviceName = "ScreenTimeMonitor";
        var sc = System.ServiceProcess.ServiceController.GetServices()
            .FirstOrDefault(s => s.ServiceName == serviceName);

        if (sc != null)
        {
            Console.WriteLine($"Service Name: {sc.ServiceName}");
            Console.WriteLine($"Display Name: {sc.DisplayName}");
            Console.WriteLine($"Status: {(sc.Status == System.ServiceProcess.ServiceControllerStatus.Running ? "✓ RUNNING" : "✗ STOPPED")}");
            Console.WriteLine($"Startup Type: {sc.StartType}");
        }
        else
        {
            Console.WriteLine("✗ Service not found. The service may not be installed.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Error checking service status: {ex.Message}");
    }
}

void ShowCompatibilityReport()
{
    Console.WriteLine("System Compatibility Report");
    Console.WriteLine("───────────────────────────────────────────────────\n");

    try
    {
        var processorCount = Environment.ProcessorCount;
        var totalMemory = GC.GetTotalMemory(false) / 1024 / 1024;
        var osVersion = Environment.OSVersion.VersionString;

        Console.WriteLine("SYSTEM INFORMATION:");
        Console.WriteLine($"  OS Version: {osVersion}");
        Console.WriteLine($"  Processor Cores: {processorCount}");
        Console.WriteLine($"  Available Memory: ~{totalMemory} MB\n");

        // Simple compatibility check
        Console.WriteLine("COMPATIBILITY STATUS:");
        if (processorCount >= 2 && totalMemory >= 1024)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ✓ System meets recommended specifications");
            Console.ResetColor();
        }
        else if (totalMemory >= 512)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  ⚠ System has limited resources but should work");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ System has insufficient resources");
            Console.ResetColor();
        }

        Console.WriteLine("\nRECOMMENDATIONS:");
        if (processorCount < 2)
            Console.WriteLine("  • Consider using a multi-core processor for better performance");
        if (totalMemory < 2048)
            Console.WriteLine("  • 2GB+ RAM recommended for optimal performance");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

// --- IPC client support ---
// Lazy-create IPC client so we can reuse it across menu choices

async Task EnsureIpcClientConnectedAsync()
{
    if (_ipcClient == null)
    {
        _ipcClient = new ScreenTimeMonitor.UI.Services.IPCClient(_pipeName);
        _ipcClient.OnMessageReceived += msg =>
        {
            Console.WriteLine($"\n[IPC] Message: {msg}\n");
        };
    }

    if (!_ipcClient.IsConnected)
    {
        Console.WriteLine("Connecting to service pipe...\n");
        try
        {
            await _ipcClient.ConnectAsync(3000);
            Console.WriteLine("Connected to service IPC pipe.\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to connect: {ex.Message}\n");
        }
    }
}

async Task SendPingAsync()
{
    if (_ipcClient == null || !_ipcClient.IsConnected)
    {
        Console.WriteLine("IPC client not connected. Attempting to connect...\n");
        await EnsureIpcClientConnectedAsync();
    }

    if (_ipcClient != null && _ipcClient.IsConnected)
    {
        Console.WriteLine("Sending PING...\n");
        var resp = await _ipcClient.SendPingAsync(2000);
        Console.WriteLine(resp == null ? "No response (timeout)" : $"Response: {resp}");
    }
    else
    {
        Console.WriteLine("Unable to send PING - IPC not connected.");
    }
}

async Task RunLiveActivityViewAsync()
{
    Console.WriteLine("Entering Live Activity view. Press 'q' then Enter to exit.\n");

    // Ensure connected
    await EnsureIpcClientConnectedAsync();

    if (_ipcClient == null || !_ipcClient.IsConnected)
    {
        Console.WriteLine("Could not connect to service IPC. Live Activity unavailable.");
        return;
    }

    // Temporary handler
    void Handler(string msg)
    {
        var ts = DateTime.Now.ToString("HH:mm:ss");
        Console.WriteLine($"[{ts}] {msg.TrimEnd()}\n");
    }

    _ipcClient.OnMessageReceived += Handler;

    try
    {
        while (true)
        {
            var input = Console.ReadLine();
            if (string.Equals(input, "q", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
        }
    }
    finally
    {
        _ipcClient.OnMessageReceived -= Handler;
        Console.WriteLine("Exiting Live Activity view.\n");
    }
}
