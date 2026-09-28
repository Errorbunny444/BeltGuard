import { useState, useEffect, useRef } from 'react';
import { mqttClient } from '../mqttClient';
import { MQTT_TOPICS } from '../topics';
import { MQTTConnectionStatus, VisionPayload } from '../types/mqtt';
import { sensorService } from '../services/sensorService';
import { healthEngine } from '@/analytics/healthEngine';
import { dashboardService } from '@/services/dashboard.service';
import { apiService } from '@/services/api.service';
import { DefectSeverity } from '@/types/status';

export function useMQTT() {
  const [status, setStatus] = useState<MQTTConnectionStatus>(() => mqttClient.getStatus());
  const lastEventRef = useRef<{ defect: string; time: number }>({ defect: '', time: 0 });

  useEffect(() => {
    // 1. Listen for connection status changes
    const unsubStatus = mqttClient.onStatusChange((newStatus) => {
      setStatus(newStatus);
      if (newStatus !== 'CONNECTED') {
        sensorService.handleBrokerDisconnect();
      }
    });

    // 2. Subscribe to beltguard/vision topic
    const unsubVision = mqttClient.subscribe(
      MQTT_TOPICS.VISION,
      (_topic, data: VisionPayload) => {
        try {
          const isCamOnline = Boolean(data.camera);
          const isModelLoaded = Boolean(data.model);
          const isRunning = Boolean(data.detectionRunning);
          const hasDefects = Boolean(
            data.detections && data.detections.length > 0 && data.primaryDefect && data.primaryDefect !== '--'
          );

          // Update camera viewport streaming
          if (isCamOnline || isRunning) {
            dashboardService.updateCameraFrame(apiService.videoFeedUrl);
          } else {
            dashboardService.updateCameraFrame(null);
          }

          // Append to event log on defect
          if (hasDefects && data.primaryDefect && data.primaryDefect !== '--') {
            const now = Date.now();
            if (
              now - lastEventRef.current.time > 4000 ||
              lastEventRef.current.defect !== data.primaryDefect
            ) {
              lastEventRef.current = { defect: data.primaryDefect, time: now };

              const defectLower = data.primaryDefect.toLowerCase();
              const severity: DefectSeverity =
                defectLower.includes('tear') || defectLower.includes('large')
                  ? 'Critical'
                  : 'Medium';

              const confPct = Math.round((data.confidence || 0) * 100);

              dashboardService.appendEvent({
                time: new Date().toLocaleTimeString('en-US', { hour12: false }),
                source: 'MQTT / YOLOv8',
                event: `${data.primaryDefect} detected (Confidence: ${confPct}%)`,
                severity,
              });
            }
          }

          // Update 2x3 Status Cards
          dashboardService.updateDetection({
            camera: isCamOnline ? 'ONLINE' : 'OFFLINE',
            model: isModelLoaded ? 'LOADED' : 'NOT LOADED',
            detection: isRunning ? (hasDefects ? 'ALERT' : 'ACTIVE') : 'INACTIVE',
            primaryDefect: data.primaryDefect || '--',
            confidence: data.confidence > 0 ? Math.round(data.confidence * 100) : null,
            detectionCount: data.count || 0,
            operationalStatus: hasDefects
              ? 'Warning'
              : isRunning
              ? 'Active Monitoring'
              : 'System Ready',
            statusMessage: hasDefects
              ? `Defect identified: ${data.primaryDefect}`
              : isRunning
              ? 'Live YOLO inference active (MQTT)'
              : 'Waiting for camera feed...',
          });

          // Update Belt Intelligence Engine with Vision Telemetry
          healthEngine.updateVision({
            timestamp: data.timestamp || new Date().toISOString(),
            camera: isCamOnline,
            model: isModelLoaded,
            detectionRunning: isRunning,
            primaryDefect: data.primaryDefect || '--',
            confidence: data.confidence || 0,
            count: data.count || 0,
            detections: data.detections || [],
          });
        } catch (err) {
          console.warn('[useMQTT] Error handling vision message:', err);
        }
      }
    );

    // 3. Periodic 1-second health evaluation for vision freshness
    const freshnessInterval = setInterval(() => {
      healthEngine.evaluate();
    }, 1000);

    // Initial connection
    mqttClient.connect();

    return () => {
      clearInterval(freshnessInterval);
      unsubStatus();
      unsubVision();
    };
  }, []);

  return { status };
}


