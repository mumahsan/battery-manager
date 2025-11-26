using SmartBatteryAgent;
using SmartBatteryAgent.Services;
using Serilog;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        path: Path.Combine(AppContext.BaseDirectory, "logs", "smart-battery-agent-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7)
    .CreateLogger();

try
{
    Log.Information("═══════════════════════════════════════════════");
    Log.Information("🧠 Smart Battery Agent - AI-Powered Battery Care");
    Log.Information("═══════════════════════════════════════════════");

    var builder = Host.CreateApplicationBuilder(args);

    // Add Serilog
    builder.Services.AddSerilog();

    // Register services
    builder.Services.AddSingleton<ISystemDetector, SystemDetector>();
    builder.Services.AddSingleton<IBatteryMonitor, CrossPlatformBatteryMonitor>();
    builder.Services.AddSingleton<IBestPracticesAnalyzer, BestPracticesAnalyzer>();
    builder.Services.AddSingleton<INotificationService, CrossPlatformNotificationService>();

    // Register main worker
    builder.Services.AddHostedService<SmartBatteryWorker>();

    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
