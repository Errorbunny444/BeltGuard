import React from 'react';
import { Radio } from 'lucide-react';
import { MQTTConnectionStatus } from '@/mqtt/types/mqtt';

import { mqttClient } from '@/mqtt/mqttClient';

interface MqttStatusCardProps {
  status: MQTTConnectionStatus;
  className?: string;
}

export const MqttStatusCard: React.FC<MqttStatusCardProps> = ({ status, className = '' }) => {
  const brokerUrl = mqttClient.getBrokerUrl();

  const getStatusConfig = () => {
    switch (status) {
      case 'CONNECTED':
        return {
          badge: 'bg-emerald-500',
          ping: 'bg-emerald-400',
          text: 'text-emerald-300',
          bg: 'bg-emerald-950/40 border-emerald-800/40',
          label: 'MQTT CONNECTED',
          subtext: brokerUrl,
        };
      case 'CONNECTING':
        return {
          badge: 'bg-amber-500',
          ping: 'bg-amber-400',
          text: 'text-amber-300',
          bg: 'bg-amber-950/40 border-amber-800/40',
          label: 'MQTT CONNECTING',
          subtext: 'Broker handshake...',
        };
      case 'RECONNECTING':
        return {
          badge: 'bg-amber-500',
          ping: 'bg-amber-400',
          text: 'text-amber-300',
          bg: 'bg-amber-950/40 border-amber-800/40',
          label: 'MQTT RECONNECTING',
          subtext: 'Retrying connection...',
        };
      case 'DISCONNECTED':
      default:
        return {
          badge: 'bg-red-500',
          ping: '',
          text: 'text-red-300',
          bg: 'bg-red-950/40 border-red-800/40',
          label: 'MQTT DISCONNECTED',
          subtext: 'Broker unreachable',
        };
    }
  };

  const cfg = getStatusConfig();

  return (
    <div
      className={`flex items-center space-x-2.5 px-3 py-1.5 rounded-lg border text-xs font-mono transition-colors ${cfg.bg} ${className}`}
      title={`MQTT Broker: ${brokerUrl} (${status})`}
    >
      <div className="relative flex h-2.5 w-2.5 shrink-0">
        {cfg.ping && (
          <span className={`animate-ping absolute inline-flex h-full w-full rounded-full opacity-50 ${cfg.ping}`} />
        )}
        <span className={`relative inline-flex rounded-full h-2.5 w-2.5 ${cfg.badge}`} />
      </div>

      <div className="flex flex-col text-left select-none">
        <span className={`font-bold tracking-wider text-[10px] leading-tight ${cfg.text}`}>
          {cfg.label}
        </span>
        <span className="text-[9px] text-slate-400 leading-tight">
          {cfg.subtext}
        </span>
      </div>

      <Radio className={`w-3.5 h-3.5 ml-1 ${cfg.text} opacity-80`} />
    </div>
  );
};
