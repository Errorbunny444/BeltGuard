import React from 'react';
import { Activity } from 'lucide-react';
import { HealthAssessment } from '@/types/health';

interface HealthScoreCardProps {
  assessment: HealthAssessment;
}

export const HealthScoreCard: React.FC<HealthScoreCardProps> = ({ assessment }) => {
  const { score, condition, penalties } = assessment;

  // Condition color mapping
  let ringColor = '#10b981'; // Emerald
  let conditionBadgeClass = 'bg-emerald-500/10 text-emerald-400 border-emerald-500/30';

  if (condition === 'CRITICAL') {
    ringColor = '#ef4444';
    conditionBadgeClass = 'bg-red-500/15 text-red-400 border-red-500/40 animate-pulse';
  } else if (condition === 'HIGH_RISK') {
    ringColor = '#f97316';
    conditionBadgeClass = 'bg-orange-500/15 text-orange-400 border-orange-500/40';
  } else if (condition === 'WARNING') {
    ringColor = '#f59e0b';
    conditionBadgeClass = 'bg-amber-500/15 text-amber-400 border-amber-500/40';
  }

  // Circular gauge math (radius = 27, circumference = ~169.65)
  const radius = 27;
  const circumference = 2 * Math.PI * radius;
  const offset = circumference - (circumference * Math.max(0, Math.min(100, score))) / 100;

  return (
    <div className="flex items-center justify-between">
      {/* Left: Metric Title & Status */}
      <div className="flex items-center space-x-3.5">
        <div className="w-11 h-11 rounded-xl bg-slate-800/80 border border-slate-700/60 flex items-center justify-center text-slate-300 shadow-inner">
          <Activity className="w-6 h-6" />
        </div>
        <div>
          <div className="flex items-center space-x-2">
            <h3 className="text-sm font-semibold text-white tracking-wide">
              Conveyor Health Index
            </h3>
            <span
              className={`px-2 py-0.5 rounded-full text-[10px] font-bold border tracking-wider uppercase ${conditionBadgeClass}`}
            >
              {condition.replace('_', ' ')}
            </span>
          </div>
          <p className="text-[11px] text-slate-400 mt-0.5">
            Real-time multi-source condition assessment
          </p>
        </div>
      </div>

      {/* Right: Circular Gauge */}
      <div className="flex items-center space-x-4">
        {penalties.totalPenalty > 0 && (
          <div className="hidden sm:flex flex-col text-right text-[10px] text-slate-400 font-mono">
            {penalties.defectPenalty > 0 && <span>Defects: -{penalties.defectPenalty}</span>}
            {penalties.sensorPenalty > 0 && <span>Sensors: -{penalties.sensorPenalty}</span>}
            {penalties.freshnessPenalty > 0 && <span>Freshness: -{penalties.freshnessPenalty}</span>}
          </div>
        )}

        <div className="relative w-16 h-16 flex items-center justify-center shrink-0">
          <svg className="w-full h-full -rotate-90" viewBox="0 0 64 64">
            {/* Background Track Ring */}
            <circle
              cx="32"
              cy="32"
              r={radius}
              stroke="#1e293b"
              strokeWidth="5"
              fill="none"
            />
            {/* Progress Arc */}
            <circle
              cx="32"
              cy="32"
              r={radius}
              stroke={ringColor}
              strokeWidth="5"
              fill="none"
              strokeDasharray={circumference}
              strokeDashoffset={offset}
              strokeLinecap="round"
              className="transition-all duration-700"
            />
          </svg>

          {/* Centered Ring Text */}
          <div className="absolute inset-0 flex flex-col items-center justify-center text-center select-none">
            <span className="text-base font-extrabold text-white leading-none font-mono">
              {score}
            </span>
            <span className="text-[8px] text-slate-400 font-semibold leading-tight mt-0.5">
              /100
            </span>
          </div>
        </div>
      </div>
    </div>
  );
};
