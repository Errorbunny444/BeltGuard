# ESP32 condition-monitoring firmware

`SIH_FINAL_CODE_3/SIH_FINAL_CODE_3.ino` is the supplied sketch, with only the access-point SSID/password values and matching header comments sanitized. The sketch sits in an identically named directory so Arduino IDE can open it directly.

## Hardware and functions

- ESP32 (exact board variant is not recorded).
- MPU6050 on I2C: SDA GPIO 21, SCL GPIO 22; code selects +/-4 g and uses `getMotion6`.
- DHT22 on GPIO 4: ambient temperature and humidity.
- SSD1306 128x64 OLED on I2C at address `0x3C`.

The sketch calibrates/filters acceleration, calculates vibration, a 100-sample RMS window and peak indicators, applies condition thresholds, and drives an OLED and HTTP dashboard. Approximately 200 Hz is the intended sampling rate, not a timing guarantee established in this preparation. Health index is a prototype indicator, not remaining useful life.

## Libraries

Install the ESP32 Arduino board package (provides WiFi and WebServer), an MPU6050 library compatible with `initialize`, `testConnection`, `setFullScaleAccelRange` and `getMotion6` (I2Cdevlib-style API), Adafruit DHT sensor library, Adafruit GFX and Adafruit SSD1306. Install dependencies requested by the library manager, including Adafruit Unified Sensor/BusIO where applicable. `Wire` and `math.h` come with the toolchain. Original library versions were not supplied; the exact MPU6050 package must be verified when compiling.

## Flash and run

1. Open the sketch in Arduino IDE and select your actual ESP32 board and serial port.
2. In a local working copy, replace `YOUR_WIFI_SSID` and `YOUR_WIFI_PASSWORD`. Use a valid SoftAP password of at least eight characters. Do not commit local credentials.
3. Wire the devices as above using voltage levels appropriate for your modules; consult their datasheets.
4. Verify/compile, then upload. Open Serial Monitor at the baud rate configured by the sketch (`115200`).
5. Join the configured ESP32 access point. Browse to the IP printed by the board (normally `http://192.168.4.1`). `/` serves the UI and `/data` serves sensor data.

This dashboard is embedded in the sketch; no separate Node project is required. This firmware does not publish the MQTT sensor messages expected by the main React dashboard. Calibrate thresholds against the actual rig before interpreting condition indicators. Hardware compilation/flashing was not available during repository preparation.
