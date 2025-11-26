namespace BatteryManagerService.Models
{
    /// <summary>
    /// Configuration settings for battery management thresholds and intervals.
    /// </summary>
    public class BatteryManagerConfig
    {
        /// <summary>
        /// Upper battery threshold percentage (default: 80%).
        /// Charging should stop and notifications triggered at this level.
        /// </summary>
        public int UpperThreshold { get; set; } = 80;

        /// <summary>
        /// Lower battery threshold percentage (default: 20%).
        /// Charging should start and notifications triggered at this level.
        /// </summary>
        public int LowerThreshold { get; set; } = 20;

        /// <summary>
        /// Interval in seconds between battery status polls (default: 15 seconds).
        /// </summary>
        public int PollIntervalSeconds { get; set; } = 15;

        /// <summary>
        /// Interval in minutes between voice prompt repeats (default: 1 minute).
        /// </summary>
        public int VoiceRepeatMinutes { get; set; } = 1;
    }
}
