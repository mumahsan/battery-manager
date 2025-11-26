using SmartBatteryAgent.Models;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace SmartBatteryAgent.Services
{
    /// <summary>
    /// Cross-platform system information detector
    /// </summary>
    public interface ISystemDetector
    {
        SystemInfo DetectSystem();
    }

    public class SystemDetector : ISystemDetector
    {
        private readonly ILogger<SystemDetector> _logger;

        public SystemDetector(ILogger<SystemDetector> logger)
        {
            _logger = logger;
        }

        public SystemInfo DetectSystem()
        {
            var info = new SystemInfo
            {
                OperatingSystem = DetectOS(),
                OSVersion = Environment.OSVersion.Version.ToString(),
                DeviceType = DetectDeviceType()
            };

            try
            {
                if (info.OperatingSystem == OSType.Windows)
                {
                    DetectWindowsInfo(info);
                }
                else if (info.OperatingSystem == OSType.Linux)
                {
                    DetectLinuxInfo(info);
                }
                else if (info.OperatingSystem == OSType.MacOS)
                {
                    DetectMacOSInfo(info);
                }

                _logger.LogInformation("System detected: {OS} {Version}, {Device} - {Manufacturer} {Model}",
                    info.OperatingSystem, info.OSVersion, info.DeviceType, info.Manufacturer, info.Model);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not detect complete system information");
            }

            return info;
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

        private DeviceType DetectDeviceType()
        {
            // Check if battery exists - if no battery, likely a desktop
            var hasBattery = CheckForBattery();
            return hasBattery ? DeviceType.Laptop : DeviceType.Desktop;
        }

        private bool CheckForBattery()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return CheckWindowsBattery();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return Directory.Exists("/sys/class/power_supply");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return true; // Most Macs are laptops with batteries
                }
            }
            catch
            {
                // Ignore errors, assume no battery
            }

            return false;
        }

        private bool CheckWindowsBattery()
        {
#if WINDOWS
            try
            {
                var powerStatus = System.Windows.Forms.SystemInformation.PowerStatus;
                return powerStatus.BatteryChargeStatus != System.Windows.Forms.BatteryChargeStatus.NoSystemBattery;
            }
            catch
            {
                return false;
            }
#else
            return false;
#endif
        }

        private void DetectWindowsInfo(SystemInfo info)
        {
#if WINDOWS
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
                foreach (System.Management.ManagementObject obj in searcher.Get())
                {
                    info.Manufacturer = obj["Manufacturer"]?.ToString() ?? "Unknown";
                    info.Model = obj["Model"]?.ToString() ?? "Unknown";
                }

                // Battery information
                using var batterySearcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_Battery");
                foreach (System.Management.ManagementObject obj in batterySearcher.Get())
                {
                    var chemistry = obj["Chemistry"]?.ToString();
                    info.BatteryType = chemistry?.Contains("Li") == true ? BatteryChemistry.LithiumIon : BatteryChemistry.Unknown;
                    info.BatteryDesignCapacity = Convert.ToInt32(obj["DesignCapacity"] ?? 0);
                    info.BatteryCurrentCapacity = Convert.ToInt32(obj["FullChargeCapacity"] ?? 0);
                    
                    if (info.BatteryDesignCapacity > 0)
                    {
                        info.BatteryHealthPercentage = (double)info.BatteryCurrentCapacity / info.BatteryDesignCapacity * 100;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not retrieve Windows system details");
            }
#endif
        }

        private void DetectLinuxInfo(SystemInfo info)
        {
            try
            {
                // Read from /sys/class/dmi/id/
                if (File.Exists("/sys/class/dmi/id/sys_vendor"))
                    info.Manufacturer = File.ReadAllText("/sys/class/dmi/id/sys_vendor").Trim();
                
                if (File.Exists("/sys/class/dmi/id/product_name"))
                    info.Model = File.ReadAllText("/sys/class/dmi/id/product_name").Trim();

                // Battery info from /sys/class/power_supply/BAT*/
                var batteryDirs = Directory.GetDirectories("/sys/class/power_supply", "BAT*");
                if (batteryDirs.Length > 0)
                {
                    var batteryPath = batteryDirs[0];
                    info.BatteryType = BatteryChemistry.LithiumIon; // Most modern laptops

                    if (File.Exists(Path.Combine(batteryPath, "cycle_count")))
                    {
                        info.BatteryCycleCount = int.Parse(File.ReadAllText(Path.Combine(batteryPath, "cycle_count")));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not retrieve Linux system details");
            }
        }

        private void DetectMacOSInfo(SystemInfo info)
        {
            try
            {
                // Use system_profiler command
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "system_profiler",
                    Arguments = "SPHardwareDataType -json",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(processInfo);
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // Parse JSON output
                    var json = Newtonsoft.Json.Linq.JObject.Parse(output);
                    var hardware = json["SPHardwareDataType"]?[0];
                    
                    info.Manufacturer = "Apple";
                    info.Model = hardware?["machine_model"]?.ToString() ?? "Mac";
                }

                info.BatteryType = BatteryChemistry.LithiumPolymer; // Most Macs use Li-Po
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not retrieve macOS system details");
                info.Manufacturer = "Apple";
                info.Model = "Mac";
            }
        }
    }
}
