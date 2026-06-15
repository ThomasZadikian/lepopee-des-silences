<script setup lang="ts">
defineProps<{
  message?: string | null;
}>();
</script>

<template>
  <aside v-if="message" class="elise-overlay">
    <div class="elise-overlay__frame">
      <div class="es-corner es-corner--frost tl" />
      <div class="es-corner es-corner--frost tr" />
      <div class="es-corner es-corner--frost bl" />
      <div class="es-corner es-corner--frost br" />
      <p class="elise-overlay__name">Elise</p>
      <p class="elise-overlay__text">{{ message }}</p>
    </div>
  </aside>
</template>

<style scoped>
.elise-overlay {
  position: absolute;
  left: var(--space-4);
  bottom: calc(var(--space-8) + 40px);
  max-width: 32rem;
  z-index: var(--z-popover);
  pointer-events: none;
}

.elise-overlay__frame {
  position: relative;
  padding: var(--space-3) var(--space-4);
  background: oklch(0.22 0.04 272 / 0.85);
  backdrop-filter: blur(6px);
  border: 1px solid var(--line-soft);
  border-radius: var(--radius-sm);
}

.elise-overlay__name {
  font-family: var(--font-caps);
  font-size: 0.56rem;
  letter-spacing: 0.22em;
  text-transform: uppercase;
  color: var(--frost-dim);
  margin: 0 0 var(--space-1);
}

.elise-overlay__text {
  font-family: var(--font);
  font-size: 0.88rem;
  line-height: 1.5;
  color: var(--ink-2);
  margin: 0;
}
</style>
