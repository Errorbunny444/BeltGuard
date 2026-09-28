import { DashboardState } from '@/types/dashboard';

export const APP_NAME = 'BELTGUARD';
export const APP_SUBTITLE = 'AI Powered Conveyor Belt Monitoring';
export const APP_SLOGAN = 'Safer Mines, Smarter Tomorrows';
export const APP_VERSION = 'v2.0';
export const FOOTER_LEFT = 'BELTGUARD v2.0 | Smart Monitoring for a Safer Tomorrow';

export const INITIAL_DASHBOARD_STATE: DashboardState = {
  status: {
    camera: 'OFFLINE',
    model: 'NOT LOADED',
    detection: 'INACTIVE',
    primaryDefect: '--',
    confidence: null,
    detectionCount: 0,
    operationalStatus: 'System Ready',
    statusMessage: 'Waiting for data...',
  },
  health: {
    score: null, // Displays as '--'
    condition: 'Overall conveyor condition',
    status: 'Waiting for Data',
  },
  camera: {
    isStreaming: false,
    frameUrl: null,
    deviceName: 'AUSHA webcam',
    resolution: '1902 × 1080',
  },
  telemetry: [],
  events: [],
};
