<script setup lang="ts">
import { ref, watch } from 'vue';
import type { CombatLogEntryDto } from '../types/combatContracts';

const props = defineProps<{
  entries: CombatLogEntryDto[];
}>();

const logContainer = ref<HTMLElement | null>(null);

watch(
  () => props.entries.length,
  () => {
    if (logContainer.value) {
      logContainer.value.scrollTop = logContainer.value.scrollHeight;
    }
  },
);
</script>

<template>
  <section class="log-panel panel">
    <p class="system-label log-panel__header">JOURNAL DE COMBAT</p>

    <div ref="logContainer" class="log-panel__entries">
      <div
        v-for="(entry, i) in entries"
        :key="i"
        class="log-panel__entry"
        :class="'log-panel__entry--' + entry.type.toLowerCase()"
      >
        <span class="log-panel__entry-type">{{ entry.type }}</span>
        <span class="log-panel__entry-message">{{ entry.message }}</span>
      </div>

      <p v-if="entries.length === 0" class="log-panel__empty">
        En attente de la première action...
      </p>
    </div>
  </section>
</template>

<style scoped>
.log-panel {
  display: grid;
  grid-template-rows: auto minmax(0, 1fr);
  gap: var(--space-2);
  padding: var(--space-3);
  min-height: 0;
  height: 100%;
  max-height: 100%;
  overflow: hidden;
}

.log-panel__header {
  color: var(--color-dim);
  flex-shrink: 0;
}

.log-panel__entries {
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
  min-height: 0;
  max-height: 100%;
}

.log-panel__entry {
  display: grid;
  grid-template-columns: 5.5rem 1fr;
  gap: var(--space-2);
  font-size: 0.75rem;
  font-family: var(--font-mono);
  padding: var(--space-1) 0;
  border-bottom: 1px solid var(--color-line);
  animation: combat-log-entry-in 220ms ease-out;
}

.log-panel__entry:last-child {
  border-bottom: none;
}

.log-panel__entry-type {
  color: var(--color-dim);
  letter-spacing: 0.05em;
}

.log-panel__entry-message {
  color: var(--color-muted);
  word-break: break-word;
}

.log-panel__entry--damageapplied .log-panel__entry-message {
  color: var(--color-blood);
}

.log-panel__entry--guardgained .log-panel__entry-message {
  color: var(--color-frost);
}

.log-panel__entry--targetdefeated .log-panel__entry-message {
  color: var(--color-gold);
}

.log-panel__entry--combatcompleted .log-panel__entry-message {
  color: var(--color-gold);
  font-weight: 600;
}

.log-panel__entry--combatfailed .log-panel__entry-message {
  color: var(--color-blood);
  font-weight: 600;
}

.log-panel__empty {
  color: var(--color-dim);
  font-size: 0.75rem;
  font-style: italic;
}

@keyframes combat-log-entry-in {
  0% {
    opacity: 0;
    transform: translateY(4px);
  }
  100% {
    opacity: 1;
    transform: translateY(0);
  }
}

@media (prefers-reduced-motion: reduce) {
  .log-panel__entry {
    animation: none;
  }
}
</style>
