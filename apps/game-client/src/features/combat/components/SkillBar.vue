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
</script>

<template>
  <section class="skill-bar">
    <template v-if="combatant">
      <span class="es-label">Gestes · {{ combatant.displayName }}</span>

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
          <span class="skill-card__meta">
            {{ skill.skillType }}
            <template v-if="skill.manaCost > 0"> · {{ skill.manaCost }} mana</template>
            <template v-if="skill.chargeCost > 0"> · {{ skill.chargeCost }} charge</template>
          </span>
        </button>
      </div>

      <template v-if="usableBattleItems.length > 0">
        <hr class="es-rule" />
        <span class="es-label">Objets de combat</span>

        <div class="skill-bar__grid">
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
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--space-2) var(--space-3);
  flex: 1;
  min-width: 0;
}

.skill-bar__grid {
  display: flex;
  gap: var(--space-2);
  flex-wrap: wrap;
}

.skill-card {
  display: flex;
  flex-direction: column;
  gap: 1px;
  text-align: left;
  border: 1px solid var(--line);
  border-radius: var(--radius-sm);
  background: var(--panel);
  padding: var(--space-1) var(--space-3);
  cursor: pointer;
  font-family: inherit;
  color: var(--ink);
  transition: border-color 0.18s ease, background 0.18s ease;
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
  font-size: 0.82rem;
  color: var(--ink-2);
}

.skill-card__meta {
  font-family: var(--font-caps);
  font-size: 0.52rem;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ink-4);
}

@media (prefers-reduced-motion: reduce) {
  .skill-card { transition: none; }
}
</style>
