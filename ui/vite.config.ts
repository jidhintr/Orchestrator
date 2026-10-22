import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'
import { VitePWA } from 'vite-plugin-pwa'

export default defineConfig(({ mode }) => {
  const proxy = { '/api': loadEnv(mode, '.', '').API_URL || 'http://localhost:5176' }
  return {
    plugins: [
      react(),
      VitePWA({
        registerType: 'autoUpdate',
        manifest: {
          name: 'Orchestrator',
          short_name: 'Orchestrator',
          theme_color: '#0a7ea4',
          background_color: '#f3f4f6',
          display: 'standalone',
          icons: [{ src: '/icon.svg', sizes: 'any', type: 'image/svg+xml', purpose: 'any maskable' }],
        },
        workbox: { navigateFallbackDenylist: [/^\/api/] },
      }),
    ],
    server: { proxy },
    preview: { proxy },
  }
})
