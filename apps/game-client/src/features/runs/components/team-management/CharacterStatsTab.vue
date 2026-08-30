<script setup lang="ts">
import { computed } from 'vue';
import type { PlayerCharacterView, PlayerStatKind } from '../../../party/types/playerTypes';
import {
  statDescriptions,
  statLabels,
  statOrder,
  statValue,
} from '../../../party/constants/statDescriptions';
import StatRadarChart from './StatRadarChart.vue';

const props = defineProps<{ character: PlayerCharacterView }>();

const radarValues = computed(() => {
  const values = {} as Record<PlayerStatKind, number>;
  for (const stat of statOrder) values[stat] = statValue(props.character.stats, stat);
  return values;
});

</script>

<template>
  <div class="cst-root">
    <header class="cst-header">
      <span class="es-label">Caractéristiques effectives</span>
      <small>Définies par le personnage, son équipement et les effets de Run.</small>
    </header>

    <div class="cst-tactical-resources">
      <div class="cst-resource">
        <span class="es-label">Déplacement</span>
        <strong>{{ character.stats.movement ?? 4 }}</strong>
        <small>Base tactique, modifiée par l’équipement et les statuts.</small>
      </div>
      <div class="cst-resource">
        <span class="es-label">Charge</span>
        <strong>0 / 5</strong>
        <small>Jauge de combat fixe, remise à zéro après chaque affrontement.</small>
      </div>
    </div>

    <div class="cst-body">
      <StatRadarChart :values="radarValues" class="cst-radar" />

      <ul class="cst-list">
        <li v-for="stat in statOrder" :key="stat" class="cst-row">
          <div class="cst-row__info">
            <span class="cst-row__label">{{ statLabels[stat] }}</span>
            <p class="cst-row__desc">{{ statDescriptions[stat] }}</p>
          </div>
          <span class="cst-row__value">
            {{ statValue(character.stats, stat) }}
          </span>
        </li>
      </ul>
    </div>
  </div>
</template>

<style scoped>
.cst-root {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.cst-header {
  display: flex;
  align-items: baseline;
  gap: 12px;
  padding-bottom: 10px;
  border-bottom: 1px solid var(--line-soft);
}

.cst-points {
  font-family: var(--font-mono);
  font-size: 20px;
  color: var(--mint-dim);
}

.cst-actions {
  margin-left: auto;
  display: flex;
  gap: 8px;
}

.cst-btn {
  font-family: var(--font-mono);
  font-size: 10px;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  padding: 6px 14px;
  border: 1px solid var(--line-soft);
  background: transparent;
  color: var(--ink-3);
  cursor: pointer;
  transition: opacity 0.15s, border-color 0.15s, color 0.15s;
}

.cst-btn:disabled {
  opacity: 0.35;
  cursor: not-allowed;
}

.cst-btn--ghost:not(:disabled):hover {
  border-color: var(--ink-3);
  color: var(--ink-2);
}

.cst-btn--primary {
  border-color: var(--mint-dim);
  color: var(--mint-dim);
}

.cst-btn--primary:not(:disabled):hover {
  background: var(--panel-2);
}

.cst-body {
  display: grid;
  grid-template-columns: minmax(240px, 340px) 1fr;
  gap: 24px;
  align-items: start;
}

.cst-tactical-resources {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px;
}

.cst-resource {
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 4px 12px;
  padding: 10px 12px;
  border: 1px solid var(--line-soft);
  background: var(--panel-2);
}

.cst-resource strong {
  color: var(--mint-dim);
  font-family: var(--font-mono);
}

.cst-resource small {
  grid-column: 1 / -1;
  color: var(--ink-4);
  font-size: 11px;
}

.cst-radar {
  position: sticky;
  top: 0;
}

.cst-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.cst-row {
  display: grid;
  grid-template-columns: 1fr auto auto;
  align-items: center;
  gap: 14px;
  padding: 10px 0;
  border-bottom: 1px solid var(--line-soft);
}

.cst-row__info {
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.cst-row__label {
  font-size: 13px;
  font-weight: 600;
  color: var(--ink-2);
}

.cst-row__desc {
  margin: 0;
  font-size: 12px;
  line-height: 1.4;
  color: var(--ink-4);
  max-width: 60ch;
}

.cst-row__value {
  font-family: var(--font-mono);
  font-size: 13px;
  color: var(--ink-3);
  white-space: nowrap;
}

.cst-row__pending {
  color: var(--mint-dim);
}

.cst-row__controls {
  display: flex;
  align-items: center;
  gap: 6px;
}

.cst-row__staged {
  min-width: 14px;
  text-align: center;
  font-family: var(--font-mono);
  font-size: 12px;
  color: var(--mint-dim);
}

.cst-row__step {
  min-width: 28px;
  height: 28px;
  padding: 0 6px;
  border: 1px solid var(--mint-dim);
  background: transparent;
  color: var(--mint-dim);
  font-family: var(--font-mono);
  font-size: 13px;
  cursor: pointer;
  transition: opacity 0.15s, background 0.15s;
}

.cst-row__step:disabled {
  opacity: 0.3;
  cursor: not-allowed;
}

.cst-row__step:not(:disabled):hover {
  background: var(--panel-2);
}

.cst-error {
  font-family: var(--font-mono);
  font-size: 11px;
  color: var(--danger-dim);
  margin: 0;
}

@media (max-width: 720px) {
  .cst-body {
    grid-template-columns: 1fr;
  }

  .cst-radar {
    position: static;
  }
}
</style>
