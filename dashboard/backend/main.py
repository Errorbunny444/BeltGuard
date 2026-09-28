import sys
import os
from pathlib import Path

# Ensure root directory is on PYTHONPATH
ROOT_DIR = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT_DIR))

from flask import Flask
from flask_cors import CORS

from backend.vision.camera import CameraManager
from backend.vision.model import ModelManager
from backend.vision.detector import DefectDetector
from backend.routes import create_api_blueprint
from backend.mqtt.client import MqttClient
from backend.mqtt.publisher import VisionMqttPublisher

def create_app():
    app = Flask(__name__)
    CORS(app)  # Enable Cross-Origin Resource Sharing for React Frontend

    # 1. Initialize MQTT Client & Publisher
    mqtt_client = MqttClient(client_id="beltguard_python_core", broker_host="127.0.0.1", broker_port=1883)
    mqtt_client.start()
    mqtt_publisher = VisionMqttPublisher(mqtt_client)

    # 2. Initialize Hardware & Model Components
    camera = CameraManager(camera_index=0, width=1920, height=1080)
    model = ModelManager(model_path="models/best.pt")
    detector = DefectDetector(camera, model, mqtt_publisher=mqtt_publisher)

    # 3. Start detection pipeline immediately on boot
    detector.start()

    # 4. Register API Endpoints
    bp = create_api_blueprint(detector)
    app.register_blueprint(bp)

    @app.route('/')
    def root():
        return {
            "service": "BeltGuard AI Vision API",
            "version": "2.0",
            "mqtt": {
                "connected": mqtt_client.is_connected,
                "broker": f"{mqtt_client.broker_host}:{mqtt_client.broker_port}"
            },
            "status": detector.get_status_payload()
        }

    return app

if __name__ == '__main__':
    app = create_app()
    port = int(os.environ.get("PORT", 8000))
    print(f"🚀 BeltGuard Backend Server running on http://127.0.0.1:{port}")
    # Run threaded for video streaming support
    app.run(host='0.0.0.0', port=port, threaded=True, debug=False)
