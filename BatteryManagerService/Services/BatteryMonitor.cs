using System.Management;

namespace BatteryManagerService.Services
{
    /// <summary>
    /// Interface for monitoring battery status using WMI.
    /// </summary>
    public interface IBatteryMonitor
    {
        /// <summary>
        /// Gets the current battery charge percentage (0-100).
        /// </summary>
        int GetBatteryPercentage();

        /// <summary>
        /// Determines whether AC power is currently connected.
        /// </summary>
        bool IsACPowerConnected();
    }

    /// <summary>
    /// Battery monitor implementation using WMI (Windows Management Instrumentation).
    /// </summary>
    public class WmiBatteryMonitor : IBatteryMonitor
    {
        private readonly ILogger<WmiBatteryMonitor> _logger;

        public WmiBatteryMonitor(ILogger<WmiBatteryMonitor> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Queries WMI for current battery charge percentage.
        /// </summary>
        public int GetBatteryPercentage()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT EstimatedChargeRemaining FROM Win32_Battery");
                using var collection = searcher.Get();

                foreach (ManagementObject obj in collection)
                {
                    var charge = Convert.ToInt32(obj["EstimatedChargeRemaining"]);
                    _logger.LogDebug("Battery charge: {Charge}%", charge);
                    return charge;
                }

                // No battery found (desktop PC)
                _logger.LogWarning("No battery detected. Returning 100%.");
                return 100;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading battery percentage from WMI");
                return -1;
            }
        }

        /// <summary>
        /// Queries WMI to determine if AC power is connected.
        /// Uses Win32_Battery.BatteryStatus where 2 = AC power connected.
        /// </summary>
        public bool IsACPowerConnected()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT BatteryStatus FROM Win32_Battery");
                using var collection = searcher.Get();

                foreach (ManagementObject obj in collection)
                {
                    var status = Convert.ToUInt16(obj["BatteryStatus"]);
                    // BatteryStatus: 1 = Discharging, 2 = AC connected, 3 = Fully Charged, etc.
                    bool isAC = (status == 2 || status == 3);
                    _logger.LogDebug("AC Power Connected: {IsAC} (Status: {Status})", isAC, status);
                    return isAC;
                }

                // No battery = assume AC connected
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading AC power status from WMI");
                return true; // Fail-safe: assume AC connected
            }
        }
    }
}
