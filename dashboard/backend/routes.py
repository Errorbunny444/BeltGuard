import time
from flask import Blueprint, jsonify, Response, request

def create_api_blueprint(detector):
    bp = Blueprint('api', __name__)

    @bp.route('/status', methods=['GET'])
    def get_status():
        """Returns the system vision & model status."""
        return jsonify(detector.get_status_payload())

    @bp.route('/detections', methods=['GET'])
    def get_detections():
        """Returns the latest YOLO defects and detection count."""
        return jsonify(detector.get_detections_payload())

    @bp.route('/frame', methods=['GET'])
    def get_frame():
        """Returns the single latest annotated JPEG frame."""
        jpeg = detector.get_latest_jpeg()
        if jpeg is None:
            return ("Camera feed standby", 503)
        return Response(jpeg, mimetype='image/jpeg')

    @bp.route('/video_feed', methods=['GET'])
    def video_feed():
        """Returns an MJPEG stream of real-time annotated camera frames."""
        def generate():
            while True:
                jpeg = detector.get_latest_jpeg()
                if jpeg is not None:
                    yield (b'--frame\r\n'
                           b'Content-Type: image/jpeg\r\n\r\n' + jpeg + b'\r\n')
                time.sleep(0.033)

        return Response(generate(), mimetype='multipart/x-mixed-replace; boundary=frame')

    @bp.route('/start', methods=['POST'])
    def start_detection():
        """Starts the vision inference pipeline."""
        success = detector.start()
        return jsonify({"success": success, "status": detector.get_status_payload()})

    @bp.route('/stop', methods=['POST'])
    def stop_detection():
        """Stops the vision inference pipeline."""
        detector.stop()
        return jsonify({"success": True, "status": detector.get_status_payload()})

    @bp.route('/simulation', methods=['POST'])
    def toggle_simulation():
        """Toggles simulation feed for testing without physical camera."""
        data = request.get_json(silent=True) or {}
        enabled = data.get("enabled", True)
        detector.camera.enable_simulation(enabled)
        return jsonify({"simulation": enabled, "camera": detector.camera.is_connected})

    return bp
