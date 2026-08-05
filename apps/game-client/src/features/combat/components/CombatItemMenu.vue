<script setup lang="ts">
/**
 * Sélecteur d'objets utilisables en combat — ouvert depuis le bouton "Objets" de la barre
 * d'action. Choisir une ligne arme l'objet exactement comme un clic sur un sort (le ciblage
 * se fait ensuite sur la grille) ; la modale se ferme aussitôt.
 */
import { watch } from 'vue';

import type { CombatUsableItemDto } from '../types/combatContracts';

const props = defineProps<{
  open: boolean;
  items: CombatUsableItemDto[];
  selectedItemId: string | null;
}>();

const emit = defineEmits<{ select: [itemId: string]; close: [] }>();

function onKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') emit('close');
}

watch(() => props.open, (open) => {
  if (open) document.addEventListener('keydown', onKeydown);
  else document.removeEventListener('keydown', onKeydown);
});

function itemEffectLabel(effectType: string): string {
  if (effectType.includes('Heal')) return 'Soin';
  if (effectType.includes('Mana')) return 'Mana';
  if (effectType.includes('Charge')) return 'Charge';
  if (effectType.includes('Guard')) return 'Garde';
  if (effectType.includes('Revive')) return 'Réanimation';
  return effectType;
}

function itemEffectSummary(item: CombatUsableItemDto): string {
  if (item.effectType === 'RevivePercent') return `Réanimation à ${item.effectAmount} %`;
  const isPercent = item.effectType.toLowerCase().includes('percent');
  return `${itemEffectLabel(item.effectType)} ${item.effectAmount}${isPercent ? ' %' : ''}`;
}

function itemMeta(item: CombatUsableItemDto): string {
  return `Portée ${item.tacticalRange} · ${item.tacticalAreaShape} · ×${item.quantity}`;
}
</script>

<template>
  <Teleport to="body">
    <Transition name="item-menu-fade">
      <div v-if="open" class="item-menu-backdrop" @click.self="emit('close')">
        <div class="item-menu" role="dialog" :aria-modal="true" aria-label="Objets utilisables">
          <div class="item-menu__head">
            <span class="item-menu__title">Objets</span>
            <button type="button" class="item-menu__close" title="Fermer" @click="emit('close')">✕</button>
          </div>

          <ul v-if="items.length" class="item-menu__list">
            <li v-for="item in items" :key="item.itemId">
              <button
                type="button"
                class="item-menu__row"
                :class="{ 'item-menu__row--armed': item.itemId === selectedItemId }"
                @click="emit('select', item.itemId)"
              >
                <span class="item-menu__row-name">{{ item.displayName }}</span>
                <span class="item-menu__row-effect">{{ itemEffectSummary(item) }}</span>
                <span class="item-menu__row-meta">{{ itemMeta(item) }}</span>
              </button>
            </li>
          </ul>
          <p v-else class="item-menu__empty">Aucun objet utilisable en combat.</p>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.item-menu-backdrop {
  position: fixed;
  inset: 0;
  z-index: 220;
  display: grid;
  place-items: center;
  background: color-mix(in oklch, var(--void), transparent 30%);
  backdrop-filter: blur(2px);
  padding: 24px;
}

.item-menu {
  width: min(22rem, 100%);
  max-height: min(70vh, 520px);
  overflow-y: auto;
  padding: 16px 18px;
  display: grid;
  gap: 12px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background: var(--panel);
  box-shadow: var(--shadow-panel, 0 24px 80px rgba(0, 0, 0, .4));
}

.item-menu__head {
  display: flex;
  align-items: center;
}

.item-menu__title {
  font-family: var(--font-mono);
  font-size: 11px;
  letter-spacing: .08em;
  text-transform: uppercase;
  color: var(--ink-3);
}

.item-menu__close {
  all: unset;
  margin-left: auto;
  cursor: pointer;
  color: var(--ink-4);
  padding: 4px;
}

.item-menu__close:hover { color: var(--ink); }

.item-menu__list {
  margin: 0;
  padding: 0;
  list-style: none;
  display: grid;
  gap: 6px;
}

.item-menu__row {
  all: unset;
  box-sizing: border-box;
  display: grid;
  grid-template-columns: 1fr auto;
  column-gap: 10px;
  row-gap: 2px;
  width: 100%;
  padding: 8px 10px;
  border: 1px solid var(--line);
  border-radius: 5px;
  background: var(--panel-2);
  cursor: pointer;
  transition: border-color 120ms ease, background 120ms ease;
}

.item-menu__row:hover { border-color: var(--line-strong); }

.item-menu__row--armed {
  border-color: var(--mint-dim);
  background: color-mix(in oklch, var(--mint-dim), var(--panel-2) 78%);
}

.item-menu__row-name { font-size: 13px; font-weight: 600; color: var(--ink); }
.item-menu__row-effect {
  grid-column: 2;
  grid-row: 1;
  font-size: 12px;
  color: var(--mint-dim);
  font-variant-numeric: tabular-nums;
}
.item-menu__row-meta {
  grid-column: 1 / -1;
  font-size: 11px;
  color: var(--ink-4);
  font-variant-numeric: tabular-nums;
}

.item-menu__empty {
  margin: 0;
  font-size: 12.5px;
  color: var(--ink-4);
  font-style: italic;
}

.item-menu-fade-enter-active,
.item-menu-fade-leave-active { transition: opacity 0.15s ease; }
.item-menu-fade-enter-from,
.item-menu-fade-leave-to { opacity: 0; }

@media (prefers-reduced-motion: reduce) {
  .item-menu-fade-enter-active,
  .item-menu-fade-leave-active { transition: none; }
}
</style>
