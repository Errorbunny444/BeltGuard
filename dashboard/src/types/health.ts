export type BeltCondition = 'HEALTHY' | 'WARNING' | 'HIGH_RISK' | 'CRITICAL';

export type ConditionSeverity = 'NORMAL' | 'LOW' | 'MEDIUM' | 'HIGH' | 'CRITICAL';

export type AlertLevel = 'INFO' | 'WARNING' | 'HIGH' | 'CRITICAL';

export interface HealthAlert {
  id: string;
  key: string;
  level: AlertLevel;
  title: string;
  message: string;
  timestamp: string;
}

export interface HealthPenalties {
  defectPenalty: number;
  sensorPenalty: number;
  freshnessPenalty: number;
  totalPenalty: number;
}

export interface HealthAssessment {
  score: number;
  condition: BeltCondition;
  severity: ConditionSeverity;
  recommendation: string;
  reasons: string[];
  alerts: HealthAlert[];
  penalties: HealthPenalties;
  visionStatus: 'ONLINE' | 'STALE' | 'OFFLINE';
  sensorStatus: 'ONLINE' | 'STALE' | 'OFFLINE';
  lastEvaluated: string;
}
