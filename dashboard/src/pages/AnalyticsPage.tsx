import React from 'react';
import { Activity, Flame, Gauge, Zap, ShieldAlert, CheckCircle2 } from 'lucide-react';
import { SensorCards } from '@/components/dashboard/SensorCards';
import { AnalyticsChart } from '@/components/dashboard/AnalyticsChart';
import { useSensorTelemetry } from '@/mqtt/hooks/useSensorTelemetry';
import { useHealthEngine } from '@/analytics/useHealthEngine';
import { AlertBanner } from '@/components/health/AlertBanner';

export const AnalyticsPage: React.FC = () => {
  const { sensorState, telemetryHistory } = useSensorTelemetry();
  const assessment = useHealthEngine();

  // Compute live stats from history buffer
  const temps = telemetryHistory.map((p) => p.temperature).filter((t): t is number => t !== null);
  const vibs = telemetryHistory.map((p) => p.vibration).filter((v): v is number => v !== null);
  const speeds = telemetryHistory.map((p) => p.beltSpeed).filter((s): s is number => s !== null);

  const maxTemp = temps.length > 0 ? Math.max(...temps).toFixed(1) : '--';
  const maxVib = vibs.length > 0 ? Math.max(...vibs).toFixed(2) : '--';
  const avgSpeed = speeds.length > 0 ? (speeds.reduce((a, b) => a + b, 0) / speeds.length).toFixed(2) : '--';
  const sampleCount = telemetryHistory.length;

  return (
    <div className="flex-1 flex flex-col space-y-3 min-h-0 overflow-y-auto pr-1 pb-1">
      {/* Alert Banner */}
      <AlertBanner assessment={assessment} />

      {/* Top Header Bar */}
      <div className="flex items-center justify-between bg-industrial-surface border border-industrial-border rounded-xl px-4 py-2.5 shadow-industrial-sm">
        <div className="flex items-center space-x-2.5">
          <div className="w-8 h-8 rounded-lg bg-cyan-500/10 border border-cyan-500/20 flex items-center justify-center text-cyan-400">
            <Activity className="w-4 h-4" />
          </div>
          <div>
            <h1 className="text-sm font-bold text-white uppercase tracking-wider">
              Multi-Sensor Telemetry &amp; Analytics
            </h1>
            <p className="text-[11px] text-slate-400">
              Live ESP32 telemetry stream on topic <span className="font-mono text-cyan-400">beltguard/sensors</span>
            </p>
          </div>
        </div>

        <div className="flex items-center space-x-2">
          <span className="px-2.5 py-1 rounded-md text-[10px] font-mono bg-slate-800 border border-slate-700 text-slate-300">
            Buffer: {sampleCount}/60 samples
          </span>
          <span className="px-2.5 py-1 rounded-md text-[10px] font-mono bg-emerald-950/40 border border-emerald-800/40 text-emerald-400 flex items-center space-x-1.5">
            <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse" />
            <span>Rate: 1.0 Hz</span>
          </span>
        </div>
      </div>

      {/* Sensor Metric Cards */}
      <SensorCards sensorState={sensorState} />

      {/* 4 Quick Stat Metric Cards */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-2.5">
        <div className="bg-industrial-surface/80 border border-industrial-border rounded-xl p-3 shadow-industrial-sm flex items-center space-x-3">
          <div className="w-8 h-8 rounded-lg bg-orange-500/10 border border-orange-500/20 flex items-center justify-center text-orange-400 shrink-0">
            <Flame className="w-4 h-4" />
          </div>
          <div>
            <div className="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Peak Temp</div>
            <div className="text-lg font-extrabold text-white font-mono">{maxTemp} <span className="text-xs font-normal text-slate-400">°C</span></div>
          </div>
        </div>

        <div className="bg-industrial-surface/80 border border-industrial-border rounded-xl p-3 shadow-industrial-sm flex items-center space-x-3">
          <div className="w-8 h-8 rounded-lg bg-sky-500/10 border border-sky-500/20 flex items-center justify-center text-sky-400 shrink-0">
            <Activity className="w-4 h-4" />
          </div>
          <div>
            <div className="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Peak Vibration</div>
            <div className="text-lg font-extrabold text-white font-mono">{maxVib} <span className="text-xs font-normal text-slate-400">g</span></div>
          </div>
        </div>

        <div className="bg-industrial-surface/80 border border-industrial-border rounded-xl p-3 shadow-industrial-sm flex items-center space-x-3">
          <div className="w-8 h-8 rounded-lg bg-emerald-500/10 border border-emerald-500/20 flex items-center justify-center text-emerald-400 shrink-0">
            <Gauge className="w-4 h-4" />
          </div>
          <div>
            <div className="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Avg Speed</div>
            <div className="text-lg font-extrabold text-white font-mono">{avgSpeed} <span className="text-xs font-normal text-slate-400">m/s</span></div>
          </div>
        </div>

        <div className="bg-industrial-surface/80 border border-industrial-border rounded-xl p-3 shadow-industrial-sm flex items-center space-x-3">
          <div className="w-8 h-8 rounded-lg bg-purple-500/10 border border-purple-500/20 flex items-center justify-center text-purple-400 shrink-0">
            <Zap className="w-4 h-4" />
          </div>
          <div>
            <div className="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Health Index</div>
            <div className="text-lg font-extrabold text-white font-mono">{assessment.score} <span className="text-xs font-normal text-slate-400">/100</span></div>
          </div>
        </div>
      </div>

      {/* Main Expanded Multi-Line Oscilloscope Chart */}
      <div className="flex-1 min-h-[300px] flex flex-col">
        <AnalyticsChart telemetry={telemetryHistory} />
      </div>

      {/* Condition & Penalty Diagnostic Audit */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        {/* Left: Penalty Audit Table */}
        <div className="bg-industrial-surface border border-industrial-border rounded-xl p-3.5 shadow-industrial-sm flex flex-col justify-between">
          <div>
            <div className="flex items-center space-x-2 mb-2">
              <ShieldAlert className="w-4 h-4 text-amber-400" />
              <h3 className="text-xs font-bold text-white uppercase tracking-wider">
                Condition Assessment Breakdown
              </h3>
            </div>
            <div className="space-y-1.5 text-xs">
              <div className="flex justify-between py-1 border-b border-slate-800 text-slate-300">
                <span>Base Health Index</span>
                <span className="font-mono text-emerald-400 font-bold">100</span>
              </div>
              <div className="flex justify-between py-1 border-b border-slate-800 text-slate-300">
                <span>Vision Defect Deductions</span>
                <span className="font-mono text-red-400 font-bold">
                  {assessment.penalties.defectPenalty > 0 ? `-${assessment.penalties.defectPenalty}` : '0'}
                </span>
              </div>
              <div className="flex justify-between py-1 border-b border-slate-800 text-slate-300">
                <span>Sensor Anomaly Deductions</span>
                <span className="font-mono text-amber-400 font-bold">
                  {assessment.penalties.sensorPenalty > 0 ? `-${assessment.penalties.sensorPenalty}` : '0'}
                </span>
              </div>
              <div className="flex justify-between py-1 border-b border-slate-800 text-slate-300">
                <span>Data Freshness Deductions</span>
                <span className="font-mono text-slate-400 font-bold">
                  {assessment.penalties.freshnessPenalty > 0 ? `-${assessment.penalties.freshnessPenalty}` : '0'}
                </span>
              </div>
              <div className="flex justify-between pt-1.5 font-bold text-white">
                <span>Net Conveyor Health Index</span>
                <span className="font-mono text-cyan-400 text-sm">{assessment.score} / 100</span>
              </div>
            </div>
          </div>
        </div>

        {/* Right: Active Diagnostics & Recommended Action */}
        <div className="bg-industrial-surface border border-industrial-border rounded-xl p-3.5 shadow-industrial-sm flex flex-col justify-between">
          <div>
            <div className="flex items-center space-x-2 mb-2">
              <CheckCircle2 className="w-4 h-4 text-emerald-400" />
              <h3 className="text-xs font-bold text-white uppercase tracking-wider">
                Operator Maintenance Directive
              </h3>
            </div>
            <div className="p-3 rounded-lg bg-slate-900/80 border border-slate-800 text-xs font-semibold text-slate-200 mb-2 leading-relaxed">
              {assessment.recommendation}
            </div>
            <div className="space-y-1">
              <div className="text-[10px] font-bold uppercase tracking-wider text-slate-400 mb-1">
                Active Diagnostic Causal Factors:
              </div>
              {assessment.reasons.map((reason, idx) => (
                <div key={idx} className="flex items-start space-x-2 text-[11px] text-slate-300">
                  <span className="text-slate-500">•</span>
                  <span>{reason}</span>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
