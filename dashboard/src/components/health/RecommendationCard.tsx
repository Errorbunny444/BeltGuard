import React from 'react';
import { ShieldCheck, Wrench, AlertTriangle, AlertOctagon } from 'lucide-react';
import { BeltCondition } from '@/types/health';

interface RecommendationCardProps {
  condition: BeltCondition;
  recommendation: string;
}

export const RecommendationCard: React.FC<RecommendationCardProps> = ({
  condition,
  recommendation,
}) => {
  const isHealthy = condition === 'HEALTHY';
  const isCritical = condition === 'CRITICAL';
  const isHighRisk = condition === 'HIGH_RISK';

  let Icon = ShieldCheck;
  let iconColor = 'text-emerald-400 bg-emerald-500/10 border-emerald-500/20';
  let cardBorder = 'border-slate-800 bg-slate-900/60';

  if (isCritical) {
    Icon = AlertOctagon;
    iconColor = 'text-red-400 bg-red-500/10 border-red-500/30 animate-pulse';
    cardBorder = 'border-red-900/50 bg-red-950/20';
  } else if (isHighRisk) {
    Icon = AlertTriangle;
    iconColor = 'text-orange-400 bg-orange-500/10 border-orange-500/30';
    cardBorder = 'border-orange-900/40 bg-orange-950/20';
  } else if (!isHealthy) {
    Icon = Wrench;
    iconColor = 'text-amber-400 bg-amber-500/10 border-amber-500/30';
    cardBorder = 'border-amber-900/40 bg-amber-950/20';
  }

  return (
    <div className={`rounded-xl border p-3 flex items-start space-x-3 transition-all ${cardBorder}`}>
      <div className={`w-8 h-8 rounded-lg border flex items-center justify-center shrink-0 ${iconColor}`}>
        <Icon className="w-4 h-4" />
      </div>
      <div className="flex-1 min-w-0">
        <div className="text-[10px] font-bold uppercase tracking-wider text-slate-400">
          Recommended Operator Action
        </div>
        <div className="text-xs font-semibold text-slate-200 mt-0.5 leading-snug">
          {recommendation}
        </div>
      </div>
    </div>
  );
};
