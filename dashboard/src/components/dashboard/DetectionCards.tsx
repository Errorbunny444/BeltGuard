import React from 'react';
import { Video, Cpu, Crosshair, AlertTriangle, BarChart2, Layers } from 'lucide-react';
import { StatusCard } from '../common/StatusCard';
import { SystemStatusState } from '@/types/status';
import { formatConfidence } from '@/utils/formatter';

interface DetectionCardsProps {
  status: SystemStatusState;
}

export const DetectionCards: React.FC<DetectionCardsProps> = ({ status }) => {
  return (
    <div className="grid grid-cols-3 gap-3">
      {/* 1. Camera Status */}
      <StatusCard
        icon={Video}
        iconBgColor="bg-red-500/10"
        iconColor="text-red-400"
        label="Camera Status"
        value={status.camera}
        valueColor="text-red-400"
        subtext="Waiting for connection"
      />

      {/* 2. Model Status */}
      <StatusCard
        icon={Cpu}
        iconBgColor="bg-amber-500/10"
        iconColor="text-amber-400"
        label="Model Status"
        value={status.model}
        valueColor="text-amber-400"
        subtext="Load YOLO model"
      />

      {/* 3. Detection Status */}
      <StatusCard
        icon={Crosshair}
        iconBgColor="bg-cyan-500/10"
        iconColor="text-cyan-400"
        label="Detection Status"
        value={status.detection}
        valueColor="text-cyan-300"
        subtext="Waiting for input"
      />

      {/* 4. Primary Defect */}
      <StatusCard
        icon={AlertTriangle}
        iconBgColor="bg-purple-500/10"
        iconColor="text-purple-400"
        label="Primary Defect"
        value={status.primaryDefect}
        valueColor="text-slate-200"
        subtext="No detection yet"
      />

      {/* 5. Confidence */}
      <StatusCard
        icon={BarChart2}
        iconBgColor="bg-blue-500/10"
        iconColor="text-blue-400"
        label="Confidence"
        value={formatConfidence(status.confidence)}
        valueColor="text-slate-200"
        subtext="No data available"
      />

      {/* 6. Detection Count */}
      <StatusCard
        icon={Layers}
        iconBgColor="bg-emerald-500/10"
        iconColor="text-emerald-400"
        label="Detection Count"
        value={status.detectionCount.toString()}
        valueColor="text-slate-200"
        subtext="Total this session"
      />
    </div>
  );
};
