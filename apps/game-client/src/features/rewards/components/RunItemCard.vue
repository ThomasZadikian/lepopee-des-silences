<script setup lang="ts">
import type { RunItemDto } from '../../runs/types/runTypes';

const props = defineProps<{
  item: RunItemDto;
  compact?: boolean;
}>();

const emit = defineEmits<{
  use: [itemId: string];
}>();

function rarityTone(rarity: string): 'sap' | 'frost' | 'gold' | '' {
  switch (rarity) {
    case 'Uncommon': return 'sap';
    case 'Rare':     return 'frost';
    case 'Epic':     return 'gold';
    default:         return '';
  }
}

function rarityLabel(rarity: string): string {
  switch (rarity) {
    case 'Uncommon': return 'Peu commun';
    case 'Rare':     return 'Rare';
    case 'Epic':     return 'Épique';
    default:         return 'Commun';
  }
}

function effectLabel(effectType: string, effectAmount: number): string {
  switch (effectType) {
    case 'Heal':              return `+${effectAmount} Vitalité`;
    case 'Guard':             return `+${effectAmount} Garde`;
    case 'ManaRestore':       return `+${effectAmount} Mana`;
    case 'ChargeRestore':     return `+${effectAmount} Charge`;
    case 'NextCombatGuard':   return `+${effectAmount} Garde (prochain combat)`;
    case 'NarrativeFragment': return 'Fragment narratif';
    case 'HealAndManaRestorePercent': return `+${effectAmount}% PV et PP`;
    default:                  return '';
  }
}

function effectTone(effectType: string): 'sap' | 'frost' | 'gold' | 'blood' | '' {
  switch (effectType) {
    case 'Heal':
    case 'ManaRestore':
    case 'ChargeRestore':
    case 'HealAndManaRestorePercent':
    case 'NextCombatGuard': return 'sap';
    case 'Guard':           return 'frost';
    case 'NarrativeFragment': return 'gold';
    case 'Damage':          return 'blood';
    default:                return '';
  }
}


</script>

<template>
  <article
    class="ric"
    :class="[
      rarityTone(item.rarity) ? `ric--${rarityTone(item.rarity)}` : 'ric--common',
      compact && 'ric--compact',
    ]"
  >
    <!-- Header row -->
    <div class="ric__head">
      <div class="ric__left">
        <span class="ric__name">{{ item.displayName }}</span>
        <span v-if="item.quantity > 1" class="ric__qty">×{{ item.quantity }}</span>
      </div>
      <span
        class="ric__rarity"
        :class="rarityTone(item.rarity) ? `ric__rarity--${rarityTone(item.rarity)}` : ''"
      >
        {{ rarityLabel(item.rarity) }}
      </span>
    </div>

    <!-- Type -->
    <p class="ric__type">{{ item.type }}</p>

    <!-- Description -->
    <p v-if="!compact" class="ric__desc">{{ item.description }}</p>

    <!-- Effect badge -->
    <div v-if="item.effectAmount > 0" class="ric__effect">
      <span
        class="ric__effect-badge"
        :class="effectTone(item.effectType) ? `ric__effect-badge--${effectTone(item.effectType)}` : ''"
      >
        {{ effectLabel(item.effectType, item.effectAmount) }}
      </span>
    </div>

    <!-- Use action — only when backend explicitly exposes isUsable -->
    <div v-if="item.isUsable === true" class="ric__actions">
      <button class="ric__use-btn" @click="emit('use', item.id)">
        Utiliser
      </button>
    </div>
  </article>
</template>

<style scoped>
.ric {
  border: 1px solid var(--line-soft);
  border-radius: 5px;
  background: linear-gradient(180deg, oklch(0.30 0.034 60 / 0.38), oklch(0.24 0.028 60 / 0.30));
  padding: 13px 14px;
  display: flex;
  flex-direction: column;
  gap: 7px;
}

.ric--sap   { border-left: 2px solid var(--sap); }
.ric--frost { border-left: 2px solid var(--frost); }
.ric--gold  { border-left: 2px solid var(--gold); }
.ric--compact { padding: 9px 11px; gap: 4px; }

.ric__head {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 8px;
}

.ric__left {
  display: flex;
  align-items: baseline;
  gap: 7px;
  min-width: 0;
}

.ric__name {
  font-family: var(--font-display);
  font-size: 16px;
  font-weight: 600;
  color: var(--ink);
  line-height: 1.2;
}

.ric--compact .ric__name { font-size: 13px; }

.ric__qty {
  font-family: var(--font-mono);
  font-size: 10px;
  color: var(--ink-4);
  flex: 0 0 auto;
}

.ric__rarity {
  flex: 0 0 auto;
  font-family: var(--font-caps);
  font-size: 9.5px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  padding: 2px 7px;
  border-radius: 3px;
  border: 1px solid var(--line);
  color: var(--ink-4);
  background: oklch(0.26 0.025 60 / 0.5);
}

.ric__rarity--sap   { border-color: oklch(0.70 0.09 162 / 0.45); color: var(--sap);   background: oklch(0.50 0.07 162 / 0.12); }
.ric__rarity--frost { border-color: oklch(0.70 0.07 232 / 0.45); color: var(--frost); background: oklch(0.50 0.06 232 / 0.12); }
.ric__rarity--gold  { border-color: oklch(0.72 0.10 85  / 0.45); color: var(--gold);  background: oklch(0.55 0.08 85  / 0.12); }

.ric__type {
  font-family: var(--font-caps);
  font-size: 9px;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: var(--ink-4);
  margin: 0;
}

.ric__desc {
  font-family: var(--font);
  font-size: 13px;
  line-height: 1.55;
  color: var(--ink-3);
  margin: 0;
}

.ric__effect { display: flex; }

.ric__effect-badge {
  font-family: var(--font-mono);
  font-size: 11px;
  letter-spacing: 0.06em;
  padding: 3px 9px;
  border-radius: 3px;
  border: 1px solid var(--line);
  color: var(--ink-3);
  background: oklch(0.24 0.025 60 / 0.5);
}

.ric__effect-badge--sap   { border-color: oklch(0.70 0.09 162 / 0.4); color: var(--sap);   background: oklch(0.50 0.07 162 / 0.1); }
.ric__effect-badge--frost { border-color: oklch(0.70 0.07 232 / 0.4); color: var(--frost); background: oklch(0.50 0.06 232 / 0.1); }
.ric__effect-badge--gold  { border-color: oklch(0.72 0.10 85  / 0.4); color: var(--gold);  background: oklch(0.55 0.08 85  / 0.1); }
.ric__effect-badge--blood { border-color: oklch(0.52 0.15 20  / 0.4); color: var(--blood); background: oklch(0.38 0.10 20  / 0.1); }

.ric__actions { margin-top: 2px; }

.ric__use-btn {
  width: 100%;
  padding: 9px 16px;
  border-radius: 4px;
  border: 1px solid var(--gold);
  background: oklch(0.55 0.08 85 / 0.15);
  color: var(--gold);
  font-family: var(--font-caps);
  font-size: 10.5px;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  cursor: pointer;
  transition: background 0.15s, box-shadow 0.15s;
  box-shadow: 0 0 18px -6px oklch(0.72 0.1 85 / 0.4);
}

.ric__use-btn:hover {
  background: oklch(0.55 0.08 85 / 0.25);
  box-shadow: 0 0 26px -6px oklch(0.72 0.1 85 / 0.55);
}
</style>
