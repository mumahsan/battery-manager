using BatteryManagerService;
using BatteryManagerService.Models;
using BatteryManagerService.Services;
using Serilog;
using System.Windows.Forms;

// Configure Serilog for logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File(
        path: Path.Combine(AppContext.BaseDirectory, "logs", "battery-manager-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

try
{
    Log.Information("Starting Battery Manager Service");

    var builder = Host.CreateApplicationBuilder(args);

    // Handle Ctrl+C for graceful shutdown
    builder.Services.Configure<HostOptions>(options =>
    {
        options.ShutdownTimeout = TimeSpan.FromSeconds(5);
    });

    // Add Serilog
    builder.Services.AddSerilog();

    // Configure from appsettings.json
    builder.Services.Configure<BatteryManagerConfig>(
        builder.Configuration.GetSection("BatteryManager"));

    // Register services
    builder.Services.AddSingleton<IBatteryMonitor, WmiBatteryMonitor>();
    builder.Services.AddSingleton<INotificationService, NotificationService>();
    builder.Services.AddSingleton<IVoiceSynthesizer, VoiceSynthesizer>();
    builder.Services.AddSingleton<ITrayIconService, TrayIconService>();

    // Register the main worker
    builder.Services.AddHostedService<BatteryManagerWorker>();

    // Enable Windows Service support
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "BatteryManagerService";
    });

    // CRITICAL: Set up Windows Forms message pump BEFORE building host
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    
    var host = builder.Build();
    
    // Get the tray icon service and initialize it on UI thread (this thread)
    var trayIconService = host.Services.GetRequiredService<ITrayIconService>();
    trayIconService.Show(); // Force initialization on UI thread
    
    // Install global keyboard hook for Ctrl+Shift+X exit
    var keyboardHook = new KeyboardHookService(
        host.Services.GetRequiredService<ILogger<KeyboardHookService>>(),
        () => 
        {
            Log.Information("Keyboard shortcut triggered - exiting");
            Application.Exit();
        });
    
    // Start the host in a background thread
    var hostTask = Task.Run(async () =>
    {
        try
        {
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Host execution error");
        }
    });

    // Run Windows Forms message pump (required for NotifyIcon to work properly)
    Application.Run(new ApplicationContext());
    
    // Cleanup
    keyboardHook.Dispose();
    
    // When application exits, stop the host
    Log.Information("Application exiting, stopping host...");
    await host.StopAsync(TimeSpan.FromSeconds(5));
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
