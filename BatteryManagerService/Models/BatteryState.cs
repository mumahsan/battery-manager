namespace BatteryManagerService.Models
{
    /// <summary>
    /// Represents the current state of the battery management system.
    /// Used for hysteresis to prevent flapping between states.
    /// </summary>
    public enum BatteryState
    {
        /// <summary>
        /// Normal operation, no alerts active.
        /// </summary>
        Normal,

        /// <summary>
        /// Battery at or above upper threshold (80%), user should power off.
        /// </summary>
        HighBatteryAlert,

        /// <summary>
        /// Battery at or below lower threshold (20%), user should connect power.
        /// </summary>
        LowBatteryAlert
    }

    /// <summary>
    /// Contains current battery status information.
    /// </summary>
    public class BatteryStatus
    {
        /// <summary>
        /// Current battery charge level (0-100).
        /// </summary>
        public int ChargeLevel { get; set; }

        /// <summary>
        /// Indicates whether AC power is connected.
        /// </summary>
        public bool IsACConnected { get; set; }

        /// <summary>
        /// Current battery management state.
        /// </summary>
        public BatteryState State { get; set; }
    }
}
