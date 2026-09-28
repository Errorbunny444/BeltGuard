import React from 'react';
import { Info } from 'lucide-react';
import { BeltCondition } from '@/types/health';

interface ReasonListProps {
  condition: BeltCondition;
  reasons: string[];
}

export const ReasonList: React.FC<ReasonListProps> = ({ reasons }) => {

  return (
    <div className="pt-2 border-t border-slate-800/80">
      <div className="flex items-center space-x-1.5 mb-1.5">
        <Info className="w-3.5 h-3.5 text-slate-400" />
        <span className="text-[10px] font-bold text-slate-400 uppercase tracking-wider">
          Condition Drivers &amp; Diagnostics
        </span>
      </div>

      <div className="space-y-1">
        {reasons.length === 0 ? (
          <div className="text-[11px] text-slate-400 italic">
            Operating parameters within standard tolerances.
          </div>
        ) : (
          reasons.map((reason, idx) => (
            <div key={idx} className="flex items-start space-x-2 text-[11px] text-slate-300">
              <span className="text-slate-500 font-bold leading-tight select-none">•</span>
              <span className="leading-tight">{reason}</span>
            </div>
          ))
        )}
      </div>
    </div>
  );
};
