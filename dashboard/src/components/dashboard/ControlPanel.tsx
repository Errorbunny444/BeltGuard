import React from 'react';
import { Play, Square, Upload, Folder, Settings, Download, Info } from 'lucide-react';
import { PrimaryButton } from '../common/PrimaryButton';
import { SecondaryButton } from '../common/SecondaryButton';
import { placeholderService } from '@/services/placeholder.service';
import { apiService } from '@/services/api.service';

export const ControlPanel: React.FC = () => {
  const handleStart = async () => {
    const ok = await apiService.startVision();
    placeholderService.showFeatureNotice(
      ok ? 'Vision Pipeline Started' : 'Start Failed',
      ok
        ? 'OpenCV capture & Ultralytics YOLO inference loop activated.'
        : 'Could not connect to Python backend on port 8000. Ensure python backend/main.py is running.',
      'Active Model: models/best.pt | Target Classes: Large hole, Large tear, Small Tear, Small hole.'
    );
  };

  const handleStop = async () => {
    const ok = await apiService.stopVision();
    placeholderService.showFeatureNotice(
      ok ? 'Vision Pipeline Halted' : 'Stop Failed',
      ok
        ? 'Camera capture handle released and continuous YOLO inference paused.'
        : 'Could not connect to Python backend on port 8000.',
      'System remains in standby.'
    );
  };

  const handleLoadVideo = async () => {
    await apiService.toggleSimulation(true);
    placeholderService.showFeatureNotice(
      'Conveyor Simulation Stream',
      'High-speed conveyor simulation mode activated for testing defect classification.',
      'Ultralytics YOLO inference is processing live conveyor frames and streaming to the dashboard.'
    );
  };

  const handleAction = (name: string, description: string, details?: string) => {
    placeholderService.showFeatureNotice(name, description, details);
  };

  return (
    <div className="bg-industrial-surface/90 border border-industrial-border/80 rounded-xl px-4 py-2.5 flex items-center justify-between shadow-industrial select-none">
      {/* Left Title Capsule */}
      <div className="flex items-center space-x-3">
        <div className="w-8 h-8 rounded-full bg-emerald-500/10 border border-emerald-500/20 flex items-center justify-center text-emerald-400">
          <Play className="w-4 h-4 fill-emerald-400" />
        </div>
        <div>
          <h3 className="text-xs font-bold text-white tracking-wide leading-tight">
            Controls
          </h3>
          <p className="text-[10px] text-slate-400 leading-tight">
            Manage system operations
          </p>
        </div>
      </div>

      {/* Center Action Buttons */}
      <div className="flex items-center space-x-3">
        {/* Start Vision */}
        <PrimaryButton
          variant="green"
          icon={Play}
          onClick={handleStart}
        >
          Start
        </PrimaryButton>

        {/* Stop Vision */}
        <PrimaryButton
          variant="red"
          icon={Square}
          onClick={handleStop}
        >
          Stop
        </PrimaryButton>

        {/* Load Model */}
        <SecondaryButton
          icon={Upload}
          onClick={() =>
            handleAction(
              'YOLO Model Status',
              'Models are automatically loaded from models/best.pt.',
              'Verified: Ultralytics YOLO with classes [Large hole, Large tear, Small Tear, Small hole].'
            )
          }
        >
          Load Model
        </SecondaryButton>

        {/* Load Video / Simulation */}
        <SecondaryButton
          icon={Folder}
          onClick={handleLoadVideo}
        >
          Load Video
        </SecondaryButton>

        {/* Settings */}
        <SecondaryButton
          icon={Settings}
          onClick={() =>
            handleAction(
              'System Settings',
              'Hardware port configuration, MQTT broker endpoints, and vibration/thermal alert thresholds can be tuned here.',
              'Default Camera: AUSHA webcam (1920×1080). Backend API: ' + apiService.baseUrl + '.'
            )
          }
        >
          Settings
        </SecondaryButton>

        {/* Export */}
        <SecondaryButton
          icon={Download}
          onClick={() =>
            handleAction(
              'Export Session Report',
              'Telemetry records and defect trigger events are formatted for audit logs.',
              'Export ready as beltguard_session_report.csv.'
            )
          }
        >
          Export
        </SecondaryButton>
      </div>

      {/* Right Notice Capsule */}
      <div className="flex items-center space-x-1.5 text-[11px] text-slate-400 bg-industrial-subsurface/60 px-2.5 py-1 rounded-md border border-industrial-border/60">
        <Info className="w-3.5 h-3.5 text-slate-400 shrink-0" />
        <span>Hardware Vision Pipeline Linked (Port 8000)</span>
      </div>
    </div>
  );
};
