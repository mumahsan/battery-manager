# Battery Manager Service - Project Structure

## Overview
Complete Windows Service application for intelligent battery management on laptops.

## Project Structure

```
Battery Management/
│
├── BatteryManagerService.sln              # Visual Studio solution file
├── README.md                              # Main documentation
├── QUICK_REFERENCE.md                     # Quick command reference
├── CONFIGURATION.md                       # Detailed configuration guide
├── appsettings.example.json               # Example configuration with comments
├── .gitignore                             # Git ignore rules
├── Install-BatteryManager.ps1             # Installation script
├── Uninstall-BatteryManager.ps1           # Uninstallation script
├── Run-Tests.ps1                          # Test runner script
│
├── BatteryManagerService/                 # Main service project
│   ├── BatteryManagerService.csproj       # Project file (.NET 8.0)
│   ├── Program.cs                         # Entry point and service configuration
│   ├── appsettings.json                   # Runtime configuration
│   ├── BatteryManagerWorker.cs            # Main worker service with state machine
│   │
│   ├── Models/                            # Data models
│   │   ├── BatteryManagerConfig.cs        # Configuration model
│   │   └── BatteryState.cs                # State enum and battery status model
│   │
│   └── Services/                          # Service implementations
│       ├── BatteryMonitor.cs              # WMI battery monitoring
│       ├── NotificationService.cs         # Windows Toast notifications
│       └── VoiceSynthesizer.cs            # Text-to-speech (SAPI)
│
└── BatteryManagerService.Tests/           # Unit test project
    ├── BatteryManagerService.Tests.csproj # Test project file
    ├── BatteryManagerWorkerTests.cs       # Worker service tests
    ├── BatteryMonitorTests.cs             # Battery monitor tests
    ├── NotificationServiceTests.cs        # Notification service tests
    └── VoiceSynthesizerTests.cs           # Voice synthesizer tests
```

## Components

### Core Service
- **Program.cs**: Service host configuration, dependency injection, Serilog setup
- **BatteryManagerWorker.cs**: Main background service with state machine and hysteresis logic

### Services
- **BatteryMonitor.cs**: WMI-based battery percentage and AC status monitoring
- **NotificationService.cs**: Windows Toast notifications with actionable buttons
- **VoiceSynthesizer.cs**: Offline text-to-speech using System.Speech.Synthesis

### Models
- **BatteryManagerConfig.cs**: Configuration settings (thresholds, intervals)
- **BatteryState.cs**: State enum (Normal, HighBatteryAlert, LowBatteryAlert) and status model

### Tests
- **BatteryManagerWorkerTests.cs**: 15+ test outlines for state transitions and hysteresis
- **BatteryMonitorTests.cs**: WMI interaction tests
- **NotificationServiceTests.cs**: Toast notification tests
- **VoiceSynthesizerTests.cs**: TTS concurrency and error handling tests

## Key Features

### 1. Smart Battery Thresholds
- **80% Upper Threshold**: Alert when battery reaches 80% with AC connected
- **20% Lower Threshold**: Alert when battery reaches 20% without AC
- **Configurable**: Both thresholds adjustable via JSON config

### 2. Hysteresis Protection
- **1% Buffer**: Prevents alert flapping
- **High Alert**: Triggers at 80%, clears at 79%
- **Low Alert**: Triggers at 20%, clears at 21%

### 3. Multi-Modal Alerts
- **Toast Notifications**: Windows 10/11 native notifications
- **Actionable Buttons**: OK and Close buttons to dismiss
- **Voice Prompts**: Offline TTS repeating at configurable intervals
- **Auto-Dismissal**: Alerts clear when conditions resolve

### 4. State Machine
```
Normal State
    ├─> HighBatteryAlert (battery ≥ 80% AND AC on)
    └─> LowBatteryAlert (battery ≤ 20% AND AC off)

HighBatteryAlert
    └─> Normal (AC off OR battery < 79%)

LowBatteryAlert
    └─> Normal (AC on OR battery > 21%)
```

### 5. Comprehensive Logging
- **Serilog**: Structured logging to console and file
- **Rolling Files**: Daily logs with 30-day retention
- **Log Levels**: Configurable (Trace, Debug, Info, Warning, Error, Critical)
- **State Changes**: All transitions logged with battery level and AC status

### 6. Concurrency Safety
- **Thread-Safe**: Lock-based state management
- **Timer Management**: Single voice timer with proper cleanup
- **Async Operations**: Non-blocking voice synthesis

## Technology Stack

- **.NET 8.0**: Latest LTS framework
- **C#**: With Allman braces, PascalCase/camelCase conventions
- **Windows Services**: Using Microsoft.Extensions.Hosting.WindowsServices
- **WMI**: System.Management for battery monitoring
- **Toast Notifications**: Microsoft.Toolkit.Uwp.Notifications
- **TTS**: System.Speech.Synthesis (SAPI 5.4)
- **Logging**: Serilog with file and console sinks
- **Testing**: xUnit, Moq, FluentAssertions

## Dependencies (NuGet)

### BatteryManagerService
- Microsoft.Extensions.Hosting (8.0.0)
- Microsoft.Extensions.Hosting.WindowsServices (8.0.0)
- Microsoft.Toolkit.Uwp.Notifications (7.1.3)
- Microsoft.Windows.SDK.Contracts (10.0.22621.2)
- Serilog (3.1.1)
- Serilog.Extensions.Hosting (8.0.0)
- Serilog.Sinks.File (5.0.0)
- Serilog.Sinks.Console (5.0.1)
- System.Management (8.0.0)
- System.Speech (8.0.0)

### BatteryManagerService.Tests
- Microsoft.NET.Test.Sdk (17.8.0)
- xUnit (2.6.2)
- xUnit.runner.visualstudio (2.5.4)
- Moq (4.20.70)
- FluentAssertions (6.12.0)
- coverlet.collector (6.0.0)

## Build & Install

### Quick Start
```powershell
# Clone or navigate to project
cd "c:\Projects\Battery Management"

# Run installer (as Administrator)
.\Install-BatteryManager.ps1
```

### Manual Build
```powershell
# Build
dotnet build BatteryManagerService.sln

# Run tests
dotnet test

# Publish
dotnet publish BatteryManagerService\BatteryManagerService.csproj -c Release -r win-x64 --self-contained false -o publish

# Install service
sc.exe create BatteryManagerService binPath= "$PWD\publish\BatteryManagerService.exe" start= auto
sc.exe start BatteryManagerService
```

## Development Workflow

1. **Make changes** to source code
2. **Run tests**: `.\Run-Tests.ps1`
3. **Build**: `dotnet build`
4. **Test locally**: `dotnet run --project BatteryManagerService\BatteryManagerService.csproj`
5. **Publish**: `dotnet publish -c Release`
6. **Install**: `.\Install-BatteryManager.ps1`

## Logs Location

After installation: `c:\Projects\Battery Management\publish\logs\`

Log files: `battery-manager-YYYYMMDD.log`

## Configuration Location

After installation: `c:\Projects\Battery Management\publish\appsettings.json`

## Service Name

**Name**: BatteryManagerService  
**Display Name**: Battery Manager Service  
**Description**: Monitors battery levels and manages charging thresholds to optimize battery health

## License

MIT License - Free to use and modify

## Platform Requirements

- Windows 10/11 (64-bit)
- .NET 8.0 Runtime
- Administrator privileges (for service installation)
- Audio output device (for voice prompts)

## Limitations

1. **No Direct Charging Control**: Windows doesn't expose APIs to control battery charging hardware
2. **User Action Required**: Service prompts user to plug/unplug AC adapter
3. **Hardware Dependent**: Battery reporting accuracy depends on laptop drivers and firmware
4. **Desktop PCs**: Service runs but won't trigger alerts on systems without batteries

## Future Enhancements

- [ ] Web dashboard for monitoring (ASP.NET Core)
- [ ] Battery health metrics and analytics
- [ ] Email/SMS notifications
- [ ] Custom notification sounds
- [ ] Machine learning for usage patterns
- [ ] Integration with smart home systems
- [ ] Support for multiple batteries (tablets with keyboard dock)

## Support & Troubleshooting

See `README.md` and `QUICK_REFERENCE.md` for detailed troubleshooting steps.

Check logs first:
```powershell
Get-Content "c:\Projects\Battery Management\publish\logs\battery-manager-*.log" -Tail 100
```

---

**Version**: 1.0.0  
**Created**: November 21, 2025  
**Author**: AI-Generated Windows Service  
**Framework**: .NET 8.0  
**Platform**: Windows 10/11 (x64)
