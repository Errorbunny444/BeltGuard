import React, { useState } from 'react';
import { FileText, ChevronDown } from 'lucide-react';
import { SystemEvent } from '@/types/events';
import { EmptyEvents } from '../placeholders/EmptyEvents';
import { getSeverityBadgeStyles } from '@/utils/formatter';

interface EventTableProps {
  events: SystemEvent[];
}

export const EventTable: React.FC<EventTableProps> = ({ events }) => {
  const [filter, setFilter] = useState('All Events');
  const [isDropdownOpen, setIsDropdownOpen] = useState(false);

  const filteredEvents =
    filter === 'All Events'
      ? events
      : events.filter((e) => e.severity.toLowerCase() === filter.toLowerCase());

  return (
    <div className="bg-industrial-surface border border-industrial-border rounded-2xl p-4 flex flex-col h-full shadow-industrial relative">
      {/* Header Bar */}
      <div className="flex items-center justify-between mb-3 select-none">
        <div className="flex items-center space-x-2">
          <FileText className="w-4 h-4 text-slate-300" />
          <h2 className="text-xs font-semibold text-slate-200 tracking-wide uppercase">
            Event Log
          </h2>
        </div>

        {/* Filter Dropdown */}
        <div className="relative">
          <button
            onClick={() => setIsDropdownOpen(!isDropdownOpen)}
            className="inline-flex items-center space-x-1.5 px-2.5 py-1 rounded-md bg-industrial-subsurface hover:bg-slate-800/80 border border-industrial-border text-xs text-slate-300 transition-colors"
          >
            <span>{filter}</span>
            <ChevronDown className="w-3.5 h-3.5 text-slate-400" />
          </button>

          {isDropdownOpen && (
            <div className="absolute right-0 mt-1 w-32 bg-industrial-subsurface border border-industrial-border rounded-lg shadow-industrial py-1 z-30">
              {['All Events', 'Critical', 'High', 'Medium', 'Low'].map((item) => (
                <button
                  key={item}
                  onClick={() => {
                    setFilter(item);
                    setIsDropdownOpen(false);
                  }}
                  className="w-full text-left px-3 py-1.5 text-xs text-slate-300 hover:bg-slate-700/60 transition-colors"
                >
                  {item}
                </button>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Table Structure */}
      <div className="flex-1 w-full flex flex-col min-h-[160px]">
        {/* Table Header Row */}
        <div className="grid grid-cols-12 gap-2 px-3 py-1.5 rounded-lg bg-industrial-subsurface/80 border border-industrial-border/60 text-[11px] font-medium text-slate-400 select-none">
          <div className="col-span-2">Time</div>
          <div className="col-span-3">Source</div>
          <div className="col-span-5">Event</div>
          <div className="col-span-2 text-right">Severity</div>
        </div>

        {/* Rows or Empty State */}
        <div className="flex-1 overflow-y-auto">
          {filteredEvents.length === 0 ? (
            <EmptyEvents />
          ) : (
            <div className="divide-y divide-industrial-border/40 mt-1">
              {filteredEvents.map((evt) => {
                const badge = getSeverityBadgeStyles(evt.severity);
                return (
                  <div
                    key={evt.id}
                    className="grid grid-cols-12 gap-2 px-3 py-2 text-xs text-slate-200 items-center hover:bg-industrial-subsurface/40 transition-colors"
                  >
                    <div className="col-span-2 font-mono text-[11px] text-slate-400">
                      {evt.time}
                    </div>
                    <div className="col-span-3 truncate text-slate-300">
                      {evt.source}
                    </div>
                    <div className="col-span-5 truncate text-slate-200">
                      {evt.event}
                    </div>
                    <div className="col-span-2 text-right">
                      <span
                        className={`inline-flex items-center space-x-1 px-2 py-0.5 rounded-full text-[10px] font-semibold border ${badge.bg} ${badge.text}`}
                      >
                        <span className={`w-1.5 h-1.5 rounded-full ${badge.dot}`} />
                        <span>{evt.severity}</span>
                      </span>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
