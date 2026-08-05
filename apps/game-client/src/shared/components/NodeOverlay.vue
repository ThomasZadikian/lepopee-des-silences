<script setup lang="ts">
withDefaults(defineProps<{
  /** Controls the stage's max width — echo (confirmation) < card (default resolution) < wide (grids) < dialogue (full-bleed, positions itself). */
  size?: 'echo' | 'card' | 'wide' | 'dialogue';
}>(), { size: 'card' });
</script>

<template>
  <div class="node-overlay" role="presentation">
    <div class="node-overlay__backdrop" aria-hidden="true" />
    <div class="node-overlay__stage" :class="`node-overlay__stage--${size}`">
      <slot />
    </div>
  </div>
</template>

<style scoped>
.node-overlay {
  position: fixed;
  inset: 0;
  z-index: var(--z-overlay, 40);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px;
}

.node-overlay__backdrop {
  position: absolute;
  inset: 0;
  background: rgba(12, 13, 18, 0.72);
  backdrop-filter: blur(4px);
  animation: node-overlay-fade 0.4s ease;
}

.node-overlay__stage {
  position: relative;
  max-height: min(86vh, 780px);
  overflow-y: auto;
  animation: node-overlay-rise 0.4s cubic-bezier(0.5, 0, 0.5, 1);
}

.node-overlay__stage--echo { width: min(420px, 92vw); }
.node-overlay__stage--card { width: min(640px, 92vw); }
.node-overlay__stage--wide { width: min(900px, 94vw); }
.node-overlay__stage--dialogue {
  width: 100%;
  height: 100%;
  max-height: none;
  overflow: visible;
}

@keyframes node-overlay-fade {
  from { opacity: 0; }
  to { opacity: 1; }
}

@keyframes node-overlay-rise {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: none; }
}

@media (prefers-reduced-motion: reduce) {
  .node-overlay__backdrop,
  .node-overlay__stage {
    animation: none;
  }
}
</style>
