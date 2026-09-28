import React from 'react';
import { LucideIcon } from 'lucide-react';
import { cn } from '@/utils/helpers';

interface StatusCardProps {
  icon: LucideIcon;
  iconBgColor: string;
  iconColor: string;
  label: string;
  value: string;
  valueColor?: string;
  subtext: string;
  className?: string;
}

export const StatusCard: React.FC<StatusCardProps> = ({
  icon: Icon,
  iconBgColor,
  iconColor,
  label,
  value,
  valueColor = 'text-white',
  subtext,
  className,
}) => {
  return (
    <div
      className={cn(
        'bg-industrial-surface/90 border border-industrial-border/80 rounded-xl p-3.5 flex items-center space-x-3.5 shadow-industrial-sm hover:border-slate-700/80 transition-colors',
        className
      )}
    >
      {/* Icon Capsule */}
      <div
        className={cn(
          'w-11 h-11 rounded-lg flex items-center justify-center shrink-0 border border-white/5',
          iconBgColor
        )}
      >
        <Icon className={cn('w-5 h-5', iconColor)} />
      </div>

      {/* Metric Content */}
      <div className="min-w-0 flex-1">
        <div className="text-[11px] font-medium text-slate-400 leading-tight truncate">
          {label}
        </div>
        <div className={cn('text-sm font-bold tracking-tight my-0.5 truncate', valueColor)}>
          {value}
        </div>
        <div className="text-[10px] text-slate-400/90 leading-tight truncate">
          {subtext}
        </div>
      </div>
    </div>
  );
};
