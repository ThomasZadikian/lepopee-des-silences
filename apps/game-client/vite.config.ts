import vue from '@vitejs/plugin-vue'
import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [
    vue({
      template: {
        compilerOptions: {
          // TresCanvas is a real Vue component (imported explicitly) — only the
          // dynamically-extended Three.js catalogue tags (TresMesh, TresPerspectiveCamera,
          // etc.) should bypass Vue's component resolution.
          isCustomElement: (tag) => tag.startsWith('Tres') && tag !== 'TresCanvas',
        },
      },
    }),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
    dedupe: ['@tresjs/core', 'three'],
  },
  optimizeDeps: {
    include: ['@tresjs/core', '@tresjs/cientos', 'three'],
  },
  test: {
    exclude: ['**/node_modules/**', '**/dist/**', '**/e2e/**'],
  },
})