import React, { useState, useEffect } from 'react';
import { User } from 'lucide-react';
import { useClock } from '@/hooks/useClock';
import { APP_NAME, APP_SUBTITLE, APP_SLOGAN } from '@/utils/constants';
import { mqttClient } from '@/mqtt/mqttClient';
import { MQTTConnectionStatus } from '@/mqtt/types/mqtt';
import { MqttStatusCard } from './MqttStatusCard';

interface HeaderProps {
  operationalStatus?: string;
  statusMessage?: string;
}

export const Header: React.FC<HeaderProps> = ({
  operationalStatus = 'System Ready',
  statusMessage = 'Waiting for data...',
}) => {
  const { dateStr, timeStr } = useClock();
  const [mqttStatus, setMqttStatus] = useState<MQTTConnectionStatus>(() => mqttClient.getStatus());

  useEffect(() => {
    return mqttClient.onStatusChange(setMqttStatus);
  }, []);

  return (
    <header className="h-16 px-5 border-b border-industrial-border bg-[#1e2431]/95 backdrop-blur flex items-center justify-between shrink-0 select-none z-20">
      {/* Brand & Subtitle */}
      <div className="flex items-center space-x-3.5">
        {/* Mountain Chevron Emblem */}
        <div className="w-10 h-10 flex items-center justify-center">
          <svg className="w-9 h-9" viewBox="0 0 48 48" fill="none">
            {/* Emerald outer chevron wing */}
            <path
              d="M4 36L18 10L24 20L14 36H4Z"
              fill="#10b981"
            />
            {/* Light Slate inner chevron wing */}
            <path
              d="M22 20L30 10L44 36H32L26 24L22 20Z"
              fill="#e2e8f0"
            />
            {/* Belt track horizontal link */}
            <path
              d="M12 30H36"
              stroke="#10b981"
              strokeWidth="2.5"
              strokeLinecap="round"
            />
          </svg>
        </div>

        <div>
          <h1 className="text-xl font-bold tracking-wider text-white flex items-center space-x-2">
            <span>{APP_NAME}</span>
          </h1>
          <p className="text-[11px] font-medium text-slate-400 tracking-normal -mt-0.5">
            {APP_SUBTITLE}
          </p>
        </div>
      </div>

      {/* Center Mission Script Motto */}
      <div className="hidden lg:flex items-center justify-center">
        <span className="font-script text-2xl text-slate-300/85 tracking-wide italic select-none">
          {APP_SLOGAN}
        </span>
      </div>

      {/* Right Telemetry & Status Badges */}
      <div className="flex items-center space-x-4">
        {/* Date & Time Capsule */}
        <div className="text-right">
          <div className="text-[11px] font-medium text-slate-400">
            {dateStr}
          </div>
          <div className="text-base font-semibold text-slate-100 font-mono tracking-tight -mt-0.5">
            {timeStr}
          </div>
        </div>

        {/* Vertical Divider */}
        <div className="h-8 w-px bg-industrial-border" />

        {/* MQTT Connection Status Indicator */}
        <MqttStatusCard status={mqttStatus} />

        {/* Operational System Ready Status */}
        <div className="flex items-center space-x-2.5 bg-industrial-surface/70 px-3.5 py-1.5 rounded-lg border border-industrial-border">
          <span className="relative flex h-2.5 w-2.5">
            <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-50"></span>
            <span className="relative inline-flex rounded-full h-2.5 w-2.5 bg-emerald-500"></span>
          </span>
          <div className="text-left">
            <div className="text-xs font-semibold text-slate-100 leading-tight">
              {operationalStatus}
            </div>
            <div className="text-[10px] text-slate-400 leading-tight">
              {statusMessage}
            </div>
          </div>
        </div>

        {/* User Avatar */}
        <button
          className="w-9 h-9 rounded-full bg-slate-800/80 border border-slate-700/60 flex items-center justify-center text-slate-300 hover:text-white hover:bg-slate-700/80 transition-colors"
          title="Operator Session"
        >
          <User className="w-4 h-4" />
        </button>
      </div>
    </header>
  );
};
