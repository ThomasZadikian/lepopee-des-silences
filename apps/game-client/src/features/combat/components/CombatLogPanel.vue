<script setup lang="ts">
import { ref, watch } from 'vue';
import type { CombatLogEntryDto } from '../types/combatContracts';

const props = defineProps<{
  entries: CombatLogEntryDto[];
}>();

const isOpen = ref(false);
const logContainer = ref<HTMLElement | null>(null);

watch(() => props.entries.length, () => {
  if (logContainer.value && isOpen.value) {
    logContainer.value.scrollTop = logContainer.value.scrollHeight;
  }
});

function toggle() {
  isOpen.value = !isOpen.value;
}
</script>

<template>
  <div class="combat-log" :class="{ 'combat-log--open': isOpen }">
    <button class="combat-log__tab" @click="toggle">
      <span class="es-label">Journal de combat</span>
      <span class="es-mono">{{ entries.length }}</span>
      <span class="combat-log__chevron">{{ isOpen ? '▾' : '▴' }}</span>
    </button>

    <div v-show="isOpen" ref="logContainer" class="combat-log__entries">
      <div
        v-for="(entry, i) in entries"
        :key="i"
        class="combat-log__entry"
        :class="'combat-log__entry--' + entry.type.toLowerCase()"
      >
        <span class="combat-log__type">{{ entry.type }}</span>
        <span class="combat-log__msg">{{ entry.message }}</span>
      </div>

      <p v-if="entries.length === 0" class="combat-log__empty">En attente de la première action…</p>
    </div>
  </div>
</template>

<style scoped>
.combat-log {
  border-top: 1px solid var(--line-soft);
}

.combat-log__tab {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  width: 100%;
  padding: var(--space-1) var(--space-4);
  background: transparent;
  border: none;
  cursor: pointer;
  font-family: inherit;
  color: var(--ink-3);
}

.combat-log__tab:hover {
  background: var(--card-soft);
}

.combat-log__chevron {
  margin-left: auto;
  font-size: 0.8rem;
  color: var(--ink-4);
}

.combat-log__entries {
  max-height: 10rem;
  overflow-y: auto;
  padding: 0 var(--space-4) var(--space-2);
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
}

.combat-log__entry {
  display: grid;
  grid-template-columns: 5.5rem 1fr;
  gap: var(--space-2);
  font-family: var(--font-mono);
  font-size: 0.72rem;
  padding: var(--space-1) 0;
  border-bottom: 1px solid var(--line-soft);
  animation: log-in 220ms ease-out;
}

.combat-log__entry:last-child {
  border-bottom: none;
}

.combat-log__type {
  color: var(--ink-4);
  letter-spacing: 0.05em;
}

.combat-log__msg {
  margin-left: 5rem;
  color: var(--ink-3);
  word-break: break-word;
}

.combat-log__entry--damageapplied .combat-log__msg { color: var(--blood); }
.combat-log__entry--guardgained .combat-log__msg { color: var(--frost); }
.combat-log__entry--targetdefeated .combat-log__msg { color: var(--gold); }
.combat-log__entry--combatcompleted .combat-log__msg { color: var(--gold); font-weight: 600; }
.combat-log__entry--combatfailed .combat-log__msg { color: var(--blood); font-weight: 600; }
.combat-log__entry--criticalhit .combat-log__msg { color: var(--gold); font-weight: 700; letter-spacing: 0.02em; }
.combat-log__entry--weaknesshit .combat-log__msg { color: var(--sap); font-weight: 600; }
.combat-log__entry--resistedhit .combat-log__msg { color: var(--ink-5); }
.combat-log__entry--immunehit  .combat-log__msg { color: var(--ink-5); font-style: italic; }

.combat-log__empty {
  color: var(--ink-4);
  font-size: 0.72rem;
  font-style: italic;
}

@keyframes log-in {
  0% { opacity: 0; transform: translateY(4px); }
  100% { opacity: 1; transform: translateY(0); }
}

@media (prefers-reduced-motion: reduce) {
  .combat-log__entry { animation: none; }
}
</style>
