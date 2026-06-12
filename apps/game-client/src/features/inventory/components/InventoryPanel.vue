<script setup lang="ts">
import type { RunItemDto } from '../../runs/types/runTypes';

const props = defineProps<{
  items: RunItemDto[];
}>();

function getRarityClass(rarity: string): string {
  switch (rarity) {
    case 'Uncommon': return 'item--uncommon';
    case 'Rare': return 'item--rare';
    case 'Epic': return 'item--epic';
    default: return 'item--common';
  }
}

function getEffectLabel(effectType: string, effectAmount: number): string {
  switch (effectType) {
    case 'Heal': return `+${effectAmount} PV`;
    case 'Guard': return `+${effectAmount} Garde`;
    case 'ManaRestore': return `+${effectAmount} Mana`;
    case 'ChargeRestore': return `+${effectAmount} Charge`;
    case 'NextCombatGuard': return `+${effectAmount} Garde (prochain combat)`;
    case 'NarrativeFragment': return 'Fragment narratif';
    default: return '';
  }
}
</script>

<template>
  <section class="inventory-panel">
    <header>
      <p class="system-label">Inventaire de run</p>
      <h3>Sac à dos</h3>
    </header>

    <div v-if="items.length === 0" class="inventory-panel__empty">
      <p class="system-label">INVENTAIRE_VIDE</p>
      <p>Ton sac est vide. Les objets que tu trouveras apparaîtront ici.</p>
    </div>

    <div v-else class="inventory-panel__items">
      <div
        v-for="item in items"
        :key="item.id"
        class="inventory-item"
        :class="getRarityClass(item.rarity)"
      >
        <div class="inventory-item__header">
          <strong>{{ item.displayName }}</strong>
          <span v-if="item.quantity > 1" class="inventory-item__qty">
            ×{{ item.quantity }}
          </span>
        </div>

        <small class="inventory-item__type">{{ item.type }}</small>

        <p class="inventory-item__desc">{{ item.description }}</p>

        <span
          v-if="item.effectAmount > 0"
          class="inventory-item__effect"
        >
          {{ getEffectLabel(item.effectType, item.effectAmount) }}
        </span>
      </div>
    </div>
  </section>
</template>

<style scoped>
.inventory-panel {
  display: grid;
  gap: var(--space-4);
}

.inventory-panel h3 {
  color: var(--color-gold);
  font-size: 1.4rem;
  text-transform: uppercase;
  letter-spacing: 0.06em;
}

.inventory-panel__empty p {
  color: var(--color-muted);
  font-size: 0.9rem;
}

.inventory-panel__items {
  display: grid;
  gap: var(--space-3);
}

.inventory-item {
  border: 1px solid var(--color-border);
  border-radius: 8px;
  padding: var(--space-3);
  background: var(--color-surface);
}

.inventory-item--common {
  border-color: var(--color-border);
}

.inventory-item--uncommon {
  border-color: var(--color-green, #4ade80);
}

.inventory-item--rare {
  border-color: var(--color-blue, #60a5fa);
}

.inventory-item--epic {
  border-color: var(--color-purple, #c084fc);
}

.inventory-item__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--space-1);
}

.inventory-item__qty {
  color: var(--color-muted);
  font-size: 0.85rem;
}

.inventory-item__type {
  color: var(--color-muted);
  font-size: 0.75rem;
  text-transform: uppercase;
}

.inventory-item__desc {
  color: var(--color-text-secondary);
  font-size: 0.85rem;
  margin: var(--space-1) 0;
}

.inventory-item__effect {
  color: var(--color-green, #4ade80);
  font-size: 0.8rem;
  font-weight: 600;
}
</style>
