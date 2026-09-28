import cv2
import threading
import time
import numpy as np
from .model import DEFECT_SEVERITY, DEFECT_COLORS

class DefectDetector:
    def __init__(self, camera_manager, model_manager, mqtt_publisher=None):
        self.camera = camera_manager
        self.model = model_manager
        self.mqtt_publisher = mqtt_publisher
        self.is_running = False
        self.lock = threading.Lock()
        
        self.latest_annotated_frame = None
        self.latest_jpeg = None
        self.latest_defects = []
        self.total_detection_count = 0
        self.thread = None
        self.last_detection_time = 0
        self.last_mqtt_publish_time = 0

    def set_mqtt_publisher(self, publisher):
        self.mqtt_publisher = publisher

    def start(self):
        with self.lock:
            if self.is_running:
                return True
            self.is_running = True

        self.camera.start()
        self.thread = threading.Thread(target=self._inference_loop, daemon=True)
        self.thread.start()
        print("[Detector] YOLO Defect Detection pipeline started.")
        return True

    def stop(self):
        with self.lock:
            self.is_running = False
        self.camera.stop()
        
        # Publish stopped state over MQTT
        if self.mqtt_publisher:
            self.mqtt_publisher.publish_vision_state(
                camera=self.camera.is_connected,
                model=self.model.is_loaded,
                detection_running=False,
                detections=[],
                count=self.total_detection_count,
                primary_defect="--",
                confidence=0.0
            )
            
        print("[Detector] YOLO Defect Detection pipeline stopped.")

    def _inference_loop(self):
        while self.is_running:
            frame = self.camera.get_frame()
            if frame is None:
                # Publish standby heartbeat over MQTT every 1s even if no camera frame yet
                now = time.time()
                if self.mqtt_publisher and (now - self.last_mqtt_publish_time > 1.0):
                    self.mqtt_publisher.publish_vision_state(
                        camera=self.camera.is_connected,
                        model=self.model.is_loaded,
                        detection_running=self.is_running,
                        detections=[],
                        count=self.total_detection_count,
                        primary_defect="--",
                        confidence=0.0
                    )
                    self.last_mqtt_publish_time = now

                time.sleep(0.05)
                continue

            annotated_frame = frame.copy()
            detected_items = []

            # Execute YOLO inference if model is loaded
            if self.model.is_loaded:
                result = self.model.predict(frame, conf=0.30)
                if result and result.boxes:
                    for box in result.boxes:
                        try:
                            cls_id = int(box.cls[0].item())
                            conf = float(box.conf[0].item())
                            xyxy = [int(v) for v in box.xyxy[0].tolist()]
                            class_name = self.model.names.get(cls_id, f"Defect-{cls_id}")
                            severity = DEFECT_SEVERITY.get(class_name, "Medium")

                            item = {
                                "className": class_name,
                                "confidence": round(conf, 2),
                                "bbox": xyxy,
                                "severity": severity
                            }
                            detected_items.append(item)

                            # Annotate frame
                            self._draw_box(annotated_frame, xyxy, class_name, conf, severity)
                        except Exception as e:
                            print(f"[Detector] Box parsing error: {e}")

            # Update cumulative detection counts
            now = time.time()
            if detected_items:
                if now - self.last_detection_time > 1.0:
                    self.total_detection_count += len(detected_items)
                    self.last_detection_time = now

            # Compress to JPEG for high-speed streaming
            ret, jpeg = cv2.imencode('.jpg', annotated_frame, [int(cv2.IMWRITE_JPEG_QUALITY), 80])
            jpeg_bytes = jpeg.tobytes() if ret else None

            with self.lock:
                self.latest_annotated_frame = annotated_frame
                self.latest_jpeg = jpeg_bytes
                self.latest_defects = detected_items

            # Publish structured MQTT message (Rate limited: whenever defects exist, or every 200ms heartbeat)
            if self.mqtt_publisher and (detected_items or (now - self.last_mqtt_publish_time > 0.20)):
                top_defect = detected_items[0]["className"] if detected_items else "--"
                top_conf = detected_items[0]["confidence"] if detected_items else 0.0
                
                self.mqtt_publisher.publish_vision_state(
                    camera=self.camera.is_connected,
                    model=self.model.is_loaded,
                    detection_running=self.is_running,
                    detections=detected_items,
                    count=self.total_detection_count,
                    primary_defect=top_defect,
                    confidence=top_conf
                )
                self.last_mqtt_publish_time = now

            time.sleep(0.033)  # Cap at ~30 FPS

    def _draw_box(self, frame, bbox, class_name, conf, severity):
        x1, y1, x2, y2 = bbox
        color = DEFECT_COLORS.get(severity, (0, 0, 239))

        # Industrial Corner Accented Bounding Box
        cv2.rectangle(frame, (x1, y1), (x2, y2), color, 2)
        corner_len = min(20, (x2 - x1) // 4, (y2 - y1) // 4)
        # Top-left corner
        cv2.line(frame, (x1, y1), (x1 + corner_len, y1), color, 4)
        cv2.line(frame, (x1, y1), (x1, y1 + corner_len), color, 4)
        # Top-right corner
        cv2.line(frame, (x2, y1), (x2 - corner_len, y1), color, 4)
        cv2.line(frame, (x2, y1), (x2, y1 + corner_len), color, 4)
        # Bottom-left corner
        cv2.line(frame, (x1, y2), (x1 + corner_len, y2), color, 4)
        cv2.line(frame, (x1, y2), (x1, y2 - corner_len), color, 4)
        # Bottom-right corner
        cv2.line(frame, (x2, y2), (x2 - corner_len, y2), color, 4)
        cv2.line(frame, (x2, y2), (x2, y2 - corner_len), color, 4)

        # Badge label with background
        label = f"{class_name} {int(conf * 100)}%"
        font = cv2.FONT_HERSHEY_SIMPLEX
        scale = 0.55
        thickness = 1
        (label_w, label_h), baseline = cv2.getTextSize(label, font, scale, thickness)
        
        # Position label above box if possible
        label_y1 = max(0, y1 - label_h - 8)
        label_y2 = label_y1 + label_h + 8
        label_x2 = x1 + label_w + 10

        cv2.rectangle(frame, (x1, label_y1), (label_x2, label_y2), color, -1)
        cv2.putText(frame, label, (x1 + 5, label_y2 - 5), font, scale, (255, 255, 255), thickness, cv2.LINE_AA)

    def get_detections_payload(self):
        with self.lock:
            return {
                "defects": self.latest_defects,
                "count": len(self.latest_defects),
                "totalCount": self.total_detection_count
            }

    def get_status_payload(self):
        with self.lock:
            return {
                "camera": self.camera.is_connected,
                "model": self.model.is_loaded,
                "detectionRunning": self.is_running
            }

    def get_latest_jpeg(self):
        with self.lock:
            return self.latest_jpeg
