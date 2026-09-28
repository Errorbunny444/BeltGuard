import cv2
import threading
import time
import numpy as np

class CameraManager:
    def __init__(self, camera_index=0, width=1920, height=1080):
        self.camera_index = camera_index
        self.width = width
        self.height = height
        self.cap = None
        self.is_running = False
        self.is_connected = False
        self.lock = threading.Lock()
        self.latest_frame = None
        self.thread = None
        self.use_simulation = False
        self._sim_offset = 0

    def start(self):
        with self.lock:
            if self.is_running:
                return True
            self.is_running = True

        self._connect_camera()
        self.thread = threading.Thread(target=self._capture_loop, daemon=True)
        self.thread.start()
        return True

    def _connect_camera(self):
        """Attempts to open physical webcam across any connected USB port (indexes 0, 1, 2)."""
        search_indices = [self.camera_index] + [i for i in [0, 1, 2] if i != self.camera_index]
        for idx in search_indices:
            try:
                cap = cv2.VideoCapture(idx, cv2.CAP_DSHOW)
                if not cap.isOpened():
                    cap = cv2.VideoCapture(idx)

                if cap.isOpened():
                    cap.set(cv2.CAP_PROP_FRAME_WIDTH, self.width)
                    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, self.height)
                    cap.set(cv2.CAP_PROP_FPS, 30)
                    ret, frame = cap.read()
                    if ret and frame is not None:
                        self.cap = cap
                        self.camera_index = idx
                        self.is_connected = True
                        with self.lock:
                            self.latest_frame = frame
                        print(f"[Camera] AUSHA Webcam connected on USB index {idx} ({frame.shape[1]}x{frame.shape[0]})")
                        return
                    cap.release()
            except Exception:
                pass

        self.is_connected = False
        if self.cap:
            try:
                self.cap.release()
            except Exception:
                pass
            self.cap = None

    def _capture_loop(self):
        consecutive_failures = 0
        last_reconnect_attempt = time.time()
        while self.is_running:
            if self.is_connected and self.cap and self.cap.isOpened():
                ret, frame = self.cap.read()
                if ret and frame is not None:
                    consecutive_failures = 0
                    with self.lock:
                        self.latest_frame = frame
                else:
                    consecutive_failures += 1
                    if consecutive_failures >= 15:
                        print(f"[Camera] Lost camera signal after {consecutive_failures} failed frames. Reconnecting...")
                        self.is_connected = False
                        if self.cap:
                            try:
                                self.cap.release()
                            except Exception:
                                pass
                            self.cap = None
                        consecutive_failures = 0
                        time.sleep(1.0)
                        self._connect_camera()
                    else:
                        time.sleep(0.04)
            else:
                # If simulation enabled
                if self.use_simulation:
                    frame = self._generate_conveyor_frame()
                    with self.lock:
                        self.latest_frame = frame
                    time.sleep(0.033)  # ~30 fps
                else:
                    # Periodically retry finding physical webcam every 2.0 seconds (Hotplug support)
                    now = time.time()
                    if now - last_reconnect_attempt > 2.0:
                        last_reconnect_attempt = now
                        self._connect_camera()
                    time.sleep(0.2)

    def _generate_conveyor_frame(self):
        """Generates a realistic moving conveyor belt simulation frame with test textures."""
        self._sim_offset = (self._sim_offset + 8) % 100
        frame = np.full((720, 1280, 3), (35, 40, 48), dtype=np.uint8)

        # Draw conveyor belt bed
        cv2.rectangle(frame, (160, 0), (1120, 720), (22, 25, 30), -1)
        cv2.rectangle(frame, (160, 0), (1120, 720), (65, 75, 90), 2)

        # Draw moving transverse roller bars / cleats
        for y in range(self._sim_offset - 100, 720 + 100, 90):
            if 0 <= y < 720:
                cv2.line(frame, (160, y), (1120, y), (45, 52, 62), 3)

        # Draw rubber grain texture
        noise = np.random.randint(-8, 8, (720, 960, 3), dtype=np.int16)
        belt_area = frame[:, 160:1120].astype(np.int16) + noise
        frame[:, 160:1120] = np.clip(belt_area, 0, 255).astype(np.uint8)

        # Overlay Conveyor Belt status HUD
        cv2.putText(frame, "BELTGUARD LIVE SIMULATION", (180, 45),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.9, (16, 185, 129), 2, cv2.LINE_AA)
        cv2.putText(frame, f"RESOLUTION: 1920x1080 (SCALED) | FPS: 30", (180, 75),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.5, (148, 163, 184), 1, cv2.LINE_AA)

        return frame

    def get_frame(self):
        with self.lock:
            if self.latest_frame is not None:
                return self.latest_frame.copy()
            return None

    def enable_simulation(self, enable=True):
        self.use_simulation = enable

    def stop(self):
        with self.lock:
            self.is_running = False
        if self.cap:
            try:
                self.cap.release()
            except Exception:
                pass
            self.cap = None
        self.is_connected = False
        print("[Camera] Stopped camera capture.")
