<script setup lang="ts">
import type { CombatUsableItemDto, CombatantRuntimeDto } from '../types/combatContracts';

defineProps<{
  combatant: CombatantRuntimeDto | null;
  selectedSkillKey: string | null;
  isPlayerTurn: boolean;
  isLoading: boolean;
  usableBattleItems: CombatUsableItemDto[];
  selectedItemId: string | null;
}>();

defineEmits<{
  selectSkill: [skillKey: string];
  selectItem: [itemId: string];
}>();

function isSkillDisabled(combatant: CombatantRuntimeDto | null): boolean {
  if (!combatant) return true;
  return combatant.status === 'Defeated';
}

function getEffectLabel(effectType: string, effectAmount: number): string {
  switch (effectType) {
    case 'Heal': return `+${effectAmount} PV`;
    case 'Guard': return `+${effectAmount} Garde`;
    case 'ManaRestore': return `+${effectAmount} Mana`;
    case 'ChargeRestore': return `+${effectAmount} Charge`;
    default: return `+${effectAmount}`;
  }
}

// function getSkillCost(skill: CombatantRuntimeDto['skills'][number]): number {
//   return skill.manaCost || skill.chargeCost;
// }

</script>

<template>
  <section class="skill-bar">
    <template v-if="combatant">
      <div class="skill-bar__head">
        <span class="skill-bar__verb">{{ combatant.mana }} PP disponibles</span>
      </div>

      <div class="skill-bar__grid">
        <button
          v-for="skill in combatant.skills"
          :key="skill.key"
          class="skill-card"
          :class="{
            'skill-card--selected': selectedSkillKey === skill.key,
            'skill-card--disabled': !isPlayerTurn || isLoading || isSkillDisabled(combatant),
          }"
          :disabled="!isPlayerTurn || isLoading || isSkillDisabled(combatant)"
          @click="$emit('selectSkill', skill.key)"
        >
          <span class="skill-card__name">{{ skill.displayName }}</span>
        </button>
      </div>

      <template v-if="usableBattleItems.length > 0">
        <div class="skill-bar__items">
          <span class="skill-bar__items-label">Besace</span>
          <button
            v-for="item in usableBattleItems"
            :key="item.itemId"
            class="skill-card skill-card--item"
            :class="{
              'skill-card--selected': selectedItemId === item.itemId,
              'skill-card--disabled': !isPlayerTurn || isLoading,
            }"
            :disabled="!isPlayerTurn || isLoading"
            @click="$emit('selectItem', item.itemId)"
          >
            <span class="skill-card__name">{{ item.displayName }}</span>
            <span class="skill-card__meta">
              {{ getEffectLabel(item.effectType, item.effectAmount) }} · ×{{ item.quantity }}
            </span>
          </button>
        </div>
      </template>
    </template>

    <template v-else>
      <span class="es-label">Aucun combattant actif</span>
    </template>
  </section>
</template>

<style scoped>
.skill-bar {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr) auto;
  align-items: center;
  gap: var(--space-2);
  flex: 1;
  min-width: 0;
}

.skill-bar__head {
  display: flex;
  flex-direction: column;
  gap: 1px;
  padding-right: var(--space-2);
}

.skill-bar__verb {
  font-family: var(--font-display);
  font-size: 0.95rem;
  font-weight: 600;
  color: var(--ink-2);
}

.skill-bar__pp {
  font-family: var(--font-caps);
  font-size: 0.5rem;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--ink-4);
  white-space: nowrap;
}

.skill-bar__grid {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-1);
  min-width: 0;
}

.skill-bar__items {
  display: flex;
  align-items: center;
  gap: var(--space-1);
  padding-left: var(--space-2);
  border-left: 1px solid var(--line-soft);
}

.skill-bar__items-label {
  font-family: var(--font-caps);
  font-size: 0.5rem;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.skill-card {
  display: flex;
  flex-direction: column;
  gap: 1px;
  text-align: left;
  border: 1px solid var(--line);
  border-radius: var(--radius-sm);
  background: var(--panel);
  padding: 3px var(--space-2);
  cursor: pointer;
  font-family: inherit;
  color: var(--ink);
  transition: border-color 0.18s ease, background 0.18s ease;
  min-width: 4.2rem;
}

.skill-card:hover:not(:disabled) {
  border-color: var(--edge-gold);
  background: var(--wash-gold);
}

.skill-card--selected {
  border-color: var(--gold) !important;
  background: oklch(0.862 0.098 86 / 0.12) !important;
}

.skill-card--disabled {
  opacity: 0.35;
  cursor: not-allowed;
}

.skill-card--item {
  border-color: color-mix(in oklch, var(--sap), transparent 60%);
}

.skill-card--item:hover:not(:disabled) {
  border-color: var(--sap);
  background: var(--wash-sap);
}

.skill-card--item.skill-card--selected {
  border-color: var(--sap) !important;
  background: oklch(0.840 0.092 162 / 0.12) !important;
}

.skill-card__name {
  font-family: var(--font);
  font-size: 0.76rem;
  color: var(--ink-2);
}

.skill-card__meta {
  font-family: var(--font-caps);
  font-size: 0.48rem;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ink-4);
}

@media (max-width: 900px) {
  .skill-bar {
    grid-template-columns: 1fr;
  }

  .skill-bar__items {
    padding-left: 0;
    border-left: none;
  }
}

@media (prefers-reduced-motion: reduce) {
  .skill-card { transition: none; }
}
</style>
