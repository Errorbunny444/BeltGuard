export interface SensorTelemetryData {
  timestamp: string;
  temperature: number | null;
  vibration: number | null;
  beltSpeed: number | null;
}
