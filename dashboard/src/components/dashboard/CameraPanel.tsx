import React from 'react';
import { Video } from 'lucide-react';
import { CameraPlaceholder } from '../placeholders/CameraPlaceholder';

interface CameraPanelProps {
  isStreaming: boolean;
  frameUrl?: string | null;
  deviceName?: string;
  resolution?: string;
}

export const CameraPanel: React.FC<CameraPanelProps> = ({
  isStreaming,
  frameUrl,
  deviceName = 'AUSHA webcam',
  resolution = '1902 × 1080',
}) => {
  return (
    <div className="bg-industrial-surface border border-industrial-border rounded-2xl p-4 flex flex-col h-full shadow-industrial">
      {/* Panel Header */}
      <div className="flex items-center justify-between mb-3 select-none">
        <div className="flex items-center space-x-2.5">
          <Video className="w-4 h-4 text-slate-300" />
          <h2 className="text-sm font-semibold text-slate-100 tracking-wide">
            Live Conveyor Feed
          </h2>
        </div>

        {/* Camera Status Pill */}
        {isStreaming ? (
          <div className="inline-flex items-center space-x-1.5 px-2.5 py-1 rounded-full bg-emerald-950/40 border border-emerald-800/40 text-[11px] font-medium text-emerald-300">
            <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse" />
            <span>Camera Live (1920×1080)</span>
          </div>
        ) : (
          <div className="inline-flex items-center space-x-1.5 px-2.5 py-1 rounded-full bg-red-950/40 border border-red-800/40 text-[11px] font-medium text-red-300">
            <span className="w-1.5 h-1.5 rounded-full bg-red-500 animate-pulse" />
            <span>Camera Offline</span>
          </div>
        )}
      </div>

      {/* Feed Canvas / Viewport */}
      <div className="flex-1 w-full relative min-h-[300px] overflow-hidden rounded-xl bg-black/90 flex items-center justify-center border border-slate-800/80 shadow-inner">
        {isStreaming && frameUrl ? (
          <img
            src={frameUrl}
            alt="Live Conveyor Feed"
            className="w-full h-full object-contain rounded-xl"
          />
        ) : (
          <CameraPlaceholder deviceName={deviceName} resolution={resolution} />
        )}
      </div>
    </div>
  );
};
