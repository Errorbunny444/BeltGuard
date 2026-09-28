import React from 'react';
import { CameraPanel } from './CameraPanel';
import { DetectionCards } from './DetectionCards';
import { BeltHealth } from './BeltHealth';
import { SensorCards } from './SensorCards';
import { AnalyticsChart } from './AnalyticsChart';
import { EventTable } from './EventTable';
import { ControlPanel } from './ControlPanel';
import { AlertBanner } from '../health/AlertBanner';
import { useHealthEngine } from '@/analytics/useHealthEngine';
import { DashboardState } from '@/types/dashboard';

interface DashboardGridProps {
  state: DashboardState;
}

export const DashboardGrid: React.FC<DashboardGridProps> = ({ state }) => {
  const assessment = useHealthEngine();

  return (
    <div className="flex-1 flex flex-col space-y-3 min-h-0 overflow-y-auto pr-1 pb-1">
      {/* Alert Banner: Displays only when condition is WARNING, HIGH_RISK, or CRITICAL */}
      <AlertBanner assessment={assessment} />

      {/* Upper Tier: Live Camera Feed (Left) & Status Metrics + Belt Health (Right) */}

      <div className="grid grid-cols-12 gap-3 shrink-0">
        {/* Left Column: Live Conveyor Feed (Dominant Area, 6/12 or 7/12) */}
        <div className="col-span-12 lg:col-span-6 xl:col-span-6 min-h-[300px]">
          <CameraPanel
            isStreaming={state.camera.isStreaming}
            frameUrl={state.camera.frameUrl}
            deviceName={state.camera.deviceName}
            resolution={state.camera.resolution}
          />
        </div>

        {/* Right Column: 2x3 Status Cards + Belt Health Card */}
        <div className="col-span-12 lg:col-span-6 xl:col-span-6 flex flex-col gap-3">
          {/* 2x3 System Status Grid */}
          <DetectionCards status={state.status} />

          {/* Belt Health Card */}
          <BeltHealth health={state.health} />
        </div>
      </div>

      {/* Middle Tier: ESP32 Sensor Cards + Multi-line Chart (Left) & Event Log (Right) */}
      <div className="grid grid-cols-12 gap-3 flex-1 min-h-[260px]">
        {/* Left: Sensor Cards on Top, Telemetry Curve underneath */}
        <div className="col-span-12 lg:col-span-6 flex flex-col gap-2.5">
          <SensorCards />
          <div className="flex-1 min-h-[190px]">
            <AnalyticsChart />
          </div>
        </div>

        {/* Right: Event Log Table */}
        <div className="col-span-12 lg:col-span-6 flex flex-col">
          <EventTable events={state.events} />
        </div>
      </div>

      {/* Lower Tier: Bottom Control Panel */}
      <div className="shrink-0 pt-0.5">
        <ControlPanel />
      </div>
    </div>
  );
};
