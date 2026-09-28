import { useState, useEffect } from 'react';

export interface ClockState {
  dateStr: string;
  timeStr: string;
}

export function useClock(): ClockState {
  const [clock, setClock] = useState<ClockState>(() => formatCurrentClock());

  useEffect(() => {
    const timer = setInterval(() => {
      setClock(formatCurrentClock());
    }, 1000);

    return () => clearInterval(timer);
  }, []);

  return clock;
}

function formatCurrentClock(): ClockState {
  const now = new Date();

  // Date format: Tue, 27 May 2025
  const dateStr = now.toLocaleDateString('en-GB', {
    weekday: 'short',
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  });

  // Time format: 10:24:36 AM
  const timeStr = now.toLocaleTimeString('en-US', {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: true,
  });

  return { dateStr, timeStr };
}
