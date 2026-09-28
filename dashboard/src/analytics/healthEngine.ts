import {
  HealthAssessment,
  HealthPenalties,
} from '@/types/health';
import { VisionData } from '@/types/vision';
import { HEALTH_RULES, mapScoreToCondition } from './healthRules';
import { alertEngine } from './alertEngine';
import { dashboardService } from '@/services/dashboard.service';

type HealthListener = (assessment: HealthAssessment) => void;

export class HealthEngine {
  private static instance: HealthEngine;

  private latestVision: VisionData | null = null;
  private lastVisionTime: number = 0;

  private listeners: Set<HealthListener> = new Set();
  private currentAssessment: HealthAssessment;

  private constructor() {
    this.currentAssessment = this.getInitialAssessment();
  }

  public static getInstance(): HealthEngine {
    if (!HealthEngine.instance) {
      HealthEngine.instance = new HealthEngine();
    }
    return HealthEngine.instance;
  }

  private getInitialAssessment(): HealthAssessment {
    return {
      score: 100,
      condition: 'HEALTHY',
      severity: 'NORMAL',
      recommendation: HEALTH_RULES.RECOMMENDATIONS.HEALTHY,
      reasons: ['System initialized. Awaiting live telemetry.'],
      alerts: [],
      penalties: {
        defectPenalty: 0,
        sensorPenalty: 0,
        freshnessPenalty: 0,
        totalPenalty: 0,
      },
      visionStatus: 'OFFLINE',
      sensorStatus: 'OFFLINE',
      lastEvaluated: new Date().toLocaleTimeString('en-US', { hour12: false }),
    };
  }

  public getAssessment(): HealthAssessment {
    return { ...this.currentAssessment };
  }

  public subscribe(listener: HealthListener): () => void {
    this.listeners.add(listener);
    listener({ ...this.currentAssessment });
    return () => {
      this.listeners.delete(listener);
    };
  }

  private notify() {
    const copy = { ...this.currentAssessment };
    this.listeners.forEach((listener) => {
      try {
        listener(copy);
      } catch (err) {
        console.error('[HealthEngine] Listener error:', err);
      }
    });

    // Keep dashboardService in sync
    dashboardService.updateHealth({
      score: copy.score,
      condition: copy.condition,
      status: copy.recommendation,
    });
  }

  /**
   * Ingest vision telemetry from beltguard/vision
   */
  public updateVision(data: VisionData) {
    this.latestVision = data;
    this.lastVisionTime = Date.now();
    this.evaluate();
  }

  public updateSensors(_data?: any) {
    // No-op in pure vision mode
  }

  /**
   * Evaluates freshness status for vision and sensor channels
   */
  public getChannelStatus(lastTime: number): 'ONLINE' | 'STALE' | 'OFFLINE' {
    if (lastTime === 0) return 'OFFLINE';
    const age = Date.now() - lastTime;
    if (age < HEALTH_RULES.DATA_FRESHNESS.STALE_THRESHOLD_MS) return 'ONLINE';
    if (age < HEALTH_RULES.DATA_FRESHNESS.OFFLINE_THRESHOLD_MS) return 'STALE';
    return 'OFFLINE';
  }

  /**
   * Evaluates the entire conveyor health index based on rule mappings
   */
  public evaluate() {
    const visionStatus = this.getChannelStatus(this.lastVisionTime);
    const sensorStatus: 'ONLINE' = 'ONLINE';

    const reasons: string[] = [];
    let defectPenalty = 0;
    let freshnessPenalty = 0;

    // -------------------------------------------------------------
    // 1. Defect-Based Penalties
    // -------------------------------------------------------------
    let primaryDefectName: string | null = null;
    let primaryConfidence: number | null = null;

    if (visionStatus !== 'OFFLINE' && this.latestVision) {
      const v = this.latestVision;
      const defectKey = (v.primaryDefect || '').trim().toLowerCase();
      primaryDefectName = v.primaryDefect || null;
      primaryConfidence = v.confidence || null;

      if (defectKey && defectKey !== '--' && defectKey !== 'none' && defectKey !== 'no defect') {
        let basePenalty = HEALTH_RULES.DEFECT_PENALTIES[defectKey];
        if (basePenalty === undefined) {
          if (defectKey.includes('large tear')) basePenalty = 35;
          else if (defectKey.includes('small tear')) basePenalty = 15;
          else if (defectKey.includes('large hole')) basePenalty = 45;
          else if (defectKey.includes('small hole')) basePenalty = 25;
          else basePenalty = HEALTH_RULES.DEFECT_PENALTIES['unknown defect'] || 10;
        }

        // Multiple detection penalty
        let countPenalty = 0;
        if (v.count > 3) {
          countPenalty = HEALTH_RULES.MULTIPLE_DEFECT_PENALTY.COUNT_GREATER_THAN_3;
        } else if (v.count > 1) {
          countPenalty = HEALTH_RULES.MULTIPLE_DEFECT_PENALTY.COUNT_GREATER_THAN_1;
        }

        let totalForDefect = basePenalty + countPenalty;

        // Confidence modifier
        const conf = v.confidence || 0;
        if (conf < HEALTH_RULES.CONFIDENCE_MODIFIERS.LOW_CONFIDENCE_THRESHOLD) {
          totalForDefect = Math.round(totalForDefect * 0.5);
        } else if (conf > HEALTH_RULES.CONFIDENCE_MODIFIERS.HIGH_CONFIDENCE_THRESHOLD) {
          totalForDefect += HEALTH_RULES.CONFIDENCE_MODIFIERS.HIGH_CONFIDENCE_EXTRA_PENALTY;
        }

        defectPenalty = totalForDefect;
        const confPct = Math.round(conf * 100);
        reasons.push(
          `${v.primaryDefect} detected with ${confPct}% confidence (-${defectPenalty} penalty)`
        );
      }
    }

    // -------------------------------------------------------------
    // 2. Vision Freshness
    // -------------------------------------------------------------
    if (this.lastVisionTime > 0 && visionStatus === 'OFFLINE') {
      freshnessPenalty += HEALTH_RULES.DATA_FRESHNESS.VISION_OFFLINE_PENALTY;
      reasons.push('YOLO vision pipeline offline (> 15s timeout) (-15)');
    }

    // -------------------------------------------------------------
    // 3. Calculate Final Score & Condition
    // -------------------------------------------------------------
    const totalPenalty = defectPenalty + freshnessPenalty;
    const finalScore = Math.max(0, Math.min(100, Math.round(HEALTH_RULES.BASE_SCORE - totalPenalty)));
    const { condition, severity } = mapScoreToCondition(finalScore);

    // If no negative factors
    if (reasons.length === 0) {
      if (visionStatus === 'ONLINE') {
        reasons.push('Visual surface inspection nominal — no tears or defects detected (Score: 100)');
      } else {
        reasons.push('All monitored parameters operating within normal safety limits.');
      }
    }

    // -------------------------------------------------------------
    // 4. Determine Recommended Operator Action
    // -------------------------------------------------------------
    let recommendation = HEALTH_RULES.RECOMMENDATIONS[condition];

    const defLower = (primaryDefectName || '').toLowerCase();
    if (defLower.includes('tear') || defLower.includes('hole')) {
      recommendation = HEALTH_RULES.RECOMMENDATIONS.LARGE_TEAR_OR_HOLE;
    }

    // -------------------------------------------------------------
    // 5. Generate Alerts
    // -------------------------------------------------------------
    const alerts = alertEngine.evaluateAlerts({
      score: finalScore,
      primaryDefect: primaryDefectName,
      confidence: primaryConfidence,
      temperature: null,
      vibration: null,
      visionStatus,
      sensorStatus: 'ONLINE',
    });

    // -------------------------------------------------------------
    // 6. Update Assessment
    // -------------------------------------------------------------
    const penalties: HealthPenalties = {
      defectPenalty,
      sensorPenalty: 0,
      freshnessPenalty,
      totalPenalty,
    };

    this.currentAssessment = {
      score: finalScore,
      condition,
      severity,
      recommendation,
      reasons: reasons.slice(0, 3), // Top 3 reasons
      alerts,
      penalties,
      visionStatus,
      sensorStatus,
      lastEvaluated: new Date().toLocaleTimeString('en-US', { hour12: false }),
    };

    this.notify();
  }
}

export const healthEngine = HealthEngine.getInstance();
