using SmartBatteryAgent.Models;
using SmartBatteryAgent.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace SmartBatteryAgent
{
    public class SmartBatteryWorker : BackgroundService
    {
        private readonly ILogger<SmartBatteryWorker> _logger;
        private readonly ISystemDetector _systemDetector;
        private readonly IBatteryMonitor _batteryMonitor;
        private readonly IBestPracticesAnalyzer _practicesAnalyzer;
        private readonly INotificationService _notificationService;
        private readonly SmartAgentConfig _config;

        private SystemInfo? _systemInfo;
        private BatteryBestPractices? _bestPractices;
        private BatteryStatus? _lastStatus;
        private bool _hasShownChargeAlert = false;
        private bool _hasShownDischargeAlert = false;

        public SmartBatteryWorker(
            ILogger<SmartBatteryWorker> logger,
            ISystemDetector systemDetector,
            IBatteryMonitor batteryMonitor,
            IBestPracticesAnalyzer practicesAnalyzer,
            INotificationService notificationService,
            IConfiguration configuration)
        {
            _logger = logger;
            _systemDetector = systemDetector;
            _batteryMonitor = batteryMonitor;
            _practicesAnalyzer = practicesAnalyzer;
            _notificationService = notificationService;
            _config = configuration.GetSection("SmartAgent").Get<SmartAgentConfig>() ?? new SmartAgentConfig();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🔋 Smart Battery Agent Starting...");
            _logger.LogInformation("═══════════════════════════════════════════════");

            // Detect system
            _systemInfo = _systemDetector.DetectSystem();
            
            // Check for battery
            if (!_batteryMonitor.HasBattery())
            {
                _logger.LogWarning("⚠️  No battery detected. This appears to be a desktop system.");
                _logger.LogInformation("Smart Battery Agent will monitor but not alert.");
                await Task.Delay(Timeout.Infinite, stoppingToken);
                return;
            }

            // Load best practices knowledge base
            await _practicesAnalyzer.LoadKnowledgeBaseAsync();

            // Get best practices for this system
            _bestPractices = await _practicesAnalyzer.GetBestPracticesAsync(_systemInfo);
            
            _logger.LogInformation("\n📊 System Information:");
            _logger.LogInformation("   Device: {Manufacturer} {Model}", _systemInfo.Manufacturer, _systemInfo.Model);
            _logger.LogInformation("   OS: {OS} {Version}", _systemInfo.OperatingSystem, _systemInfo.OSVersion);
            _logger.LogInformation("   Battery: {Type}", _systemInfo.BatteryType);
            if (_systemInfo.BatteryHealthPercentage > 0)
                _logger.LogInformation("   Battery Health: {Health:F1}%", _systemInfo.BatteryHealthPercentage);

            _logger.LogInformation("\n💡 Smart Recommendations for Your Device:");
            _logger.LogInformation("   Optimal Charge Range: {Min}% - {Max}%", 
                _bestPractices.OptimalChargeStart, _bestPractices.OptimalChargeStop);
            _logger.LogInformation("   Reasoning: {Reason}", _bestPractices.Reasoning);
            _logger.LogInformation("   Source: {Source}", _bestPractices.Source);

            if (_bestPractices.Tips.Any())
            {
                _logger.LogInformation("\n✨ Tips for Maximum Battery Lifespan:");
                foreach (var tip in _bestPractices.Tips)
                {
                    _logger.LogInformation("   • {Tip}", tip);
                }
            }

            _logger.LogInformation("\n═══════════════════════════════════════════════");
            _logger.LogInformation("🔄 Monitoring started (polling every {Interval}s)", _config.PollIntervalSeconds);
            _logger.LogInformation("═══════════════════════════════════════════════\n");

            // Show initial notification
            await _notificationService.ShowNotificationAsync(
                "Smart Battery Agent Active",
                $"Monitoring battery health. Optimal range: {_bestPractices.OptimalChargeStart}-{_bestPractices.OptimalChargeStop}%");

            // Main monitoring loop
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await MonitorBatteryAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in monitoring loop");
                }

                await Task.Delay(TimeSpan.FromSeconds(_config.PollIntervalSeconds), stoppingToken);
            }

            _logger.LogInformation("Smart Battery Agent stopped");
        }

        private async Task MonitorBatteryAsync()
        {
            var status = _batteryMonitor.GetBatteryStatus();
            if (status.Percentage < 0)
                return;

            var batteryIcon = GetBatteryIcon(status.Percentage, status.IsCharging);
            var chargeStatus = status.IsCharging ? "⚡ Charging" : "🔌 Discharging";

            _logger.LogDebug("{Icon} Battery: {Percentage}% | {Status} | Optimal: {Min}-{Max}%",
                batteryIcon, status.Percentage, chargeStatus,
                _bestPractices!.OptimalChargeStart, _bestPractices.OptimalChargeStop);

            // Check if we need to alert
            await CheckAndAlertAsync(status);

            _lastStatus = status;
        }

        private async Task CheckAndAlertAsync(BatteryStatus status)
        {
            // Alert when charging and reached upper threshold
            if (status.IsCharging && status.Percentage >= _bestPractices!.OptimalChargeStop)
            {
                if (!_hasShownChargeAlert)
                {
                    _hasShownChargeAlert = true;
                    _hasShownDischargeAlert = false;

                    var title = $"🔋 Battery at {status.Percentage}%";
                    var message = $"Optimal maximum reached ({_bestPractices.OptimalChargeStop}%). Consider unplugging to extend battery lifespan.";

                    _logger.LogWarning("⚠️  {Title} - {Message}", title, message);
                    await _notificationService.ShowNotificationAsync(title, message, NotificationPriority.High);
                }
            }
            // Alert when discharging and reached lower threshold
            else if (!status.IsCharging && status.Percentage <= _bestPractices.OptimalChargeStart)
            {
                if (!_hasShownDischargeAlert)
                {
                    _hasShownDischargeAlert = true;
                    _hasShownChargeAlert = false;

                    var title = $"🔋 Battery at {status.Percentage}%";
                    var message = $"Optimal minimum reached ({_bestPractices.OptimalChargeStart}%). Consider plugging in to maintain battery health.";

                    _logger.LogWarning("⚠️  {Title} - {Message}", title, message);
                    await _notificationService.ShowNotificationAsync(title, message, NotificationPriority.High);
                }
            }
            // Reset alerts when in optimal range
            else if (status.Percentage > _bestPractices.OptimalChargeStart + 5 && 
                     status.Percentage < _bestPractices.OptimalChargeStop - 5)
            {
                if (_hasShownChargeAlert || _hasShownDischargeAlert)
                {
                    _logger.LogInformation("✅ Battery back in optimal range ({Percentage}%)", status.Percentage);
                    _hasShownChargeAlert = false;
                    _hasShownDischargeAlert = false;
                }
            }
        }

        private string GetBatteryIcon(int percentage, bool isCharging)
        {
            if (isCharging)
                return "⚡";

            return percentage switch
            {
                >= 90 => "🔋",
                >= 60 => "🔋",
                >= 30 => "🔋",
                >= 15 => "🪫",
                _ => "🪫"
            };
        }
    }
}
