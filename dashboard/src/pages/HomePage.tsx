import React from 'react';
import { CameraPanel } from '@/components/dashboard/CameraPanel';
import { DetectionCards } from '@/components/dashboard/DetectionCards';
import { BeltHealth } from '@/components/dashboard/BeltHealth';
import { ControlPanel } from '@/components/dashboard/ControlPanel';
import { AlertBanner } from '@/components/health/AlertBanner';
import { useHealthEngine } from '@/analytics/useHealthEngine';
import { DashboardState } from '@/types/dashboard';

interface HomePageProps {
  state: DashboardState;
}

export const HomePage: React.FC<HomePageProps> = ({ state }) => {
  const assessment = useHealthEngine();

  return (
    <div className="flex-1 flex flex-col space-y-3 min-h-0 overflow-y-auto pr-1 pb-1">
      {/* Alert Banner: Only shows when condition is WARNING, HIGH_RISK, or CRITICAL */}
      <AlertBanner assessment={assessment} />

      {/* Main Grid: Live Conveyor Feed (Left) & Real-Time Intelligence Metrics (Right) */}
      <div className="grid grid-cols-12 gap-3 flex-1 min-h-[360px]">
        {/* Left Column: Live Conveyor Feed (Dominant Area, 6/12 or 7/12) */}
        <div className="col-span-12 lg:col-span-7 flex flex-col min-h-[320px]">
          <CameraPanel
            isStreaming={state.camera.isStreaming}
            frameUrl={state.camera.frameUrl}
            deviceName={state.camera.deviceName}
            resolution={state.camera.resolution}
          />
        </div>

        {/* Right Column: 2x3 Detection Telemetry Cards + Conveyor Health Index */}
        <div className="col-span-12 lg:col-span-5 flex flex-col gap-3">
          {/* 2x3 YOLO Detection & System Status Cards */}
          <DetectionCards status={state.status} />

          {/* Belt Intelligence Engine Card */}
          <div className="flex-1 flex flex-col">
            <BeltHealth health={state.health} />
          </div>
        </div>
      </div>

      {/* Lower Tier: Industrial Control Panel */}
      <div className="shrink-0 pt-0.5">
        <ControlPanel />
      </div>
    </div>
  );
};
