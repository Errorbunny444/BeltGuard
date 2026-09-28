import json
import time
import threading
import paho.mqtt.client as mqtt

class MqttClient:
    def __init__(self, client_id="beltguard_python_backend", broker_host="127.0.0.1", broker_port=1883):
        self.broker_host = broker_host
        self.broker_port = broker_port
        self.client_id = client_id
        self.is_connected = False
        self.lock = threading.Lock()

        # Initialize paho-mqtt client with reconnect resilience
        try:
            self.client = mqtt.Client(mqtt.CallbackAPIVersion.VERSION2, client_id=self.client_id)
        except AttributeError:
            self.client = mqtt.Client(client_id=self.client_id)

        self.client.on_connect = self._on_connect
        self.client.on_disconnect = self._on_disconnect

    def _on_connect(self, client, userdata, flags, rc, properties=None):
        rc_code = rc if isinstance(rc, int) else getattr(rc, 'value', 0)
        if rc_code == 0:
            self.is_connected = True
            print(f"[MQTT] Connected successfully to Mosquitto broker at {self.broker_host}:{self.broker_port}")
        else:
            self.is_connected = False
            print(f"[MQTT] Connection failed with code {rc_code}")

    def _on_disconnect(self, client, userdata, disconnect_flags, rc=0, properties=None):
        self.is_connected = False
        print(f"[MQTT] Disconnected from broker (rc: {rc}). Auto-reconnect active.")

    def start(self):
        """Starts background loop and initiates connection."""
        try:
            self.client.connect_async(self.broker_host, self.broker_port, keepalive=60)
            self.client.loop_start()
            return True
        except Exception as e:
            print(f"[MQTT] Initial connection attempt error: {e}")
            return False

    def stop(self):
        """Stops client and loop."""
        try:
            self.client.loop_stop()
            self.client.disconnect()
            self.is_connected = False
        except Exception:
            pass

    def publish(self, topic: str, payload, qos=0, retain=False) -> bool:
        """Publishes JSON string or dictionary to the given topic."""
        try:
            if isinstance(payload, (dict, list)):
                payload_str = json.dumps(payload)
            else:
                payload_str = str(payload)

            result = self.client.publish(topic, payload_str, qos=qos, retain=retain)
            return result.rc == mqtt.MQTT_ERR_SUCCESS
        except Exception as e:
            print(f"[MQTT] Publish error on topic {topic}: {e}")
            return False
