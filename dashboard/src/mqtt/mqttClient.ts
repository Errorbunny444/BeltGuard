import mqtt, { MqttClient as ClientType } from 'mqtt';
import { MQTTConnectionStatus, MQTTMessageCallback } from './types/mqtt';

export function getMqttWsUrl(): string {
  const envUrl = import.meta.env.VITE_MQTT_WS_URL;
  if (envUrl && typeof envUrl === 'string' && envUrl.trim() !== '') {
    return envUrl;
  }
  const host = typeof window !== 'undefined' && window.location.hostname ? window.location.hostname : 'localhost';
  return `ws://${host}:9001`;
}

class MqttService {
  private static instance: MqttService;
  private client: ClientType | null = null;
  private status: MQTTConnectionStatus = 'DISCONNECTED';
  private currentBrokerUrl: string = getMqttWsUrl();
  private statusListeners: Set<(status: MQTTConnectionStatus) => void> = new Set();
  private topicListeners: Map<string, Set<MQTTMessageCallback>> = new Map();
  private isConnecting: boolean = false;

  private constructor() {}

  public static getInstance(): MqttService {
    if (!MqttService.instance) {
      MqttService.instance = new MqttService();
    }
    return MqttService.instance;
  }

  public getStatus(): MQTTConnectionStatus {
    return this.status;
  }

  public getBrokerUrl(): string {
    return this.currentBrokerUrl;
  }

  public onStatusChange(callback: (status: MQTTConnectionStatus) => void): () => void {
    this.statusListeners.add(callback);
    callback(this.status);
    return () => {
      this.statusListeners.delete(callback);
    };
  }

  private setStatus(newStatus: MQTTConnectionStatus) {
    if (this.status !== newStatus) {
      this.status = newStatus;
      this.statusListeners.forEach((fn) => fn(newStatus));
    }
  }

  public connect(brokerUrl?: string) {
    if (this.client || this.isConnecting) return;

    this.isConnecting = true;
    this.setStatus('CONNECTING');

    const targetUrl = brokerUrl || getMqttWsUrl();
    this.currentBrokerUrl = targetUrl;

    try {
      this.client = mqtt.connect(targetUrl, {
        keepalive: 60,
        reconnectPeriod: 2000,
        connectTimeout: 5000,
        clean: true,
      });

      this.client.on('connect', () => {
        this.isConnecting = false;
        this.setStatus('CONNECTED');
        console.log(`[MQTT] Connected to Mosquitto Broker at ${targetUrl}`);

        // Automatically re-subscribe to all registered topics
        this.topicListeners.forEach((_, topic) => {
          this.client?.subscribe(topic, (err) => {
            if (err) console.error(`[MQTT] Failed to subscribe to ${topic}:`, err);
            else console.log(`[MQTT] Subscribed to ${topic}`);
          });
        });
      });

      this.client.on('reconnect', () => {
        this.setStatus('RECONNECTING');
      });

      this.client.on('close', () => {
        this.setStatus('DISCONNECTED');
      });

      this.client.on('offline', () => {
        this.setStatus('DISCONNECTED');
      });

      this.client.on('error', (err) => {
        console.warn('[MQTT] Error:', err.message);
        this.setStatus('DISCONNECTED');
      });

      this.client.on('message', (topic, payload) => {
        try {
          const payloadStr = payload.toString();
          const parsed = JSON.parse(payloadStr);

          const listeners = this.topicListeners.get(topic);
          if (listeners) {
            listeners.forEach((callback) => {
              try {
                callback(topic, parsed);
              } catch (cbErr) {
                console.error(`[MQTT] Error in callback for topic ${topic}:`, cbErr);
              }
            });
          }
        } catch {
          // If message is not valid JSON, ignore without crashing
        }
      });
    } catch (err) {
      this.isConnecting = false;
      this.setStatus('DISCONNECTED');
      console.warn('[MQTT] Client initialization failed:', err);
    }
  }

  public subscribe(topic: string, callback: MQTTMessageCallback): () => void {
    if (!this.topicListeners.has(topic)) {
      this.topicListeners.set(topic, new Set());
      if (this.client && this.client.connected) {
        this.client.subscribe(topic, (err) => {
          if (err) console.error(`[MQTT] Subscription error on ${topic}:`, err);
        });
      }
    }

    this.topicListeners.get(topic)!.add(callback);

    // Auto-connect if not yet connected
    if (!this.client && !this.isConnecting) {
      this.connect();
    }

    return () => {
      const listeners = this.topicListeners.get(topic);
      if (listeners) {
        listeners.delete(callback);
        if (listeners.size === 0) {
          this.topicListeners.delete(topic);
          this.client?.unsubscribe(topic);
        }
      }
    };
  }

  public disconnect() {
    if (this.client) {
      this.client.end(true);
      this.client = null;
      this.isConnecting = false;
      this.setStatus('DISCONNECTED');
    }
  }
}

export const mqttClient = MqttService.getInstance();
