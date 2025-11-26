# Configuration Guide

## Configuration File Location

After installation: `c:\Projects\Battery Management\publish\appsettings.json`

## Configuration Options

### BatteryManager Section

#### upperThreshold
- **Type:** Integer (0-100)
- **Default:** 80
- **Description:** Upper battery percentage threshold
- **Behavior:** 
  - When battery reaches this level AND AC is connected, trigger high battery alert
  - Alert clears when battery drops below (upperThreshold - 1) OR AC is disconnected
- **Recommended Range:** 75-85
- **Example Use Cases:**
  - 75: More conservative battery protection
  - 80: Standard recommendation (optimal for Lithium-ion)
  - 85: Allow slightly more charge

#### lowerThreshold
- **Type:** Integer (0-100)
- **Default:** 20
- **Description:** Lower battery percentage threshold
- **Behavior:**
  - When battery reaches this level AND AC is NOT connected, trigger low battery alert
  - Alert clears when battery rises above (lowerThreshold + 1) OR AC is connected
- **Recommended Range:** 15-25
- **Example Use Cases:**
  - 15: More runtime before warning
  - 20: Standard recommendation (prevents deep discharge)
  - 25: More conservative battery protection

#### pollIntervalSeconds
- **Type:** Integer (1-300)
- **Default:** 15
- **Description:** Battery status check interval in seconds
- **Behavior:**
  - How frequently to query WMI for battery percentage and AC connection status
  - Lower values = more responsive but slightly higher CPU usage
- **Recommended Range:** 10-30
- **Example Use Cases:**
  - 10: More responsive (for testing or critical scenarios)
  - 15: Standard recommendation (good balance)
  - 30: Lower overhead (for older systems)

#### voiceRepeatMinutes
- **Type:** Decimal (0.1-60)
- **Default:** 1
- **Description:** Voice prompt repeat interval in minutes
- **Behavior:**
  - Voice plays immediately when alert triggers
  - Then repeats at this interval until dismissed or condition clears
- **Recommended Range:** 0.5-5
- **Example Use Cases:**
  - 0.5: Very frequent reminders (30 seconds)
  - 1: Standard recommendation (every minute)
  - 2: Less frequent (every 2 minutes)
  - 5: Occasional reminders only

### Logging Section

#### LogLevel.Default
- **Type:** String
- **Default:** "Information"
- **Options:** Trace, Debug, Information, Warning, Error, Critical, None
- **Description:** Minimum log level for all categories
- **Example Use Cases:**
  - "Debug": Troubleshooting (very verbose)
  - "Information": Normal operation (recommended)
  - "Warning": Only warnings and errors
  - "Error": Only errors and critical issues

## Example Configurations

### Standard Configuration (Recommended)
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

### Conservative Battery Protection
Stricter thresholds for maximum battery longevity:
```json
{
  "BatteryManager": {
    "upperThreshold": 75,
    "lowerThreshold": 25,
    "pollIntervalSeconds": 15,
    "voiceRepeatMinutes": 1
  }
}
```

### Performance Mode
More responsive with less frequent voice prompts:
```json
{
  "BatteryManager": {
    "upperThreshold": 80,
    "lowerThreshold": 20,
    "pollIntervalSeconds": 10,
    "voiceRepeatMinutes": 2
  }
}
```

### Low Overhead Mode
For older systems or minimal CPU usage:
```json
{
  "BatteryManager": {
    "upperThreshold": 80,
    "lowerThreshold": 20,
    "pollIntervalSeconds": 30,
    "voiceRepeatMinutes": 3
  }
}
```

### Debug Configuration
For troubleshooting issues:
```json
{
  "BatteryManager": {
    "upperThreshold": 80,
    "lowerThreshold": 20,
    "pollIntervalSeconds": 10,
    "voiceRepeatMinutes": 1
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

## Applying Configuration Changes

After editing `appsettings.json`:

```powershell
# Restart the service
Restart-Service -Name "BatteryManagerService"

# Verify service restarted successfully
Get-Service -Name "BatteryManagerService"

# Check logs to confirm new configuration loaded
Get-Content "c:\Projects\Battery Management\publish\logs\battery-manager-*.log" -Tail 20
```

## Hysteresis Explained

The service uses **1% hysteresis** to prevent alert flapping:

### High Battery Alert (80%)
- **Trigger:** Battery ≥ 80% AND AC connected
- **Clear:** Battery < 79% OR AC disconnected
- **Why:** Prevents repeated alerts if battery fluctuates around 80%

### Low Battery Alert (20%)
- **Trigger:** Battery ≤ 20% AND AC not connected
- **Clear:** Battery > 21% OR AC connected
- **Why:** Prevents repeated alerts if battery fluctuates around 20%

### Example Scenario
```
Battery Level: 79% → 80% → 80% → 79% → 80%
Alert Status:  None → Triggered → Active → Cleared → Triggered

Without hysteresis:
Alert would trigger/clear repeatedly at exactly 80%

With hysteresis:
Alert triggers at 80%, stays active until 78% (below 79%)
```

## Validation Rules

The service validates configuration on startup:

- **upperThreshold:** Must be > lowerThreshold
- **lowerThreshold:** Must be < upperThreshold
- **pollIntervalSeconds:** Must be ≥ 1
- **voiceRepeatMinutes:** Must be > 0

Invalid configurations will cause the service to fail to start. Check logs for details.

## Tips

1. **Battery Longevity:** Keep charging between 20-80% for optimal Lithium-ion battery health
2. **Responsiveness:** Lower `pollIntervalSeconds` (e.g., 10) for faster detection
3. **Less Intrusive:** Increase `voiceRepeatMinutes` (e.g., 2-3) for less frequent reminders
4. **Troubleshooting:** Set `LogLevel.Default` to "Debug" to see detailed state transitions
5. **Testing:** Use extreme values temporarily (e.g., upperThreshold: 50) to trigger alerts easily
