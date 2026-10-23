/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    colors: {
      transparent: 'transparent',
      deep: 'rgb(from var(--color-bg-deep) r g b / <alpha-value>)',
      elevated: 'rgb(from var(--color-bg-elevated) r g b / <alpha-value>)',
      dark: 'rgb(from var(--color-bg-elevated) r g b / <alpha-value>)',
      border: 'rgb(from var(--color-border) r g b / <alpha-value>)',
      surface: 'rgb(from var(--color-border) r g b / <alpha-value>)',
      primary: 'rgb(from var(--color-primary) r g b / <alpha-value>)',
      accent: 'rgb(from var(--color-accent) r g b / <alpha-value>)',
      muted: 'rgb(from var(--color-text-muted) r g b / <alpha-value>)',
      light: 'rgb(from var(--color-text-light) r g b / <alpha-value>)',
    },
    extend: {
      fontFamily: {
        display: ['"Space Grotesk"', 'sans-serif'],
        body: ['Inter', 'sans-serif'],
        serif: ['"Instrument Serif"', 'serif'],
      },
      borderRadius: {
        '4xl': '2rem',
        '5xl': '2.5rem',
        '6xl': '3rem',
      },
      animation: {
        'fade-up': 'fadeUp 0.8s ease-out forwards',
        'fade-in': 'fadeIn 0.6s ease-out forwards',
        marquee: 'marquee 30s linear infinite',
      },
      keyframes: {
        fadeUp: {
          '0%': { opacity: '0', transform: 'translateY(30px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        fadeIn: {
          '0%': { opacity: '0' },
          '100%': { opacity: '1' },
        },
        marquee: {
          '0%': { transform: 'translateX(0)' },
          '100%': { transform: 'translateX(-50%)' },
        },
      },
    },
  },
  plugins: [],
};
