import vue from '@vitejs/plugin-vue';
import vuetify from 'vite-plugin-vuetify';
import { defineConfig } from 'vitest/config';
export default defineConfig({
    plugins: [vue(), vuetify({ autoImport: true })],
    resolve: { alias: { '@': '/src' } },
    test: {
        globals: true,
        environment: 'jsdom',
        setupFiles: ['./src/tests/setup.ts'],
        css: false,
        server: {
            deps: {
                inline: ['vuetify']
            }
        },
        coverage: {
            provider: 'v8',
            reporter: ['text', 'lcov'],
            include: ['src/**/*.{ts,vue}'],
            exclude: [
                'node_modules/**',
                '**/*.d.ts',
                'src/tests/**',
                'src/main.ts',
                'src/api/**',
                'src/router/index.ts',
                'src/plugins/**',
                'src/views/admin/**',
                'src/components/HelloWorld.vue',
                'src/interfaces/**',
                'src/components/common/**',
                'src/main.ts',
                'src/components/FoxSprite.vue',
                'App.vue',
            ], // tu avais une typo ici aussi
            thresholds: { lines: 60 },
        }
    }
});
