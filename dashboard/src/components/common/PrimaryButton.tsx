import React from 'react';
import { LucideIcon } from 'lucide-react';
import { cn } from '@/utils/helpers';

interface PrimaryButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'green' | 'red';
  icon?: LucideIcon;
  children: React.ReactNode;
}

export const PrimaryButton: React.FC<PrimaryButtonProps> = ({
  variant = 'green',
  icon: Icon,
  children,
  className,
  ...props
}) => {
  const variantStyles = {
    green: 'bg-emerald-600 hover:bg-emerald-500 text-white border-emerald-500/30 shadow-sm active:scale-[0.98]',
    red: 'bg-red-800/90 hover:bg-red-700 text-white border-red-700/40 shadow-sm active:scale-[0.98]',
  };

  return (
    <button
      className={cn(
        'inline-flex items-center justify-center space-x-2 px-4 py-2 rounded-lg text-xs font-semibold border transition-all duration-150',
        variantStyles[variant],
        className
      )}
      {...props}
    >
      {Icon && <Icon className="w-3.5 h-3.5" />}
      <span>{children}</span>
    </button>
  );
};
