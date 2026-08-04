<script setup lang="ts">
defineProps<{
  message?: string | null;
}>();
</script>

<template>
  <aside v-if="message" class="elise-overlay">
    <div class="elise-overlay__frame">
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
  padding: var(--space-3) var(--space-4);
  background: var(--panel);
  backdrop-filter: blur(6px);
  border-left: 2px solid var(--mint-dim);
}

.elise-overlay__name {
  font-family: var(--font-mono);
  font-size: 10px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--mint-dim);
  margin: 0 0 var(--space-1);
}

.elise-overlay__text {
  font-family: var(--font);
  font-size: 0.88rem;
  line-height: 1.5;
  color: var(--ink-2);
  margin: 0;
  font-style: italic;
}
</style>
