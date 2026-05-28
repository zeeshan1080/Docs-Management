/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{html,ts}'],
  theme: {
    extend: {
      colors: {
        portal: {
          50: '#f0f8ff',
          100: '#e2f2fb',
          200: '#b8dff4',
          300: '#8fc8ec',
          400: '#82c3ec',
          500: '#5eb0e5',
          600: '#3d9bd9',
          700: '#2d82bc',
          800: '#286994',
          900: '#25587a',
          950: '#1a3d52',
        },
      },
      fontFamily: {
        sans: ['Inter', 'ui-sans-serif', 'system-ui', 'sans-serif'],
      },
      backgroundImage: {
        'portal-body':
          'linear-gradient(165deg, #f4f9ff 0%, #eef6ff 42%, #fafdff 78%, #f0f7ff 100%)',
      },
      boxShadow: {
        glow: '0 0 0 1px rgb(130 195 236 / 0.14), 0 14px 42px -12px rgb(61 155 217 / 0.22)',
        'glow-sm': '0 0 0 1px rgb(130 195 236 / 0.1), 0 8px 26px -8px rgb(61 155 217 / 0.16)',
        'portal-float': '0 4px 24px -4px rgb(15 23 42 / 0.06), 0 12px 36px -10px rgb(130 195 236 / 0.18)',
      },
      keyframes: {
        fadeIn: {
          '0%': { opacity: '0' },
          '100%': { opacity: '1' },
        },
        slideUp: {
          '0%': { opacity: '0', transform: 'translateY(14px) scale(0.98)' },
          '100%': { opacity: '1', transform: 'translateY(0) scale(1)' },
        },
        floaty: {
          '0%, 100%': { transform: 'translateY(0)' },
          '50%': { transform: 'translateY(-4px)' },
        },
        breathe: {
          '0%, 100%': { transform: 'scale(1)' },
          '50%': { transform: 'scale(1.07)' },
        },
        'glow-pulse': {
          '0%, 100%': { boxShadow: '0 0 0 0 rgb(61 155 217 / 0)' },
          '50%': {
            boxShadow: '0 0 0 6px rgb(61 155 217 / 0.12), 0 8px 28px -6px rgb(61 155 217 / 0.2)',
          },
        },
      },
      animation: {
        'fade-in': 'fadeIn 0.22s ease-out forwards',
        'slide-up': 'slideUp 0.32s cubic-bezier(0.16, 1, 0.3, 1) forwards',
        floaty: 'floaty 4s ease-in-out infinite',
        breathe: 'breathe 3.2s ease-in-out infinite',
        'glow-pulse': 'glow-pulse 2.4s ease-in-out infinite',
      },
    },
  },
  plugins: [],
};
