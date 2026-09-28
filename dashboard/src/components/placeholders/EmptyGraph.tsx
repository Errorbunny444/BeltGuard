import React from 'react';
import { TrendingUp } from 'lucide-react';

export const EmptyGraph: React.FC = () => {
  return (
    <div className="absolute inset-0 flex flex-col items-center justify-center text-center p-4 select-none pointer-events-none z-10">
      <div className="w-10 h-10 rounded-xl bg-slate-800/60 border border-slate-700/60 flex items-center justify-center text-amber-400/90 mb-2 shadow-inner">
        <TrendingUp className="w-5 h-5 animate-pulse" />
      </div>
      <div className="text-xs font-semibold text-slate-200 tracking-wide">
        Waiting for Telemetry
      </div>
      <div className="text-[11px] text-slate-400 mt-0.5">
        Awaiting ESP32 sensor messages on <span className="font-mono text-cyan-400">beltguard/sensors</span>
      </div>
    </div>
  );
};
