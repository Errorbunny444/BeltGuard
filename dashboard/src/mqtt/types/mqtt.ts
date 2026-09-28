export interface DefectItem {
  className: string;
  confidence: number;
  bbox: number[];
}

export interface VisionPayload {
  timestamp: string;
  camera: boolean;
  model: boolean;
  detectionRunning: boolean;
  primaryDefect: string;
  confidence: number;
  count: number;
  detections: DefectItem[];
}

/**
 * Phase 4 ESP32 Sensor Ingestion Contract
 */
export interface SensorTelemetry {
  temperature: number;
  vibration: number;
  beltSpeed: number;
  timestamp: string;
}

export type TemperatureStatus = 'NORMAL' | 'WARNING' | 'CRITICAL';
export type VibrationStatus = 'NORMAL' | 'WARNING' | 'CRITICAL';
export type BeltSpeedStatus = 'STOPPED' | 'LOW' | 'RUNNING' | 'NORMAL';
export type SensorDeviceState = 'WAITING' | 'ONLINE' | 'OFFLINE';

export interface SensorPoint {
  time: string;
  temperature: number;
  vibration: number;
  beltSpeed: number;
}

export interface SensorDataState {
  deviceState: SensorDeviceState;
  temperature: number | null;
  vibration: number | null;
  beltSpeed: number | null;
  temperatureStatus: TemperatureStatus | null;
  vibrationStatus: VibrationStatus | null;
  beltSpeedStatus: BeltSpeedStatus | null;
  lastUpdated: string | null;
}

export type MQTTConnectionStatus =
  | 'CONNECTING'
  | 'CONNECTED'
  | 'DISCONNECTED'
  | 'RECONNECTING';

export type MQTTMessageCallback = (topic: string, message: any) => void;

