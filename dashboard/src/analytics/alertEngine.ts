import { HealthAlert, AlertLevel } from '@/types/health';
import { dashboardService } from '@/services/dashboard.service';
import { DefectSeverity } from '@/types/status';

interface AlertTriggerParams {
  score: number;
  primaryDefect: string | null;
  confidence: number | null;
  temperature: number | null;
  vibration: number | null;
  visionStatus: 'ONLINE' | 'STALE' | 'OFFLINE';
  sensorStatus: 'ONLINE' | 'STALE' | 'OFFLINE';
}

class AlertEngine {
  private static instance: AlertEngine;
  private cooldownMap: Map<string, number> = new Map();
  private readonly COOLDOWN_MS = 10000; // 10 seconds debounce

  private constructor() {}

  public static getInstance(): AlertEngine {
    if (!AlertEngine.instance) {
      AlertEngine.instance = new AlertEngine();
    }
    return AlertEngine.instance;
  }

  /**
   * Evaluates conditions and returns active alerts, debouncing event logging.
   */
  public evaluateAlerts(params: AlertTriggerParams): HealthAlert[] {
    const alerts: HealthAlert[] = [];
    const now = Date.now();
    const timeStr = new Date().toLocaleTimeString('en-US', { hour12: false });

    // Helper to add and potentially log an alert
    const addAlert = (
      key: string,
      level: AlertLevel,
      title: string,
      message: string,
      logSource: 'HEALTH ENGINE' | 'VISION' | 'SENSOR'
    ) => {
      alerts.push({
        id: `alert-${key}-${now}`,
        key,
        level,
        title,
        message,
        timestamp: timeStr,
      });

      // Check debouncing
      const lastLogged = this.cooldownMap.get(key) || 0;
      if (now - lastLogged >= this.COOLDOWN_MS) {
        this.cooldownMap.set(key, now);

        let severity: DefectSeverity = 'Medium';
        if (level === 'CRITICAL') severity = 'Critical';
        else if (level === 'HIGH') severity = 'High';
        else if (level === 'WARNING') severity = 'Medium';
        else severity = 'Low';

        dashboardService.appendEvent({
          time: timeStr,
          source: logSource,
          event: message,
          severity,
        });
      }
    };

    // 1. Health Score Alerts
    if (params.score < 30) {
      addAlert(
        'HEALTH_SCORE_CRITICAL',
        'CRITICAL',
        'Critical Health Condition',
        `Conveyor Health Index dropped to ${params.score}/100 (CRITICAL)`,
        'HEALTH ENGINE'
      );
    } else if (params.score < 60) {
      addAlert(
        'HEALTH_SCORE_WARNING',
        'WARNING',
        'Belt Health Warning',
        `Conveyor Health Index dropped to ${params.score}/100 (WARNING)`,
        'HEALTH ENGINE'
      );
    }

    // 2. Vision Defect Alerts
    const defectLower = (params.primaryDefect || '').toLowerCase();
    const confPct = params.confidence ? Math.round(params.confidence * 100) : 0;

    if (defectLower.includes('large tear')) {
      addAlert(
        'DEFECT_LARGE_TEAR',
        'CRITICAL',
        'Severe Defect: Large Tear',
        `Large Tear detected with ${confPct}% confidence. Immediate inspection required.`,
        'VISION'
      );
    } else if (defectLower.includes('large hole')) {
      addAlert(
        'DEFECT_LARGE_HOLE',
        'CRITICAL',
        'Severe Defect: Large Hole',
        `Large Hole detected with ${confPct}% confidence. Structural belt damage.`,
        'VISION'
      );
    }

    // 3. Sensor Critical Alerts
    if (params.temperature !== null && params.temperature > 65.0) {
      addAlert(
        'SENSOR_TEMP_CRITICAL',
        'CRITICAL',
        'Critical Bearing Temperature',
        `Roller/bearing temperature reached ${params.temperature.toFixed(1)}°C (> 65.0°C)`,
        'SENSOR'
      );
    }

    if (params.vibration !== null && params.vibration > 0.60) {
      addAlert(
        'SENSOR_VIB_CRITICAL',
        'CRITICAL',
        'Severe Mechanical Vibration',
        `Conveyor vibration reached ${params.vibration.toFixed(2)} g (> 0.60 g)`,
        'SENSOR'
      );
    }

    // 4. Telemetry Freshness Alerts
    if (params.sensorStatus === 'OFFLINE') {
      addAlert(
        'SENSOR_CHANNEL_OFFLINE',
        'WARNING',
        'Sensor Telemetry Offline',
        'ESP32 sensor telemetry has timed out (> 15s without messages)',
        'HEALTH ENGINE'
      );
    }

    if (params.visionStatus === 'OFFLINE') {
      addAlert(
        'VISION_CHANNEL_OFFLINE',
        'WARNING',
        'Vision Pipeline Offline',
        'YOLO camera feed is offline (> 15s without inferences)',
        'HEALTH ENGINE'
      );
    }

    return alerts;
  }

  /**
   * Reset cooldowns (e.g. for testing)
   */
  public reset() {
    this.cooldownMap.clear();
  }
}

export const alertEngine = AlertEngine.getInstance();
