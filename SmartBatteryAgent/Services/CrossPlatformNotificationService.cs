using SmartBatteryAgent.Models;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace SmartBatteryAgent.Services
{
    /// <summary>
    /// Cross-platform notification service that adapts to OS and version
    /// </summary>
    public interface INotificationService
    {
        Task ShowNotificationAsync(string title, string message, NotificationPriority priority = NotificationPriority.Normal);
        bool IsSupported();
    }

    public enum NotificationPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    public class CrossPlatformNotificationService : INotificationService
    {
        private readonly ILogger<CrossPlatformNotificationService> _logger;
        private readonly OSType _osType;
        private readonly string _osVersion;

        public CrossPlatformNotificationService(ILogger<CrossPlatformNotificationService> logger)
        {
            _logger = logger;
            _osType = DetectOS();
            _osVersion = Environment.OSVersion.Version.ToString();
        }

        private OSType DetectOS()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return OSType.Windows;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return OSType.Linux;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return OSType.MacOS;
            return OSType.Unknown;
        }

        public bool IsSupported()
        {
            return _osType != OSType.Unknown;
        }

        public async Task ShowNotificationAsync(string title, string message, NotificationPriority priority = NotificationPriority.Normal)
        {
            try
            {
                _logger.LogInformation("Showing {Priority} notification: {Title}", priority, title);

                switch (_osType)
                {
                    case OSType.Windows:
                        await ShowWindowsNotificationAsync(title, message, priority);
                        break;
                    case OSType.Linux:
                        await ShowLinuxNotificationAsync(title, message, priority);
                        break;
                    case OSType.MacOS:
                        await ShowMacOSNotificationAsync(title, message, priority);
                        break;
                    default:
                        await ShowConsoleNotificationAsync(title, message, priority);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing notification, falling back to console");
                await ShowConsoleNotificationAsync(title, message, priority);
            }
        }

        private async Task ShowWindowsNotificationAsync(string title, string message, NotificationPriority priority)
        {
            var version = Environment.OSVersion.Version;
            
            // Windows 10 build 17763+ supports modern toast notifications
            if (version.Major >= 10 && version.Build >= 17763)
            {
                try
                {
                    // Use Windows.UI.Notifications for modern toast
                    var toastXml = $@"
                        <toast>
                            <visual>
                                <binding template='ToastGeneric'>
                                    <text>{title}</text>
                                    <text>{message}</text>
                                </binding>
                            </visual>
                            <audio src='ms-winsoundevent:Notification.Default' />
                        </toast>";

                    _logger.LogDebug("Sending Windows 10+ toast notification");
                    // In a real implementation, would use Microsoft.Toolkit.Uwp.Notifications
                    Console.WriteLine($"\n🔔 {title}\n   {message}\n");
                }
                catch
                {
                    // Fallback to balloon tip
                    Console.WriteLine($"\n💬 {title}: {message}\n");
                }
            }
            else
            {
                // Windows 7/8 - use balloon tip or console
                Console.WriteLine($"\n💬 {title}: {message}\n");
            }

            await Task.CompletedTask;
        }

        private async Task ShowLinuxNotificationAsync(string title, string message, NotificationPriority priority)
        {
            try
            {
                // Use notify-send if available
                var urgency = priority switch
                {
                    NotificationPriority.Critical => "critical",
                    NotificationPriority.High => "normal",
                    _ => "low"
                };

                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "notify-send",
                    Arguments = $"--urgency={urgency} \"{title}\" \"{message}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var process = System.Diagnostics.Process.Start(processInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    _logger.LogDebug("Sent Linux notification via notify-send");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "notify-send not available, using console");
            }

            // Fallback to console
            await ShowConsoleNotificationAsync(title, message, priority);
        }

        private async Task ShowMacOSNotificationAsync(string title, string message, NotificationPriority priority)
        {
            try
            {
                // Use osascript to show notification
                var script = $"display notification \"{message}\" with title \"{title}\"";
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "osascript",
                    Arguments = $"-e '{script}'",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var process = System.Diagnostics.Process.Start(processInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    _logger.LogDebug("Sent macOS notification");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "osascript failed, using console");
            }

            // Fallback to console
            await ShowConsoleNotificationAsync(title, message, priority);
        }

        private async Task ShowConsoleNotificationAsync(string title, string message, NotificationPriority priority)
        {
            var icon = priority switch
            {
                NotificationPriority.Critical => "🚨",
                NotificationPriority.High => "⚠️",
                NotificationPriority.Normal => "🔔",
                _ => "ℹ️"
            };

            var color = priority switch
            {
                NotificationPriority.Critical => ConsoleColor.Red,
                NotificationPriority.High => ConsoleColor.Yellow,
                _ => ConsoleColor.Cyan
            };

            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine($"\n{icon} {title}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"   {message}");
            Console.ForegroundColor = originalColor;
            Console.WriteLine();

            await Task.CompletedTask;
        }
    }
}
