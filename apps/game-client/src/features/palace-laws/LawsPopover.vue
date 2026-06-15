<script setup lang="ts">
import type { ActivePalaceLawDto } from '../../runs/types/runTypes';

defineProps<{
  laws?: ActivePalaceLawDto[] | null;
}>();

const emit = defineEmits<{
  close: [];
}>();
</script>

<template>
  <div class="laws-popover" @keydown.escape="emit('close')">
    <div class="laws-popover__header">
      <span class="es-kicker">Lois du Palais</span>
      <button class="es-btn es-btn--ghost" @click="emit('close')">✕</button>
    </div>

    <hr class="es-rule" />

    <div v-if="!laws || laws.length === 0" class="laws-popover__empty">
      <p class="es-body">Aucune loi active pour cette run.</p>
    </div>

    <div v-else class="laws-popover__list">
      <article v-for="law in laws" :key="`${law.key}-${law.version}`" class="laws-popover__law">
        <strong class="laws-popover__law-name">{{ law.displayName }}</strong>
        <p v-if="law.description" class="laws-popover__law-desc">{{ law.description }}</p>
        <div class="laws-popover__law-meta">
          <span v-for="domain in law.domains" :key="domain" class="es-chip">{{ domain }}</span>
        </div>
      </article>
    </div>
  </div>
</template>

<style scoped>
.laws-popover {
  position: absolute;
  top: 0;
  right: 0;
  bottom: 40px;
  width: 300px;
  display: flex;
  flex-direction: column;
  padding: var(--space-4);
  background: oklch(0.22 0.04 272 / 0.92);
  border-left: 1px solid var(--line-soft);
  backdrop-filter: blur(8px);
  overflow-y: auto;
  z-index: var(--z-drawer);
}

.laws-popover__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.laws-popover__empty {
  padding: var(--space-4) 0;
}

.laws-popover__list {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.laws-popover__law {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
  padding: var(--space-3);
  background: var(--card-soft);
  border: 1px solid var(--line-soft);
  border-radius: var(--radius-sm);
}

.laws-popover__law-name {
  font-family: var(--font-display);
  font-size: 0.95rem;
  color: var(--gold);
}

.laws-popover__law-desc {
  margin: 0;
  font-size: 0.82rem;
  color: var(--ink-3);
  line-height: 1.5;
}

.laws-popover__law-meta {
  display: flex;
  gap: var(--space-1);
  flex-wrap: wrap;
  margin-top: var(--space-1);
}
</style>
