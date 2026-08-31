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
        // Keep a single quality contract across the frontend: every principal coverage
        // dimension must retain at least 85%, providing headroom above the former 80% gate.
        lines: 85,
        branches: 85,
        functions: 85,
        statements: 85,
      },
    },
  },
})
