export default defineNuxtConfig({
  compatibilityDate: '2024-11-01',
  devtools: { enabled: false },
  modules: ['@nuxtjs/tailwindcss'],
  css: ['~/assets/css/main.css'],
  app: {
    baseURL: process.env.NUXT_APP_BASE_URL || '/',
    head: {
      title: 'Grimoire Exodus — Official Download',
      htmlAttrs: { lang: 'en' },
      meta: [
        { name: 'description', content: 'Download Grimoire Exodus from the official source or send a support report through GitHub Issues.' },
        { name: 'theme-color', content: '#17131f' },
        { name: 'color-scheme', content: 'dark' }
      ]
    }
  },
  nitro: {
    preset: 'static'
  },
  runtimeConfig: {
    public: {
      issueApiUrl: process.env.NUXT_PUBLIC_ISSUE_API_URL || 'https://grimoire-exodus-issue-relay.0110harold.workers.dev'
    }
  },
  typescript: {
    strict: true,
    typeCheck: true
  }
})
