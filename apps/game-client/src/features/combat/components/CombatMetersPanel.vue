<script setup lang="ts">
import { computed, ref } from 'vue';
import type { CombatRuntimeDto } from '../types/combatContracts';
import type { CombatMetricsState } from '../composables/useCombatMetrics';

const props = defineProps<{
  metrics: CombatMetricsState;
  combat: CombatRuntimeDto | null;
}>();

type Row = {
  id: string;
  name: string;
  value: number;
  percent: number;
};

type ReportMetric = 'damageDealt' | 'guardAbsorbed' | 'healingDone' | 'turnsTaken';

const metricOptions: { key: ReportMetric; label: string; footer: string; suffix: string }[] = [
  { key: 'damageDealt', label: 'Verbe infligé', footer: 'Dégâts portés à travers la faille', suffix: '' },
  { key: 'guardAbsorbed', label: 'Silence encaissé', footer: 'Dégâts absorbés par le choeur', suffix: '' },
  { key: 'healingDone', label: 'Mémoire rendue', footer: 'PV & PP restitués aux voix', suffix: ' PV' },
  { key: 'turnsTaken', label: 'Tours pris', footer: 'Initiatives jouées ce combat', suffix: '×' },
];

const allyMetric = ref<ReportMetric>('damageDealt');
const enemyMetric = ref<ReportMetric>('damageDealt');

function optionFor(metric: ReportMetric) {
  return metricOptions.find((option) => option.key === metric) ?? metricOptions[0];
}

function rowsFor(side: 'Player' | 'Enemy', metric: ReportMetric): Row[] {
  const combatants = side === 'Player'
    ? props.combat?.allies ?? []
    : props.combat?.enemies ?? [];

  const rows = combatants.map((combatant) => ({
    id: combatant.id,
    name: combatant.displayName,
    value: props.metrics.contributions[combatant.id]?.[metric] ?? 0,
    percent: 0,
  }));

  const max = Math.max(1, ...rows.map((row) => row.value));
  return rows
    .map((row) => ({ ...row, percent: Math.round((row.value / max) * 100) }))
    .sort((a, b) => b.value - a.value);
}

const allyRows = computed(() => rowsFor('Player', allyMetric.value));
const enemyRows = computed(() => rowsFor('Enemy', enemyMetric.value));
const allyTotal = computed(() => allyRows.value.reduce((total, row) => total + row.value, 0));
const enemyTotal = computed(() => enemyRows.value.reduce((total, row) => total + row.value, 0));
const allyOption = computed(() => optionFor(allyMetric.value));
const enemyOption = computed(() => optionFor(enemyMetric.value));
</script>

<template>
  <section class="damage-report">
    <header class="damage-report__header">
      <span class="damage-report__title">△ Le relevé · Contributions du tour {{ combat?.turnNumber ?? '—' }}</span>
      <span class="damage-report__hint">Chaque camp suit sa propre métrique</span>
    </header>

    <div class="damage-report__body">
      <article class="damage-report__side damage-report__side--allies">
        <div class="damage-report__side-head">
          <span>◆ Les Voix · {{ combat?.allies.length ?? 0 }}</span>
          <label class="damage-report__select">
            △
            <select v-model="allyMetric">
              <option v-for="option in metricOptions" :key="option.key" :value="option.key">
                {{ option.label }}
              </option>
            </select>
          </label>
        </div>

        <ol class="damage-report__rows">
          <li v-for="(row, index) in allyRows" :key="row.id" class="damage-row">
            <span class="damage-row__rank">{{ index + 1 }}</span>
            <span class="damage-row__name">{{ row.name }}</span>
            <span class="damage-row__bar"><span :style="{ width: row.percent + '%' }" /></span>
            <span class="damage-row__value">{{ row.value }}</span>
            <span class="damage-row__pct">{{ row.percent }}%</span>
          </li>
        </ol>

        <footer class="damage-report__footer">
          <span>{{ allyOption.footer }}</span>
          <strong>Total {{ allyTotal }}{{ allyOption.suffix }}</strong>
        </footer>
      </article>

      <article class="damage-report__side damage-report__side--enemies">
        <div class="damage-report__side-head">
          <span>◆ Les Manifestations · {{ combat?.enemies.length ?? 0 }}</span>
          <label class="damage-report__select">
            △
            <select v-model="enemyMetric">
              <option v-for="option in metricOptions" :key="option.key" :value="option.key">
                {{ option.label }}
              </option>
            </select>
          </label>
        </div>

        <ol class="damage-report__rows">
          <li v-for="(row, index) in enemyRows" :key="row.id" class="damage-row">
            <span class="damage-row__rank">{{ index + 1 }}</span>
            <span class="damage-row__name">{{ row.name }}</span>
            <span class="damage-row__bar"><span :style="{ width: row.percent + '%' }" /></span>
            <span class="damage-row__value">{{ row.value }}</span>
            <span class="damage-row__pct">{{ row.percent }}%</span>
          </li>
        </ol>

        <footer class="damage-report__footer">
          <span>{{ enemyOption.footer }}</span>
          <strong>Total {{ enemyTotal }}{{ enemyOption.suffix }}</strong>
        </footer>
      </article>
    </div>
  </section>
</template>

<style scoped>
.damage-report {
  border: 1px solid var(--line-soft);
  background: linear-gradient(180deg, oklch(0.2 0.04 272 / 0.98), oklch(0.17 0.04 272 / 0.98));
}

.damage-report__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-4);
  padding: var(--space-3) var(--space-4);
  border-bottom: 1px solid var(--line-soft);
}

.damage-report__title,
.damage-report__hint,
.damage-report__side-head,
.damage-report__footer {
  font-family: var(--font-caps);
  letter-spacing: 0.18em;
  text-transform: uppercase;
}

.damage-report__title {
  font-size: 0.62rem;
  color: var(--ink-3);
}

.damage-report__hint {
  font-size: 0.52rem;
  color: var(--ink-5);
}

.damage-report__body {
  display: grid;
  grid-template-columns: 1fr 1fr;
}

.damage-report__side {
  padding: var(--space-4);
}

.damage-report__side + .damage-report__side {
  border-left: 1px solid var(--line-soft);
}

.damage-report__side-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: var(--space-3);
  margin-bottom: var(--space-3);
  font-size: 0.58rem;
  color: var(--frost);
}

.damage-report__side--enemies .damage-report__side-head {
  color: var(--blood);
}

.damage-report__select {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  border: 1px solid var(--line-soft);
  border-radius: 3px;
  padding: 0 var(--space-2);
  color: var(--ink-4);
  background: oklch(0.18 0.035 272 / 0.65);
}

.damage-report__select select {
  min-height: 1.6rem;
  border: none;
  background: transparent;
  color: inherit;
  font: inherit;
  letter-spacing: inherit;
  text-transform: inherit;
  cursor: pointer;
}

.damage-report__select select:focus {
  outline: none;
}

.damage-report__rows {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
  list-style: none;
  margin: 0;
  padding: 0;
}

.damage-row {
  display: grid;
  grid-template-columns: 1.2rem 7rem minmax(7rem, 1fr) 3rem 2.4rem;
  align-items: center;
  gap: var(--space-2);
}

.damage-row__rank,
.damage-row__value,
.damage-row__pct {
  font-family: var(--font-mono);
  font-size: 0.62rem;
  color: var(--ink-5);
}

.damage-row__name {
  font-family: var(--font);
  font-size: 0.72rem;
  color: var(--ink-2);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.damage-row__bar {
  height: 7px;
  background: oklch(0.08 0.02 272 / 0.9);
  border-radius: 999px;
  overflow: hidden;
}

.damage-row__bar span {
  display: block;
  height: 100%;
  min-width: 2px;
  background: linear-gradient(90deg, var(--blood-dim), var(--blood));
  border-radius: inherit;
}

.damage-report__footer {
  display: flex;
  justify-content: space-between;
  gap: var(--space-3);
  margin-top: var(--space-4);
  padding-top: var(--space-3);
  border-top: 1px solid var(--line-soft);
  font-size: 0.56rem;
  color: var(--ink-5);
}

.damage-report__footer strong {
  color: var(--ink-2);
}

@media (max-width: 900px) {
  .damage-report__body {
    grid-template-columns: 1fr;
  }

  .damage-report__side + .damage-report__side {
    border-left: none;
    border-top: 1px solid var(--line-soft);
  }

  .damage-row {
    grid-template-columns: 1rem 5rem minmax(5rem, 1fr) 2.5rem;
  }

  .damage-row__pct {
    display: none;
  }
}
</style>
