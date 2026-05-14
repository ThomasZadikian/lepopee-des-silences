import js from '@eslint/js'
import pluginVue from 'eslint-plugin-vue'
import tseslint from 'typescript-eslint'

export default [
  js.configs.recommended,
  ...pluginVue.configs['flat/recommended'],
  ...tseslint.configs.recommended,
  {
    files: ['src/**/*.{ts,vue}'],
    rules: {
      'no-console': 'warn',
      'no-debugger': 'error',
      '@typescript-eslint/no-explicit-any': 'warn',
      'vue/multi-word-component-names': 'off'
    }
  },
  {
    ignores: ['dist/**', 'node_modules/**', 'coverage/**']
  }
]