<script setup lang="ts">
import type { CombatantRuntimeDto } from '../types/combatContracts';

defineProps<{
  combatant: CombatantRuntimeDto | null;
  selectedSkillKey: string | null;
  isPlayerTurn: boolean;
  isLoading: boolean;
}>();

defineEmits<{
  selectSkill: [skillKey: string];
}>();

function isSkillDisabled(combatant: CombatantRuntimeDto | null): boolean {
  if (!combatant) return true;
  if (combatant.status === 'Defeated') return true;
  return false;
}
</script>

<template>
  <section class="skill-bar panel">
    <template v-if="combatant">
      <p class="system-label skill-bar__header">
        ACTIONS · {{ combatant.displayName }}
      </p>

      <div class="skill-bar__grid">
        <button
          v-for="skill in combatant.skills"
          :key="skill.key"
          class="skill-bar__button"
          :class="{
            'skill-bar__button--selected': selectedSkillKey === skill.key,
          }"
          :disabled="!isPlayerTurn || isLoading || isSkillDisabled(combatant)"
          @click="$emit('selectSkill', skill.key)"
        >
          <span class="skill-bar__button-name">{{ skill.displayName }}</span>
          <span class="skill-bar__button-meta">
            {{ skill.skillType }}
            <template v-if="skill.manaCost > 0"> · {{ skill.manaCost }} MANA</template>
            <template v-if="skill.chargeCost > 0"> · {{ skill.chargeCost }} CHG</template>
          </span>
          <span class="skill-bar__button-target">Cible : {{ skill.targetingType }}</span>
        </button>
      </div>
    </template>

    <template v-else>
      <p class="skill-bar__empty system-label">Aucun combattant actif</p>
    </template>
  </section>
</template>

<style scoped>
.skill-bar {
  display: grid;
  gap: var(--space-3);
  padding: var(--space-4);
}

.skill-bar__header {
  color: var(--color-dim);
}

.skill-bar__grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(10rem, 1fr));
  gap: var(--space-2);
}

.skill-bar__button {
  display: grid;
  gap: 2px;
  text-align: left;
  border: 1px solid var(--color-line);
  border-radius: var(--radius-sm);
  background: var(--color-panel);
  padding: var(--space-2) var(--space-3);
  cursor: pointer;
  transition: border-color 0.15s, background 0.15s;
  font-family: inherit;
  color: var(--color-ink);
}

.skill-bar__button:hover:not(:disabled) {
  border-color: var(--color-gold);
  background: color-mix(in oklch, var(--color-gold), transparent 92%);
}

.skill-bar__button--selected {
  border-color: var(--color-gold) !important;
  background: color-mix(in oklch, var(--color-gold), transparent 85%) !important;
}

.skill-bar__button:disabled {
  opacity: 0.35;
  cursor: not-allowed;
}

.skill-bar__button-name {
  font-weight: 600;
  font-size: 0.85rem;
}

.skill-bar__button-meta {
  font-size: 0.7rem;
  color: var(--color-muted);
}

.skill-bar__button-target {
  font-size: 0.65rem;
  color: var(--color-dim);
}

.skill-bar__empty {
  color: var(--color-dim);
}
</style>
