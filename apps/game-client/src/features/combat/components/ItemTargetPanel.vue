<script setup lang="ts">
import type { CombatantRuntimeDto } from '../types/combatContracts';

defineProps<{
  itemName: string;
  side: 'ally' | 'enemy';
  targets: CombatantRuntimeDto[];
}>();

const emit = defineEmits<{
  select: [combatantId: string];
  back: [];
}>();

function hpRatio(c: CombatantRuntimeDto): number {
  return c.maxVitality > 0 ? c.currentVitality / c.maxVitality : 0;
}
</script>

<template>
  <section
    class="target-panel"
    :class="side === 'enemy' ? 'target-panel--enemy' : 'target-panel--ally'"
    role="listbox"
    :aria-label="side === 'enemy' ? 'Choisir une manifestation' : 'Choisir une voix'"
    @click.stop
  >
    <div class="target-panel__intro">
      <header class="target-panel__head">
        <button class="target-panel__back" @click="emit('back')">‹ Objets</button>
        <span class="target-panel__title">
          {{ side === 'enemy' ? 'Frapper une manifestation' : 'Soigner une voix' }}
        </span>
      </header>
      <p class="target-panel__sub es-mono">« {{ itemName }} »</p>
    </div>

    <ul class="target-panel__list">
      <li v-for="t in targets" :key="t.id">
        <button class="target-row" role="option" @click="emit('select', t.id)">
          <span class="target-row__body">
            <span class="target-row__name">{{ t.displayName }}</span>
            <span class="target-row__arch">{{ t.archetype }}</span>
          </span>
          <span class="target-row__gauge">
            <span
              class="target-row__gauge-fill"
              :style="{ width: hpRatio(t) * 100 + '%' }"
            />
          </span>
          <span class="target-row__hp es-mono">{{ t.currentVitality }}/{{ t.maxVitality }}</span>
        </button>
      </li>
      <li v-if="targets.length === 0" class="target-panel__empty es-dim">
        Aucune cible valide.
      </li>
    </ul>
  </section>
</template>

<style scoped>
.target-panel {
  width: 100%;
  padding: 0;
}

.target-panel__head {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  margin-bottom: var(--space-1);
}

.target-panel__back {
  border: 1px solid var(--line);
  background: none;
  color: var(--ink-3);
  font-family: var(--font-caps);
  font-size: 0.56rem;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  padding: 2px 8px;
  border-radius: var(--radius-sm);
  cursor: pointer;
  transition: border-color 0.15s ease, color 0.15s ease;
}

.target-panel__back:hover { border-color: var(--edge-gold); color: var(--ink); }

.target-panel__title {
  font-family: var(--font);
  font-size: 0.88rem;
  color: var(--ink);
}

.target-panel__sub {
  font-size: 0.62rem;
  color: var(--ink-4);
  margin: 0 0 var(--space-2);
}

.target-panel__list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
  max-height: 18rem;
  overflow-y: auto;
}

.target-row {
  display: flex;
  align-items: center;
  gap: var(--space-1);
  width: 100%;
  text-align: left;
  border: 1px solid var(--line-soft);
  border-radius: var(--radius-sm);
  background: var(--card-soft);
  padding: var(--space-1) var(--space-2);
  cursor: pointer;
  font-family: inherit;
  color: var(--ink);
  transition: border-color 0.16s ease, background 0.16s ease, transform 0.16s ease;
}

.target-panel--ally .target-row:hover {
  border-color: var(--edge-frost);
  background: var(--wash-frost);
  transform: translateX(2px);
}

.target-panel--enemy .target-row:hover {
  border-color: color-mix(in oklch, var(--blood), transparent 40%);
  background: var(--wash-blood);
  transform: translateX(2px);
}

.target-row__body {
  display: flex;
  flex-direction: column;
  gap: 1px;
  flex: 1 1 auto;
  min-width: 0;
}

.target-row__name {
  font-family: var(--font);
  font-size: 0.86rem;
  color: var(--ink-2);
}

.target-row__arch {
  font-family: var(--font-caps);
  font-size: 0.5rem;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.target-row__gauge {
  position: relative;
  flex: 0 0 4rem;
  height: 3px;
  background: var(--panel);
  border-radius: 2px;
  overflow: hidden;
}

.target-row__gauge-fill {
  position: absolute;
  inset: 0 auto 0 0;
  height: 100%;
  border-radius: 2px;
  transition: width 0.3s ease;
}

.target-panel--ally .target-row__gauge-fill { background: var(--frost-dim); }
.target-panel--enemy .target-row__gauge-fill { background: var(--blood); }

.target-row__hp {
  font-size: 0.62rem;
  color: var(--ink-4);
  flex: 0 0 auto;
}

.target-panel__empty {
  font-size: 0.78rem;
  padding: var(--space-2) var(--space-3);
}

@media (prefers-reduced-motion: reduce) {
  .target-row { transition: none; }
}
</style>
