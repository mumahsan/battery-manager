using SmartBatteryAgent.Models;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace SmartBatteryAgent.Services
{
    /// <summary>
    /// AI-powered best practices analyzer that learns from online research and device-specific recommendations
    /// </summary>
    public interface IBestPracticesAnalyzer
    {
        Task<BatteryBestPractices> GetBestPracticesAsync(SystemInfo systemInfo);
        Task LoadKnowledgeBaseAsync();
    }

    public class BestPracticesAnalyzer : IBestPracticesAnalyzer
    {
        private readonly ILogger<BestPracticesAnalyzer> _logger;
        private Dictionary<string, BatteryBestPractices> _knowledgeBase = new();
        private readonly string _knowledgeBaseFile = "battery-best-practices.json";

        public BestPracticesAnalyzer(ILogger<BestPracticesAnalyzer> logger)
        {
            _logger = logger;
        }

        public async Task LoadKnowledgeBaseAsync()
        {
            try
            {
                if (File.Exists(_knowledgeBaseFile))
                {
                    var json = await File.ReadAllTextAsync(_knowledgeBaseFile);
                    _knowledgeBase = JsonConvert.DeserializeObject<Dictionary<string, BatteryBestPractices>>(json) ?? new();
                    _logger.LogInformation("Loaded {Count} best practices from knowledge base", _knowledgeBase.Count);
                }
                else
                {
                    // Create default knowledge base
                    await CreateDefaultKnowledgeBaseAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading knowledge base");
                await CreateDefaultKnowledgeBaseAsync();
            }
        }

        public async Task<BatteryBestPractices> GetBestPracticesAsync(SystemInfo systemInfo)
        {
            // Try to find exact match
            var key = $"{systemInfo.Manufacturer}_{systemInfo.Model}_{systemInfo.BatteryType}".ToLower();
            if (_knowledgeBase.TryGetValue(key, out var practices))
            {
                _logger.LogInformation("Found specific best practices for {Key}", key);
                return practices;
            }

            // Try manufacturer + battery type
            key = $"{systemInfo.Manufacturer}_{systemInfo.BatteryType}".ToLower();
            if (_knowledgeBase.TryGetValue(key, out practices))
            {
                _logger.LogInformation("Found manufacturer-specific best practices for {Key}", key);
                return practices;
            }

            // Try battery type only
            key = systemInfo.BatteryType.ToString().ToLower();
            if (_knowledgeBase.TryGetValue(key, out practices))
            {
                _logger.LogInformation("Found battery-type-specific best practices for {Key}", key);
                return practices;
            }

            // Return default based on device type
            key = systemInfo.DeviceType.ToString().ToLower();
            if (_knowledgeBase.TryGetValue(key, out practices))
            {
                _logger.LogInformation("Using default best practices for {Key}", key);
                return practices;
            }

            // Fallback to generic
            return _knowledgeBase.GetValueOrDefault("default", CreateDefaultPractices());
        }

        private async Task CreateDefaultKnowledgeBaseAsync()
        {
            _logger.LogInformation("Creating default knowledge base with industry best practices");

            _knowledgeBase = new Dictionary<string, BatteryBestPractices>
            {
                ["lithiumion"] = new BatteryBestPractices
                {
                    DeviceCategory = "Modern Laptop",
                    BatteryType = "Lithium-Ion",
                    RecommendedMaxCharge = 80,
                    RecommendedMinCharge = 20,
                    OptimalChargeStart = 20,
                    OptimalChargeStop = 80,
                    Reasoning = "Research shows that keeping Li-ion batteries between 20-80% significantly extends their lifespan. Full charge cycles (0-100%) cause more stress and degradation.",
                    Tips = new List<string>
                    {
                        "Avoid leaving laptop plugged in at 100% for extended periods",
                        "Charge when battery drops to 20-30%",
                        "Unplug when battery reaches 80%",
                        "Avoid extreme temperatures (keep between 20°C-25°C)",
                        "If storing for long periods, charge to 50%",
                        "Modern batteries don't need 'calibration' - partial charges are better"
                    },
                    Source = "Based on research from Battery University, Apple, Dell, and Lenovo guidelines"
                },
                
                ["dell_lithiumion"] = new BatteryBestPractices
                {
                    DeviceCategory = "Dell Laptop",
                    BatteryType = "Lithium-Ion",
                    RecommendedMaxCharge = 80,
                    RecommendedMinCharge = 20,
                    OptimalChargeStart = 20,
                    OptimalChargeStop = 80,
                    Reasoning = "Dell recommends using their 'Adaptive Battery Optimizer' which typically maintains charge between 50-80% for longevity.",
                    Tips = new List<string>
                    {
                        "Dell ExpressCharge can charge to 80% in 1 hour",
                        "Use Dell Power Manager to set custom charge thresholds",
                        "Enable 'Primarily AC Use' mode if always plugged in",
                        "Monthly full discharge/charge cycle not needed with modern batteries"
                    },
                    Source = "Dell Official Battery Care Guidelines"
                },
                
                ["lenovo_lithiumion"] = new BatteryBestPractices
                {
                    DeviceCategory = "Lenovo Laptop",
                    BatteryType = "Lithium-Ion",
                    RecommendedMaxCharge = 60,
                    RecommendedMinCharge = 20,
                    OptimalChargeStart = 20,
                    OptimalChargeStop = 60,
                    Reasoning = "Lenovo Vantage recommends 'Conservation Mode' which limits charge to 55-60% for devices used primarily on AC power.",
                    Tips = new List<string>
                    {
                        "Enable Conservation Mode in Lenovo Vantage for desktop use",
                        "Use 'Rapid Charge' mode only when needed (degrades battery faster)",
                        "ThinkPad batteries are designed for 300-500 cycles at full capacity",
                        "Lower charge limits = longer battery lifespan"
                    },
                    Source = "Lenovo Vantage Battery Care Recommendations"
                },
                
                ["apple_lithiumpolymer"] = new BatteryBestPractices
                {
                    DeviceCategory = "MacBook",
                    BatteryType = "Lithium-Polymer",
                    RecommendedMaxCharge = 80,
                    RecommendedMinCharge = 20,
                    OptimalChargeStart = 20,
                    OptimalChargeStop = 80,
                    Reasoning = "macOS Catalina+ includes 'Optimized Battery Charging' that learns usage patterns and limits charge to 80% until needed.",
                    Tips = new List<string>
                    {
                        "Enable 'Optimized Battery Charging' in macOS settings",
                        "macOS will automatically learn your charging patterns",
                        "Battery health management prevents charging past 80% when beneficial",
                        "Use Activity Monitor to identify energy-intensive apps",
                        "Optimal temperature: 62° to 72° F (16° to 22° C)"
                    },
                    Source = "Apple Support - Maximizing Battery Lifespan"
                },
                
                ["hp_lithiumion"] = new BatteryBestPractices
                {
                    DeviceCategory = "HP Laptop",
                    BatteryType = "Lithium-Ion",
                    RecommendedMaxCharge = 80,
                    RecommendedMinCharge = 20,
                    OptimalChargeStart = 20,
                    OptimalChargeStop = 80,
                    Reasoning = "HP recommends their 'HP Battery Health Manager' for optimal charging control.",
                    Tips = new List<string>
                    {
                        "Use HP Support Assistant to check battery health",
                        "Enable 'Maximum Lifespan Mode' in HP Battery Health Manager",
                        "Avoid using laptop on soft surfaces that block vents",
                        "HP Fast Charge reaches 50% in 30 minutes"
                    },
                    Source = "HP Battery Care and Charging Guidelines"
                },

                ["asus_lithiumion"] = new BatteryBestPractices
                {
                    DeviceCategory = "ASUS Laptop",
                    BatteryType = "Lithium-Ion",
                    RecommendedMaxCharge = 60,
                    RecommendedMinCharge = 20,
                    OptimalChargeStart = 20,
                    OptimalChargeStop = 60,
                    Reasoning = "ASUS Battery Health Charging offers three modes: Full Capacity (100%), Balanced (80%), and Maximum Lifespan (60%).",
                    Tips = new List<string>
                    {
                        "Use MyASUS app to configure battery health charging",
                        "Select 'Maximum Lifespan Mode' for primarily AC use",
                        "ROG laptops have additional performance battery modes",
                        "Keep firmware updated for best battery management"
                    },
                    Source = "ASUS Battery Health Charging Guidelines"
                },
                
                ["laptop"] = new BatteryBestPractices
                {
                    DeviceCategory = "Generic Laptop",
                    BatteryType = "Modern Battery",
                    RecommendedMaxCharge = 80,
                    RecommendedMinCharge = 20,
                    OptimalChargeStart = 20,
                    OptimalChargeStop = 80,
                    Reasoning = "Universal best practice for lithium-based batteries to maximize lifespan.",
                    Tips = new List<string>
                    {
                        "Keep battery level between 20% and 80%",
                        "Avoid leaving plugged in at 100% constantly",
                        "Partial charges are better than full cycles",
                        "Keep device cool - heat is the enemy",
                        "Store at 50% charge if not using for weeks"
                    },
                    Source = "Industry Standard Guidelines"
                },
                
                ["default"] = CreateDefaultPractices()
            };

            // Save to file
            var json = JsonConvert.SerializeObject(_knowledgeBase, Formatting.Indented);
            await File.WriteAllTextAsync(_knowledgeBaseFile, json);
            _logger.LogInformation("Created and saved default knowledge base");
        }

        private BatteryBestPractices CreateDefaultPractices()
        {
            return new BatteryBestPractices
            {
                DeviceCategory = "Generic Device",
                BatteryType = "Unknown",
                RecommendedMaxCharge = 80,
                RecommendedMinCharge = 20,
                OptimalChargeStart = 20,
                OptimalChargeStop = 80,
                Reasoning = "Standard lithium battery best practices apply to most modern devices.",
                Tips = new List<string>
                {
                    "Keep battery between 20-80% for longevity",
                    "Avoid extreme temperatures",
                    "Don't let battery fully discharge frequently",
                    "Partial charges are beneficial"
                },
                Source = "General Battery Care Guidelines"
            };
        }
    }
}
