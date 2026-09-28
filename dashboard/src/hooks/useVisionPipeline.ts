import { useEffect, useRef } from 'react';
import { apiService } from '@/services/api.service';
import { dashboardService } from '@/services/dashboard.service';
import { healthEngine } from '@/analytics/healthEngine';
import { DefectSeverity } from '@/types/status';

export function useVisionPipeline() {
  const lastEventRef = useRef<{ defect: string; time: number }>({ defect: '', time: 0 });

  useEffect(() => {
    let isMounted = true;

    const poll = async () => {
      try {
        const [status, detections] = await Promise.all([
          apiService.getStatus(),
          apiService.getDetections(),
        ]);

        if (!isMounted) return;

        if (status) {
          const isCamOnline = status.camera;
          const isModelLoaded = status.model;
          const isRunning = status.detectionRunning;
          const hasDefects = detections && detections.defects && detections.defects.length > 0;

          // 1. Update Camera Stream URL
          if (isCamOnline || isRunning) {
            dashboardService.updateCameraFrame(apiService.videoFeedUrl);
          } else {
            dashboardService.updateCameraFrame(null);
          }

          // 2. Parse Top Defect
          let primaryDefect = '--';
          let confidence: number | null = null;
          let detectionCount = detections?.totalCount ?? detections?.count ?? 0;

          if (hasDefects && detections.defects.length > 0) {
            const sorted = [...detections.defects].sort((a, b) => b.confidence - a.confidence);
            const top = sorted[0];
            primaryDefect = top.className;
            confidence = Math.round(top.confidence * 100);

            // 3. Append to Event Log (with 4-second debouncing per defect type)
            const now = Date.now();
            if (
              now - lastEventRef.current.time > 4000 ||
              lastEventRef.current.defect !== top.className
            ) {
              lastEventRef.current = { defect: top.className, time: now };
              
              const severity: DefectSeverity =
                top.className.toLowerCase().includes('tear') || top.className.toLowerCase().includes('large')
                  ? 'Critical'
                  : 'Medium';

              dashboardService.appendEvent({
                time: new Date().toLocaleTimeString('en-US', { hour12: false }),
                source: 'YOLOv8 / CAM-01',
                event: `${top.className} detected (Confidence: ${confidence}%)`,
                severity,
              });
            }
          }

          // 4. Update 2x3 Status Cards
          dashboardService.updateDetection({
            camera: isCamOnline ? 'ONLINE' : 'OFFLINE',
            model: isModelLoaded ? 'LOADED' : 'NOT LOADED',
            detection: isRunning ? (hasDefects ? 'ALERT' : 'ACTIVE') : 'INACTIVE',
            primaryDefect,
            confidence,
            detectionCount,
            operationalStatus: hasDefects
              ? 'Warning'
              : isRunning
              ? 'Active Monitoring'
              : 'System Ready',
            statusMessage: hasDefects
              ? `Defect identified: ${primaryDefect}`
              : isRunning
              ? 'Live YOLO inference active'
              : 'Waiting for camera feed...',
          });

          // 5. Update Belt Intelligence Engine with Vision Telemetry
          healthEngine.updateVision({
            timestamp: new Date().toISOString(),
            camera: isCamOnline,
            model: isModelLoaded,
            detectionRunning: isRunning,
            primaryDefect,
            confidence: (confidence ?? 0) / 100,
            count: detectionCount,
            detections: (detections?.defects || []).map((d) => ({
              className: d.className,
              confidence: d.confidence,
              bbox: d.bbox,
            })),
          });
        }
      } catch (err) {
        console.warn('[VisionPipeline] Polling error:', err);
      }
    };

    // Immediate poll, then every 1000ms
    poll();
    const interval = setInterval(poll, 1000);

    return () => {
      isMounted = false;
      clearInterval(interval);
    };
  }, []);
}
