/// <reference types="vitest/config" />
import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import { VitePWA } from 'vite-plugin-pwa';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const proxyTarget = env.VITE_API_PROXY_TARGET || 'http://localhost:5000';

  return {
    plugins: [
      react(),
      tailwindcss(),
      VitePWA({
        disable: process.env.VITEST === 'true',
        registerType: 'autoUpdate',
        injectRegister: 'auto',
        manifest: {
          name: 'خيوط — سوق الأقمشة والنسيج',
          short_name: 'خيوط',
          description: 'سوق B2B يربط ورش الخياطة بموردي الأقمشة والخيوط',
          lang: 'ar',
          dir: 'rtl',
          display: 'standalone',
          start_url: '/',
          theme_color: '#0f1115',
          background_color: '#0f1115',
          icons: [
            { src: '/icons/icon-192.png', sizes: '192x192', type: 'image/png' },
            { src: '/icons/icon-512.png', sizes: '512x512', type: 'image/png' },
            { src: '/icons/icon-512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
          ],
        },
        workbox: {
          navigateFallback: '/index.html',
          globPatterns: ['**/*.{js,css,html,png,svg,webp}'],
          runtimeCaching: [
            {
              urlPattern: /\/media\/.+\.(webp|png|jpe?g)$/i,
              handler: 'CacheFirst',
              options: {
                cacheName: 'khyout-media',
                expiration: { maxEntries: 200, maxAgeSeconds: 60 * 60 * 24 * 30 },
              },
            },
          ],
        },
      }),
    ],
    server: {
      port: 5173,
      proxy: {
        '/api': {
          target: proxyTarget,
          changeOrigin: true,
        },
      },
    },
    test: {
      environment: 'jsdom',
      globals: true,
      setupFiles: ['./src/test/setup.ts'],
      css: false,
    },
  };
});
