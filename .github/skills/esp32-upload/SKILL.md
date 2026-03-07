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
