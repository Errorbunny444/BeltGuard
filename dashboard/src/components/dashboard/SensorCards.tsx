import React from 'react';
import { Thermometer, Activity, Gauge } from 'lucide-react';
import { useSensorTelemetry } from '@/mqtt/hooks/useSensorTelemetry';
import { SensorDataState, TemperatureStatus, VibrationStatus, BeltSpeedStatus } from '@/mqtt/types/mqtt';

interface SensorCardsProps {
  sensorState?: SensorDataState;
}

export const SensorCards: React.FC<SensorCardsProps> = ({ sensorState: propSensorState }) => {
  const { sensorState: hookSensorState } = useSensorTelemetry();
  const sensorState = propSensorState || hookSensorState;

  const isOnline = sensorState.deviceState === 'ONLINE';
  const isWaiting = sensorState.deviceState === 'WAITING';
  const isOffline = sensorState.deviceState === 'OFFLINE';

  const getStatusBadge = (
    status: TemperatureStatus | VibrationStatus | BeltSpeedStatus | null,
    fallbackText?: string
  ) => {
    if (isWaiting) {
      return (
        <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-semibold bg-slate-800 text-amber-300/90 border border-amber-500/20 tracking-wide">
          <span className="w-1.5 h-1.5 rounded-full bg-amber-400 animate-pulse mr-1.5" />
          Waiting for ESP32...
        </span>
      );
    }

    if (isOffline) {
      return (
        <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-semibold bg-red-950/40 text-red-400 border border-red-500/30 tracking-wide">
          <span className="w-1.5 h-1.5 rounded-full bg-red-500 mr-1.5" />
          Sensor Offline
        </span>
      );
    }

    const currentStatus = status || fallbackText || 'NORMAL';
    if (currentStatus === 'CRITICAL' || currentStatus === 'STOPPED') {
      return (
        <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-bold bg-red-500/15 text-red-400 border border-red-500/40 tracking-wide animate-pulse">
          <span className="w-1.5 h-1.5 rounded-full bg-red-500 mr-1.5" />
          {currentStatus}
        </span>
      );
    }

    if (currentStatus === 'WARNING' || currentStatus === 'LOW') {
      return (
        <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-bold bg-amber-500/15 text-amber-400 border border-amber-500/40 tracking-wide">
          <span className="w-1.5 h-1.5 rounded-full bg-amber-400 mr-1.5" />
          {currentStatus}
        </span>
      );
    }

    return (
      <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-bold bg-emerald-500/15 text-emerald-400 border border-emerald-500/30 tracking-wide">
        <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 mr-1.5" />
        {currentStatus === 'RUNNING' ? 'RUNNING' : 'NORMAL'}
      </span>
    );
  };

  return (
    <div className="grid grid-cols-1 sm:grid-cols-3 gap-2.5">
      {/* 1. Temperature Card */}
      <div className="bg-industrial-surface border border-industrial-border rounded-xl p-3 shadow-industrial-sm flex flex-col justify-between relative overflow-hidden transition-all">
        <div className="flex items-center justify-between mb-1.5">
          <div className="flex items-center space-x-2">
            <div className="w-7 h-7 rounded-lg bg-orange-500/10 border border-orange-500/20 flex items-center justify-center text-orange-400">
              <Thermometer className="w-4 h-4" />
            </div>
            <span className="text-[11px] font-bold text-slate-300 tracking-wider uppercase">
              Temperature
            </span>
          </div>
          {getStatusBadge(sensorState.temperatureStatus)}
        </div>

        <div className="flex items-baseline space-x-1.5 mt-1">
          <span className="text-2xl font-extrabold text-white font-mono tracking-tight">
            {isOnline && sensorState.temperature !== null
              ? sensorState.temperature.toFixed(1)
              : '--'}
          </span>
          <span className="text-xs font-semibold text-slate-400">°C</span>
        </div>

        <div className="mt-1 text-[10px] text-slate-500 flex justify-between items-center">
          <span>Target: &lt; 50.0°C</span>
          {isOnline && sensorState.lastUpdated && (
            <span className="text-slate-400 font-mono">{sensorState.lastUpdated}</span>
          )}
        </div>
      </div>

      {/* 2. Vibration Card */}
      <div className="bg-industrial-surface border border-industrial-border rounded-xl p-3 shadow-industrial-sm flex flex-col justify-between relative overflow-hidden transition-all">
        <div className="flex items-center justify-between mb-1.5">
          <div className="flex items-center space-x-2">
            <div className="w-7 h-7 rounded-lg bg-sky-500/10 border border-sky-500/20 flex items-center justify-center text-sky-400">
              <Activity className="w-4 h-4" />
            </div>
            <span className="text-[11px] font-bold text-slate-300 tracking-wider uppercase">
              Vibration
            </span>
          </div>
          {getStatusBadge(sensorState.vibrationStatus)}
        </div>

        <div className="flex items-baseline space-x-1.5 mt-1">
          <span className="text-2xl font-extrabold text-white font-mono tracking-tight">
            {isOnline && sensorState.vibration !== null
              ? sensorState.vibration.toFixed(2)
              : '--'}
          </span>
          <span className="text-xs font-semibold text-slate-400">g</span>
        </div>

        <div className="mt-1 text-[10px] text-slate-500 flex justify-between items-center">
          <span>Target: &lt; 0.30 g</span>
          {isOnline && sensorState.lastUpdated && (
            <span className="text-slate-400 font-mono">{sensorState.lastUpdated}</span>
          )}
        </div>
      </div>

      {/* 3. Belt Speed Card */}
      <div className="bg-industrial-surface border border-industrial-border rounded-xl p-3 shadow-industrial-sm flex flex-col justify-between relative overflow-hidden transition-all">
        <div className="flex items-center justify-between mb-1.5">
          <div className="flex items-center space-x-2">
            <div className="w-7 h-7 rounded-lg bg-emerald-500/10 border border-emerald-500/20 flex items-center justify-center text-emerald-400">
              <Gauge className="w-4 h-4" />
            </div>
            <span className="text-[11px] font-bold text-slate-300 tracking-wider uppercase">
              Belt Speed
            </span>
          </div>
          {getStatusBadge(sensorState.beltSpeedStatus, 'RUNNING')}
        </div>

        <div className="flex items-baseline space-x-1.5 mt-1">
          <span className="text-2xl font-extrabold text-white font-mono tracking-tight">
            {isOnline && sensorState.beltSpeed !== null
              ? sensorState.beltSpeed.toFixed(2)
              : '--'}
          </span>
          <span className="text-xs font-semibold text-slate-400">m/s</span>
        </div>

        <div className="mt-1 text-[10px] text-slate-500 flex justify-between items-center">
          <span>Nominal: &gt; 0.80 m/s</span>
          {isOnline && sensorState.lastUpdated && (
            <span className="text-slate-400 font-mono">{sensorState.lastUpdated}</span>
          )}
        </div>
      </div>
    </div>
  );
};
