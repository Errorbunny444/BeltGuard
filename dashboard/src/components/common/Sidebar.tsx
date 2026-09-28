import React from 'react';
import { Home, LineChart, FileText, Settings, Info } from 'lucide-react';
import { cn } from '@/utils/helpers';

interface SidebarProps {
  currentTab: string;
  onTabChange: (tab: string) => void;
}

export const Sidebar: React.FC<SidebarProps> = ({ currentTab, onTabChange }) => {
  const navItems = [
    { id: 'Home', label: 'Home', icon: Home },
    { id: 'Analytics', label: 'Analytics', icon: LineChart },
    { id: 'Events', label: 'Events', icon: FileText },
    { id: 'Settings', label: 'Settings', icon: Settings },
    { id: 'About', label: 'About', icon: Info },
  ];

  const handleNavClick = (id: string) => {
    onTabChange(id);
  };

  return (
    <aside className="w-28 bg-[#1a202c] border-r border-industrial-border flex flex-col justify-between py-4 select-none shrink-0 z-10">
      {/* Top Nav Buttons */}
      <nav className="space-y-1.5 px-2">
        {navItems.map((item) => {
          const Icon = item.icon;
          const isActive = currentTab === item.id;
          return (
            <button
              key={item.id}
              onClick={() => handleNavClick(item.id)}
              className={cn(
                'w-full flex items-center space-x-2.5 px-3 py-2.5 rounded-lg text-xs font-medium transition-all duration-150',
                isActive
                  ? 'bg-emerald-900/30 text-emerald-400 border border-emerald-800/40 shadow-sm'
                  : 'text-slate-400 hover:text-slate-200 hover:bg-industrial-surface/50'
              )}
            >
              <Icon className={cn('w-4 h-4 shrink-0', isActive ? 'text-emerald-400' : 'text-slate-400')} />
              <span>{item.label}</span>
            </button>
          );
        })}
      </nav>

      {/* Bottom Mountain Range Slogan Watermark */}
      <div className="px-2 pt-4 text-center">
        {/* Mountain Range Line Sketch */}
        <div className="w-full flex justify-center opacity-25 hover:opacity-40 transition-opacity mb-2">
          <svg className="w-20 h-10" viewBox="0 0 100 40" fill="none">
            <path
              d="M5 38L28 14L48 32L68 18L95 38"
              stroke="#94a3b8"
              strokeWidth="1.5"
              strokeLinejoin="round"
            />
            <path
              d="M20 38L38 24L58 38"
              stroke="#64748b"
              strokeWidth="1"
              strokeLinejoin="round"
            />
          </svg>
        </div>
        <p className="text-[9px] font-medium text-slate-500 leading-tight">
          Built for a<br />
          <span className="text-slate-400">Safer, Smarter</span><br />
          Tomorrow
        </p>
      </div>
    </aside>
  );
};
