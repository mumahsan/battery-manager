# Battery Manager Service - System Diagrams

## Component Architecture

```
┌───────────────────────────────────────────────────────────────────┐
│                        Program.cs (Entry Point)                    │
│  • Serilog Configuration                                          │
│  • Dependency Injection Setup                                     │
│  • Windows Service Host                                           │
└─────────────────────────────┬─────────────────────────────────────┘
                              │
                              ▼
┌───────────────────────────────────────────────────────────────────┐
│                    BatteryManagerWorker                           │
│  • Background Service (BackgroundService)                         │
│  • State Machine (Normal/HighAlert/LowAlert)                     │
│  • Hysteresis Logic                                              │
│  • Timer Management                                              │
└───────┬───────────┬──────────────┬────────────────┬──────────────┘
        │           │              │                │
        ▼           ▼              ▼                ▼
┌──────────┐ ┌──────────┐ ┌──────────────┐ ┌──────────────┐
│ Battery  │ │  Voice   │ │ Notification │ │    Config    │
│ Monitor  │ │Synthesizer│ │   Service    │ │    Model     │
└──────────┘ └──────────┘ └──────────────┘ └──────────────┘
     │             │              │                │
     ▼             ▼              ▼                ▼
┌─────────┐ ┌─────────┐ ┌──────────────┐ ┌──────────────┐
│   WMI   │ │  SAPI   │ │Windows Toast │ │appsettings   │
│Win32_   │ │ System. │ │     API      │ │   .json      │
│Battery  │ │ Speech  │ │              │ │              │
└─────────┘ └─────────┘ └──────────────┘ └──────────────┘
```

## State Machine Flow

```
                  Start Service
                       │
                       ▼
              ┌────────────────┐
              │  Normal State  │◄──────────────┐
              │                │               │
              │ • No alerts    │               │
              │ • Monitoring   │               │
              └────────┬───────┘               │
                       │                       │
            Check Battery & AC                 │
                       │                       │
        ┌──────────────┼──────────────┐        │
        │              │              │        │
        ▼              │              ▼        │
  Battery ≥ 80%       │        Battery ≤ 20%  │
  AND AC on          │         AND AC off    │
        │              │              │        │
        ▼              │              ▼        │
┌──────────────┐      │      ┌──────────────┐ │
│HighBattery   │      │      │ LowBattery   │ │
│    Alert     │      │      │    Alert     │ │
│              │      │      │              │ │
│• Show toast  │      │      │• Show toast  │ │
│• Start voice │      │      │• Start voice │ │
│• Timer active│      │      │• Timer active│ │
└──────┬───────┘      │      └──────┬───────┘ │
       │              │             │         │
       │              │             │         │
  AC unplugged        │        AC plugged     │
  OR battery < 79%    │        OR battery >21%│
       │              │             │         │
       └──────────────┴─────────────┴─────────┘
```

## Sequence Diagram: High Battery Alert

```
User    Service    Battery    Notification    Voice
 │         │       Monitor         │            │
 │         │          │            │            │
 │      Poll         │            │            │
 │      ────►        │            │            │
 │         │      GetPercentage   │            │
 │         │      ────────►       │            │
 │         │      80%, AC on      │            │
 │         │      ◄────────       │            │
 │         │          │            │            │
 │    Check State    │            │            │
 │    (Normal → High)│            │            │
 │         │          │            │            │
 │         │          │    ShowNotification     │
 │         │          │    ────────►            │
 │         │          │            │            │
 │    ◄────┴──────────┴────────────┘            │
 │  Toast appears                   │            │
 │  "Battery at 80%"                │            │
 │         │          │            │    SpeakAsync
 │         │          │            │    ─────────►
 │         │          │            │   "Battery at 80%"
 │    ◄────┴──────────┴────────────┴────────────┘
 │  Voice plays                                  │
 │         │          │            │             │
 │  [Wait 1 minute]  │            │             │
 │         │          │            │    SpeakAsync
 │         │          │            │    ─────────►
 │    ◄────┴──────────┴────────────┴────────────┘
 │  Voice repeats                                │
 │         │          │            │             │
 │  Click OK         │            │             │
 │  ────────►        │            │             │
 │         │      StopVoiceTimer  │             │
 │         │      ────────────────┴──────────────►
 │         │          │            │   CancelSpeech
 │         │          │            │             │
 │         │          │    RemoveNotification    │
 │         │          │    ────────►             │
 │    ◄────┴──────────┴────────────┘             │
 │  Notification dismissed                       │
 │  Voice stopped                                │
```

## Hysteresis Visualization

```
Battery %
   │
100├─────────────────────────────────────────────
   │
 90├─────────────────────────────────────────────
   │
 80├─────────────┬─────────TRIGGER─────────────── High Alert ON
   │             │         ▲                      (with AC)
   │             │         │
   │             ▼         │ No re-trigger
 79├─────────────CLEAR─────┼──────────────────── High Alert OFF
   │                       │                      (hysteresis)
   │                       │
 78├───────────────────────┘
   │
   │
 40├─────────────────────────────────────────────
   │
 30├─────────────────────────────────────────────
   │
   │
 21├─────────────CLEAR─────┬──────────────────── Low Alert OFF
   │                       │                      (hysteresis)
   │             ▲         │
   │             │         ▼ No re-trigger
 20├─────────────┴─────────TRIGGER─────────────── Low Alert ON
   │                                              (without AC)
   │
 10├─────────────────────────────────────────────
   │
  0└─────────────────────────────────────────────
             Time →
```

## Data Flow Diagram

```
┌──────────────────────────────────────────────────────────────┐
│                    External Systems                          │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌────────────┐    ┌──────────────┐    ┌────────────────┐  │
│  │    WMI     │    │ Windows Toast│    │ Audio Device   │  │
│  │Win32_Battery    │ Notification │    │  (Speakers)    │  │
│  └──────┬─────┘    └───────▲──────┘    └────────▲───────┘  │
│         │                  │                     │          │
└─────────┼──────────────────┼─────────────────────┼──────────┘
          │                  │                     │
          ▼                  │                     │
     ┌─────────┐             │                     │
     │Battery  │             │                     │
     │Monitor  │             │                     │
     └────┬────┘             │                     │
          │                  │                     │
          │ Battery Level    │                     │
          │ & AC Status      │                     │
          ▼                  │                     │
     ┌──────────────────┐    │                     │
     │Battery Manager   │    │                     │
     │     Worker       │    │                     │
     │                  │    │                     │
     │ ┌──────────────┐ │    │                     │
     │ │State Machine │ │    │                     │
     │ │ • Normal     │ │    │                     │
     │ │ • HighAlert  │◄┼────┼─────────┐           │
     │ │ • LowAlert   │ │    │         │           │
     │ └──────┬───────┘ │    │         │           │
     └────────┼─────────┘    │         │           │
              │              │         │           │
              ▼              │         │           │
       ┌────────────┐        │         │           │
       │   Config   │        │         │           │
       │(thresholds)│        │         │           │
       └────────────┘        │         │           │
              │              │         │           │
              ▼              │         │           │
        Alert Decision       │         │           │
              │              │         │           │
    ┌─────────┴─────────┐    │         │           │
    ▼                   ▼    │         │           │
┌─────────┐        ┌─────────┴─┐       │           │
│Notification      │   Voice   │       │           │
│ Service  │       │Synthesizer│       │           │
└────┬─────┘       └─────┬─────┘       │           │
     │                   │             │           │
     │ Show Toast        │ Speak       │           │
     └───────────────────┼─────────────┘           │
                         └─────────────────────────┘
```

## Threading & Concurrency

```
┌────────────────────────────────────────────────────────┐
│                    Main Thread                         │
│                                                        │
│  ┌──────────────────────────────────────────────┐    │
│  │        BackgroundService Loop                 │    │
│  │                                               │    │
│  │  while (!stoppingToken.IsCancellationRequested) │
│  │  {                                            │    │
│  │      MonitorBatteryAsync();                   │    │
│  │      await Task.Delay(pollInterval);          │    │
│  │  }                                            │    │
│  └───────────────┬──────────────────────────────┘    │
│                  │                                    │
└──────────────────┼────────────────────────────────────┘
                   │
     ┌─────────────┼─────────────┐
     │             │             │
     ▼             ▼             ▼
┌─────────┐  ┌──────────┐  ┌──────────┐
│ State   │  │  Voice   │  │  Toast   │
│  Lock   │  │  Timer   │  │ Handler  │
│(object) │  │(Thread)  │  │(Callback)│
└─────────┘  └──────────┘  └──────────┘
     │             │             │
     │             │             │
     ▼             ▼             ▼
Protects    Timer Callback   Button Click
_currentState    │              Handler
              ┌──▼───┐            │
              │Speech│            │
              │Queue │            │
              └──────┘            │
                  │               │
              Semaphore           │
              (1 at a time)       │
                  │               │
                  └───────┬───────┘
                          │
                    Cancellation
                      Source
```

## Configuration Impact Flow

```
appsettings.json
      │
      ├─► upperThreshold (80)
      │         │
      │         ▼
      │   When battery ≥ 80% AND AC on
      │         │
      │         ▼
      │   Trigger HighBatteryAlert
      │         │
      │         └─► Show notification
      │
      ├─► lowerThreshold (20)
      │         │
      │         ▼
      │   When battery ≤ 20% AND AC off
      │         │
      │         ▼
      │   Trigger LowBatteryAlert
      │         │
      │         └─► Show notification
      │
      ├─► pollIntervalSeconds (15)
      │         │
      │         ▼
      │   Task.Delay(15 seconds) between checks
      │         │
      │         └─► Higher = less CPU, less responsive
      │             Lower = more CPU, more responsive
      │
      └─► voiceRepeatMinutes (1)
              │
              ▼
        Timer interval = 60 seconds
              │
              └─► Voice plays every 60 seconds
                  until dismissed or cleared
```

## Log Flow

```
Service Events
      │
      ├─► State Changes
      │      │
      │      ├─► Normal → HighBatteryAlert
      │      │   "HIGH BATTERY ALERT: Battery at 80%"
      │      │
      │      ├─► Normal → LowBatteryAlert
      │      │   "LOW BATTERY ALERT: Battery at 20%"
      │      │
      │      └─► Back to Normal
      │          "Condition cleared (Battery: 79%, AC: false)"
      │
      ├─► Battery Readings
      │      │
      │      └─► "Battery: 75%, AC: true, State: Normal"
      │
      ├─► User Actions
      │      │
      │      └─► "User dismissed high battery notification"
      │
      ├─► Voice Events
      │      │
      │      ├─► "Speaking: Battery at 80%"
      │      └─► "Voice timer stopped"
      │
      └─► Errors
             │
             ├─► "Error reading battery from WMI"
             └─► "Error showing notification"
                      │
                      ▼
             ┌─────────────────┐
             │   Serilog       │
             │                 │
             │  ┌───────────┐  │
             │  │ Console   │  │  (Development)
             │  └───────────┘  │
             │                 │
             │  ┌───────────┐  │
             │  │File Sink  │  │  (Production)
             │  │Daily Roll │  │
             │  │30-day     │  │
             │  └───────────┘  │
             └─────────────────┘
                      │
                      ▼
        logs/battery-manager-20251121.log
```

---

**Legend:**
- `│`, `├`, `└`, `─`: Flow connectors
- `▼`, `▲`, `►`, `◄`: Direction indicators
- `┌`, `┐`, `└`, `┘`: Boxes
- `◄─`, `─►`: Bidirectional flow

**Note**: These are ASCII diagrams for documentation. For formal design docs, consider using Mermaid, PlantUML, or Draw.io.
