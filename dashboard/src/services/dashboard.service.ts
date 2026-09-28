import { DashboardState, TelemetryPoint, BeltHealthState } from '@/types/dashboard';
import { SystemStatusState } from '@/types/status';
import { SystemEvent } from '@/types/events';
import { INITIAL_DASHBOARD_STATE } from '@/utils/constants';

type Listener = (state: DashboardState) => void;

class DashboardService {
  private state: DashboardState = { ...INITIAL_DASHBOARD_STATE };
  private listeners: Set<Listener> = new Set();

  /**
   * Subscribe a component to state updates
   */
  public subscribe(listener: Listener): () => void {
    this.listeners.add(listener);
    listener(this.state);
    return () => {
      this.listeners.delete(listener);
    };
  }

  public getState(): DashboardState {
    return this.state;
  }

  private notify() {
    this.listeners.forEach((listener) => listener({ ...this.state }));
  }

  // =========================================================================
  // Future Scope Integration APIs
  // =========================================================================

  /**
   * Placeholder to update live camera frame from OpenCV / WebRTC stream
   */
  public updateCameraFrame(frameUrlOrBlob: string | null) {
    this.state = {
      ...this.state,
      camera: {
        ...this.state.camera,
        isStreaming: !!frameUrlOrBlob,
        frameUrl: frameUrlOrBlob,
      },
    };
    this.notify();
  }

  /**
   * Placeholder to update YOLO inference detections
   */
  public updateDetection(data: Partial<SystemStatusState>) {
    this.state = {
      ...this.state,
      status: {
        ...this.state.status,
        ...data,
      },
    };
    this.notify();
  }

  /**
   * Placeholder to append sensor telemetry (Vibration, Temperature)
   */
  public updateSensorData(telemetryPoint: TelemetryPoint) {
    const updatedTelemetry = [...this.state.telemetry.slice(-59), telemetryPoint];
    this.state = {
      ...this.state,
      telemetry: updatedTelemetry,
    };
    this.notify();
  }

  /**
   * Placeholder to update calculated Belt Health
   */
  public updateHealth(health: Partial<BeltHealthState>) {
    this.state = {
      ...this.state,
      health: {
        ...this.state.health,
        ...health,
      },
    };
    this.notify();
  }

  /**
   * Placeholder to append a new event to the event log
   */
  public appendEvent(eventData: Omit<SystemEvent, 'id' | 'timestamp'>) {
    const newEvent: SystemEvent = {
      ...eventData,
      id: `evt-${Date.now()}-${Math.random().toString(36).substr(2, 4)}`,
      timestamp: new Date().toISOString(),
    };
    this.state = {
      ...this.state,
      events: [newEvent, ...this.state.events].slice(0, 100),
    };
    this.notify();
  }
}

export const dashboardService = new DashboardService();
