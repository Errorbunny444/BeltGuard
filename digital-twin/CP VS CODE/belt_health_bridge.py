"""Teammate integration boundary: combine sensor/vision dictionaries and publish to Unity.

CLI reads one complete JSON object per stdin line. No camera or serial protocol is assumed.
Example from another Python program:
    with BeltHealthBridge() as bridge:
        bridge.publish(sensor_reading, vision_result, joint_id='J-03')
"""
import argparse
import json
import math
import socket
import sys

SENSOR_FIELDS = ("belt_speed", "motor_current", "vibration", "temperature", "encoder_position")
VISION_FIELDS = ("damage_detected", "damage_type", "damage_severity", "confidence", "belt_position")


def validate(packet):
    required = SENSOR_FIELDS + VISION_FIELDS + ("joint_id",)
    missing = [key for key in required if key not in packet]
    if missing:
        raise ValueError("Missing fields: " + ", ".join(missing))
    if packet["joint_id"] not in [f"J-{i:02}" for i in range(1, 6)]:
        raise ValueError("joint_id must be J-01 through J-05")
    for key in SENSOR_FIELDS + ("damage_severity", "confidence", "belt_position", "health_score"):
        if key not in packet:
            continue
        value = packet[key]
        if isinstance(value, bool) or not isinstance(value, (int, float)) or not math.isfinite(value):
            raise ValueError(f"{key} must be a finite number")
    for key in ("belt_speed", "motor_current", "vibration"):
        if packet[key] < 0:
            raise ValueError(f"{key} must be nonnegative")
    for key, maximum in (("damage_severity", 100), ("confidence", 1), ("health_score", 100)):
        if key in packet and not 0 <= packet[key] <= maximum:
            raise ValueError(f"{key} out of range")
    if type(packet["damage_detected"]) is not bool or not isinstance(packet["damage_type"], str):
        raise ValueError("Invalid damage flag/type")
    if packet.get("reset_demo"):
        raise ValueError("Hardware bridge cannot issue demo resets; use the simulator")


class BeltHealthBridge:
    def __init__(self, port=5055):
        if not 1 <= port <= 65535:
            raise ValueError("Invalid port")
        self.target = ("127.0.0.1", port)
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

    def publish(self, sensors, vision=None, joint_id=None):
        packet = dict(sensors)
        if vision is not None:
            # Vision cannot overwrite motor readings or speed.
            packet.update({key: vision[key] for key in VISION_FIELDS if key in vision})
        if joint_id is not None:
            packet["joint_id"] = joint_id
        validate(packet)
        packet["data_source"] = "python_bridge"
        payload = json.dumps(packet, allow_nan=False).encode("utf-8")
        if len(payload) > 8192:
            raise ValueError("Packet exceeds Unity's 8192-byte limit")
        self.socket.sendto(payload, self.target)

    def __enter__(self):
        return self

    def __exit__(self, *args):
        self.socket.close()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--port", type=int, default=5055)
    args = parser.parse_args()
    with BeltHealthBridge(args.port) as bridge:
        for line in sys.stdin:
            if not line.strip():
                continue
            try:
                bridge.publish(json.loads(line))
            except (ValueError, TypeError) as error:
                print(f"Rejected: {error}", file=sys.stderr)


if __name__ == "__main__":
    main()
