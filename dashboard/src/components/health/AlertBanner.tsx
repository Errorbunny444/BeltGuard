import React from 'react';
import { AlertTriangle, AlertOctagon } from 'lucide-react';
import { HealthAssessment } from '@/types/health';

interface AlertBannerProps {
  assessment: HealthAssessment;
}

export const AlertBanner: React.FC<AlertBannerProps> = ({ assessment }) => {
  const { condition, recommendation, alerts } = assessment;

  // Only show when condition is WARNING, HIGH_RISK, or CRITICAL
  if (condition === 'HEALTHY') {
    return null;
  }

  const isCritical = condition === 'CRITICAL';
  const isHighRisk = condition === 'HIGH_RISK';

  const containerBg = isCritical
    ? 'bg-red-950/80 border-red-500/50 text-red-200'
    : isHighRisk
    ? 'bg-orange-950/70 border-orange-500/50 text-orange-200'
    : 'bg-amber-950/60 border-amber-500/40 text-amber-200';

  const badgeBg = isCritical
    ? 'bg-red-500 text-white'
    : isHighRisk
    ? 'bg-orange-500 text-white'
    : 'bg-amber-500 text-slate-900';

  const IconComponent = isCritical ? AlertOctagon : AlertTriangle;

  const topAlertMessage = alerts.length > 0 ? alerts[0].message : recommendation;

  return (
    <div
      className={`w-full rounded-xl border px-4 py-2.5 flex items-center justify-between shadow-lg transition-all ${containerBg}`}
    >
      <div className="flex items-center space-x-3 min-w-0">
        <div className="shrink-0">
          <IconComponent className={`w-5 h-5 ${isCritical ? 'text-red-400 animate-pulse' : 'text-amber-400'}`} />
        </div>
        <div className="flex items-center space-x-2.5 min-w-0 flex-wrap">
          <span className={`px-2 py-0.5 rounded text-[10px] font-black uppercase tracking-wider ${badgeBg}`}>
            {condition.replace('_', ' ')}
          </span>
          <span className="text-xs font-medium truncate max-w-[550px]">
            {topAlertMessage}
          </span>
        </div>
      </div>

      <div className="hidden sm:flex items-center space-x-2 shrink-0 text-[11px] opacity-90 pl-3">
        <span className="font-semibold underline">Action Required</span>
      </div>
    </div>
  );
};
