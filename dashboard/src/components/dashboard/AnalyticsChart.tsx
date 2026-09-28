import React from 'react';
import { Activity } from 'lucide-react';
import {
  ResponsiveContainer,
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
} from 'recharts';
import { useSensorTelemetry } from '@/mqtt/hooks/useSensorTelemetry';
import { SensorPoint } from '@/mqtt/types/mqtt';
import { EmptyGraph } from '../placeholders/EmptyGraph';

interface AnalyticsChartProps {
  telemetry?: SensorPoint[];
}

export const AnalyticsChart: React.FC<AnalyticsChartProps> = ({ telemetry: propTelemetry }) => {
  const { telemetryHistory, sensorState } = useSensorTelemetry();

  // Use provided prop or fallback to hook data
  const telemetry = propTelemetry !== undefined ? propTelemetry : telemetryHistory;
  const hasRealData = telemetry.length > 0;
  const isOnline = sensorState.deviceState === 'ONLINE';

  // Baseline empty timeline labels matching industrial oscilloscope layout
  const defaultTicks = ['00:00', '00:10', '00:20', '00:30', '00:40', '00:50', '01:00'];
  const dummyPlaceholderData = defaultTicks.map((tick) => ({
    time: tick,
    temperature: null,
    vibration: null,
    beltSpeed: null,
  }));

  const chartData = hasRealData ? telemetry : dummyPlaceholderData;

  return (
    <div className="bg-industrial-surface border border-industrial-border rounded-xl p-3.5 flex flex-col h-full shadow-industrial relative">
      {/* Header Bar */}
      <div className="flex items-center justify-between mb-2 select-none z-20">
        <div className="flex items-center space-x-2">
          <Activity className="w-4 h-4 text-cyan-400" />
          <h2 className="text-xs font-semibold text-slate-200 tracking-wide uppercase">
            Multi-Sensor Real-Time Telemetry
          </h2>
          {isOnline && hasRealData && (
            <span className="flex items-center space-x-1 px-1.5 py-0.5 rounded bg-emerald-500/10 border border-emerald-500/20 text-[10px] font-mono text-emerald-400">
              <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse" />
              <span>LIVE ({telemetry.length})</span>
            </span>
          )}
        </div>

        {/* Legend */}
        <div className="flex items-center space-x-3 text-[10px] font-medium">
          <div className="flex items-center space-x-1">
            <span className="w-2 h-2 rounded-full bg-orange-500" />
            <span className="text-slate-300">Temp (°C)</span>
          </div>
          <div className="flex items-center space-x-1">
            <span className="w-2 h-2 rounded-full bg-sky-400" />
            <span className="text-slate-300">Vibration (g)</span>
          </div>
          <div className="flex items-center space-x-1">
            <span className="w-2 h-2 rounded-full bg-emerald-400" />
            <span className="text-slate-300">Speed (m/s)</span>
          </div>
        </div>
      </div>

      {/* Plot Container */}
      <div className="flex-1 w-full min-h-[160px] relative">
        {!hasRealData && <EmptyGraph />}

        <ResponsiveContainer width="100%" height="100%">
          <LineChart data={chartData} margin={{ top: 8, right: 8, left: -20, bottom: 0 }}>
            <CartesianGrid strokeDasharray="0" stroke="#182338" vertical={false} />
            <XAxis
              dataKey="time"
              stroke="#64748b"
              fontSize={10}
              tickLine={false}
              axisLine={{ stroke: '#1e293b' }}
              dy={4}
            />
            {/* Left Y-Axis: Temperature (°C) */}
            <YAxis
              yAxisId="temp"
              domain={[0, 100]}
              ticks={[0, 25, 50, 75, 100]}
              stroke="#64748b"
              fontSize={10}
              tickLine={false}
              axisLine={{ stroke: '#1e293b' }}
              unit="°C"
            />
            {/* Right Y-Axis: Vibration (g) & Belt Speed (m/s) */}
            <YAxis
              yAxisId="motion"
              orientation="right"
              domain={[0, 3.0]}
              ticks={[0, 1.0, 2.0, 3.0]}
              stroke="#64748b"
              fontSize={10}
              tickLine={false}
              axisLine={{ stroke: '#1e293b' }}
            />
            {hasRealData && (
              <>
                <Tooltip
                  contentStyle={{
                    backgroundColor: '#0f172a',
                    borderColor: '#334155',
                    borderRadius: '8px',
                    fontSize: '11px',
                    color: '#f8fafc',
                    boxShadow: '0 4px 12px rgba(0,0,0,0.5)',
                  }}
                  formatter={(value: any, name: any) => {
                    if (typeof value !== 'number') return [value, name];
                    if (name.includes('Temperature')) return [`${value.toFixed(1)} °C`, name];
                    if (name.includes('Vibration')) return [`${value.toFixed(2)} g`, name];
                    if (name.includes('Belt Speed')) return [`${value.toFixed(2)} m/s`, name];
                    return [value, name];
                  }}
                />
                <Line
                  yAxisId="temp"
                  type="monotone"
                  dataKey="temperature"
                  stroke="#f97316"
                  strokeWidth={2}
                  dot={false}
                  name="Temperature (°C)"
                  isAnimationActive={false}
                />
                <Line
                  yAxisId="motion"
                  type="monotone"
                  dataKey="vibration"
                  stroke="#38bdf8"
                  strokeWidth={2}
                  dot={false}
                  name="Vibration (g)"
                  isAnimationActive={false}
                />
                <Line
                  yAxisId="motion"
                  type="monotone"
                  dataKey="beltSpeed"
                  stroke="#10b981"
                  strokeWidth={2}
                  dot={false}
                  name="Belt Speed (m/s)"
                  isAnimationActive={false}
                />
              </>
            )}
          </LineChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
};
