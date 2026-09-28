# Confirmed components

| Component | Evidence / role | Connection in main firmware |
| --- | --- | --- |
| ESP32 | WiFi/WebServer, SoftAP edge node | Exact board variant unspecified |
| MPU6050 | `MPU6050.h`, motion readings, vibration processing | I2C SDA 21, SCL 22 |
| DHT22 | `DHT.h`, `DHT_TYPE DHT22`, ambient temperature/humidity | Data GPIO 4 |
| SSD1306 128x64 OLED | Adafruit SSD1306/GFX local display | Shared I2C, address 0x3C, reset -1 |
| Camera, model unspecified | OpenCV VideoCapture | AI script index 2; dashboard backend index 0 |

The sketch's acceleration range is +/-4 g and its RMS window is 100 samples. Do not infer sensor calibration, electrical ratings, conveyor motor specifications, camera resolution capability or measurement accuracy from code alone. Check the actual modules before wiring or powering the rig.
