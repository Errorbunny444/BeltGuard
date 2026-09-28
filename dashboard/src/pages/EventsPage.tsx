import React, { useState, useMemo } from 'react';
import { FileText, Search, Filter, Download, Trash2, ShieldAlert } from 'lucide-react';
import { SystemEvent } from '@/types/events';
import { getSeverityBadgeStyles } from '@/utils/formatter';
import { dashboardService } from '@/services/dashboard.service';

interface EventsPageProps {
  events: SystemEvent[];
}

export const EventsPage: React.FC<EventsPageProps> = ({ events }) => {
  const [searchQuery, setSearchQuery] = useState('');
  const [severityFilter, setSeverityFilter] = useState('All');
  const [sourceFilter, setSourceFilter] = useState('All');

  // Filtered list
  const filteredEvents = useMemo(() => {
    return events.filter((evt) => {
      const matchesSearch =
        evt.event.toLowerCase().includes(searchQuery.toLowerCase()) ||
        evt.source.toLowerCase().includes(searchQuery.toLowerCase());

      const matchesSeverity =
        severityFilter === 'All' || evt.severity.toLowerCase() === severityFilter.toLowerCase();

      const matchesSource =
        sourceFilter === 'All' || evt.source.toLowerCase().includes(sourceFilter.toLowerCase());

      return matchesSearch && matchesSeverity && matchesSource;
    });
  }, [events, searchQuery, severityFilter, sourceFilter]);

  // Statistics
  const criticalCount = events.filter((e) => e.severity === 'Critical').length;
  const highCount = events.filter((e) => e.severity === 'High').length;
  const mediumCount = events.filter((e) => e.severity === 'Medium').length;

  const handleExportCSV = () => {
    if (events.length === 0) return;
    const headers = 'ID,Time,Source,Event,Severity,Timestamp\n';
    const rows = events
      .map(
        (e) =>
          `"${e.id}","${e.time}","${e.source}","${e.event.replace(/"/g, '""')}","${e.severity}","${e.timestamp}"`
      )
      .join('\n');
    const blob = new Blob([headers + rows], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `beltguard_events_${new Date().toISOString().slice(0, 10)}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  const handleClearLog = () => {
    if (window.confirm('Are you sure you want to clear the active event log session?')) {
      // Clear via empty events array
      (dashboardService as any).state = {
        ...(dashboardService as any).state,
        events: [],
      };
      (dashboardService as any).notify();
    }
  };

  return (
    <div className="flex-1 flex flex-col space-y-3 min-h-0 overflow-hidden pr-1 pb-1">
      {/* Top Header & Statistics */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-industrial-surface border border-industrial-border rounded-xl p-3.5 shadow-industrial-sm shrink-0">
        <div className="flex items-center space-x-2.5">
          <div className="w-8 h-8 rounded-lg bg-purple-500/10 border border-purple-500/20 flex items-center justify-center text-purple-400">
            <FileText className="w-4 h-4" />
          </div>
          <div>
            <h1 className="text-sm font-bold text-white uppercase tracking-wider">
              Industrial Event Audit Trail
            </h1>
            <p className="text-[11px] text-slate-400">
              Persistent anomaly alerts, defect detections, and system alarms
            </p>
          </div>
        </div>

        {/* Statistic Badges */}
        <div className="flex items-center space-x-2 flex-wrap gap-y-1">
          <span className="px-2.5 py-1 rounded-md text-[10px] font-mono bg-slate-800 border border-slate-700 text-slate-300">
            Total: {events.length}
          </span>
          <span className="px-2.5 py-1 rounded-md text-[10px] font-mono bg-red-950/50 border border-red-800/40 text-red-400">
            Critical: {criticalCount}
          </span>
          <span className="px-2.5 py-1 rounded-md text-[10px] font-mono bg-orange-950/50 border border-orange-800/40 text-orange-400">
            High: {highCount}
          </span>
          <span className="px-2.5 py-1 rounded-md text-[10px] font-mono bg-amber-950/50 border border-amber-800/40 text-amber-400">
            Warnings: {mediumCount}
          </span>
        </div>
      </div>

      {/* Filter & Action Toolbar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2.5 bg-industrial-surface/90 border border-industrial-border rounded-xl p-3 shrink-0">
        <div className="flex items-center space-x-2 flex-1 max-w-md">
          <div className="relative flex-1">
            <Search className="w-3.5 h-3.5 text-slate-400 absolute left-2.5 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Search by defect, keyword, or source..."
              className="w-full pl-8 pr-3 py-1.5 rounded-lg bg-industrial-subsurface border border-industrial-border text-xs text-slate-200 placeholder-slate-500 focus:outline-none focus:border-cyan-500/50"
            />
          </div>
        </div>

        <div className="flex items-center space-x-2 flex-wrap">
          {/* Severity Filter */}
          <div className="flex items-center space-x-1.5">
            <Filter className="w-3.5 h-3.5 text-slate-400" />
            <select
              value={severityFilter}
              onChange={(e) => setSeverityFilter(e.target.value)}
              className="px-2.5 py-1.5 rounded-lg bg-industrial-subsurface border border-industrial-border text-xs text-slate-300 focus:outline-none cursor-pointer"
            >
              <option value="All">All Severities</option>
              <option value="Critical">Critical</option>
              <option value="High">High</option>
              <option value="Medium">Medium</option>
              <option value="Low">Low</option>
            </select>
          </div>

          {/* Source Filter */}
          <select
            value={sourceFilter}
            onChange={(e) => setSourceFilter(e.target.value)}
            className="px-2.5 py-1.5 rounded-lg bg-industrial-subsurface border border-industrial-border text-xs text-slate-300 focus:outline-none cursor-pointer"
          >
            <option value="All">All Sources</option>
            <option value="HEALTH ENGINE">Health Engine</option>
            <option value="VISION">YOLO Vision</option>
          </select>

          {/* Actions */}
          <button
            onClick={handleExportCSV}
            title="Export CSV"
            className="inline-flex items-center space-x-1 px-2.5 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-xs text-slate-300 border border-slate-700 transition-colors"
          >
            <Download className="w-3.5 h-3.5" />
            <span>CSV</span>
          </button>

          <button
            onClick={handleClearLog}
            title="Clear Event Log"
            className="inline-flex items-center space-x-1 px-2.5 py-1.5 rounded-lg bg-red-950/40 hover:bg-red-900/60 text-xs text-red-300 border border-red-800/50 transition-colors"
          >
            <Trash2 className="w-3.5 h-3.5" />
            <span>Clear</span>
          </button>
        </div>
      </div>

      {/* Full-Height Event Table Container */}
      <div className="flex-1 min-h-0 bg-industrial-surface border border-industrial-border rounded-xl shadow-industrial overflow-hidden flex flex-col">
        <div className="overflow-y-auto flex-1 custom-scrollbar">
          <table className="w-full text-left border-collapse">
            <thead className="sticky top-0 bg-industrial-subsurface border-b border-industrial-border z-10 select-none">
              <tr>
                <th className="py-2.5 px-4 text-[11px] font-bold text-slate-400 uppercase tracking-wider w-24">
                  Time
                </th>
                <th className="py-2.5 px-4 text-[11px] font-bold text-slate-400 uppercase tracking-wider w-36">
                  Source
                </th>
                <th className="py-2.5 px-4 text-[11px] font-bold text-slate-400 uppercase tracking-wider">
                  Event Description
                </th>
                <th className="py-2.5 px-4 text-[11px] font-bold text-slate-400 uppercase tracking-wider text-right w-28">
                  Severity
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-industrial-border/40 font-mono text-xs">
              {filteredEvents.length === 0 ? (
                <tr>
                  <td colSpan={4} className="text-center py-16 text-slate-500">
                    <ShieldAlert className="w-8 h-8 mx-auto mb-2 opacity-40 text-slate-400" />
                    No logged events match the active search or filter.
                  </td>
                </tr>
              ) : (
                filteredEvents.map((evt) => {
                  const severityBadge = getSeverityBadgeStyles(evt.severity);
                  return (
                    <tr key={evt.id} className="hover:bg-industrial-subsurface/40 transition-colors">
                      <td className="py-2.5 px-4 text-slate-400 whitespace-nowrap">{evt.time}</td>
                      <td className="py-2.5 px-4 whitespace-nowrap">
                        <span className="px-2 py-0.5 rounded text-[10px] font-semibold font-sans bg-slate-800 border border-slate-700 text-slate-300">
                          {evt.source}
                        </span>
                      </td>
                      <td className="py-2.5 px-4 text-slate-200 font-sans font-medium">
                        {evt.event}
                      </td>
                      <td className="py-2.5 px-4 text-right whitespace-nowrap">
                        <span
                          className={`inline-block px-2 py-0.5 rounded-full text-[10px] font-bold font-sans tracking-wide uppercase border ${severityBadge.bg} ${severityBadge.text}`}
                        >
                          {evt.severity}
                        </span>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
