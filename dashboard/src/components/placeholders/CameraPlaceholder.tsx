import React from 'react';
import { Video } from 'lucide-react';

interface CameraPlaceholderProps {
  deviceName?: string;
  resolution?: string;
}

export const CameraPlaceholder: React.FC<CameraPlaceholderProps> = ({
  deviceName = 'AUSHA webcam',
  resolution = '1902 × 1080',
}) => {
  return (
    <div className="relative w-full h-full min-h-[260px] rounded-xl overflow-hidden bg-[#181e29] border border-industrial-border flex items-center justify-center group">
      {/* Industrial Perspective Conveyor Backdrop SVG */}
      <svg
        className="absolute inset-0 w-full h-full opacity-20 object-cover pointer-events-none"
        preserveAspectRatio="xMidYMid slice"
        viewBox="0 0 800 450"
      >
        <defs>
          <linearGradient id="conveyorGrad" x1="0%" y1="0%" x2="0%" y2="100%">
            <stop offset="0%" stopColor="#475569" stopOpacity="0.8" />
            <stop offset="100%" stopColor="#1e293b" stopOpacity="0.2" />
          </linearGradient>
        </defs>
        {/* Conveyor Bed Perspective Rails */}
        <polygon points="260,180 540,180 720,450 80,450" fill="url(#conveyorGrad)" />
        {/* Rollers */}
        <line x1="280" y1="200" x2="520" y2="200" stroke="#64748b" strokeWidth="3" />
        <line x1="250" y1="230" x2="550" y2="230" stroke="#64748b" strokeWidth="4" />
        <line x1="210" y1="270" x2="590" y2="270" stroke="#64748b" strokeWidth="5" />
        <line x1="160" y1="320" x2="640" y2="320" stroke="#64748b" strokeWidth="6" />
        <line x1="100" y1="380" x2="700" y2="380" stroke="#64748b" strokeWidth="7" />
        {/* Frame trusses */}
        <line x1="260" y1="180" x2="80" y2="450" stroke="#94a3b8" strokeWidth="3" />
        <line x1="540" y1="180" x2="720" y2="450" stroke="#94a3b8" strokeWidth="3" />
      </svg>

      {/* Viewfinder Corner Reticles */}
      <div className="absolute top-3 left-3 w-4 h-4 border-t-2 border-l-2 border-slate-600/80 rounded-tl-sm pointer-events-none" />
      <div className="absolute top-3 right-3 w-4 h-4 border-t-2 border-r-2 border-slate-600/80 rounded-tr-sm pointer-events-none" />
      <div className="absolute bottom-3 left-3 w-4 h-4 border-b-2 border-l-2 border-slate-600/80 rounded-bl-sm pointer-events-none" />
      <div className="absolute bottom-3 right-3 w-4 h-4 border-b-2 border-r-2 border-slate-600/80 rounded-br-sm pointer-events-none" />

      {/* Center Camera Offline Prompt */}
      <div className="relative z-10 text-center px-4 max-w-sm">
        <div className="w-14 h-14 mx-auto mb-3 rounded-2xl bg-slate-800/60 border border-slate-700/60 flex items-center justify-center text-slate-400/90 shadow-inner">
          <Video className="w-7 h-7" />
        </div>

        <h3 className="text-sm font-semibold text-slate-200 tracking-wide mb-1">
          Waiting for Camera...
        </h3>

        <p className="text-xs text-slate-400 leading-relaxed">
          Connect the <span className="text-slate-300 font-medium">{deviceName}</span> ({resolution}) to start live monitoring.
        </p>
      </div>
    </div>
  );
};
