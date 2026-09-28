import React from 'react';
import { LucideIcon } from 'lucide-react';

interface SectionTitleProps {
  icon: LucideIcon;
  title: string;
  rightAccessory?: React.ReactNode;
}

export const SectionTitle: React.FC<SectionTitleProps> = ({
  icon: Icon,
  title,
  rightAccessory,
}) => {
  return (
    <div className="flex items-center justify-between mb-2 select-none">
      <div className="flex items-center space-x-2">
        <Icon className="w-4 h-4 text-slate-300" />
        <h2 className="text-xs font-semibold text-slate-200 tracking-wide uppercase">
          {title}
        </h2>
      </div>
      {rightAccessory && <div>{rightAccessory}</div>}
    </div>
  );
};
