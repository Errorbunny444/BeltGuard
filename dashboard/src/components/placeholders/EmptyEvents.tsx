import React from 'react';
import { FileText } from 'lucide-react';

export const EmptyEvents: React.FC = () => {
  return (
    <div className="h-44 flex flex-col items-center justify-center text-center p-4 select-none">
      <div className="w-10 h-10 rounded-xl bg-slate-800/50 border border-slate-700/50 flex items-center justify-center text-slate-400 mb-2">
        <FileText className="w-5 h-5" />
      </div>
      <div className="text-xs font-semibold text-slate-300">
        No events available
      </div>
      <div className="text-[11px] text-slate-500 mt-0.5">
        Detection events, sensor alerts and system logs will appear here.
      </div>
    </div>
  );
};
