<script setup lang="ts">
import type { CombatInstanceDto } from '../types/combatTypes';

const props = defineProps<{
  combat: CombatInstanceDto;
}>();

function getName(combatantId: string) {
  return props.combat.combatants.find((combatant) => combatant.id === combatantId)?.displayName
    ?? combatantId.slice(0, 8);
}
</script>

<template>
  <section class="turn-order">
    <p class="system-label">Initiative</p>

    <ol>
      <li
        v-for="combatantId in combat.turnOrder"
        :key="combatantId"
        :class="{ 'turn-order__current': combatantId === combat.currentActorId }"
      >
        {{ getName(combatantId) }}
      </li>
    </ol>
  </section>
</template>

<style scoped>
.turn-order {
  border-top: 1px solid color-mix(in oklch, var(--color-line), transparent 35%);
  border-bottom: 1px solid color-mix(in oklch, var(--color-line), transparent 35%);
  padding: var(--space-3) 0;
}

.turn-order ol {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
  padding: 0;
  margin: var(--space-2) 0 0;
  list-style: none;
}

.turn-order li {
  color: var(--color-muted);
  font-family: var(--font-mono);
  font-size: 0.72rem;
  text-transform: uppercase;
}

.turn-order li::after {
  content: '›';
  margin-left: var(--space-2);
  color: var(--color-dim);
}

.turn-order li:last-child::after {
  content: '';
}

.turn-order__current {
  color: var(--color-gold) !important;
}
</style>