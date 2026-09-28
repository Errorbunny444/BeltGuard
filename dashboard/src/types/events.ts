import { DefectSeverity } from './status';

export interface SystemEvent {
  id: string;
  time: string;
  source: string;
  event: string;
  severity: DefectSeverity;
  timestamp: string;
}

export type EventFilter = 'All Events' | 'Critical' | 'High' | 'Medium' | 'Low' | 'Normal';
