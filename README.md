# WireView Pro 2 - Corsair Xeneon Edge Integration

An integration system for the WireView Pro 2, which enables the creation of Corsair Xeneon Edge (iCUE) widgets that display information similar to the official WireView Pro 2 application. 

> [!CAUTION]
> **Compatibility with the official WireView Pro 2 application**
>
> This integration can run alongside the official WireView Pro 2 application, but both compete for the same device mutex. This, rarely, has two practical consequences:
>
> - **Launch order matters.** Always start the official WireView Pro 2 application first, and only then start the background service (or reboot the PC with the service already installed). If the service acquires the mutex first, the WireView Pro 2 application will launch but might not display any sensor data.
>
> - **If either application hangs or stops updating**, the most likely cause is a stale mutex held by the background service. To release it, restart the service from within the widget: **double-tap or long-press** the widget → **Device Configuration** → scroll to the bottom → **Restart service**. This terminates and restarts the background service, releasing the mutex and allowing both applications to resume normal operation.

Keep in mind that it's in my immediate plans to implement a way to timeout the mutex hold thus, avoiding this issue entirely. For the time being, refer to the guide above in case you encounter any freezes on either application.

# Why does this exist?

This integration was built out of a desire to display WireView Pro 2 sensor readings, particularly per-pin currents for an RTX 5090, on a Corsair Xeneon Edge monitor. The official WireView Pro 2 application works well, but running it on a secondary monitor in portrait orientation caused portions of the UI to be clipped, and dedicating screen real estate to it meant losing space that could be used more productively. The Xeneon Edge, sitting directly in the line of sight, is a natural fit for this kind of at-a-glance telemetry.

## How does this work?

The core of the integration is a Windows background service that communicates with the WireView Pro 2 over USB and periodically polls its sensor data. When a WireView Pro 2 is detected (USB connection and USB header both required), the service starts a polling loop and makes the latest readings available over a local HTTP server.

**Sensor information includes:**
- Pin 1 to 6 current (A)
- Pin 1 to 6 power (W)
- Pin 1 to 6 voltage (V)
- Fault status indicators (overcurrent, overpower, current imbalance, overtemperature, wire overcurrent)
- Total power and current (W and A)
- Sense pin wattage capability
- All temperatures, including external probes 1 and 2 when connected

The service also reads the current device configuration from the WireView Pro 2, as set by the user through the official application, and exposes it alongside the sensor data. The widget uses this configuration to accurately replicate the fault detection and warning behaviour of the official application.

**Device configuration includes:**
- Friendly name
- Backlight percentage
- Logging interval
- Fan mode, temperature source, duty and temperature range (minimum/maximum)
- Temperature limit (°C)
- Current limit (A), wire current limit (A), power limit (W)
- Imbalance limit (%) and imbalance minimum load current (A)
- Shutdown wait time (s)
- Device logging interval (s)
- Sensor averaging setting
- Per-fault enable flags for display, buzzer, soft power cutoff, and hard power cutoff

Sensor data and device configuration are served over a lightweight local HTTP server (Kestrel) on `localhost:59273`. The following endpoints are available:

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/config` | Returns the current service configuration. The only configurable parameter is the sensor polling interval. |
| `POST` | `/config` | Updates the service configuration. Changes take effect immediately. |
| `GET` | `/sensors` | Returns a snapshot of the most recently polled sensor values. Freshness depends on the configured polling interval. |
| `GET` | `/device/config` | Returns the full device configuration as read from the WireView Pro 2. |
| `POST` | `/faults/clear` | Clears one or more fault status bits on the device. The request body specifies which status and log bits to clear independently, allowing individual fault acknowledgement. |
| `POST` | `/restart` | Gracefully restarts the background service. Useful for releasing the device mutex when running alongside the official WireView Pro 2 application. |
| `WebSocket` | `/ws` | Pushes sensor data to all connected subscribers in real time, at the configured polling interval. Supports multiple simultaneous connections. |

The included Corsair Xeneon Edge widget connects to `/ws` on startup and renders incoming data in real time. All widget-side features are described in the section below.

**Note:** The service writes diagnostic events to the Windows Event Viewer under **Windows Logs → Application → WireViewSensorService**.

## Corsair Xeneon Edge Widget

The included Corsair Xeneon Edge widget is a bit reminiscent of the official WireView Pro 2 application display (to provide a familiar viewing experience). It provides the same information displayed by the WireView Pro 2 application, with the addition of a few more niceties.

**Views:**  
The widget offers four display views, selectable from iCUE widget settings:

- **Pins view:** Vertical bar chart for pins P1–P6. Supports three metrics: current (A), power (W), and voltage (V). Optional features include a Y-axis with configurable grid lines, a per-bar background, a maximum imbalance footer, a per-pin power/current/voltage values footer, and an imbalance threshold limit line. All font sizes and colors are individually configurable.
- **Temperatures view:** Arc gauge display for temperature sensors. Supports board sensors (Onboard In / Onboard Out) and external sensors (External 1 / External 2). Temperatures can be displayed in °C or °F. Each sensor can be shown or hidden independently, with configurable arc width, gauge colors, and font sizes.
- **Power view:** Row-based summary of total power (W), total current (A), voltage (V), cable rating (W), and system load percentage. An optional load bar is available. Each row can be individually shown or hidden with configurable colors and font sizes.
- **Faults view:** Grid table listing all fault types (Overtemperature, Overcurrent, Wire Overcurrent, Overpower, Current Imbalance) with their current status and historical log. Active faults can be cleared by tapping them, which sends the clear request to the WireView Pro 2 device via the service.

**Multi-panel layout:**  
On the Corsair Xeneon Edge, the widget can display up to two or three views simultaneously side-by-side, making full use of the available screen width. Panel combinations are configurable from iCUE settings.

**Fault warning banner:**  
A floating banner slides in from the top of the widget whenever the WireView Pro 2 device reports an active fault. The banner shows the fault type (or a count when multiple are active) and can be dismissed per occurrence. It reappears automatically if the fault condition persists after dismissal. Warning color, text color, font size, and background transparency are all configurable.

**Fault details dialog:**  
A secondary overlay that automatically appears when one or more faults are active and the Faults view is not currently visible in any panel. It shows all fault types with their Status and Log state using colour-coded icons. Active faults (Status or Log) can be cleared individually by tapping their icon. The dialog is dismissed with the close button and will reappear on the next data update if faults remain.

**Device configuration overlay:**  
Accessible by double-tapping anywhere on the widget (or long-press, configurable). Displays the full WireView Pro 2 device configuration as read from the device: fan settings, fault thresholds, averaging, logging intervals, and the enabled/disabled state of display, buzzer, soft power cutoff, and hard power cutoff per fault type. Includes a **Restart service** button that gracefully restarts the background Windows service.

**Real-time telemetry:**  
Sensor data is delivered via a WebSocket connection to the background service. The polling interval is configurable from iCUE settings (minimum 200 ms). On connection loss the widget automatically reconnects with exponential backoff and shows a clear error state while the service is unavailable.

**Configurable UI:**  
Virtually every visual element of the widget is configurable through iCUE widget settings: colors, font sizes, show/hide toggles, temperature unit, arc width, and more. Changes take effect immediately.

## Installation

**Prerequisites:** No external dependencies or runtimes are required. The installation package must contain the following files in the same directory before running the script:
- `WireViewMonitor.Service.exe`
- `aspnetcorev2_inprocess.dll`
- `install.ps1`
- `uninstall.ps1`
- `WireViewPro2.icuewidget` (the Corsair iCUE widget file)

**Installing the background service:**

1. Open an elevated PowerShell terminal and run `.\install.ps1`. Administrator privileges are required.
2. The script will install the service to `%ProgramFiles%\WireViewSensorService`, register it as a Windows service that starts automatically, configure automatic recovery on failure, and start it immediately.
3. Once complete, verify that the service is running: open **Task Manager → Services** and look for `WireViewSensorService`, or run `Get-Service WireViewSensorService` in PowerShell.

**Installing the iCUE widget:**

1. Install manually the widget by importing `WireViewPro2.icuewidget` to iCUE's widget library
2. Add the widget to a Corsair Xeneon Edge page.
3. Configure the widget through the iCUE widget settings panel.

**Uninstalling:**

1. Open an elevated PowerShell terminal and run `.\uninstall.ps1`. Administrator privileges are required. The script stops and removes the Windows service and deletes all installed files.
2. Remove the widget from iCUE manually

**Note:** The service writes diagnostic logs to the Windows Event Viewer under **Windows Logs → Application → WireViewSensorService**.

## Missing features

The following features are present in the official WireView Pro 2 application but are not currently implemented in this integration. The table indicates whether each feature is technically feasible and whether it is planned for a future release.

| Feature | Can implement | Planned |
|---|:---:|:---:|
| Device configuration (write) | ✅ | ❌ |
| Device logging | ✅ | ✅ |
| Device monitoring charts | ✅ | ❌ |
| PC shutdown on fault through OS | ✅ | ✅ |
| Firmware updates | ✅ | ❌ |

**Notes:**

- **Device configuration (write):** The service already reads device configuration. Writing configuration back to the device (e.g. changing fault thresholds or fan settings from the widget) is feasible via the same USB protocol but is not planned. I do not want to risk bricking devices due to bad writes or unforseen errors. Use the official WireView Pro 2 application to update your device configuration. Then double tap or long press your WireView Pro 2 iCUE widget to show the device configuration. Hit the refresh button to allow the widget to fetch the new values.

- **Device logging:** The WireView Pro 2 stores an log of sensor readouts. Exposing and displaying these logs through the widget is planned.

- **Device monitoring charts:** The widget runs in a full HTML/JS environment, so real-time line charts of sensor history (current, power, temperature over time) are fully feasible using SVG or Canvas. Not currently planned but architecturally straightforward.

- **PC shutdown on fault through OS:** The service can detect active faults and issue a Windows shutdown command. Planned as an opt-in safety feature for unattended use cases. This feature however will be specifically for users who have not installed the provided front panel jumper cable that restarts the computer through motherboard signals.

- **Firmware updates:** Delivering firmware to the device over USB is technically possible but is not planned. Use the official WireView Pro 2 application for firmware updates. High risk of bricking in case of faulty implementation and practically unable to test it without risk.

## Acknowledgements
- [LibreHardWareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) - WireView Pro 2 polling code
- [WireViewDeviceLib](https://github.com/ElmorLabs-ThermalGrizzly/WireViewDeviceLib) - ElmorLabs / Thermal Grizzly official library
- [

## Disclaimer

The WireView Pro 2 device and the WireView Pro 2 software application are products of **Thermal Grizzly**. All associated trademarks, intellectual property, and copyrights remain the property of Thermal Grizzly. This project is entirely independent and is not affiliated with, endorsed by, or produced in cooperation with Thermal Grizzly or ElmorLabs in any way.

This integration was developed through personal research and reverse engineering for private use, and is provided free of charge. No warranties, **expressed or implied**, are offered. Use it at your own risk.

**Regarding device interaction:** This integration communicates with the WireView Pro 2 using the same USB protocol as the official WireView Pro 2 application. It does not introduce any new hardware commands or exploit undocumented functionality. All additional features (multi-view layout, customisable display, warning overlays, fault details dialog) are implemented entirely within the iCUE widget and the read-only sensor polling service. In theory, using this integration alongside the official application is safe, as it performs the same operations the official software performs.

That said, **any risk associated with running this software alongside your WireView Pro 2 device, including but not limited to warranty implications or hardware damage, is entirely at your own discretion.** If you have concerns about warranty coverage or device compatibility, please contact **Thermal Grizzly** directly before use.