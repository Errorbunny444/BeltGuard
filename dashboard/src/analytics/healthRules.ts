import { BeltCondition, ConditionSeverity } from '@/types/health';

/**
 * Phase 5: Belt Intelligence Engine Rule Configurations
 * Fully configurable thresholds and penalty mappings.
 */
export const HEALTH_RULES = {
  BASE_SCORE: 100,

  DEFECT_PENALTIES: {
    'none': 0,
    'no defect': 0,
    'small tear': 15,
    'large tear': 35,
    'small hole': 25,
    'large hole': 45,
    'belt joint': 5,
    'unknown defect': 10,
  } as Record<string, number>,

  MULTIPLE_DEFECT_PENALTY: {
    COUNT_GREATER_THAN_1: 5,
    COUNT_GREATER_THAN_3: 10,
  },

  CONFIDENCE_MODIFIERS: {
    LOW_CONFIDENCE_THRESHOLD: 0.50, // < 50% -> 50% penalty reduction
    HIGH_CONFIDENCE_THRESHOLD: 0.90, // > 90% -> additional 5 penalty
    HIGH_CONFIDENCE_EXTRA_PENALTY: 5,
  },

  TEMPERATURE_THRESHOLDS: {
    NORMAL_MAX: 50.0,
    WARNING_MAX: 65.0,
    WARNING_PENALTY: 15,
    CRITICAL_PENALTY: 30,
  },

  VIBRATION_THRESHOLDS: {
    NORMAL_MAX: 0.30,
    WARNING_MAX: 0.60,
    WARNING_PENALTY: 15,
    CRITICAL_PENALTY: 30,
  },

  BELT_SPEED_THRESHOLDS: {
    STOPPED: 0.05,
    STOPPED_PENALTY: 10,
    LOW_MAX: 0.50,
    LOW_PENALTY: 5,
    NORMAL_MAX: 2.0,
    HIGH_PENALTY: 10,
  },

  DATA_FRESHNESS: {
    STALE_THRESHOLD_MS: 5000,
    OFFLINE_THRESHOLD_MS: 15000,
    SENSOR_OFFLINE_PENALTY: 10,
    VISION_OFFLINE_PENALTY: 15,
  },

  CONDITION_THRESHOLDS: {
    HEALTHY_MIN: 85,
    WARNING_MIN: 60,
    HIGH_RISK_MIN: 30,
  },

  RECOMMENDATIONS: {
    HEALTHY: 'Continue normal operation.',
    WARNING: 'Monitor belt condition and schedule inspection.',
    HIGH_RISK: 'Reduce conveyor speed and inspect belt as soon as possible.',
    CRITICAL: 'Stop conveyor and perform immediate maintenance inspection.',
    LARGE_TEAR_OR_HOLE: 'Severe surface defect detected. Stop or slow conveyor and inspect affected belt section.',
    HIGH_VIBRATION: 'High vibration detected. Check rollers, pulleys, belt alignment, and joint condition.',
    HIGH_TEMPERATURE: 'High belt temperature detected. Check friction, loading, pulley slip, and bearing condition.',
  },
};

/**
 * Maps health score to BeltCondition and ConditionSeverity
 */
export function mapScoreToCondition(score: number): { condition: BeltCondition; severity: ConditionSeverity } {
  if (score >= HEALTH_RULES.CONDITION_THRESHOLDS.HEALTHY_MIN) {
    return { condition: 'HEALTHY', severity: 'NORMAL' };
  }
  if (score >= HEALTH_RULES.CONDITION_THRESHOLDS.WARNING_MIN) {
    return { condition: 'WARNING', severity: 'MEDIUM' };
  }
  if (score >= HEALTH_RULES.CONDITION_THRESHOLDS.HIGH_RISK_MIN) {
    return { condition: 'HIGH_RISK', severity: 'HIGH' };
  }
  return { condition: 'CRITICAL', severity: 'CRITICAL' };
}
