import React from 'react';
import { Cpu, Layers, ShieldCheck, HardDrive, Terminal } from 'lucide-react';
import { APP_NAME, APP_SUBTITLE, APP_VERSION } from '@/utils/constants';

export const AboutPage: React.FC = () => {
  return (
    <div className="flex-1 flex flex-col space-y-3 min-h-0 overflow-y-auto pr-1 pb-1">
      {/* Top Hero Banner */}
      <div className="bg-gradient-to-r from-emerald-950/40 via-industrial-surface to-slate-900 border border-industrial-border rounded-xl p-4 shadow-industrial-sm flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div className="flex items-center space-x-3.5">
          <div className="w-12 h-12 rounded-xl bg-emerald-500/10 border border-emerald-500/20 flex items-center justify-center text-emerald-400 shrink-0">
            <ShieldCheck className="w-7 h-7" />
          </div>
          <div>
            <div className="flex items-center space-x-2">
              <h1 className="text-lg font-black text-white tracking-wide">{APP_NAME}</h1>
              <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
                {APP_VERSION}
              </span>
            </div>
            <p className="text-xs text-slate-300 mt-0.5 font-medium">{APP_SUBTITLE}</p>
            <p className="text-[11px] text-slate-500">Smart India Hackathon (SIH) 2024 — Industrial Innovation</p>
          </div>
        </div>

        <div className="flex items-center space-x-2 shrink-0">
          <span className="px-3 py-1.5 rounded-lg text-xs font-semibold bg-industrial-subsurface border border-industrial-border text-emerald-400">
            Safer Mines, Smarter Tomorrows
          </span>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        {/* End-to-End System Architecture */}
        <div className="bg-industrial-surface border border-industrial-border rounded-xl p-4 shadow-industrial-sm flex flex-col">
          <div className="flex items-center space-x-2 mb-2.5 shrink-0">
            <Layers className="w-4 h-4 text-cyan-400" />
            <h2 className="text-xs font-bold text-white uppercase tracking-wider">
              End-to-End System Architecture
            </h2>
          </div>

          <div className="flex-1 flex items-center justify-center min-h-[240px] w-full">
            <img
              src="/system_architecture.png"
              alt="Complete System Architecture"
              className="h-full w-auto max-w-full object-contain rounded-lg border border-slate-700/60 shadow-md"
            />
          </div>
        </div>

        {/* Hardware Bill of Materials (BOM) */}
        <div className="bg-industrial-surface border border-industrial-border rounded-xl p-4 shadow-industrial-sm flex flex-col">
          <div className="flex items-center space-x-2 mb-2.5 shrink-0">
            <Cpu className="w-4 h-4 text-amber-400" />
            <h2 className="text-xs font-bold text-white uppercase tracking-wider">
              Hardware &amp; Sensor Specifications
            </h2>
          </div>

          <div className="space-y-2 text-xs">
            <div className="p-2 rounded-lg bg-industrial-subsurface/80 border border-industrial-border">
              <div className="font-semibold text-white">AUSHA Full HD Industrial Camera</div>
              <div className="text-[11px] text-slate-400 mt-0.5">
                1920 × 1080 @ 30 FPS, DirectShow backend, auto-exposure calibrated for conveyor lighting.
              </div>
            </div>

            <div className="p-2 rounded-lg bg-industrial-subsurface/80 border border-industrial-border">
              <div className="font-semibold text-white">YOLOv8 Real-Time Inference Engine</div>
              <div className="text-[11px] text-slate-400 mt-0.5">
                Ultralytics neural network trained for high-speed tear, rip, and perforation detection on conveyor belts.
              </div>
            </div>

            <div className="p-2 rounded-lg bg-industrial-subsurface/80 border border-industrial-border">
              <div className="font-semibold text-white">Eclipse Mosquitto MQTT Broker</div>
              <div className="text-[11px] text-slate-400 mt-0.5">
                Dual-bridge industrial broker bridging TCP Port 1883 and WebSocket Port 9001 for zero-latency SCADA streaming.
              </div>
            </div>

            <div className="p-2 rounded-lg bg-industrial-subsurface/80 border border-industrial-border">
              <div className="font-semibold text-white">MPU6050 6-Axis Accelerometer (Vibration)</div>
              <div className="text-[11px] text-slate-400 mt-0.5">
                I2C motion &amp; vibration sensor measuring RMS vibration acceleration (g) for bearing anomaly alerts.
              </div>
            </div>

            <div className="p-2 rounded-lg bg-industrial-subsurface/80 border border-industrial-border">
              <div className="font-semibold text-white">0.96&quot; I2C OLED Display (SSD1306)</div>
              <div className="text-[11px] text-slate-400 mt-0.5">
                On-device edge monitor displaying live temperature, vibration RMS, and MQTT connection status.
              </div>
            </div>
          </div>
        </div>

        {/* Software Technology Stack */}
        <div className="bg-industrial-surface border border-industrial-border rounded-xl p-4 shadow-industrial-sm flex flex-col justify-between">
          <div>
            <div className="flex items-center space-x-2 mb-3">
              <Terminal className="w-4 h-4 text-purple-400" />
              <h2 className="text-xs font-bold text-white uppercase tracking-wider">
                Software &amp; Framework Stack
              </h2>
            </div>

            <div className="grid grid-cols-2 gap-2 text-xs">
              <div className="p-2 rounded-lg bg-industrial-subsurface border border-industrial-border">
                <span className="text-[10px] text-slate-400 block font-mono">VISION BACKEND</span>
                <span className="font-semibold text-slate-200">Python 3.10 + OpenCV</span>
              </div>
              <div className="p-2 rounded-lg bg-industrial-subsurface border border-industrial-border">
                <span className="text-[10px] text-slate-400 block font-mono">OBJECT DETECTION</span>
                <span className="font-semibold text-slate-200">Ultralytics YOLOv8</span>
              </div>
              <div className="p-2 rounded-lg bg-industrial-subsurface border border-industrial-border">
                <span className="text-[10px] text-slate-400 block font-mono">MQTT BROKER</span>
                <span className="font-semibold text-slate-200">Eclipse Mosquitto 2.1.2</span>
              </div>
              <div className="p-2 rounded-lg bg-industrial-subsurface border border-industrial-border">
                <span className="text-[10px] text-slate-400 block font-mono">FRONTEND CLIENT</span>
                <span className="font-semibold text-slate-200">React 18 + TypeScript</span>
              </div>
              <div className="p-2 rounded-lg bg-industrial-subsurface border border-industrial-border">
                <span className="text-[10px] text-slate-400 block font-mono">TELEMETRY CHARTS</span>
                <span className="font-semibold text-slate-200">Recharts Multi-Line</span>
              </div>
              <div className="p-2 rounded-lg bg-industrial-subsurface border border-industrial-border">
                <span className="text-[10px] text-slate-400 block font-mono">MICRO-ANIMATIONS</span>
                <span className="font-semibold text-slate-200">Framer Motion</span>
              </div>
            </div>
          </div>
        </div>

        {/* Project Objectives & Impact */}
        <div className="bg-industrial-surface border border-industrial-border rounded-xl p-4 shadow-industrial-sm flex flex-col justify-between">
          <div>
            <div className="flex items-center space-x-2 mb-3">
              <HardDrive className="w-4 h-4 text-emerald-400" />
              <h2 className="text-xs font-bold text-white uppercase tracking-wider">
                Industrial Impact &amp; Safety Goals
              </h2>
            </div>

            <p className="text-xs text-slate-300 leading-relaxed mb-3">
              BeltGuard prevents catastrophic conveyor longitudinal rip events, idler roller fires, and unplanned production halts in deep mines and mineral processing plants by continuous edge AI defect recognition fused with multi-sensor telemetry.
            </p>

            <div className="space-y-1 text-[11px] text-slate-400">
              <div>✓ Zero-latency defect identification (&lt; 50ms inference time)</div>
              <div>✓ Multi-sensor thermal and vibration bearing overload detection</div>
              <div>✓ Continuous Conveyor Health Index (0–100) condition scoring</div>
              <div>✓ Deterministic, actionable maintenance recommendations</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
