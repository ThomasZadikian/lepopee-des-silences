<script setup lang="ts">
withDefaults(defineProps<{ text: string; placement?: 'top' | 'bottom' }>(), {
  placement: 'top',
});
</script>

<template>
  <span class="stat-tooltip" :class="`stat-tooltip--${placement}`" tabindex="0">
    <slot />
    <span class="stat-tooltip__bubble" role="tooltip">{{ text }}</span>
  </span>
</template>

<style scoped>
.stat-tooltip {
  position: relative;
  display: inline-flex;
  align-items: center;
  cursor: help;
  border-bottom: 1px dotted var(--ink-5, oklch(0.45 0.015 275));
  outline: none;
}

.stat-tooltip__bubble {
  position: absolute;
  left: 50%;
  transform: translateX(-50%) translateY(4px);
  min-width: 190px;
  max-width: 260px;
  width: max-content;
  padding: 9px 11px;
  border-radius: 5px;
  border: 1px solid var(--line-strong, oklch(0.42 0.03 60 / 0.7));
  background: oklch(0.16 0.02 270 / 0.97);
  color: var(--ink-2, oklch(0.78 0.02 275));
  font-family: var(--font, serif);
  font-size: 11.5px;
  font-weight: 400;
  line-height: 1.45;
  letter-spacing: normal;
  text-transform: none;
  text-align: left;
  white-space: normal;
  box-shadow: 0 8px 24px -6px oklch(0.1 0.02 270 / 0.6);
  opacity: 0;
  visibility: hidden;
  pointer-events: none;
  transition: opacity 0.15s ease, transform 0.15s ease;
  z-index: 60;
}

.stat-tooltip--top .stat-tooltip__bubble {
  bottom: 100%;
  transform: translateX(-50%) translateY(4px);
}

.stat-tooltip--bottom .stat-tooltip__bubble {
  top: 100%;
  transform: translateX(-50%) translateY(-4px);
}

.stat-tooltip:hover .stat-tooltip__bubble,
.stat-tooltip:focus-visible .stat-tooltip__bubble {
  opacity: 1;
  visibility: visible;
  transform: translateX(-50%) translateY(0);
}

@media (prefers-reduced-motion: reduce) {
  .stat-tooltip__bubble {
    transition: none;
  }
}
</style>
