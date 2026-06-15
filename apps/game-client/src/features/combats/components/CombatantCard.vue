<script setup lang="ts">
import type { CombatantSnapshotDto } from '../types/combatTypes';

defineProps<{
  combatant: CombatantSnapshotDto;
  isCurrentActor?: boolean;
  isSelectedTarget?: boolean;
  selectable?: boolean;
}>();

defineEmits<{
  select: [combatantId: string];
}>();

function healthPercent(combatant: CombatantSnapshotDto) {
  return Math.max(0, Math.min(100, (combatant.currentHealth / combatant.maxHealth) * 100));
}
</script>

<template>
  <article
    class="combatant-card"
    :class="{
      'combatant-card--current': isCurrentActor,
      'combatant-card--target': isSelectedTarget,
      'combatant-card--defeated': combatant.isDefeated,
      'combatant-card--selectable': selectable,
    }"
    @click="selectable && !combatant.isDefeated ? $emit('select', combatant.id) : undefined"
  >
    <header>
      <div>
        <p class="system-label">{{ combatant.side }}</p>
        <h3>{{ combatant.displayName }}</h3>
      </div>

      <span v-if="isCurrentActor" class="combatant-card__turn">TOUR</span>
      <span v-if="isSelectedTarget" class="combatant-card__target">CIBLE</span>
    </header>

    <div class="combatant-card__portrait">
      {{ combatant.templateKey }}
    </div>

    <div class="combatant-card__health">
      <div
        class="combatant-card__health-fill"
        :style="{ width: `${healthPercent(combatant)}%` }"
      />
    </div>

    <footer>
      <span>PV {{ combatant.currentHealth }} / {{ combatant.maxHealth }}</span>
      <span>ATK {{ combatant.attack }} · DEF {{ combatant.defense }} · SPD {{ combatant.speed }}</span>
    </footer>
  </article>
</template>

<style scoped>
.combatant-card {
  padding: var(--space-4);
  border: 1px solid color-mix(in oklch, var(--line), transparent 30%);
  background: color-mix(in oklch, var(--panel), transparent 5%);
  cursor: default;
}

.combatant-card--selectable {
  cursor: pointer;
}

.combatant-card--selectable:hover {
  border-color: var(--frost);
}

.combatant-card--current {
  border-color: color-mix(in oklch, var(--gold), transparent 20%);
}

.combatant-card--target {
  border-color: var(--blood);
  box-shadow: 0 0 24px color-mix(in oklch, var(--blood), transparent 70%);
}

.combatant-card--defeated {
  opacity: 0.45;
  filter: grayscale(0.8);
}

.combatant-card header {
  display: flex;
  justify-content: space-between;
  gap: var(--space-3);
}

.combatant-card h3 {
  margin: var(--space-1) 0;
  color: var(--ink);
}

.combatant-card__turn,
.combatant-card__target {
  font-family: var(--font-mono);
  font-size: 0.65rem;
  letter-spacing: 0.18em;
  text-transform: uppercase;
}

.combatant-card__turn {
  color: var(--gold);
}

.combatant-card__target {
  color: var(--blood);
}

.combatant-card__portrait {
  height: 7rem;
  display: grid;
  place-items: center;
  margin: var(--space-4) 0;
  color: var(--ink-4);
  border: 1px solid color-mix(in oklch, var(--line), transparent 45%);
  background: radial-gradient(circle, var(--panel-2), var(--void));
  font-family: var(--font-mono);
  font-size: 0.65rem;
  text-transform: uppercase;
}

.combatant-card__health {
  height: 0.45rem;
  background: var(--void);
  border: 1px solid color-mix(in oklch, var(--line), transparent 30%);
}

.combatant-card__health-fill {
  height: 100%;
  background: var(--blood);
}

.combatant-card footer {
  display: grid;
  gap: var(--space-1);
  margin-top: var(--space-3);
  color: var(--ink-3);
  font-family: var(--font-mono);
  font-size: 0.7rem;
}
</style>