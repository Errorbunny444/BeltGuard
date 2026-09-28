"""Exercise actual simulator datagrams over localhost, including reset and omitted score."""
import importlib.util
import json
from pathlib import Path
import socket
import subprocess
import sys
import unittest

SCRIPT = Path(__file__).resolve().parents[1] / "CP VS CODE/simulate_belt_health_udp.py"
spec = importlib.util.spec_from_file_location("simulator", SCRIPT)
sim = importlib.util.module_from_spec(spec)
spec.loader.exec_module(sim)
bridge_spec = importlib.util.spec_from_file_location("bridge", SCRIPT.with_name("belt_health_bridge.py"))
bridge = importlib.util.module_from_spec(bridge_spec)
bridge_spec.loader.exec_module(bridge)


class SimulatorTests(unittest.TestCase):
    def test_bridge_merges_without_overwriting_sensors(self):
        packet = sim.packet_for("critical")
        sensors = {key: packet[key] for key in bridge.SENSOR_FIELDS}
        vision = {key: packet[key] for key in bridge.VISION_FIELDS}
        vision["belt_speed"] = 999
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as listener:
            listener.bind(("127.0.0.1", 0)); listener.settimeout(2)
            with bridge.BeltHealthBridge(listener.getsockname()[1]) as sender:
                sender.publish(sensors, vision, "J-03")
                received = json.loads(listener.recv(8192))
                self.assertEqual(received["belt_speed"], sensors["belt_speed"])
                self.assertEqual(received["damage_severity"], 78)
                self.assertNotIn("health_score", received)

    def test_bridge_rejects_invalid_readings_and_remote_reset(self):
        for changes in ({"vibration": float("nan")}, {"health_score": -2}, {"joint_id": "J-99"},
                        {"reset_demo": True}, {"confidence": 2}):
            packet = sim.packet_for("healthy"); packet.update(changes)
            with self.assertRaises(ValueError):
                bridge.validate(packet)

    def test_cycle_over_udp(self):
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as listener:
            listener.bind(("127.0.0.1", 0))
            listener.settimeout(5)
            process = subprocess.Popen([sys.executable, str(SCRIPT), "--port", str(listener.getsockname()[1]),
                                        "--interval", "0.02", "--hold", "1", "--count", "6"],
                                       stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
            try:
                packets = [json.loads(listener.recv(8192)) for _ in range(6)]
                stdout, stderr = process.communicate(timeout=5)
                self.assertEqual(process.returncode, 0, stderr)
                self.assertEqual([p["health_score"] for p in packets], [96, 72, 52, 25, 5, 100])
                self.assertTrue(packets[-1]["reset_demo"])
                self.assertTrue(all(not p.get("reset_demo") for p in packets[:-1]))
                self.assertEqual(packets[4]["belt_speed"], 0)
            finally:
                if process.poll() is None:
                    process.kill()
                    process.communicate()

    def test_fallback_payload(self):
        packet = sim.packet_for("rupture", omit_health=True)
        self.assertNotIn("health_score", packet)
        self.assertEqual(packet["damage_severity"], 100)
        self.assertEqual(packet["vibration"], 10)
        self.assertEqual(packet["temperature"], 80)
        self.assertEqual(packet["motor_current"], 4.8)

    def test_manual_scenario(self):
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as listener:
            listener.bind(("127.0.0.1", 0))
            listener.settimeout(5)
            process = subprocess.Popen([sys.executable, str(SCRIPT), "--port", str(listener.getsockname()[1]),
                                        "--interval", "0.05", "--manual", "--count", "4"],
                                       stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
            try:
                process.stdin.write("4\n"); process.stdin.flush()
                packets = [json.loads(listener.recv(8192)) for _ in range(4)]
                process.communicate(timeout=5)
                self.assertEqual(packets[-1]["health_score"], 25)
            finally:
                if process.poll() is None:
                    process.kill(); process.communicate()


if __name__ == "__main__":
    unittest.main(verbosity=2)
