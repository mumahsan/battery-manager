using Microsoft.Toolkit.Uwp.Notifications;

namespace BatteryManagerService.Services
{
    /// <summary>
    /// Interface for managing Windows Toast notifications.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Shows a notification with the specified message and tag.
        /// </summary>
        void ShowNotification(string message, string tag);

        /// <summary>
        /// Removes a notification by its tag.
        /// </summary>
        void RemoveNotification(string tag);

        /// <summary>
        /// Registers action handlers for notification buttons.
        /// </summary>
        void RegisterActionHandler(string action, Action handler);
    }

    /// <summary>
    /// Notification service using Windows Toast notifications with actionable buttons.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly Dictionary<string, Action> _actionHandlers = new();

        public NotificationService(ILogger<NotificationService> logger)
        {
            _logger = logger;
            
            // Register toast activation handler
            ToastNotificationManagerCompat.OnActivated += OnToastActivated;
        }

        /// <summary>
        /// Shows a toast notification with OK and Close buttons.
        /// </summary>
        public void ShowNotification(string message, string tag)
        {
            try
            {
                _logger.LogInformation("Showing notification: {Message} (Tag: {Tag})", message, tag);

                new ToastContentBuilder()
                    .AddText("Battery Manager")
                    .AddText(message)
                    .AddButton(new ToastButton()
                        .SetContent("OK")
                        .AddArgument("action", $"{tag}_ok"))
                    .AddButton(new ToastButton()
                        .SetContent("Close")
                        .AddArgument("action", $"{tag}_close"))
                    .Show(toast =>
                    {
                        toast.Tag = tag;
                        toast.Group = "BatteryAlerts";
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing notification: {Message}", message);
            }
        }

        /// <summary>
        /// Removes a notification programmatically when condition clears.
        /// </summary>
        public void RemoveNotification(string tag)
        {
            try
            {
                _logger.LogInformation("Removing notification with tag: {Tag}", tag);
                ToastNotificationManagerCompat.History.Remove(tag, "BatteryAlerts");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing notification with tag: {Tag}", tag);
            }
        }

        /// <summary>
        /// Registers a callback handler for a specific action (e.g., "high_battery_ok").
        /// </summary>
        public void RegisterActionHandler(string action, Action handler)
        {
            _actionHandlers[action] = handler;
            _logger.LogDebug("Registered action handler: {Action}", action);
        }

        /// <summary>
        /// Handles toast activation events when user clicks buttons.
        /// </summary>
        private void OnToastActivated(ToastNotificationActivatedEventArgsCompat e)
        {
            try
            {
                var args = ToastArguments.Parse(e.Argument);
                if (args.TryGetValue("action", out var action))
                {
                    _logger.LogInformation("Toast action activated: {Action}", action);

                    if (_actionHandlers.TryGetValue(action, out var handler))
                    {
                        handler.Invoke();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling toast activation");
            }
        }
    }
}
