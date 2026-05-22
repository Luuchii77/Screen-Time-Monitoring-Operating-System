using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScreenTimeMonitor.Service.Database;
using ScreenTimeMonitor.Service.Services;
using ScreenTimeMonitor.Service.Utilities;

// Ensure Windows Event Log source is registered (for Windows Service)
EventLogSetup.EnsureEventSourceExists();

// Configure the host
var hostBuilder = Host.CreateDefaultBuilder(args);

// When running interactively for development, avoid the Windows Service lifetime
// so console logs and behaviors are easier to observe. In production the host
// will still use the Windows Service lifetime.
if (!Environment.UserInteractive)
{
    hostBuilder = hostBuilder.UseWindowsService();
}

IHost host = hostBuilder
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        // Add configuration
        services.Configure<HostOptions>(options =>
        {
            options.ShutdownTimeout = System.TimeSpan.FromSeconds(30);
        });

        // Add logging
        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.AddDebug();
        });

        // Register database context and initializer
        services.AddSingleton<DatabaseContext>();
        services.AddSingleton<DatabaseInitializer>();

        // Register repositories
        services.AddScoped<IAppUsageRepository, AppUsageRepository>();
        services.AddScoped<ISystemMetricsRepository, SystemMetricsRepository>();
        services.AddScoped<IDailyAppSummaryRepository, DailyAppSummaryRepository>();
        services.AddScoped<IDailySystemSummaryRepository, DailySystemSummaryRepository>();

        // Register core services - BackgroundProcessMonitorService needs access to repositories
        services.AddSingleton<IWindowMonitoringService, WindowMonitoringService>();
        services.AddSingleton<IBackgroundProcessMonitorService>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<BackgroundProcessMonitorService>>();
            var appUsageRepo = provider.CreateScope().ServiceProvider.GetRequiredService<IAppUsageRepository>();
            return new BackgroundProcessMonitorService(logger, appUsageRepo);
        });
        services.AddSingleton<ISystemMetricsService, SystemMetricsService>();
        services.AddSingleton<IDataCollectionService, DataCollectionService>();
        services.AddSingleton<IIPCService, IPCService>();
        services.AddSingleton<IHealthCheckService, HealthCheckService>();

        // Register hosted services
        services.AddHostedService<MonitoringHostedService>();
        services.AddHostedService<DataCleanupService>();  // FIX: Data retention cleanup
    })
    .ConfigureLogging((context, logging) =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddDebug();

        // Configure file logging
        var logDirectory = context.Configuration["Paths:LogDirectory"] 
            ?? AppConstants.DefaultLogDirectory;
        LoggerSetup.ConfigureFileLogging(logDirectory);

        // Add Windows Event Log provider when running as service
        if (context.HostingEnvironment.IsProduction())
        {
            logging.AddEventLog(settings =>
            {
                settings.SourceName = EventLogSetup.GetEventSourceName();
                settings.LogName = "Application";
            });
        }
    })
    .Build();

// Get logger
var logger = host.Services.GetRequiredService<ILogger<Program>>();

try
{
    logger.LogStartup("Screen Time Monitor Service starting...");
    EventLogSetup.WriteInformationEvent("Screen Time Monitor Service started successfully");
    await host.RunAsync();
    logger.LogStartup("Screen Time Monitor Service stopped normally");
    EventLogSetup.WriteInformationEvent("Screen Time Monitor Service stopped normally");
}
catch (Exception ex)
{
    logger.LogError(ex, "Screen Time Monitor Service crashed");
    EventLogSetup.WriteErrorEvent("Screen Time Monitor Service crashed unexpectedly", ex);
    Environment.Exit(ExitCodes.Failure);
}

/// <summary>
/// Exit codes for the application
/// </summary>
public static class ExitCodes
{
    public const int Success = 0;
    public const int Failure = 1;
}

