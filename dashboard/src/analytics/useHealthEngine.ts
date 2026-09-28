import { useState, useEffect } from 'react';
import { healthEngine } from './healthEngine';
import { HealthAssessment } from '@/types/health';

export function useHealthEngine(): HealthAssessment {
  const [assessment, setAssessment] = useState<HealthAssessment>(() =>
    healthEngine.getAssessment()
  );

  useEffect(() => {
    return healthEngine.subscribe(setAssessment);
  }, []);

  return assessment;
}
