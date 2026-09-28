import {
  SensorTelemetry,
  SensorDataState,
  SensorPoint,
  TemperatureStatus,
  VibrationStatus,
  BeltSpeedStatus,
} from '../types/mqtt';

type SensorListener = (state: SensorDataState, history: SensorPoint[]) => void;

class SensorService {
  private static instance: SensorService;

  private state: SensorDataState = {
    deviceState: 'WAITING',
    temperature: null,
    vibration: null,
    beltSpeed: null,
    temperatureStatus: null,
    vibrationStatus: null,
    beltSpeedStatus: null,
    lastUpdated: null,
  };

  private history: SensorPoint[] = [];
  private listeners: Set<SensorListener> = new Set();
  private watchdogTimer: ReturnType<typeof setTimeout> | null = null;
  private readonly TIMEOUT_MS = 5000;
  private readonly MAX_HISTORY_POINTS = 60;

  private constructor() {}

  public static getInstance(): SensorService {
    if (!SensorService.instance) {
      SensorService.instance = new SensorService();
    }
    return SensorService.instance;
  }

  public getState(): SensorDataState {
    return { ...this.state };
  }

  public getHistory(): SensorPoint[] {
    return [...this.history];
  }

  public subscribe(listener: SensorListener): () => void {
    this.listeners.add(listener);
    listener({ ...this.state }, [...this.history]);
    return () => {
      this.listeners.delete(listener);
    };
  }

  private notify() {
    const currentState = { ...this.state };
    const currentHistory = [...this.history];
    this.listeners.forEach((listener) => {
      try {
        listener(currentState, currentHistory);
      } catch (err) {
        console.error('[SensorService] Listener notification error:', err);
      }
    });
  }

  /**
   * Reset or arm the watchdog timer. If no telemetry arrives within TIMEOUT_MS,
   * mark the sensor device state as OFFLINE.
   */
  private armWatchdog() {
    if (this.watchdogTimer) {
      clearTimeout(this.watchdogTimer);
    }

    this.watchdogTimer = setTimeout(() => {
      if (this.state.deviceState === 'ONLINE') {
        this.state = {
          ...this.state,
          deviceState: 'OFFLINE',
        };
        this.notify();
      }
    }, this.TIMEOUT_MS);
  }

  /**
   * Classify temperature reading individually
   */
  public classifyTemperature(temp: number): TemperatureStatus {
    if (temp > 70) return 'CRITICAL';
    if (temp >= 50) return 'WARNING';
    return 'NORMAL';
  }

  /**
   * Classify vibration reading individually
   */
  public classifyVibration(vib: number): VibrationStatus {
    if (vib > 0.6) return 'CRITICAL';
    if (vib >= 0.3) return 'WARNING';
    return 'NORMAL';
  }

  /**
   * Classify belt speed reading individually
   */
  public classifyBeltSpeed(speed: number): BeltSpeedStatus {
    if (speed < 0.1) return 'STOPPED';
    if (speed < 0.8) return 'LOW';
    return 'RUNNING';
  }

  /**
   * Ingest and validate incoming payload from beltguard/sensors
   */
  public handleMessage(payload: unknown) {
    if (!payload || typeof payload !== 'object') {
      console.warn('[SensorService] Rejected non-object payload:', payload);
      return;
    }

    const data = payload as Partial<SensorTelemetry>;

    // Validate numeric values and guard against missing fields / NaN
    const hasTemp = typeof data.temperature === 'number' && !isNaN(data.temperature);
    const hasVib = typeof data.vibration === 'number' && !isNaN(data.vibration);
    const hasSpeed = typeof data.beltSpeed === 'number' && !isNaN(data.beltSpeed);

    if (!hasTemp || !hasVib || !hasSpeed) {
      console.warn('[SensorService] Missing or malformed sensor fields:', data);
      return;
    }

    const temperature = Number(data.temperature);
    const vibration = Number(data.vibration);
    const beltSpeed = Number(data.beltSpeed);

    // Format timestamp
    let timeLabel: string;
    try {
      if (data.timestamp) {
        const parsedDate = new Date(data.timestamp);
        if (!isNaN(parsedDate.getTime())) {
          timeLabel = parsedDate.toLocaleTimeString('en-US', {
            hour12: false,
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit',
          });
        } else {
          timeLabel = new Date().toLocaleTimeString('en-US', { hour12: false });
        }
      } else {
        timeLabel = new Date().toLocaleTimeString('en-US', { hour12: false });
      }
    } catch {
      timeLabel = new Date().toLocaleTimeString('en-US', { hour12: false });
    }

    // Update state
    this.state = {
      deviceState: 'ONLINE',
      temperature,
      vibration,
      beltSpeed,
      temperatureStatus: this.classifyTemperature(temperature),
      vibrationStatus: this.classifyVibration(vibration),
      beltSpeedStatus: this.classifyBeltSpeed(beltSpeed),
      lastUpdated: timeLabel,
    };

    // Update rolling history buffer (keep last MAX_HISTORY_POINTS samples)
    const newPoint: SensorPoint = {
      time: timeLabel,
      temperature,
      vibration,
      beltSpeed,
    };

    this.history = [...this.history.slice(-(this.MAX_HISTORY_POINTS - 1)), newPoint];

    // Arm 5-second watchdog
    this.armWatchdog();

    // Broadcast to UI
    this.notify();
  }

  /**
   * Notify service if broker connection dropped
   */
  public handleBrokerDisconnect() {
    if (this.state.deviceState === 'ONLINE') {
      this.state = {
        ...this.state,
        deviceState: 'OFFLINE',
      };
      this.notify();
    }
  }

  /**
   * Reset service (e.g. for testing)
   */
  public reset() {
    if (this.watchdogTimer) {
      clearTimeout(this.watchdogTimer);
      this.watchdogTimer = null;
    }
    this.state = {
      deviceState: 'WAITING',
      temperature: null,
      vibration: null,
      beltSpeed: null,
      temperatureStatus: null,
      vibrationStatus: null,
      beltSpeedStatus: null,
      lastUpdated: null,
    };
    this.history = [];
    this.notify();
  }
}

export const sensorService = SensorService.getInstance();
