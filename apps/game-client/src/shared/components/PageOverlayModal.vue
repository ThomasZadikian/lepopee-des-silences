<script setup lang="ts">
defineEmits<{ close: [] }>();
</script>

<template>
  <div class="pom-backdrop" @click.self="$emit('close')">
    <div class="pom-panel">
      <button class="pom-close" type="button" aria-label="Fermer" @click="$emit('close')">✕</button>
      <div class="pom-scroll">
        <slot />
      </div>
    </div>
  </div>
</template>

<style scoped>
.pom-backdrop {
  position: fixed;
  inset: 0;
  z-index: var(--z-modal);
  display: flex;
  align-items: center;
  justify-content: center;
  background: oklch(0.08 0.02 60 / 0.72);
  backdrop-filter: blur(4px);
  padding: var(--space-4);
}

.pom-panel {
  /* Fixed size regardless of content — a short page and a long one both get the
     same 80vw x 80vh frame, with .pom-scroll taking up any slack internally,
     instead of the panel stretching/shrinking to fit whatever's inside it. */
  position: relative;
  width: 80vw;
  height: 80vh;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  border-radius: var(--radius-md);
  border: 1px solid var(--line-strong);
  background: var(--bg-2);
  box-shadow: var(--shadow-deep);
}

.pom-close {
  position: absolute;
  top: 14px;
  right: 14px;
  z-index: 2;
  background: none;
  border: none;
  cursor: pointer;
  color: var(--ink-4);
  font-size: 14px;
  padding: 6px;
  border-radius: 3px;
  transition: color 0.15s;
}

.pom-close:hover {
  color: var(--ink);
}

.pom-scroll {
  overflow-y: auto;
  flex: 1;
  min-height: 0;
}
</style>
