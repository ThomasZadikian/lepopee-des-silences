<script setup lang="ts">
defineProps<{ currentPage: number; totalPages: number }>();
const emit = defineEmits<{ previous: []; next: [] }>();
</script>

<template>
  <div class="grimoire-pagination">
    <button
      type="button"
      class="grimoire-page-btn"
      :disabled="currentPage <= 1"
      @click="emit('previous')"
    >
      ‹ Précédent
    </button>
    <span class="grimoire-page-indicator">Page {{ currentPage }} / {{ totalPages }}</span>
    <button
      type="button"
      class="grimoire-page-btn"
      :disabled="currentPage >= totalPages"
      @click="emit('next')"
    >
      Suivant ›
    </button>
  </div>
</template>

<style scoped>
.grimoire-pagination {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 16px;
  padding-top: 8px;
}

.grimoire-page-btn {
  font-family: var(--font-caps, var(--font));
  font-size: 10px;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  padding: 6px 14px;
  border-radius: 4px;
  border: 1px solid var(--line-soft);
  background: transparent;
  color: var(--ink-3);
  cursor: pointer;
  transition: opacity 0.15s, border-color 0.15s, color 0.15s;
}

.grimoire-page-btn:disabled {
  opacity: 0.35;
  cursor: not-allowed;
}

.grimoire-page-btn:not(:disabled):hover {
  border-color: var(--ink-3);
  color: var(--ink-2);
}

.grimoire-page-indicator {
  font-family: var(--font-mono, monospace);
  font-size: 12px;
  color: var(--ink-3);
}
</style>
