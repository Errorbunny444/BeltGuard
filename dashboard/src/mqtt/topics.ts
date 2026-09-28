export const MQTT_TOPICS = {
  VISION: 'beltguard/vision',
  SENSORS: 'beltguard/sensors',
  SYSTEM: 'beltguard/system',
  ALERTS: 'beltguard/alerts',
} as const;

export type MqttTopic = typeof MQTT_TOPICS[keyof typeof MQTT_TOPICS];
