export interface SystemStatusResponse {
  camera: boolean;
  model: boolean;
  detectionRunning: boolean;
}

export interface DefectItem {
  className: string;
  confidence: number;
  bbox: number[];
  severity?: string;
}

export interface DetectionsResponse {
  defects: DefectItem[];
  count: number;
  totalCount?: number;
}

export function getApiBaseUrl(): string {
  const envUrl = import.meta.env.VITE_API_BASE_URL;
  if (envUrl && typeof envUrl === 'string' && envUrl.trim() !== '') {
    return envUrl.replace(/\/+$/, '');
  }
  const host = typeof window !== 'undefined' && window.location.hostname ? window.location.hostname : 'localhost';
  return `http://${host}:8000`;
}

class ApiService {
  public get baseUrl(): string {
    return getApiBaseUrl();
  }

  public get videoFeedUrl(): string {
    return `${this.baseUrl}/video_feed`;
  }

  async getStatus(): Promise<SystemStatusResponse | null> {
    try {
      const res = await fetch(`${this.baseUrl}/status`, { cache: 'no-store' });
      if (!res.ok) return null;
      return await res.json();
    } catch {
      return null;
    }
  }

  async getDetections(): Promise<DetectionsResponse | null> {
    try {
      const res = await fetch(`${this.baseUrl}/detections`, { cache: 'no-store' });
      if (!res.ok) return null;
      return await res.json();
    } catch {
      return null;
    }
  }

  async startVision(): Promise<boolean> {
    try {
      const res = await fetch(`${this.baseUrl}/start`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
      });
      return res.ok;
    } catch {
      return false;
    }
  }

  async stopVision(): Promise<boolean> {
    try {
      const res = await fetch(`${this.baseUrl}/stop`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
      });
      return res.ok;
    } catch {
      return false;
    }
  }

  async toggleSimulation(enabled: boolean): Promise<boolean> {
    try {
      const res = await fetch(`${this.baseUrl}/simulation`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ enabled }),
      });
      return res.ok;
    } catch {
      return false;
    }
  }
}

export const apiService = new ApiService();
