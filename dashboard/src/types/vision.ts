export interface Detection {
  className: string;
  confidence: number;
  bbox: number[];
}

export interface VisionData {
  timestamp: string;
  camera: boolean;
  model: boolean;
  detectionRunning: boolean;
  primaryDefect: string;
  confidence: number;
  count: number;
  detections: Detection[];
}
