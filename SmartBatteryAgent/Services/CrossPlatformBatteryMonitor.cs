using SmartBatteryAgent.Models;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace SmartBatteryAgent.Services
{
    /// <summary>
    /// Cross-platform battery monitor
    /// </summary>
    public interface IBatteryMonitor
    {
        BatteryStatus GetBatteryStatus();
        bool HasBattery();
    }

    public class CrossPlatformBatteryMonitor : IBatteryMonitor
    {
        private readonly ILogger<CrossPlatformBatteryMonitor> _logger;
        private readonly OSType _osType;

        public CrossPlatformBatteryMonitor(ILogger<CrossPlatformBatteryMonitor> logger)
        {
            _logger = logger;
            _osType = DetectOS();
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

        public bool HasBattery()
        {
            try
            {
                var status = GetBatteryStatus();
                return status.Percentage >= 0;
            }
            catch
            {
                return false;
            }
        }

        public BatteryStatus GetBatteryStatus()
        {
            return _osType switch
            {
                OSType.Windows => GetWindowsBatteryStatus(),
                OSType.Linux => GetLinuxBatteryStatus(),
                OSType.MacOS => GetMacOSBatteryStatus(),
                _ => new BatteryStatus { Percentage = -1 }
            };
        }

        private BatteryStatus GetWindowsBatteryStatus()
        {
#if WINDOWS
            try
            {
                var powerStatus = System.Windows.Forms.SystemInformation.PowerStatus;
                var status = new BatteryStatus
                {
                    Percentage = (int)(powerStatus.BatteryLifePercent * 100),
                    IsACConnected = powerStatus.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Online,
                    IsCharging = powerStatus.BatteryChargeStatus.HasFlag(System.Windows.Forms.BatteryChargeStatus.Charging),
                    RemainingMinutes = powerStatus.BatteryLifeRemaining / 60,
                    Timestamp = DateTime.Now
                };

                // Get additional WMI info
                using var searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_Battery");
                foreach (System.Management.ManagementObject obj in searcher.Get())
                {
                    if (obj["EstimatedChargeRemaining"] != null)
                        status.Percentage = Convert.ToInt32(obj["EstimatedChargeRemaining"]);
                    
                    status.IsCharging = Convert.ToUInt16(obj["BatteryStatus"]) == 2;
                }

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Windows battery status");
                return new BatteryStatus { Percentage = -1 };
            }
#else
            return new BatteryStatus { Percentage = -1 };
#endif
        }

        private BatteryStatus GetLinuxBatteryStatus()
        {
            try
            {
                var batteryDirs = Directory.GetDirectories("/sys/class/power_supply", "BAT*");
                if (batteryDirs.Length == 0)
                    return new BatteryStatus { Percentage = -1 };

                var batteryPath = batteryDirs[0];
                var status = new BatteryStatus { Timestamp = DateTime.Now };

                // Read capacity
                var capacityFile = Path.Combine(batteryPath, "capacity");
                if (File.Exists(capacityFile))
                    status.Percentage = int.Parse(File.ReadAllText(capacityFile).Trim());

                // Read status
                var statusFile = Path.Combine(batteryPath, "status");
                if (File.Exists(statusFile))
                {
                    var statusText = File.ReadAllText(statusFile).Trim().ToLower();
                    status.IsCharging = statusText == "charging";
                    status.IsACConnected = statusText == "charging" || statusText == "full";
                }

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Linux battery status");
                return new BatteryStatus { Percentage = -1 };
            }
        }

        private BatteryStatus GetMacOSBatteryStatus()
        {
            try
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "pmset",
                    Arguments = "-g batt",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(processInfo);
                if (process == null)
                    return new BatteryStatus { Percentage = -1 };

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var status = new BatteryStatus { Timestamp = DateTime.Now };

                // Parse output: "Now drawing from 'Battery Power' -InternalBattery-0 (id=1234567)	85%; discharging; 3:45 remaining"
                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Contains('%'))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(line, @"(\d+)%");
                        if (match.Success)
                            status.Percentage = int.Parse(match.Groups[1].Value);

                        status.IsCharging = line.Contains("charging") && !line.Contains("discharging");
                        status.IsACConnected = line.Contains("AC Power") || status.IsCharging;
                    }
                }

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting macOS battery status");
                return new BatteryStatus { Percentage = -1 };
            }
        }
    }
}
