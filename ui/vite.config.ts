import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'
import { VitePWA } from 'vite-plugin-pwa'

export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: 'autoUpdate',
      manifest: {
        name: 'Orchestrator',
        short_name: 'Orchestrator',
        theme_color: '#1f3a5f',
        background_color: '#f5f7fa',
        display: 'standalone',
        icons: [{ src: '/icon.svg', sizes: 'any', type: 'image/svg+xml', purpose: 'any maskable' }],
      },
      workbox: { navigateFallbackDenylist: [/^\/api/] },
    }),
  ],
  server: { proxy: { '/api': 'http://localhost:5176' } },
})
