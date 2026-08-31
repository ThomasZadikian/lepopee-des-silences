import vue from '@vitejs/plugin-vue'
import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vitest/config'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  test: {
    exclude: ['**/node_modules/**', '**/dist/**', '**/e2e/**'],
    coverage: {
      provider: 'v8',
      reportsDirectory: 'coverage',
      reporter: ['text', 'json-summary', 'lcov', 'cobertura'],
      include: ['src/**/*.{ts,vue}'],
      exclude: [
        'src/**/*.d.ts',
        'src/**/*.test.ts',
        'src/**/testFixtures.ts',
        'src/tests/**',
        'src/main.ts',
        // Canvas renderers and route composition roots are integration surfaces. Their
        // behaviour is exercised by component/Playwright tests; counting every draw branch
        // in the unit gate would reward brittle canvas mocks instead of domain assertions.
        'src/features/combat/components/TacticalCombatScene.vue',
        'src/features/palace-map/TacticalGridMap.vue',
        'src/features/palace-map/composables/sorts.ts',
        'src/pages/RunPage.vue',
      ],
      thresholds: {
        // 80% is the hard CI contract. We still target >=85% during development, but
        // a result between 80% and 85% must remain green and is tracked as internal headroom.
        lines: 80,
        branches: 80,
        functions: 80,
        statements: 80,
      },
    },
  },
})
