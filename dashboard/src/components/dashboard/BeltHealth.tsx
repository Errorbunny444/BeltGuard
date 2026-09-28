import React from 'react';
import { BeltHealthState } from '@/types/dashboard';
import { useHealthEngine } from '@/analytics/useHealthEngine';
import { HealthScoreCard } from '../health/HealthScoreCard';
import { RecommendationCard } from '../health/RecommendationCard';
import { ReasonList } from '../health/ReasonList';

interface BeltHealthProps {
  health?: BeltHealthState;
}

export const BeltHealth: React.FC<BeltHealthProps> = () => {
  const assessment = useHealthEngine();

  // Determine progress track color
  let trackColor = 'bg-emerald-500';
  if (assessment.condition === 'CRITICAL') trackColor = 'bg-red-500';
  else if (assessment.condition === 'HIGH_RISK') trackColor = 'bg-orange-500';
  else if (assessment.condition === 'WARNING') trackColor = 'bg-amber-500';

  return (
    <div className="bg-industrial-surface border border-industrial-border rounded-xl p-4 shadow-industrial-sm flex flex-col gap-3 flex-1 transition-all">
      {/* 1. Conveyor Health Index Score & Gauge */}
      <HealthScoreCard assessment={assessment} />

      {/* Condition Progress Bar */}
      <div className="w-full h-1.5 rounded-full overflow-hidden bg-slate-800/80 border border-slate-700/40 relative">
        <div
          className={`h-full ${trackColor} rounded-full transition-all duration-700`}
          style={{ width: `${Math.max(0, Math.min(100, assessment.score))}%` }}
        />
      </div>

      {/* 2. Operator Recommendation */}
      <RecommendationCard
        condition={assessment.condition}
        recommendation={assessment.recommendation}
      />

      {/* 3. Top Diagnostics & Reasons */}
      <ReasonList
        condition={assessment.condition}
        reasons={assessment.reasons}
      />
    </div>
  );
};
