import React from 'react';
import { LucideIcon } from 'lucide-react';
import { cn } from '@/utils/helpers';

interface SecondaryButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  icon?: LucideIcon;
  children: React.ReactNode;
}

export const SecondaryButton: React.FC<SecondaryButtonProps> = ({
  icon: Icon,
  children,
  className,
  ...props
}) => {
  return (
    <button
      className={cn(
        'inline-flex items-center justify-center space-x-2 px-3.5 py-2 rounded-lg text-xs font-medium text-slate-200 bg-industrial-subsurface hover:bg-slate-700/60 border border-industrial-border hover:border-slate-600/80 transition-all duration-150 active:scale-[0.98]',
        className
      )}
      {...props}
    >
      {Icon && <Icon className="w-3.5 h-3.5 text-slate-300" />}
      <span>{children}</span>
    </button>
  );
};
