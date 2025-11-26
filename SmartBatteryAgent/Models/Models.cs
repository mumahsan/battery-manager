namespace SmartBatteryAgent.Models
{
    /// <summary>
    /// Operating System types
    /// </summary>
    public enum OSType
    {
        Windows,
        Linux,
        MacOS,
        Unknown
    }

    /// <summary>
    /// Device types
    /// </summary>
    public enum DeviceType
    {
        Laptop,
        Desktop,
        Tablet,
        Unknown
    }

    /// <summary>
    /// Battery chemistry types
    /// </summary>
    public enum BatteryChemistry
    {
        LithiumIon,
        LithiumPolymer,
        NiMH,
        Unknown
    }

    /// <summary>
    /// System information detected from the device
    /// </summary>
    public class SystemInfo
    {
        public OSType OperatingSystem { get; set; }
        public string OSVersion { get; set; } = string.Empty;
        public DeviceType DeviceType { get; set; }
        public string Manufacturer { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public BatteryChemistry BatteryType { get; set; }
        public int BatteryDesignCapacity { get; set; }
        public int BatteryCurrentCapacity { get; set; }
        public int BatteryCycleCount { get; set; }
        public double BatteryHealthPercentage { get; set; }
    }

    /// <summary>
    /// Best practices for battery management
    /// </summary>
    public class BatteryBestPractices
    {
        public string DeviceCategory { get; set; } = string.Empty;
        public string BatteryType { get; set; } = string.Empty;
        public int RecommendedMaxCharge { get; set; }
        public int RecommendedMinCharge { get; set; }
        public int OptimalChargeStart { get; set; }
        public int OptimalChargeStop { get; set; }
        public string Reasoning { get; set; } = string.Empty;
        public List<string> Tips { get; set; } = new();
        public string Source { get; set; } = string.Empty;
    }

    /// <summary>
    /// Configuration for smart battery agent
    /// </summary>
    public class SmartAgentConfig
    {
        public int PollIntervalSeconds { get; set; } = 30;
        public bool EnableVoiceAlerts { get; set; } = true;
        public bool EnableNotifications { get; set; } = true;
        public bool AutoLearnFromUsage { get; set; } = true;
        public string NotificationStyle { get; set; } = "Auto"; // Auto, Native, Console
    }

    /// <summary>
    /// Battery status information
    /// </summary>
    public class BatteryStatus
    {
        public int Percentage { get; set; }
        public bool IsCharging { get; set; }
        public bool IsACConnected { get; set; }
        public int RemainingMinutes { get; set; }
        public double ChargeRate { get; set; }
        public int Temperature { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
