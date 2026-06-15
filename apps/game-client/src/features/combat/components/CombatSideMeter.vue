<script setup lang="ts">
import { ref } from 'vue';
import type { CombatSideMetrics, CombatMeterMetric } from '../composables/useCombatMetrics';
import { metricLabels } from '../composables/useCombatMetrics';

const props = defineProps<{
  title: string;
  side: 'allies' | 'enemies';
  metrics: CombatSideMetrics;
}>();

const selected = ref<CombatMeterMetric>('damageTaken');

const metricOptions: CombatMeterMetric[] = [
  'damageDealt',
  'damageTaken',
  'healingDone',
  'healingReceived',
  'guardAbsorbed',
  'guardGained',
  'netVitalityLoss',
];

function hasGuardData(): boolean {
  return props.metrics.guardAbsorbed > 0 || props.metrics.guardGained > 0;
}
</script>

<template>
  <div class="side-meter">
    <span class="side-meter__title">{{ title }}</span>

    <div class="side-meter__selector">
      <select v-model="selected" class="side-meter__select">
        <option
          v-for="m in metricOptions"
          :key="m"
          :value="m"
          :disabled="(m === 'guardAbsorbed' || m === 'guardGained') && !hasGuardData()"
        >
          {{ metricLabels[m] }}
        </option>
      </select>
    </div>

    <div class="side-meter__value">
      {{ metrics[selected] }}
    </div>
  </div>
</template>

<style scoped>
.side-meter {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
  padding: var(--space-2) var(--space-3);
  background: var(--card-soft);
  border: 1px solid var(--line-soft);
  border-radius: var(--radius-sm);
  min-width: 140px;
}

.side-meter__title {
  font-family: var(--font-caps);
  font-size: 0.52rem;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.side-meter__select {
  width: 100%;
  padding: 2px var(--space-1);
  background: var(--panel);
  border: 1px solid var(--line-soft);
  border-radius: 2px;
  color: var(--ink-3);
  font-family: var(--font);
  font-size: 0.72rem;
  cursor: pointer;
  appearance: none;
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='8' height='4'%3E%3Cpath d='M0 0l4 4 4-4' fill='none' stroke='%23808098' stroke-width='1'/%3E%3C/svg%3E");
  background-repeat: no-repeat;
  background-position: right 6px center;
  padding-right: 18px;
}

.side-meter__select:focus {
  outline: none;
  border-color: var(--edge-frost);
}

.side-meter__value {
  font-family: var(--font-display);
  font-size: 1.8rem;
  font-weight: 600;
  color: var(--ink);
  line-height: 1;
}
</style>
