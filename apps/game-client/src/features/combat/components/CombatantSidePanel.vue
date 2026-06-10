<script setup lang="ts">
import type { CombatantRuntimeDto } from '../types/combatContracts';

defineProps<{
  combatant: CombatantRuntimeDto | null;
}>();
</script>

<template>
  <section class="side-panel panel">
    <template v-if="combatant">
      <p class="system-label side-panel__header-info">
        {{ combatant.side === 'Player' ? 'ALLIÉ' : 'ENNEMI' }}
        ·
        {{ combatant.archetype }}
      </p>
      <h3 class="side-panel__name">{{ combatant.displayName }}</h3>

      <div class="side-panel__stats">
        <div class="side-panel__stat">
          <span class="side-panel__stat-label">VIE</span>
          <span class="side-panel__stat-value">
            {{ combatant.currentVitality }} / {{ combatant.maxVitality }}
          </span>
        </div>
        <div class="side-panel__stat">
          <span class="side-panel__stat-label">GUARD</span>
          <span class="side-panel__stat-value">{{ combatant.guard }}</span>
        </div>
        <div class="side-panel__stat">
          <span class="side-panel__stat-label">MANA</span>
          <span class="side-panel__stat-value">{{ combatant.mana }}</span>
        </div>
        <div class="side-panel__stat">
          <span class="side-panel__stat-label">CHARGE</span>
          <span class="side-panel__stat-value">{{ combatant.charge }}</span>
        </div>
      </div>

      <div v-if="combatant.skills.length > 0" class="side-panel__section">
        <p class="system-label">COMPÉTENCES</p>
        <ul class="side-panel__skills">
          <li
            v-for="skill in combatant.skills"
            :key="skill.key"
            class="side-panel__skill"
          >
            <div class="side-panel__skill-header">
              <span class="side-panel__skill-name">{{ skill.displayName }}</span>
              <span class="side-panel__skill-cost">
                <template v-if="skill.manaCost > 0">{{ skill.manaCost }} MANA</template>
                <template v-if="skill.chargeCost > 0">{{ skill.chargeCost }} CHG</template>
                <template v-if="skill.manaCost === 0 && skill.chargeCost === 0">—</template>
              </span>
            </div>
            <div class="side-panel__skill-meta">
              {{ skill.skillType }} · {{ skill.targetingType }} · PUISSANCE {{ skill.basePower }}
            </div>
          </li>
        </ul>
      </div>

      <div class="side-panel__status">
        État : <strong>{{ combatant.status }}</strong>
      </div>
    </template>

    <template v-else>
      <p class="side-panel__empty system-label">SÉLECTIONNE UN COMBATTANT</p>
    </template>
  </section>
</template>

<style scoped>
.side-panel {
  display: grid;
  gap: var(--space-3);
  align-content: start;
  padding: var(--space-4);
}

.side-panel__header-info {
  color: var(--color-dim);
}

.side-panel__name {
  margin: 0;
  color: var(--color-ink);
  letter-spacing: 0.04em;
}

.side-panel__stats {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--space-2);
}

.side-panel__stat {
  display: grid;
  gap: 0;
}

.side-panel__stat-label {
  font-size: 0.65rem;
  letter-spacing: 0.12em;
  color: var(--color-dim);
}

.side-panel__stat-value {
  font-family: var(--font-mono);
  font-size: 0.9rem;
  color: var(--color-ink);
}

.side-panel__section {
  display: grid;
  gap: var(--space-2);
}

.side-panel__skills {
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  gap: var(--space-2);
}

.side-panel__skill {
  border: 1px solid var(--color-line);
  border-radius: var(--radius-sm);
  padding: var(--space-2);
}

.side-panel__skill-header {
  display: flex;
  justify-content: space-between;
  gap: var(--space-2);
}

.side-panel__skill-name {
  font-weight: 600;
  font-size: 0.85rem;
}

.side-panel__skill-cost {
  font-size: 0.7rem;
  color: var(--color-muted);
  font-family: var(--font-mono);
}

.side-panel__skill-meta {
  font-size: 0.7rem;
  color: var(--color-dim);
  margin-top: var(--space-1);
}

.side-panel__status {
  font-size: 0.8rem;
  color: var(--color-muted);
}

.side-panel__empty {
  color: var(--color-dim);
}
</style>
