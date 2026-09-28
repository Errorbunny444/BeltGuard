import { DefectSeverity } from '@/types/status';

export function formatConfidence(conf: number | null): string {
  if (conf === null || conf === undefined) return '--';
  const val = conf > 1 ? conf : conf * 100;
  return `${val.toFixed(0)}%`;
}

export function formatScore(score: number | null): string {
  if (score === null || score === undefined) return '--';
  return Math.round(score).toString();
}

export function getSeverityBadgeStyles(severity: DefectSeverity): { bg: string; text: string; dot: string } {
  switch (severity) {
    case 'Critical':
      return {
        bg: 'bg-red-500/10 border-red-500/20',
        text: 'text-red-400',
        dot: 'bg-red-500',
      };
    case 'High':
      return {
        bg: 'bg-orange-500/10 border-orange-500/20',
        text: 'text-orange-400',
        dot: 'bg-orange-500',
      };
    case 'Medium':
      return {
        bg: 'bg-amber-500/10 border-amber-500/20',
        text: 'text-amber-400',
        dot: 'bg-amber-500',
      };
    case 'Low':
      return {
        bg: 'bg-blue-500/10 border-blue-500/20',
        text: 'text-blue-400',
        dot: 'bg-blue-500',
      };
    default:
      return {
        bg: 'bg-slate-500/10 border-slate-500/20',
        text: 'text-slate-400',
        dot: 'bg-slate-500',
      };
  }
}
