import type { Config } from 'tailwindcss'

export default <Partial<Config>>{
  content: [
    './app.vue',
    './components/**/*.{vue,js,ts}',
    './pages/**/*.vue'
  ],
  theme: {
    extend: {
      colors: {
        exodus: {
          50: '#f8f3ff',
          200: '#dec7ff',
          300: '#c49cff',
          400: '#a970f4',
          500: '#8c4bd3',
          600: '#7136ae',
          900: '#2a1f36',
          950: '#17131f'
        }
      },
      boxShadow: {
        arcane: '0 0 0 1px rgba(169,112,244,.2), 0 30px 90px rgba(0,0,0,.5), 0 0 55px rgba(113,54,174,.14)'
      }
    }
  },
  plugins: []
}
