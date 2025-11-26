# Battery Manager Service - Quick Reference

## Installation (Administrator PowerShell)

```powershell
# Navigate to project directory
cd "c:\Projects\Battery Management"

# Run installer script
.\Install-BatteryManager.ps1

# Or manual installation:
dotnet publish BatteryManagerService\BatteryManagerService.csproj -c Release -r win-x64 --self-contained false -o publish
sc.exe create BatteryManagerService binPath= "c:\Projects\Battery Management\publish\BatteryManagerService.exe" start= auto DisplayName= "Battery Manager Service"
sc.exe start BatteryManagerService
```

## Service Management

```powershell
# Check status
Get-Service -Name "BatteryManagerService"

# Start service
Start-Service -Name "BatteryManagerService"

# Stop service
Stop-Service -Name "BatteryManagerService"

# Restart service (after config changes)
Restart-Service -Name "BatteryManagerService"

# View service details
Get-Service -Name "BatteryManagerService" | Format-List *
```

## View Logs

```powershell
# View recent logs
Get-Content "c:\Projects\Battery Management\publish\logs\battery-manager-*.log" -Tail 50

# Monitor logs in real-time
Get-Content "c:\Projects\Battery Management\publish\logs\battery-manager-*.log" -Wait -Tail 20

# View today's log
$today = Get-Date -Format "yyyyMMdd"
Get-Content "c:\Projects\Battery Management\publish\logs\battery-manager-$today.log"
```

## Configuration

**File:** `c:\Projects\Battery Management\publish\appsettings.json`

```json
{
  "BatteryManager": {
    "upperThreshold": 80,
    "lowerThreshold": 20,
    "pollIntervalSeconds": 15,
    "voiceRepeatMinutes": 1
  }
}
```

**After editing, restart service:**
```powershell
Restart-Service -Name "BatteryManagerService"
```

## Troubleshooting

### Service won't start
```powershell
# Check event logs
Get-EventLog -LogName Application -Source BatteryManagerService -Newest 20

# Verify .NET Runtime
dotnet --list-runtimes

# Run in console mode for debugging
cd "c:\Projects\Battery Management\BatteryManagerService"
dotnet run
```

### No notifications
```powershell
# Check Windows notification settings
# Settings > System > Notifications > Battery Manager Service

# Test notifications manually
[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime]
```

### No voice prompts
```powershell
# Test System.Speech
Add-Type -AssemblyName System.Speech
$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
$synth.Speak("Test speech")
```

## Uninstallation

```powershell
# Run uninstaller script
.\Uninstall-BatteryManager.ps1

# Or manual uninstallation:
Stop-Service -Name "BatteryManagerService"
sc.exe delete BatteryManagerService
Remove-Item "c:\Projects\Battery Management\publish" -Recurse -Force
```

## Development

### Build
```powershell
dotnet build BatteryManagerService.sln
```

### Run tests
```powershell
dotnet test BatteryManagerService.Tests\BatteryManagerService.Tests.csproj
```

### Run in development mode
```powershell
dotnet run --project BatteryManagerService\BatteryManagerService.csproj
```

## Behavior Summary

| Battery Level | AC Status | Action |
|--------------|-----------|--------|
| ≥ 80% | Connected | Alert: "Power off" (voice + notification) |
| ≥ 80% | Disconnected | No alert |
| ≤ 20% | Disconnected | Alert: "Connect power" (voice + notification) |
| ≤ 20% | Connected | No alert |
| 79% (from 80%) | Any | Clear high battery alert |
| 21% (from 20%) | Any | Clear low battery alert |

**Hysteresis:** 1% buffer prevents flapping (80%→79% to clear, 20%→21% to clear)

**Voice Prompts:** Repeat every 1 minute until dismissed or condition clears

**User Dismissal:** Click OK or Close on notification to stop voice prompts
