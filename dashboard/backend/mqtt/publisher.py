import time
from datetime import datetime, timezone
from typing import List, Dict, Any, Optional
from .client import MqttClient
from .topics import TOPIC_VISION

class VisionMqttPublisher:
    def __init__(self, mqtt_client: MqttClient):
        self.mqtt = mqtt_client
        self.topic = TOPIC_VISION
        self._last_publish_time = 0

    def publish_vision_state(
        self,
        camera: bool,
        model: bool,
        detection_running: bool,
        detections: List[Dict[str, Any]],
        count: int,
        primary_defect: Optional[str] = None,
        confidence: Optional[float] = None
    ) -> bool:
        """
        Publishes structured JSON detection payload to beltguard/vision topic.
        """
        # Calculate top defect if not provided
        if not primary_defect and detections:
            sorted_dets = sorted(detections, key=lambda d: d.get("confidence", 0), reverse=True)
            top = sorted_dets[0]
            primary_defect = top.get("className", "--")
            confidence = top.get("confidence", 0.0)
        elif not primary_defect:
            primary_defect = "--"
            confidence = 0.0

        payload = {
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "camera": bool(camera),
            "model": bool(model),
            "detectionRunning": bool(detection_running),
            "primaryDefect": primary_defect if primary_defect else "--",
            "confidence": round(float(confidence), 2) if confidence is not None else 0.0,
            "count": int(count),
            "detections": [
                {
                    "className": d.get("className", "Unknown"),
                    "confidence": round(float(d.get("confidence", 0)), 2),
                    "bbox": d.get("bbox", [])
                }
                for d in detections
            ]
        }

        return self.mqtt.publish(self.topic, payload)
