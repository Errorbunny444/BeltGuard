import React from 'react';
import { FOOTER_LEFT } from '@/utils/constants';

export const Footer: React.FC = () => {
  return (
    <footer className="h-7 px-5 border-t border-industrial-border bg-[#151a24] flex items-center justify-between text-[11px] text-slate-400 select-none shrink-0">
      <div className="flex items-center space-x-2">
        <span>{FOOTER_LEFT}</span>
      </div>
    </footer>
  );
};
