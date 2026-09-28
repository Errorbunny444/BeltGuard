# Main dashboard

React/TypeScript/Vite frontend with a Flask/OpenCV/YOLO backend. Source, public assets, lockfile, build configuration and the original model are retained. No application logic was rewritten. The supplied standalone health-rule test is retained in `tests/`; it duplicates rules and should not be treated as an integration test of the React engine.

## Prerequisites

Node.js/npm compatible with the supplied Vite lockfile, Python with pip, a working camera, and Mosquitto for MQTT messaging. The frontend was installed and built successfully during preparation; exact validation versions are in [the report](../docs/PREPARATION_REPORT.md).

## Frontend

From `dashboard`:

```powershell
npm install
npm run dev
```

For reproducible locked installation, use `npm ci`. Open `http://localhost:5173`.

Optional: copy `.env.example` to `.env`. Blank values use the browser's current hostname with API port **8000** and MQTT WebSocket port **9001**. For an override set `VITE_API_BASE_URL` and `VITE_MQTT_WS_URL` to your local endpoints. Never put credentials in `VITE_*` variables: these are browser-visible. Restart Vite after configuration changes. The original `.env` was deliberately excluded.

```powershell
npm run build
npm run preview
```

`npm run build` runs TypeScript checks and Vite. `preview` only serves a built frontend; it does not start Python or MQTT.

## Python backend

In a second terminal, also from `dashboard`:

```powershell
python -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install -r backend/requirements.txt
python backend/main.py
```

This starts the Flask API on port **8000**, selects camera index **0** at a requested 1920x1080, loads `models/best.pt`, and starts detection. Change camera settings locally in `backend/main.py` if needed. The code also supports `PORT` as an environment override. `qrcode`/Pillow are used by the retained optional `backend/generate_qr.py` utility. Requirement versions were not supplied and are not pinned by guesswork.

The backend returns camera/model status and detections, serves the video stream, and publishes vision messages to a TCP MQTT broker at **127.0.0.1:1883**. The original 5,460,299-byte checkpoint is kept at `models/best.pt`. Two identical fallback copies and the expanded `best/` checkpoint directory were omitted; the existing loader resolves the kept file.

## MQTT services

The supplied `backend/mqtt/mosquitto.conf` is a **WebSocket listener bridged to a separate TCP broker**, not a combined single-broker configuration. Start a local Mosquitto broker on 1883 first (or use an existing one). Then start a second Mosquitto process with the supplied bridge configuration:

```powershell
mosquitto -p 1883
```

In another terminal from `dashboard`:

```powershell
mosquitto -c backend/mqtt/mosquitto.conf
```

The second process exposes WebSockets on 9001 and bridges topics to 1883. Do not start another broker on a port already occupied. This supplied configuration permits anonymous local-demo access and binds WebSockets on all interfaces; use it only on a trusted development network. A secured deployment needs separate configuration.

The supplied ESP32 firmware serves HTTP and does **not** publish the MQTT sensor topic expected here. Vision can operate through the backend, while physical sensor telemetry requires an additional publisher/adapter matching `src/mqtt/topics.ts` and `src/mqtt/services/sensorService.ts`. Simulator controls and placeholder displays are prototype features, not proof of physical measurements.

## Verification limits

`npm ci --offline --ignore-scripts --no-audit --no-fund` and `npm run build` passed using the local npm cache. Vite emitted a bundle-size warning. Backend Python parsed successfully; camera inference and broker integration were not exercised because those runtime dependencies/hardware were not installed for preparation. See the preparation report for remaining checks.
