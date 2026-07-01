<script setup lang="ts">
import { computed, ref } from 'vue';
import type { CombatRuntimeDto } from '../types/combatContracts';
import type { CombatMetricsState } from '../composables/useCombatMetrics';

const props = defineProps<{
  metrics: CombatMetricsState;
  combat: CombatRuntimeDto | null;
}>();

type ReportMetric = 'damageDealt' | 'damageTaken' | 'guardAbsorbed' | 'healingDone' | 'healingReceived' | 'turnsTaken';

type Row = {
  id: string;
  name: string;
  value: number;
  percent: number;
  guardPercent: number;
};

const metricOptions: { key: ReportMetric; icon: string; label: string; footer: string; suffix: string }[] = [
  { key: 'damageDealt',     icon: '△', label: 'Dégâts infligés',  footer: 'Dégâts portés à travers la faille', suffix: '' },
  { key: 'damageTaken',     icon: '▽', label: 'Dégâts subis',     footer: 'Dégâts subis',                      suffix: '' },
  { key: 'guardAbsorbed',   icon: '◇', label: 'Dégâts absorbés',  footer: 'Dégâts absorbés par la garde',      suffix: '' },
  { key: 'healingDone',     icon: '◈', label: 'Soins offert',     footer: 'PV & PP restitués',                 suffix: ' PV' },
  { key: 'healingReceived', icon: '◉', label: 'Soins reçus',      footer: 'PV & PP reçus',                    suffix: ' PV' },
  { key: 'turnsTaken',      icon: '≡', label: 'Tours pris',       footer: 'Initiatives jouées ce combat',     suffix: '×' },
];

const allyMetric  = ref<ReportMetric>('damageDealt');
const enemyMetric = ref<ReportMetric>('damageDealt');
const allyOpen    = ref(false);
const enemyOpen   = ref(false);

function selectAlly(key: ReportMetric)  { allyMetric.value  = key; allyOpen.value  = false; }
function selectEnemy(key: ReportMetric) { enemyMetric.value = key; enemyOpen.value = false; }
function closeAll() { allyOpen.value = false; enemyOpen.value = false; }

function optionFor(metric: ReportMetric) {
  return metricOptions.find((o) => o.key === metric) ?? metricOptions[0];
}

function rowsFor(side: 'Player' | 'Enemy', metric: ReportMetric): Row[] {
  const combatants = side === 'Player'
    ? props.combat?.allies ?? []
    : props.combat?.enemies ?? [];

  const raw = combatants.map((c) => {
    const contrib = props.metrics.contributions[c.id];
    const value = contrib?.[metric] ?? 0;
    const damageToGuard = metric === 'damageDealt' ? (contrib?.damageToGuard ?? 0) : 0;
    return { id: c.id, name: c.displayName, value, damageToGuard };
  });

  const max = Math.max(1, ...raw.map((r) => r.value));
  return raw
    .map((r) => ({
      id: r.id,
      name: r.name,
      value: r.value,
      percent: Math.round((r.value / max) * 100),
      guardPercent: Math.round((r.damageToGuard / max) * 100),
    }))
    .sort((a, b) => b.value - a.value);
}

function barFillClass(metric: ReportMetric): string {
  if (metric === 'guardAbsorbed')                                return 'bar-fill--frost';
  if (metric === 'healingDone' || metric === 'healingReceived')  return 'bar-fill--gold';
  if (metric === 'turnsTaken')                                   return 'bar-fill--ink';
  return 'bar-fill--blood';
}

const allyRows   = computed(() => rowsFor('Player', allyMetric.value));
const enemyRows  = computed(() => rowsFor('Enemy',  enemyMetric.value));
const allyTotal  = computed(() => allyRows.value.reduce((s, r) => s + r.value, 0));
const enemyTotal = computed(() => enemyRows.value.reduce((s, r) => s + r.value, 0));
const allyOption  = computed(() => optionFor(allyMetric.value));
const enemyOption = computed(() => optionFor(enemyMetric.value));
</script>

<template>
  <div v-if="allyOpen || enemyOpen" class="ms-backdrop" @click="closeAll" />
  <section class="damage-report">
    <header class="damage-report__header">
      <span class="damage-report__title">△ Damage Meter · Contributions du tour {{ combat?.turnNumber ?? '—' }}</span>
    </header>

    <div class="damage-report__body">
      <!-- ── Alliés ── -->
      <article class="damage-report__side damage-report__side--allies">
        <div class="damage-report__side-head">
          <span>◆ Alliés · {{ combat?.allies.length ?? 0 }}</span>
          <div class="ms" :class="{ 'ms--open': allyOpen }">
            <button class="ms__trigger" @click.stop="allyOpen = !allyOpen; enemyOpen = false">
              <span class="ms__icon">{{ allyOption.icon }}</span>
              <span class="ms__label">{{ allyOption.label }}</span>
              <span class="ms__caret">▾</span>
            </button>
            <div v-if="allyOpen" class="ms__panel">
              <button
                v-for="o in metricOptions" :key="o.key"
                class="ms__option" :class="{ 'ms__option--active': allyMetric === o.key }"
                @click="selectAlly(o.key)"
              >
                <span class="ms__option-icon">{{ o.icon }}</span>
                <span class="ms__option-body">
                  <span class="ms__option-label">{{ o.label }}</span>
                  <span class="ms__option-desc">{{ o.footer }}</span>
                </span>
                <span v-if="allyMetric === o.key" class="ms__option-dot">●</span>
              </button>
            </div>
          </div>
        </div>

        <ol class="damage-report__rows">
          <li v-for="(row, i) in allyRows" :key="row.id" class="damage-row">
            <span class="damage-row__rank">{{ i + 1 }}</span>
            <span class="damage-row__name">{{ row.name }}</span>
            <span class="damage-row__bar">
              <span
                class="damage-row__bar-fill"
                :class="barFillClass(allyMetric)"
                :style="{ width: (row.percent - row.guardPercent) + '%' }"
              />
              <span
                v-if="row.guardPercent > 0"
                class="damage-row__bar-fill bar-fill--guard"
                :style="{ width: row.guardPercent + '%' }"
              />
            </span>
            <span class="damage-row__value">{{ row.value }}-{{row.guardPercent}}% {{ allyOption.suffix }}</span>
          </li>
        </ol>

        <footer class="damage-report__footer">
          <span>{{ allyOption.footer }}</span>
          <strong>Total {{ allyTotal }}{{ allyOption.suffix }}</strong>
        </footer>
      </article>

      <!-- ── Ennemis ── -->
      <article class="damage-report__side damage-report__side--enemies">
        <div class="damage-report__side-head">
          <span>◆ Les Manifestations · {{ combat?.enemies.length ?? 0 }}</span>
          <div class="ms" :class="{ 'ms--open': enemyOpen }">
            <button class="ms__trigger" @click.stop="enemyOpen = !enemyOpen; allyOpen = false">
              <span class="ms__icon">{{ enemyOption.icon }}</span>
              <span class="ms__label">{{ enemyOption.label }}</span>
              <span class="ms__caret">▾</span>
            </button>
            <div v-if="enemyOpen" class="ms__panel">
              <button
                v-for="o in metricOptions" :key="o.key"
                class="ms__option" :class="{ 'ms__option--active': enemyMetric === o.key }"
                @click="selectEnemy(o.key)"
              >
                <span class="ms__option-icon">{{ o.icon }}</span>
                <span class="ms__option-body">
                  <span class="ms__option-label">{{ o.label }}</span>
                  <span class="ms__option-desc">{{ o.footer }}</span>
                </span>
                <span v-if="enemyMetric === o.key" class="ms__option-dot">●</span>
              </button>
            </div>
          </div>
        </div>

        <ol class="damage-report__rows">
          <li v-for="(row, i) in enemyRows" :key="row.id" class="damage-row">
            <span class="damage-row__rank">{{ i + 1 }}</span>
            <span class="damage-row__name">{{ row.name }}</span>
            <span class="damage-row__bar">
              <span
                class="damage-row__bar-fill"
                :class="barFillClass(enemyMetric)"
                :style="{ width: (row.percent - row.guardPercent) + '%' }"
              />
              <span
                v-if="row.guardPercent > 0"
                class="damage-row__bar-fill bar-fill--guard"
                :style="{ width: row.guardPercent + '%' }"
              />
            </span>
            <span class="damage-row__value">{{ row.value }}-{{row.guardPercent}}% {{ enemyOption.suffix }}</span>
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
  background: linear-gradient(180deg, oklch(0.2 0.04 60 / 0.98), oklch(0.17 0.04 60 / 0.98));
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
  margin-left: 45%;
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

/* ── Custom metric select ── */
.ms-backdrop {
  position: fixed;
  inset: 0;
  z-index: calc(var(--z-popover) - 1);
}

.ms {
  position: relative;
}

.ms__trigger {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 0 var(--space-2);
  min-height: 1.6rem;
  border: 1px solid var(--line-soft);
  border-radius: 3px;
  background: oklch(0.18 0.035 60 / 0.65);
  color: var(--ink-4);
  font: inherit;
  letter-spacing: inherit;
  text-transform: inherit;
  cursor: pointer;
  white-space: nowrap;
}

.ms__trigger:hover {
  border-color: var(--line);
  color: var(--ink-3);
}

.ms--open .ms__trigger {
  border-color: var(--line);
  color: var(--ink-2);
}

.ms__icon {
  font-size: 0.75rem;
  flex-shrink: 0;
}

.ms__label {
  font-size: 0.58rem;
}

.ms__caret {
  font-size: 0.55rem;
  opacity: 0.5;
  margin-left: 2px;
}

.ms__panel {
  position: absolute;
  bottom: calc(100% + 4px);
  right: 0;
  z-index: var(--z-popover);
  min-width: 15rem;
  border: 1px solid var(--line);
  border-radius: var(--radius-sm);
  background: oklch(0.20 0.040 60 / 0.98);
  backdrop-filter: blur(8px);
  box-shadow: var(--shadow-deep);
  overflow: hidden;
}

.ms__option {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  width: 100%;
  padding: var(--space-2) var(--space-3);
  border: none;
  background: transparent;
  color: var(--ink-3);
  font: inherit;
  text-align: left;
  cursor: pointer;
}

.ms__option + .ms__option {
  border-top: 1px solid var(--line-soft);
}

.ms__option:hover {
  background: oklch(0.26 0.045 60 / 0.8);
  color: var(--ink-2);
}

.ms__option--active {
  color: var(--ink);
}

.ms__option-icon {
  font-size: 0.85rem;
  flex-shrink: 0;
  color: var(--ink-4);
  width: 1rem;
  text-align: center;
}

.ms__option--active .ms__option-icon {
  color: var(--ink-2);
}

.ms__option-body {
  display: flex;
  flex-direction: column;
  gap: 1px;
  flex: 1;
}

.ms__option-label {
  font-size: 0.68rem;
  font-weight: 500;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.ms__option-desc {
  font-size: 0.56rem;
  color: var(--ink-5);
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.ms__option-dot {
  font-size: 0.4rem;
  color: var(--ink-4);
  flex-shrink: 0;
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
  grid-template-columns: 1.2rem 7rem minmax(7rem, 1fr) 3.5rem 2.4rem;
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

.damage-row__value {
  font-size: 0.75rem;
}

.damage-row__name {
  font-family: var(--font);
  font-size: 0.72rem;
  color: var(--ink-2);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* Bar — flex for stacking vitality + guard segments */
.damage-row__bar {
  display: flex;
  height: 10px;
  background: oklch(0.08 0.02 60 / 0.9);
  border-radius: 1px;
  overflow: hidden;
}

.damage-row__bar-fill {
  flex: 0 0 auto;
  height: 100%;
  min-width: 0;
  transition: width 0.8s ease;
}

/* Segment colors */
.bar-fill--blood  { background: linear-gradient(90deg, var(--ink-2), var(--ink-5)); }
.bar-fill--guard  { background: color-mix(in oklch, var(--gold-dim), transparent 5%); }
.bar-fill--frost  { background: linear-gradient(90deg, var(--gold), var(--gold-deep)); }
.bar-fill--gold   { background: linear-gradient(90deg, var(--sap), var(--sap)); }
.bar-fill--ink    { background: var(--ink-4); }

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
    grid-template-columns: 1rem 5rem minmax(5rem, 1fr) 3rem;
  }

  .damage-row__pct {
    display: none;
  }
}
</style>
