# Smart Battery Agent - AI-Powered Cross-Platform Battery Management

An intelligent, cross-platform battery management agent that automatically detects your device and applies manufacturer-specific best practices for battery care.

## 🌟 Features

### Intelligence
- **Auto-Detection**: Automatically identifies your device manufacturer, model, OS, and battery type
- **Smart Recommendations**: Applies manufacturer-specific best practices (Dell, Lenovo, HP, ASUS, Apple, etc.)
- **Research-Based**: Uses compiled knowledge from Battery University, manufacturer guidelines, and industry research
- **Adaptive Thresholds**: Different recommendations based on your specific device and battery chemistry

### Cross-Platform Support
- ✅ **Windows** (7, 8, 10, 11+)
- ✅ **Linux** (Ubuntu, Fedora, Arch, etc.)
- ✅ **macOS** (10.14+)

### Smart Notifications
- Adapts notification style to your OS and version
- Windows 10+: Modern toast notifications
- Linux: notify-send integration
- macOS: Native notification center
- Fallback: Beautiful console notifications

### Manufacturer-Specific Optimization
Pre-configured optimal charging ranges for:
- **Dell**: 20-80% (ExpressCharge aware)
- **Lenovo**: 20-60% (Conservation Mode compatible)
- **HP**: 20-80% (Battery Health Manager)
- **ASUS**: 20-60% (Maximum Lifespan Mode)
- **Apple**: 20-80% (Optimized Battery Charging)
- **Generic**: 20-80% (Universal best practice)

## 🚀 Quick Start

### Prerequisites
- .NET 10.0 Runtime (cross-platform)
- **Windows**: No additional requirements
- **Linux**: `notify-send` (usually pre-installed)
- **macOS**: No additional requirements

### Installation

```bash
# Clone or download
cd SmartBatteryAgent

# Build
dotnet build

# Run
dotnet run
```

### Build for Your Platform

```bash
# Windows
dotnet publish -c Release -r win-x64 --self-contained

# Linux
dotnet publish -c Release -r linux-x64 --self-contained

# macOS Intel
dotnet publish -c Release -r osx-x64 --self-contained

# macOS Apple Silicon
dotnet publish -c Release -r osx-arm64 --self-contained
```

## 📊 How It Works

1. **System Detection**: Identifies your device, OS, manufacturer, and battery type
2. **Best Practices Lookup**: Matches your device to research-based recommendations
3. **Smart Monitoring**: Continuously monitors battery level
4. **Intelligent Alerts**: Notifies when to charge/discharge based on your device's optimal range

## 💡 Best Practices Database

The agent includes a comprehensive knowledge base with:
- **Lithium-Ion batteries**: 20-80% optimal range (most laptops)
- **Lithium-Polymer batteries**: 20-80% optimal range (MacBooks, tablets)
- **Manufacturer overrides**: Device-specific recommendations
- **Research sources**: Battery University, Apple, Dell, Lenovo, HP, ASUS guidelines

### Example Recommendations

**Dell Laptop (Li-ion)**:
- Charge between: 20-80%
- Reasoning: Dell ExpressCharge + longevity balance
- Tips: Use Dell Power Manager for custom thresholds

**Lenovo ThinkPad (Li-ion)**:
- Charge between: 20-60%
- Reasoning: Lenovo Conservation Mode for maximum lifespan
- Tips: Enable Conservation Mode in Lenovo Vantage

**MacBook (Li-Po)**:
- Charge between: 20-80%
- Reasoning: Matches macOS Optimized Battery Charging behavior
- Tips: Enable battery health management in macOS

## ⚙️ Configuration

Edit `appsettings.json`:

```json
{
  "SmartAgent": {
    "PollIntervalSeconds": 30,
    "EnableNotifications": true,
    "NotificationStyle": "Auto"
  }
}
```

## 📱 Platform-Specific Details

### Windows
- Uses WMI for detailed battery information
- Modern toast notifications on Windows 10+
- Detects manufacturer via Win32_ComputerSystem
- Shows battery health percentage

### Linux
- Reads from `/sys/class/power_supply/BAT*`
- Uses `notify-send` for notifications
- Detects manufacturer from `/sys/class/dmi/id/`
- Shows battery cycle count if available

### macOS
- Uses `pmset -g batt` for battery status
- Uses `osascript` for native notifications
- Detects system via `system_profiler`
- Optimized for both Intel and Apple Silicon

## 🎯 Use Cases

### For Laptop Users
- Extends battery lifespan by preventing full charge cycles
- Learns your device's specific requirements
- Adapts to manufacturer recommendations

### For Desktop Users
- Detects no battery and runs in monitoring-only mode
- No annoying alerts for non-battery systems

### For Multi-OS Users
- Same tool works everywhere
- Consistent behavior across platforms
- Unified configuration

## 📖 Example Output

```
═══════════════════════════════════════════════
🧠 Smart Battery Agent - AI-Powered Battery Care
═══════════════════════════════════════════════

📊 System Information:
   Device: Dell Inc. Latitude 7420
   OS: Windows 11.0.22000
   Battery: Lithium-Ion
   Battery Health: 94.2%

💡 Smart Recommendations for Your Device:
   Optimal Charge Range: 20% - 80%
   Reasoning: Dell recommends using their 'Adaptive Battery Optimizer'...
   Source: Dell Official Battery Care Guidelines

✨ Tips for Maximum Battery Lifespan:
   • Dell ExpressCharge can charge to 80% in 1 hour
   • Use Dell Power Manager to set custom charge thresholds
   • Enable 'Primarily AC Use' mode if always plugged in
   • Monthly full discharge/charge cycle not needed

═══════════════════════════════════════════════
🔄 Monitoring started (polling every 30s)
═══════════════════════════════════════════════

🔋 Battery: 75% | 🔌 Discharging | Optimal: 20-80%
🔋 Battery: 82% | ⚡ Charging | Optimal: 20-80%
⚠️  Battery at 82% - Optimal maximum reached. Consider unplugging...
```

## 🔬 Technical Details

### Architecture
- .NET 10.0 cross-platform
- Dependency injection
- Async/await throughout
- Structured logging (Serilog)

### Battery Detection
- **Windows**: WMI (Win32_Battery) + SystemInformation API
- **Linux**: sysfs (`/sys/class/power_supply`)
- **macOS**: `pmset` command

### Knowledge Base
- JSON-based best practices database
- Manufacturer-specific rules
- Battery chemistry awareness
- Research citations

## 🤝 Contributing

The knowledge base can be extended with more:
- Device-specific recommendations
- Battery chemistry variations
- Regional power management guidelines
- User-contributed learnings

## 📝 License

Open source - use and modify as needed

## 🙏 Acknowledgments

Research compiled from:
- Battery University (batteryuniversity.com)
- Apple Support Documentation
- Dell, Lenovo, HP, ASUS official guidelines
- Linux kernel power management documentation
- Industry best practices for lithium-based batteries

---

**Note**: This tool provides recommendations based on research. It cannot physically control charging hardware - you must manually plug/unplug your device based on alerts.
