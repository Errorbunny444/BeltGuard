export function useTheme() {
  return {
    theme: 'dark' as const,
    colors: {
      bgMain: '#181d27',
      bgCard: '#242b3b',
      bgCardSub: '#2c3548',
      borderCard: '#384359',
      accentGreen: '#10b981',
      accentRed: '#ef4444',
      accentAmber: '#f59e0b',
      accentBlue: '#38bdf8',
    },
  };
}
