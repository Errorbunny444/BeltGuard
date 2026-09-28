/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        industrial: {
          darkest: '#141822',
          darker: '#181d27',        // Soft medium-dark slate base
          dark: '#1d2330',          // Secondary canvas
          surface: '#242b3b',       // Primary card surface (light greyish-slate)
          subsurface: '#2c3548',    // Sub-card and pill background
          elevated: '#353f56',      // Hover and popup elevation
          border: '#384359',        // Soft structural card borders
          borderSubtle: '#43506b',  // Highlights
        },
        status: {
          online: '#22c55e',
          offline: '#ef4444',
          warning: '#f59e0b',
          info: '#38bdf8',
          defect: '#a855f7',
        }
      },
      fontFamily: {
        sans: ['Inter', 'Segoe UI', '-apple-system', 'BlinkMacSystemFont', 'sans-serif'],
        mono: ['JetBrains Mono', 'Fira Code', 'monospace'],
        script: ['Caveat', 'Dancing Script', 'Brush Script MT', 'cursive'],
      },
      boxShadow: {
        'industrial': '0 4px 18px -2px rgba(0, 0, 0, 0.28)',
        'industrial-sm': '0 2px 8px -1px rgba(0, 0, 0, 0.22)',
        'glow-green': '0 0 15px -3px rgba(34, 197, 94, 0.25)',
        'glow-red': '0 0 15px -3px rgba(239, 68, 68, 0.25)',
      }
    },
  },
  plugins: [],
}
