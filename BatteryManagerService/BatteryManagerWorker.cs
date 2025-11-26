using BatteryManagerService.Models;
using BatteryManagerService.Services;
using Microsoft.Extensions.Options;

namespace BatteryManagerService
{
    /// <summary>
    /// Main background service for battery management.
    /// Monitors battery status, triggers notifications and voice prompts with hysteresis.
    /// </summary>
    public class BatteryManagerWorker : BackgroundService
    {
        private readonly ILogger<BatteryManagerWorker> _logger;
        private readonly BatteryManagerConfig _config;
        private readonly IBatteryMonitor _batteryMonitor;
        private readonly INotificationService _notificationService;
        private readonly IVoiceSynthesizer _voiceSynthesizer;
        private readonly ITrayIconService _trayIconService;

        private BatteryState _currentState = BatteryState.Normal;
        private System.Threading.Timer? _voiceTimer;
        private readonly object _stateLock = new();
        private int _lastAlertBatteryLevel = 0;
        private string _currentVoiceMessage = string.Empty;

        private const string HighBatteryTag = "high_battery";
        private const string LowBatteryTag = "low_battery";

        public BatteryManagerWorker(
            ILogger<BatteryManagerWorker> logger,
            IOptions<BatteryManagerConfig> config,
            IBatteryMonitor batteryMonitor,
            INotificationService notificationService,
            IVoiceSynthesizer voiceSynthesizer,
            ITrayIconService trayIconService)
        {
            _logger = logger;
            _config = config.Value;
            _batteryMonitor = batteryMonitor;
            _notificationService = notificationService;
            _voiceSynthesizer = voiceSynthesizer;
            _trayIconService = trayIconService;

            // Register notification button handlers
            _notificationService.RegisterActionHandler($"{HighBatteryTag}_ok", OnHighBatteryDismissed);
            _notificationService.RegisterActionHandler($"{HighBatteryTag}_close", OnHighBatteryDismissed);
            _notificationService.RegisterActionHandler($"{LowBatteryTag}_ok", OnLowBatteryDismissed);
            _notificationService.RegisterActionHandler($"{LowBatteryTag}_close", OnLowBatteryDismissed);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Battery Manager Service starting...");
            _logger.LogInformation("Configuration: Upper={Upper}%, Lower={Lower}%, Poll={Poll}s, Voice={Voice}min",
                _config.UpperThreshold, _config.LowerThreshold, _config.PollIntervalSeconds, _config.VoiceRepeatMinutes);

            // Show tray icon
            _trayIconService.Show();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await MonitorBatteryAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in battery monitoring loop");
                }

                await Task.Delay(TimeSpan.FromSeconds(_config.PollIntervalSeconds), stoppingToken);
            }

            _logger.LogInformation("Battery Manager Service stopping...");
            _trayIconService.Hide();
            StopVoiceTimer();
        }

        /// <summary>
        /// Main monitoring logic with hysteresis to prevent flapping.
        /// </summary>
        private async Task MonitorBatteryAsync()
        {
            var chargeLevel = _batteryMonitor.GetBatteryPercentage();
            var isACConnected = _batteryMonitor.IsACPowerConnected();

            if (chargeLevel < 0)
            {
                _logger.LogWarning("Invalid battery reading, skipping this cycle");
                return;
            }

            _logger.LogDebug("Battery: {Charge}%, AC: {AC}, State: {State}", 
                chargeLevel, isACConnected, _currentState);

            // Update tray icon with current battery level
            _trayIconService.UpdateBatteryLevel(chargeLevel, isACConnected);

            lock (_stateLock)
            {
                var previousState = _currentState;

                // State machine with hysteresis
                switch (_currentState)
                {
                    case BatteryState.Normal:
                        // Transition to HighBatteryAlert when >= 80% and AC connected
                        if (chargeLevel >= _config.UpperThreshold && isACConnected)
                        {
                            TransitionToHighBatteryAlert(chargeLevel);
                        }
                        // Transition to LowBatteryAlert when <= 20% and AC not connected
                        else if (chargeLevel <= _config.LowerThreshold && !isACConnected)
                        {
                            TransitionToLowBatteryAlert(chargeLevel);
                        }
                        break;

                    case BatteryState.HighBatteryAlert:
                        // Clear alert if: AC disconnected OR battery dropped below 79% (hysteresis)
                        if (!isACConnected || chargeLevel < _config.UpperThreshold - 1)
                        {
                            TransitionToNormal("High battery condition cleared", chargeLevel, isACConnected);
                        }
                        // If battery percentage changed, update voice message and speak immediately
                        else if (chargeLevel != _lastAlertBatteryLevel)
                        {
                            _logger.LogWarning("Battery changed to {Charge}%. Updating alert.", chargeLevel);
                            UpdateAlertForNewPercentage(chargeLevel, true);
                        }
                        break;

                    case BatteryState.LowBatteryAlert:
                        // Clear alert if: AC connected OR battery rose above 21% (hysteresis)
                        if (isACConnected || chargeLevel > _config.LowerThreshold + 1)
                        {
                            TransitionToNormal("Low battery condition cleared", chargeLevel, isACConnected);
                        }
                        // If battery percentage changed, update voice message and speak immediately
                        else if (chargeLevel != _lastAlertBatteryLevel)
                        {
                            _logger.LogWarning("Battery changed to {Charge}%. Updating alert.", chargeLevel);
                            UpdateAlertForNewPercentage(chargeLevel, false);
                        }
                        break;
                }

                // Log state changes
                if (previousState != _currentState)
                {
                    _logger.LogInformation("State changed: {PreviousState} -> {CurrentState} (Battery: {Charge}%, AC: {AC})",
                        previousState, _currentState, chargeLevel, isACConnected);
                }
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Transitions to HighBatteryAlert state (80% reached).
        /// </summary>
        private void TransitionToHighBatteryAlert(int chargeLevel)
        {
            _currentState = BatteryState.HighBatteryAlert;
            _lastAlertBatteryLevel = chargeLevel;
            _logger.LogWarning("HIGH BATTERY ALERT: Battery at {Charge}%. Please power off.", chargeLevel);

            // Create message with exact percentage
            var message = $"Battery at {chargeLevel} percent. Please power off immediately.";
            _currentVoiceMessage = message;

            // Show notification
            _notificationService.ShowNotification(
                $"Battery at {chargeLevel}%. Please power off.",
                HighBatteryTag);

            // Start voice prompts with exact percentage
            StartVoiceTimer(message);
        }

        /// <summary>
        /// Transitions to LowBatteryAlert state (20% reached).
        /// </summary>
        private void TransitionToLowBatteryAlert(int chargeLevel)
        {
            _currentState = BatteryState.LowBatteryAlert;
            _lastAlertBatteryLevel = chargeLevel;
            _logger.LogWarning("LOW BATTERY ALERT: Battery at {Charge}%. Please connect power.", chargeLevel);

            // Create message with exact percentage
            var message = $"Battery at {chargeLevel} percent. Please connect power immediately.";
            _currentVoiceMessage = message;

            // Show notification
            _notificationService.ShowNotification(
                $"Battery at {chargeLevel}%. Please connect power.",
                LowBatteryTag);

            // Start voice prompts with exact percentage
            StartVoiceTimer(message);
        }

        /// <summary>
        /// Transitions back to Normal state when conditions clear.
        /// </summary>
        private void TransitionToNormal(string reason, int chargeLevel, bool isACConnected)
        {
            _logger.LogInformation("{Reason} (Battery: {Charge}%, AC: {AC})", reason, chargeLevel, isACConnected);

            // Remove notifications
            _notificationService.RemoveNotification(HighBatteryTag);
            _notificationService.RemoveNotification(LowBatteryTag);

            // Stop voice prompts
            StopVoiceTimer();

            _currentState = BatteryState.Normal;
        }

        /// <summary>
        /// Updates the alert message when battery percentage changes during an alert.
        /// </summary>
        private void UpdateAlertForNewPercentage(int chargeLevel, bool isHighBatteryAlert)
        {
            _lastAlertBatteryLevel = chargeLevel;

            // Create new message with updated percentage
            string message;
            string notificationTag;

            if (isHighBatteryAlert)
            {
                message = $"Battery at {chargeLevel} percent. Please power off immediately.";
                notificationTag = HighBatteryTag;
            }
            else
            {
                message = $"Battery at {chargeLevel} percent. Please connect power immediately.";
                notificationTag = LowBatteryTag;
            }

            _currentVoiceMessage = message;

            // Update notification
            _notificationService.RemoveNotification(notificationTag);
            _notificationService.ShowNotification(
                $"Battery at {chargeLevel}%. Please {(isHighBatteryAlert ? "power off" : "connect power")}.",
                notificationTag);

            // Speak immediately with new percentage
            _logger.LogInformation("Battery percentage changed, speaking immediately: {Message}", message);
            _ = _voiceSynthesizer.SpeakAsync(message);

            // Restart timer with new message
            StartVoiceTimer(message);
        }

        /// <summary>
        /// Starts a timer to repeat voice prompts at configured intervals.
        /// </summary>
        private void StartVoiceTimer(string message)
        {
            StopVoiceTimer(); // Ensure no existing timer

            var interval = TimeSpan.FromMinutes(_config.VoiceRepeatMinutes);
            
            // Speak immediately - don't use Task.Run, call directly
            _logger.LogInformation("Starting voice alert: {Message}", message);
            _ = _voiceSynthesizer.SpeakAsync(message);

            // Then repeat on timer
            _voiceTimer = new System.Threading.Timer(
                _ => 
                {
                    _logger.LogInformation("Voice timer triggered: {Message}", message);
                    _ = _voiceSynthesizer.SpeakAsync(message);
                },
                null,
                interval,
                interval);

            _logger.LogInformation("Voice timer started: repeating every {Interval} minutes", _config.VoiceRepeatMinutes);
        }

        /// <summary>
        /// Stops the voice repeat timer and cancels ongoing speech.
        /// </summary>
        private void StopVoiceTimer()
        {
            if (_voiceTimer != null)
            {
                _voiceTimer.Dispose();
                _voiceTimer = null;
                _voiceSynthesizer.CancelSpeech();
                _logger.LogDebug("Voice timer stopped");
            }
        }

        /// <summary>
        /// Handler when user clicks OK/Close on high battery notification.
        /// </summary>
        private void OnHighBatteryDismissed()
        {
            lock (_stateLock)
            {
                if (_currentState == BatteryState.HighBatteryAlert)
                {
                    _logger.LogInformation("User dismissed high battery notification");
                    StopVoiceTimer();
                    _notificationService.RemoveNotification(HighBatteryTag);
                }
            }
        }

        /// <summary>
        /// Handler when user clicks OK/Close on low battery notification.
        /// </summary>
        private void OnLowBatteryDismissed()
        {
            lock (_stateLock)
            {
                if (_currentState == BatteryState.LowBatteryAlert)
                {
                    _logger.LogInformation("User dismissed low battery notification");
                    StopVoiceTimer();
                    _notificationService.RemoveNotification(LowBatteryTag);
                }
            }
        }

        public override void Dispose()
        {
            StopVoiceTimer();
            base.Dispose();
        }
    }
}
