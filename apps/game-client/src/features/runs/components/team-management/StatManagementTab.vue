<script setup lang="ts">
import type { PlayerCharacterView, PlayerStatKind } from '../../../party/types/playerTypes';
import { usePlayerStore } from '../../../party/stores/playerStore';
import { statDescriptions, statLabels, statOrder, statValue } from '../../../party/constants/statDescriptions';

const props = defineProps<{ character: PlayerCharacterView }>();

const playerStore = usePlayerStore();

function spendPoint(stat: PlayerStatKind) {
  if (playerStore.isLoading || playerStore.unspentStatPoints <= 0) return;
  playerStore.spendStatPoint(props.character.id, stat);
}
</script>

<template>
  <div class="smt-root">
    <header class="smt-header">
      <span class="es-label">Points disponibles</span>
      <span class="smt-points">{{ playerStore.unspentStatPoints }}</span>
    </header>

    <ul class="smt-list">
      <li v-for="stat in statOrder" :key="stat" class="smt-row">
        <div class="smt-row__info">
          <span class="smt-row__label">{{ statLabels[stat] }}</span>
          <p class="smt-row__desc">{{ statDescriptions[stat] }}</p>
        </div>
        <span class="smt-row__value">{{ statValue(character.stats, stat) }}</span>
        <button
          type="button"
          class="smt-row__add"
          :disabled="playerStore.isLoading || playerStore.unspentStatPoints <= 0"
          @click="spendPoint(stat)"
        >
          +1
        </button>
      </li>
    </ul>

    <p v-if="playerStore.error" class="smt-error">{{ playerStore.error }}</p>
  </div>
</template>

<style scoped>
.smt-root {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.smt-header {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  padding-bottom: 10px;
  border-bottom: 1px solid var(--line-soft);
}

.smt-points {
  font-family: var(--font-mono, monospace);
  font-size: 20px;
  color: var(--gold);
}

.smt-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.smt-row {
  display: grid;
  grid-template-columns: 1fr auto auto;
  align-items: center;
  gap: 14px;
  padding: 10px 0;
  border-bottom: 1px solid var(--line-soft);
}

.smt-row__info {
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.smt-row__label {
  font-size: 13px;
  font-weight: 600;
  color: var(--ink-2);
}

.smt-row__desc {
  margin: 0;
  font-size: 12px;
  line-height: 1.4;
  color: var(--ink-4);
  max-width: 60ch;
}

.smt-row__value {
  font-family: var(--font-mono, monospace);
  font-size: 13px;
  color: var(--ink-3);
}

.smt-row__add {
  width: 28px;
  height: 28px;
  border-radius: 4px;
  border: 1px solid var(--frost);
  background: oklch(0.50 0.06 232 / 0.12);
  color: var(--frost);
  font-family: var(--font-mono, monospace);
  font-size: 13px;
  cursor: pointer;
  transition: opacity 0.15s, background 0.15s;
}

.smt-row__add:disabled {
  opacity: 0.3;
  cursor: not-allowed;
}

.smt-row__add:not(:disabled):hover {
  background: oklch(0.50 0.06 232 / 0.22);
}

.smt-error {
  font-family: var(--font-mono, monospace);
  font-size: 11px;
  color: var(--blood);
  margin: 0;
}
</style>
