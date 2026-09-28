# Implemented boundaries

| Producer | Processing / transport | Consumer | Status in supplied source |
| --- | --- | --- | --- |
| MPU6050, DHT22 | ESP32 filtering, RMS, condition rules | OLED and HTTP `/`, `/data` | Implemented firmware, hardware run not verified here |
| Camera | Standalone YOLO/OpenCV | Annotated desktop preview | Source and model supplied |
| Camera | Flask inference, HTTP stream/status, MQTT vision | React dashboard | Source supplied; frontend builds |
| External sensor publisher | MQTT | React telemetry subscriber | Subscriber supplied; main firmware is HTTP-only |
| Keyboard / Python synthetic data | Scenario state / localhost UDP 5055 | Unity health visualization | Implemented demonstrator; Python transport tests pass |
| External sensor + vision dictionaries | Python health bridge | Unity UDP receiver | Bridge supplied; actual physical adapters not supplied |

Do not draw an uninterrupted sensor-to-AI-to-dashboard-to-Unity pipeline as an implemented integration. Firmware vibration monitoring can run independently; vision has both standalone and web-backed implementations. Unity's health model is demonstrative and heuristic. Main-dashboard sensor thresholds also differ from the firmware's local rules; no equivalence or common calibration is established.
