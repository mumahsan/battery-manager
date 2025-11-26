# Battery Manager

A Windows application that helps extend laptop battery life by alerting users to disconnect power at 80% and connect power at 20%, with visual, audio, and voice notifications.

## Features

- 🔋 **Smart Battery Monitoring**: Monitors battery level every 15 seconds
- 📊 **System Tray Icon**: Shows real-time battery percentage with color coding
  - Red: ≤20% (Low battery)
  - White: 21-79% (Normal)
  - Green: ≥80% (High battery)
- 🔊 **Voice Alerts**: Speaks exact battery percentage with action prompts
- 🔔 **Toast Notifications**: Windows 10/11 native notifications
- ⚡ **Hysteresis Logic**: Prevents alert flapping at threshold boundaries (1% buffer)
- 🎯 **Real-time Updates**: Voice alerts triggered immediately on percentage changes
- ⌨️ **Easy Exit**: Press Ctrl+Shift+X to close the application
- 🚀 **Auto-Start**: Launches automatically on Windows login

- **Automatic Alert Dismissal**

## How It Works

### High Battery Alert (≥80%)
When battery reaches 80% while charging:
- System tray icon turns green
- Toast notification appears
- Voice announces: "Battery at [X] percent. Please power off immediately."
- Voice repeats every minute AND whenever percentage changes
- Alert clears when AC is disconnected or battery drops below 79%

### Low Battery Alert (≤20%)
When battery drops to 20% while unplugged:
- System tray icon turns red
- Toast notification appears
- Voice announces: "Battery at [X] percent. Please connect power immediately."
- Voice repeats every minute AND whenever percentage changes
- Alert clears when AC is connected or battery rises above 21%

## Installation

### Prerequisites
- Windows 10 version 1809 (build 17763) or later
- .NET 8.0 Runtime (if not using self-contained build)

### Quick Install

1. Run `install.bat` as Administrator
2. The application will be installed to: `%LOCALAPPDATA%\BatteryManager`
3. A startup shortcut will be created automatically

### Manual Installation

```bash
# Build and publish
cd BatteryManagerService
dotnet publish -c Release -r win-x64 --self-contained false

# Copy to installation directory
xcopy /Y /E bin\Release\net8.0-windows10.0.17763.0\win-x64\publish\* %LOCALAPPDATA%\BatteryManager\

# Create startup shortcut (optional)
# Add shortcut to: %APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup
```
```

## Usage

### Starting the Application
- **Auto-start**: Launches on Windows login (if installed via install.bat)
- **Manual start**: Run `BatteryManagerService.exe` from installation directory
- **From Start Menu**: Search for "Battery Manager"

### Exiting the Application
- **Keyboard**: Press `Ctrl+Shift+X`
- **System Tray**: Right-click the battery icon → Exit
- **Task Manager**: End the BatteryManagerService process

### Configuration

Edit `appsettings.json` to customize settings:

```json
{
  "BatteryManager": {
    "UpperThreshold": 80,           // Alert when battery reaches this % (charging)
    "LowerThreshold": 20,            // Alert when battery drops to this % (discharging)
    "PollIntervalSeconds": 15,       // How often to check battery level
    "VoiceRepeatMinutes": 1          // Minutes between voice repeats
  }
}
```

## Uninstallation

Run `uninstall.bat` to completely remove the application:
- Stops all running instances
- Removes installation directory
- Removes startup shortcut

1. **At 80% battery (AC connected):**
   - Toast notification appears: "Battery at 80%. Please power off."
   - Voice prompt plays immediately and repeats every minute
   - Click OK/Close to dismiss voice prompts (notification also dismissed)
   - Alert automatically clears when you unplug AC or battery drops to 79%

2. **At 20% battery (AC not connected):**
   - Toast notification appears: "Battery at 20%. Please connect power."
   - Voice prompt plays immediately and repeats every minute
   - Click OK/Close to dismiss voice prompts (notification also dismissed)
   - Alert automatically clears when you plug in AC or battery rises to 21%

## Uninstallation

Open **PowerShell as Administrator**:

```powershell
# Stop the service
Stop-Service -Name "BatteryManagerService"

## Technical Details

### Architecture
- **Platform**: .NET 8.0 / Windows Forms
- **Battery Monitoring**: WMI (Win32_Battery)
- **Notifications**: Windows.UI.Notifications (Toast)
- **Voice Synthesis**: System.Speech.Synthesis (SAPI)
- **Logging**: Serilog (Console + File)

### Project Structure
```
BatteryManagerService/
├── Models/
│   └── BatteryManagerConfig.cs    # Configuration model
├── Services/
│   ├── WmiBatteryMonitor.cs       # Battery monitoring via WMI
│   ├── NotificationService.cs     # Toast notifications
│   ├── VoiceSynthesizer.cs        # Text-to-speech
│   ├── TrayIconService.cs         # System tray icon
│   └── KeyboardHookService.cs     # Global hotkey handler
├── BatteryManagerWorker.cs        # Main background service
├── Program.cs                     # Application entry point
└── appsettings.json               # Configuration file
```

### State Machine

The application uses a state machine with three states:

1. **Normal**: No alerts, monitoring battery level
2. **HighBatteryAlert**: Battery ≥80% and AC connected
3. **LowBatteryAlert**: Battery ≤20% and AC disconnected

Hysteresis (1% buffer) prevents rapid state transitions:
- High alert clears at 79% (not 80%)
- Low alert clears at 21% (not 20%)

### System Requirements
- **OS**: Windows 10 (1809+) or Windows 11
- **RAM**: ~50 MB
- **Disk**: ~30 MB
- **CPU**: Negligible (background monitoring)
- **.NET**: .NET 8.0 Runtime

### Logging

Logs are written to:
- **Console**: Real-time output (when running in terminal)
- **File**: `logs/battery-manager-YYYYMMDD.log`
  - Rolling daily logs
  - Retained for 30 days
  - Located in application directory

## Troubleshooting

### Icon doesn't appear in system tray
- Ensure the app is running (check Task Manager)
- Try restarting the application
- Check Windows notification area settings

### Voice not working
- Verify system audio is not muted
- Check Windows Speech settings
- Ensure SAPI voices are installed (default Windows voices)

### App doesn't start automatically
- Check if shortcut exists in Startup folder: `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup`
- Re-run `install.bat` to recreate the shortcut

### Battery percentage not updating
- Verify WMI service is running: `services.msc` → "Windows Management Instrumentation"
- Check logs for error messages

### Exit hotkey (Ctrl+Shift+X) not working
- Another application may be using the same hotkey
- Use right-click context menu on tray icon instead

### View Logs

Default log path: `%LOCALAPPDATA%\BatteryManager\logs\battery-manager-[date].log`

```powershell
# View recent logs
Get-Content "$env:LOCALAPPDATA\BatteryManager\logs\battery-manager-*.log" -Tail 100
```

## Development

### Building from Source

```bash
# Clone repository
git clone <repository-url>
cd "Battery Management"

# Restore dependencies
cd BatteryManagerService
dotnet restore

# Build
dotnet build

# Run in development
dotnet run

# Publish for release
dotnet publish -c Release -r win-x64 --self-contained false
```

### Testing

```bash
# Run unit tests
cd BatteryManagerService.Tests
dotnet test

# Run with verbose logging
dotnet run --configuration Debug
```

### Dependencies

- Microsoft.Extensions.Hosting (8.0.1)
- Microsoft.Extensions.Hosting.WindowsServices (8.0.1)
- Microsoft.Toolkit.Uwp.Notifications (7.1.3)
- Serilog (4.1.0)
- System.Management (8.0.0)
- System.Speech (8.0.0)

## License

This project is provided as-is for personal use.

## Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## Support

For issues, questions, or feature requests, please create an issue in the repository.

## Changelog

### Version 1.0.0 (Initial Release)
- Battery monitoring at 80%/20% thresholds
- System tray icon with real-time percentage display
- Voice alerts with exact percentage
- Toast notifications
- Hysteresis to prevent alert flapping
- Voice alerts on percentage changes
- Auto-start on Windows login
- Global exit hotkey (Ctrl+Shift+X)
- Installation/uninstallation scripts

## Acknowledgments

- Built with .NET 8.0 and Windows Forms
- Uses Microsoft Toolkit for UWP notifications
- Voice synthesis powered by System.Speech (SAPI)
**Version**: 1.0.0  
**Target Framework**: .NET 8.0  
**Platform**: Windows 10/11 (x64)
