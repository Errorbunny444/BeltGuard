export type CameraStatus = 'ONLINE' | 'OFFLINE' | 'CONNECTING' | 'ERROR';
export type ModelStatus = 'LOADED' | 'NOT LOADED' | 'LOADING' | 'ERROR';
export type DetectionStatus = 'ACTIVE' | 'INACTIVE' | 'WAITING' | 'ALERT';
export type SystemOperationalStatus = 'System Ready' | 'Active Monitoring' | 'Warning' | 'Critical';

export type DefectSeverity = 'Normal' | 'Low' | 'Medium' | 'High' | 'Critical';

export interface SystemStatusState {
  camera: CameraStatus;
  model: ModelStatus;
  detection: DetectionStatus;
  primaryDefect: string;
  confidence: number | null;
  detectionCount: number;
  operationalStatus: SystemOperationalStatus;
  statusMessage: string;
}
