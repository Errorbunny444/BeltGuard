import { useState, useEffect } from 'react';
import { sensorService } from '../services/sensorService';
import { SensorDataState, SensorPoint } from '../types/mqtt';

export function useSensorTelemetry() {
  const [sensorState, setSensorState] = useState<SensorDataState>(() =>
    sensorService.getState()
  );
  const [telemetryHistory, setTelemetryHistory] = useState<SensorPoint[]>(() =>
    sensorService.getHistory()
  );

  useEffect(() => {
    const unsubscribe = sensorService.subscribe((state, history) => {
      setSensorState(state);
      setTelemetryHistory(history);
    });

    return unsubscribe;
  }, []);

  return {
    sensorState,
    telemetryHistory,
  };
}
