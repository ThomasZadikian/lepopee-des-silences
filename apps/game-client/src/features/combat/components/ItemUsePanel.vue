<script setup lang="ts">
import type { CombatUsableItemDto } from '../types/combatContracts';

defineProps<{
  items: CombatUsableItemDto[];
  selectedItemId: string | null;
}>();

const emit = defineEmits<{
  select: [itemId: string];
  close: [];
}>();

function effectLabel(item: CombatUsableItemDto): string {
  switch (item.effectType) {
    case 'Heal': return `+${item.effectAmount} PV`;
    case 'Guard': return `+${item.effectAmount} Garde`;
    case 'ManaRestore': return `+${item.effectAmount} Mana`;
    case 'ChargeRestore': return `+${item.effectAmount} Charge`;
    case 'HealAndManaRestorePercent': return `+${item.effectAmount}% PV et PP`;
    case 'Damage': return `−${item.effectAmount} PV`;
    default: return `${item.effectAmount}`;
  }
}

function targetLabel(item: CombatUsableItemDto): string {
  switch (item.targetingType) {
    case 'Self': return 'Soi';
    case 'SingleAlly': return 'Une voix';
  }
}

function isOffensive(item: CombatUsableItemDto): boolean {
  return item.effectType === 'Damage';
}
</script>

<template>
  <section
    class="item-panel"
    role="listbox"
    aria-label="Objets utilisables en combat"
    @click.stop
  >
    <header class="item-panel__head">
      <span class="es-label">La Besace</span>
      <button class="item-panel__close" aria-label="Fermer" @click="emit('close')">✕</button>
    </header>

    <ul class="item-panel__list">
      <li v-for="item in items" :key="item.itemId">
        <button
          class="item-row"
          :class="{
            'item-row--selected': selectedItemId === item.itemId,
            'item-row--offensive': isOffensive(item),
          }"
          role="option"
          :aria-selected="selectedItemId === item.itemId"
          @click="emit('select', item.itemId)"
        >
          <span class="item-row__glyph" aria-hidden="true">{{ isOffensive(item) ? '✶' : '◆' }}</span>
          <span class="item-row__body">
            <span class="item-row__name">{{ item.displayName }}</span>
            <span class="item-row__meta">
              {{ effectLabel(item) }} · {{ targetLabel(item) }}
            </span>
          </span>
          <span class="item-row__qty">×{{ item.quantity }}</span>
        </button>
      </li>
    </ul>
  </section>
</template>

<style scoped>
.item-panel {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  width: 100%;
  min-width: 0;
}

.item-panel__head {
  display: flex;
  align-items: center;
  gap: var(--space-1);
  flex: 0 0 auto;
}

.item-panel__close {
  border: 1px solid var(--line);
  background: none;
  color: var(--ink-4);
  font-size: 0.72rem;
  cursor: pointer;
  padding: 2px 7px;
  border-radius: var(--radius-sm);
  transition: color 0.15s ease, border-color 0.15s ease;
}

.item-panel__close:hover { color: var(--ink-2); border-color: var(--line-strong); }

.item-panel__list {
  list-style: none;
  margin: 0;
  padding: var(--space-1) 0;
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
  width: 100%;
}

.item-panel__list > li { flex: 0 0 auto; }

.item-row {
  display: flex;
  align-items: center;
  gap: var(--space-1);
  width: 100%;
  min-width: 10rem;
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

.item-row:hover {
  border-color: var(--edge-frost);
  background: var(--wash-sap);
  transform: translateY(-2px);
}

.item-row--selected {
  border-color: var(--sap) !important;
  background: oklch(0.840 0.092 162 / 0.14) !important;
}

.item-row--offensive .item-row__glyph { color: var(--blood); }
.item-row--offensive:hover { background: var(--wash-blood); }

.item-row__glyph {
  font-size: 0.78rem;
  color: var(--sap);
  flex: 0 0 auto;
}

.item-row__body {
  display: flex;
  flex-direction: column;
  gap: 1px;
  flex: 1;
  min-width: 0;
}

.item-row__name {
  font-family: var(--font);
  font-size: 0.86rem;
  color: var(--ink-2);
}

.item-row__meta {
  font-family: var(--font-caps);
  font-size: 0.54rem;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.item-row__qty {
  font-family: var(--font-mono);
  font-size: 0.66rem;
  color: var(--ink-3);
  flex: 0 0 auto;
}

@media (prefers-reduced-motion: reduce) {
  .item-row { transition: none; }
}
</style>
