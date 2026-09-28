import React, { useState, useEffect } from 'react';
import { DashboardLayout } from '@/layout/DashboardLayout';
import { DashboardGrid } from '@/components/dashboard/DashboardGrid';
import { dashboardService } from '@/services/dashboard.service';
import { DashboardState } from '@/types/dashboard';
import { useMQTT } from '@/mqtt/hooks/useMQTT';

export const DashboardPage: React.FC = () => {
  // Real-time MQTT subscriber for beltguard/vision with automatic reconnect
  useMQTT();

  const [dashboardState, setDashboardState] = useState<DashboardState>(() =>
    dashboardService.getState()
  );

  useEffect(() => {
    return dashboardService.subscribe(setDashboardState);
  }, []);

  return (
    <DashboardLayout>
      <DashboardGrid state={dashboardState} />
    </DashboardLayout>
  );
};
