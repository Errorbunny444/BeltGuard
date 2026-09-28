import React, { useState, useEffect } from 'react';
import { MainLayout } from '@/layout/MainLayout';
import { HomePage } from '@/pages/HomePage';
import { AnalyticsPage } from '@/pages/AnalyticsPage';
import { EventsPage } from '@/pages/EventsPage';
import { SettingsPage } from '@/pages/SettingsPage';
import { AboutPage } from '@/pages/AboutPage';
import { dashboardService } from '@/services/dashboard.service';
import { DashboardState } from '@/types/dashboard';
import { useMQTT } from '@/mqtt/hooks/useMQTT';
import { useVisionPipeline } from '@/hooks/useVisionPipeline';
import { motion, AnimatePresence } from 'framer-motion';

export const App: React.FC = () => {
  // Global real-time MQTT subscriber for vision + sensors with auto-reconnect
  useMQTT();
  // Fail-safe HTTP polling pipeline ensuring camera stream is immediately detected
  useVisionPipeline();

  const [currentTab, setCurrentTab] = useState('Home');
  const [dashboardState, setDashboardState] = useState<DashboardState>(() =>
    dashboardService.getState()
  );

  useEffect(() => {
    return dashboardService.subscribe(setDashboardState);
  }, []);

  const renderActivePage = () => {
    switch (currentTab) {
      case 'Analytics':
        return <AnalyticsPage />;
      case 'Events':
        return <EventsPage events={dashboardState.events} />;
      case 'Settings':
        return <SettingsPage />;
      case 'About':
        return <AboutPage />;
      case 'Home':
      default:
        return <HomePage state={dashboardState} />;
    }
  };

  return (
    <MainLayout
      currentTab={currentTab}
      onTabChange={setCurrentTab}
      operationalStatus={dashboardState.status.operationalStatus}
      statusMessage={dashboardState.status.statusMessage}
    >
      <AnimatePresence mode="wait">
        <motion.div
          key={currentTab}
          initial={{ opacity: 0, y: 6 }}
          animate={{ opacity: 1, y: 0 }}
          exit={{ opacity: 0, y: -6 }}
          transition={{ duration: 0.15, ease: 'easeOut' }}
          className="flex-1 flex flex-col min-h-0 overflow-hidden"
        >
          {renderActivePage()}
        </motion.div>
      </AnimatePresence>
    </MainLayout>
  );
};

export default App;
