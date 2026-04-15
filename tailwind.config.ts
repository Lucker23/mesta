import type { Config } from 'tailwindcss';

const config: Config = {
  content: [
    './app/**/*.{ts,tsx}',
    './components/**/*.{ts,tsx}',
  ],
  theme: {
    extend: {
      colors: {
        midnight: '#060610',
        elevated: '#0d1117',
        teal: '#00e5ff',
        gold: '#e8a020',
        'text-primary': '#e0e6ed',
        'text-muted': '#6a7a8a',
        'text-dim': '#3a4a5a',
      },
      fontFamily: {
        orbitron: ['var(--font-orbitron)', 'sans-serif'],
        rajdhani: ['var(--font-rajdhani)', 'sans-serif'],
        exo2: ['var(--font-exo2)', 'sans-serif'],
      },
      animation: {
        'pulse-slow': 'pulse 3s cubic-bezier(0.4, 0, 0.6, 1) infinite',
        'float': 'float 6s ease-in-out infinite',
        'glow': 'glow 2s ease-in-out infinite alternate',
      },
      keyframes: {
        float: {
          '0%, 100%': { transform: 'translateY(0px)' },
          '50%': { transform: 'translateY(-10px)' },
        },
        glow: {
          '0%': { boxShadow: '0 0 5px rgba(0, 229, 255, 0.2)' },
          '100%': { boxShadow: '0 0 20px rgba(0, 229, 255, 0.6)' },
        },
      },
    },
  },
  plugins: [],
};

export default config;
