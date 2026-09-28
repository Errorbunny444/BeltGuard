import { SystemStatusState } from './status';
import { SystemEvent } from './events';

export interface TelemetryPoint {
  time: string;
  vibration: number | null;
  temperature: number | null;
  detectionActivity: number | null;
}

export interface BeltHealthState {
  score: number | null; // null represents '--' (waiting for data)
  condition: string;
  status: string;
}

export interface CameraState {
  isStreaming: boolean;
  frameUrl: string | null;
  deviceName: string;
  resolution: string;
}

export interface DashboardState {
  status: SystemStatusState;
  health: BeltHealthState;
  camera: CameraState;
  telemetry: TelemetryPoint[];
  events: SystemEvent[];
}
