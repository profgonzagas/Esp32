---
name: esp32-upload
description: "Upload (flash) Arduino/PlatformIO code to ESP32 microcontroller. Use when: deploying firmware, flashing ESP32, uploading .ino code, programming ESP32, sending code to board."
argument-hint: "Optional: COM port (default COM5) or 'monitor' to also open serial monitor after upload"
---

# ESP32 Upload

Compile and upload firmware to the ESP32 board using PlatformIO CLI.

## When to Use

- User asks to upload/flash/deploy code to the ESP32
- User wants to program the ESP32 board
- User says "faz upload", "envia pro ESP32", "grava no ESP32", "flasha"
- After making changes to `.ino` files and wanting to test on hardware

## Prerequisites

- ESP32 connected via USB
- PlatformIO CLI installed (`pio` command available)
- Project has a `platformio.ini` file

## Procedure

1. **Navigate to the PlatformIO project directory:**
   ```
   cd ESP32_Arduino_Code/ESP32_WiFi_Controller_Complete
   ```

2. **Verify ESP32 is connected** (optional but recommended on first use):
   ```
   pio device list
   ```
   The ESP32 should appear on **COM5** (USB VID:PID=0403:6001). If on a different port, update `platformio.ini` accordingly.

3. **Compile and upload:**
   ```
   pio run --target upload
   ```
   This compiles the code and flashes it to the ESP32 in one step.

4. **Verify success:** Look for `[SUCCESS]` in the output. The ESP32 resets automatically after flashing.

5. **Open serial monitor** (if requested or needed for debugging):
   ```
   pio device monitor
   ```
   Press `Ctrl+C` to exit the monitor.

## Project Layout

- **Code:** `ESP32_Arduino_Code/ESP32_WiFi_Controller_Complete/ESP32_WiFi_Controller_Complete.ino`
- **Config:** `ESP32_Arduino_Code/ESP32_WiFi_Controller_Complete/platformio.ini`
- **Board:** `esp32dev` (ESP32-D0WD-V3)
- **Upload port:** COM5 (921600 baud)
- **Monitor port:** COM5 (115200 baud)

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Port not found | Run `pio device list` to check the correct COM port. Update `upload_port` in `platformio.ini`. |
| Permission denied | Close Arduino IDE or any other serial monitor that may be using the port. |
| Upload fails with timeout | Hold the **BOOT** button on the ESP32 while uploading, release after "Connecting..." appears. |
| Compilation error | Fix the code first. Run `pio run` (without `--target upload`) to compile only. |

---

## Architecture Notes (NecroSense ESP32 Project)

### Critical: `.ino` vs `main.cpp`
- PlatformIO **always compiles `src/main.cpp`** — the `.ino` file is completely **ignored** when `src/main.cpp` exists.
- All firmware changes must be made in `src/main.cpp`, not in `ESP32_WiFi_Controller_Complete.ino`.
- The `.ino` file may be significantly out of date.

### Credentials & Secrets
- `src/secrets.h`: WiFi SSID/pass, MQTT server/port/user/pass — gitignored, keep out of source control.
- **MQTT** (ESP32): `15fdb3019b4f41baa2bd95c3caf3722b.s1.eu.hivemq.cloud`, port `8883` (TLS), using `PubSubClient`.
- **MQTT** (Dashboard JS): `wss://...hivemq.cloud:8884/mqtt`, using `mqtt.js`.
- **Firebase**: `https://necrosense-ed82a-default-rtdb.firebaseio.com/leituras.json`, rules `".write": true` (no auth needed), POST JSON.

### TLS / Heap Constraint
- `WiFiClientSecure` consumes ~40KB heap per instance.
- Running two simultaneous TLS sessions (MQTT + Firebase) causes silent Firebase failures.
- **Fix**: disconnect MQTT before opening Firebase TLS, then set `ultimaTentativaMQTT = 0` so MQTT reconnects immediately after.

### Key Timers in `main.cpp`
| Constant | Value | Purpose |
|---|---|---|
| `INTERVALO_RETRY_MQTT` | 60000 ms | MQTT reconnect interval (60s prevents loop starvation) |
| `FIREBASE_INTERVAL` | 30000 ms | Firebase save interval |
| Publish interval | 30000 ms | MQTT sensor data publish |

### `loop()` Firebase Block
```cpp
if (WiFi.status() == WL_CONNECTED) {
    unsigned long agoraFb = millis();
    if (agoraFb - ultimaSalvagemFirebase >= FIREBASE_INTERVAL) {
        ultimaSalvagemFirebase = agoraFb;
        salvarNoFirebase();
    }
}
```

### `salvarNoFirebase()` Pattern
```cpp
// 1. Disconnect MQTT to free TLS slot
if (mqttClient.connected()) {
    mqttClient.disconnect();
    mqttConectado = false;
    delay(200);
}
ultimaTentativaMQTT = 0; // reconnect immediately after

// 2. Fresh local TLS client (one session at a time)
WiFiClientSecure fbClient;
fbClient.setInsecure();
fbClient.setTimeout(10);

HTTPClient https;
https.setTimeout(10000);
https.setFollowRedirects(HTTPC_STRICT_FOLLOW_REDIRECTS);
if (https.begin(fbClient, FIREBASE_URL)) {
    https.addHeader("Content-Type", "application/json");
    int code = https.POST(body);
    https.end();
}
fbClient.stop();
```

### Dashboard Fixes
- `dashboard/google.html` and `dashboard/index.html`: changed `sessionStorage` → `localStorage` for `mqtt_u`/`mqtt_p` so MQTT credentials survive browser restarts.

### Partition
- Uses `huge_app.csv` partition to fit firmware (Flash ~59%, RAM ~19% at last build).

### Serial Monitor
```
pio device monitor -p COM5 -b 115200
```
After ~30s: look for `[Firebase] ✓ Salvo #1`  
After ~60s: look for `[MQTT] ✓ Conectado!`
