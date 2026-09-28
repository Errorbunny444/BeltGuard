import React, { useState } from 'react';
import { Settings, Wifi, Eye, Sliders, Shield, Save, Check } from 'lucide-react';
import { MQTT_TOPICS } from '@/mqtt/topics';
import { mqttClient } from '@/mqtt/mqttClient';

export const SettingsPage: React.FC = () => {
  const [brokerHost, setBrokerHost] = useState(() => {
    const envUrl = import.meta.env.VITE_MQTT_WS_URL;
    if (envUrl) {
      try {
        const u = new URL(envUrl);
        if (u.hostname) return u.hostname;
      } catch {
        // ignore
      }
    }
    return typeof window !== 'undefined' && window.location.hostname ? window.location.hostname : 'localhost';
  });
  const [tcpPort, setTcpPort] = useState('1883');
  const [wsPort, setWsPort] = useState(() => {
    const envUrl = import.meta.env.VITE_MQTT_WS_URL;
    if (envUrl) {
      try {
        const u = new URL(envUrl);
        if (u.port) return u.port;
      } catch {
        // ignore
      }
    }
    return '9001';
  });
  const [confidenceThreshold, setConfidenceThreshold] = useState(50);
  const [tempWarning, setTempWarning] = useState(50.0);
  const [tempCritical, setTempCritical] = useState(65.0);
  const [vibWarning, setVibWarning] = useState(0.30);
  const [vibCritical, setVibCritical] = useState(0.60);
  const [savedNotice, setSavedNotice] = useState(false);

  const handleSave = () => {
    setSavedNotice(true);
    // Apply new broker connection target
    mqttClient.disconnect();
    mqttClient.connect(`ws://${brokerHost}:${wsPort}`);
    setTimeout(() => setSavedNotice(false), 2500);
  };

  return (
    <div className="flex-1 flex flex-col space-y-3 min-h-0 overflow-y-auto pr-1 pb-1">
      {/* Top Header */}
      <div className="flex items-center justify-between bg-industrial-surface border border-industrial-border rounded-xl px-4 py-3 shadow-industrial-sm shrink-0">
        <div className="flex items-center space-x-2.5">
          <div className="w-8 h-8 rounded-lg bg-blue-500/10 border border-blue-500/20 flex items-center justify-center text-blue-400">
            <Settings className="w-4 h-4" />
          </div>
          <div>
            <h1 className="text-sm font-bold text-white uppercase tracking-wider">
              System &amp; Pipeline Configuration
            </h1>
            <p className="text-[11px] text-slate-400">
              Manage MQTT broker connection, YOLO inference thresholds, and sensor telemetry alarms
            </p>
          </div>
        </div>

        <button
          onClick={handleSave}
          className="inline-flex items-center space-x-1.5 px-3.5 py-1.5 rounded-lg bg-emerald-600 hover:bg-emerald-500 text-xs font-bold text-white shadow-md transition-all active:scale-95"
        >
          {savedNotice ? (
            <>
              <Check className="w-3.5 h-3.5" />
              <span>Saved!</span>
            </>
          ) : (
            <>
              <Save className="w-3.5 h-3.5" />
              <span>Save Changes</span>
            </>
          )}
        </button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-3 flex-1">
        {/* Card 1: MQTT & Network Parameters */}
        <div className="bg-industrial-surface border border-industrial-border rounded-xl p-4 shadow-industrial-sm flex flex-col justify-between">
          <div>
            <div className="flex items-center space-x-2 mb-3">
              <Wifi className="w-4 h-4 text-cyan-400" />
              <h2 className="text-xs font-bold text-white uppercase tracking-wider">
                Mosquitto MQTT Broker
              </h2>
            </div>

            <div className="space-y-3 text-xs">
              <div>
                <label className="block text-[11px] font-medium text-slate-400 mb-1">
                  Broker Host / LAN IP
                </label>
                <input
                  type="text"
                  value={brokerHost}
                  onChange={(e) => setBrokerHost(e.target.value)}
                  className="w-full px-3 py-1.5 rounded-lg bg-industrial-subsurface border border-industrial-border text-slate-200 focus:outline-none focus:border-cyan-500 font-mono"
                />
              </div>

              <div className="grid grid-cols-2 gap-2">
                <div>
                  <label className="block text-[11px] font-medium text-slate-400 mb-1">
                    TCP Port (Python Core)
                  </label>
                  <input
                    type="text"
                    value={tcpPort}
                    onChange={(e) => setTcpPort(e.target.value)}
                    className="w-full px-3 py-1.5 rounded-lg bg-industrial-subsurface border border-industrial-border text-slate-200 focus:outline-none font-mono"
                  />
                </div>
                <div>
                  <label className="block text-[11px] font-medium text-slate-400 mb-1">
                    WebSocket Port (React UI)
                  </label>
                  <input
                    type="text"
                    value={wsPort}
                    onChange={(e) => setWsPort(e.target.value)}
                    className="w-full px-3 py-1.5 rounded-lg bg-industrial-subsurface border border-industrial-border text-slate-200 focus:outline-none font-mono"
                  />
                </div>
              </div>

              <div>
                <label className="block text-[11px] font-medium text-slate-400 mb-1">
                  Topic Subscriptions
                </label>
                <div className="p-2.5 rounded-lg bg-slate-900 border border-slate-800 space-y-1 font-mono text-[11px]">
                  <div className="flex justify-between">
                    <span className="text-slate-400">Vision Telemetry:</span>
                    <span className="text-cyan-400">{MQTT_TOPICS.VISION}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-slate-400">Sensor Telemetry:</span>
                    <span className="text-emerald-400">{MQTT_TOPICS.SENSORS}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-slate-400">System Alarms:</span>
                    <span className="text-amber-400">{MQTT_TOPICS.ALERTS}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Card 2: Computer Vision & YOLO Parameters */}
        <div className="bg-industrial-surface border border-industrial-border rounded-xl p-4 shadow-industrial-sm flex flex-col justify-between">
          <div>
            <div className="flex items-center space-x-2 mb-3">
              <Eye className="w-4 h-4 text-purple-400" />
              <h2 className="text-xs font-bold text-white uppercase tracking-wider">
                YOLO Defect Detection Pipeline
              </h2>
            </div>

            <div className="space-y-3 text-xs">
              <div>
                <div className="flex justify-between text-[11px] font-medium text-slate-400 mb-1">
                  <span>Inference Confidence Threshold</span>
                  <span className="font-mono text-purple-400 font-bold">{confidenceThreshold}%</span>
                </div>
                <input
                  type="range"
                  min="25"
                  max="95"
                  value={confidenceThreshold}
                  onChange={(e) => setConfidenceThreshold(Number(e.target.value))}
                  className="w-full accent-purple-500 cursor-pointer"
                />
              </div>

              <div>
                <label className="block text-[11px] font-medium text-slate-400 mb-1">
                  Model Weights
                </label>
                <div className="px-3 py-1.5 rounded-lg bg-industrial-subsurface border border-industrial-border text-slate-300 font-mono text-[11px]">
                  models/best.pt (PyTorch Ultralytics YOLOv8)
                </div>
              </div>

              <div>
                <label className="block text-[11px] font-medium text-slate-400 mb-1">
                  Active Defect Taxonomy (4 Classes)
                </label>
                <div className="grid grid-cols-2 gap-1.5 font-mono text-[10px]">
                  <span className="px-2 py-1 rounded bg-red-950/40 border border-red-800/40 text-red-300">
                    0: Large hole (Critical)
                  </span>
                  <span className="px-2 py-1 rounded bg-red-950/40 border border-red-800/40 text-red-300">
                    1: Large tear (Critical)
                  </span>
                  <span className="px-2 py-1 rounded bg-orange-950/40 border border-orange-800/40 text-orange-300">
                    2: Small Tear (High)
                  </span>
                  <span className="px-2 py-1 rounded bg-amber-950/40 border border-amber-800/40 text-amber-300">
                    3: Small hole (Medium)
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Card 3: Multi-Sensor Alarm Thresholds */}
        <div className="bg-industrial-surface border border-industrial-border rounded-xl p-4 shadow-industrial-sm flex flex-col justify-between">
          <div>
            <div className="flex items-center space-x-2 mb-3">
              <Sliders className="w-4 h-4 text-orange-400" />
              <h2 className="text-xs font-bold text-white uppercase tracking-wider">
                Sensor Alarm Thresholds
              </h2>
            </div>

            <div className="space-y-3 text-xs">
              <div className="grid grid-cols-2 gap-2">
                <div>
                  <label className="block text-[11px] font-medium text-slate-400 mb-1">
                    Temp Warning (°C)
                  </label>
                  <input
                    type="number"
                    step="1"
                    value={tempWarning}
                    onChange={(e) => setTempWarning(Number(e.target.value))}
                    className="w-full px-3 py-1.5 rounded-lg bg-industrial-subsurface border border-industrial-border text-slate-200 font-mono"
                  />
                </div>
                <div>
                  <label className="block text-[11px] font-medium text-slate-400 mb-1">
                    Temp Critical (°C)
                  </label>
                  <input
                    type="number"
                    step="1"
                    value={tempCritical}
                    onChange={(e) => setTempCritical(Number(e.target.value))}
                    className="w-full px-3 py-1.5 rounded-lg bg-industrial-subsurface border border-industrial-border text-slate-200 font-mono"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-2">
                <div>
                  <label className="block text-[11px] font-medium text-slate-400 mb-1">
                    Vibration Warning (g)
                  </label>
                  <input
                    type="number"
                    step="0.05"
                    value={vibWarning}
                    onChange={(e) => setVibWarning(Number(e.target.value))}
                    className="w-full px-3 py-1.5 rounded-lg bg-industrial-subsurface border border-industrial-border text-slate-200 font-mono"
                  />
                </div>
                <div>
                  <label className="block text-[11px] font-medium text-slate-400 mb-1">
                    Vibration Critical (g)
                  </label>
                  <input
                    type="number"
                    step="0.05"
                    value={vibCritical}
                    onChange={(e) => setVibCritical(Number(e.target.value))}
                    className="w-full px-3 py-1.5 rounded-lg bg-industrial-subsurface border border-industrial-border text-slate-200 font-mono"
                  />
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Card 4: Belt Intelligence Rules */}
        <div className="bg-industrial-surface border border-industrial-border rounded-xl p-4 shadow-industrial-sm flex flex-col justify-between">
          <div>
            <div className="flex items-center space-x-2 mb-3">
              <Shield className="w-4 h-4 text-emerald-400" />
              <h2 className="text-xs font-bold text-white uppercase tracking-wider">
                Belt Intelligence Engine Tuning
              </h2>
            </div>

            <div className="space-y-2.5 text-xs">
              <div className="flex justify-between py-1.5 border-b border-slate-800 text-slate-300">
                <span>Baseline Condition Index</span>
                <span className="font-mono text-emerald-400 font-bold">100 / 100</span>
              </div>
              <div className="flex justify-between py-1.5 border-b border-slate-800 text-slate-300">
                <span>Alert Cooldown Window</span>
                <span className="font-mono text-slate-300">10 seconds (Anti-spam)</span>
              </div>
              <div className="flex justify-between py-1.5 border-b border-slate-800 text-slate-300">
                <span>Data Stale Timeout</span>
                <span className="font-mono text-amber-400">5 seconds</span>
              </div>
              <div className="flex justify-between py-1.5 border-b border-slate-800 text-slate-300">
                <span>Data Offline Timeout</span>
                <span className="font-mono text-red-400">15 seconds (-10 pts)</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
