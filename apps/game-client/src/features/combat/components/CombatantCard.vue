<script setup lang="ts">
import type { CombatantRuntimeDto } from '../types/combatContracts';

defineProps<{
  combatant: CombatantRuntimeDto;
  isCurrentActor: boolean;
  isSelectedTarget: boolean;
  isSelectable: boolean;
  isActivePlayer: boolean;
}>();

defineEmits<{
  select: [combatantId: string];
}>();
</script>

<template>
  <button
    class="combatant-card"
    :class="{
      'combatant-card--active': isCurrentActor,
      'combatant-card--targeted': isSelectedTarget,
      'combatant-card--selectable': isSelectable,
      'combatant-card--defeated': combatant.status === 'Defeated',
      'combatant-card--player': combatant.side === 'Player',
      'combatant-card--enemy': combatant.side === 'Enemy',
    }"
    :disabled="combatant.status === 'Defeated' || !isSelectable"
    @click="$emit('select', combatant.id)"
  >
    <div class="combatant-card__frame">
      <div class="combatant-card__archetype">
        {{ combatant.archetype }}
      </div>

      <div class="combatant-card__name">
        {{ combatant.displayName }}
      </div>

      <div class="combatant-card__bars">
        <div class="combatant-card__bar combatant-card__bar--hp">
          <span class="combatant-card__bar-label">
            {{ combatant.currentVitality }} / {{ combatant.maxVitality }}
          </span>
          <div class="combatant-card__bar-track">
            <div
              class="combatant-card__bar-fill"
              :style="{ width: (combatant.currentVitality / combatant.maxVitality) * 100 + '%' }"
            />
          </div>
        </div>

        <div
          v-if="combatant.guard > 0"
          class="combatant-card__bar combatant-card__bar--guard"
        >
          <span class="combatant-card__bar-label">GUARD {{ combatant.guard }}</span>
          <div class="combatant-card__bar-track">
            <div
              class="combatant-card__bar-fill"
              :style="{ width: Math.min((combatant.guard / combatant.maxVitality) * 100, 100) + '%' }"
            />
          </div>
        </div>
      </div>

      <div
        v-if="combatant.status === 'Defeated'"
        class="combatant-card__defeated-overlay"
      >
        <p>ABATTU</p>
      </div>

      <div
        v-if="isCurrentActor && combatant.side === 'Player'"
        class="combatant-card__actor-indicator"
      >
        <span>TOUR</span>
      </div>
    </div>
  </button>
</template>

<style scoped>
.combatant-card {
  position: relative;
  display: block;
  width: 100%;
  text-align: left;
  border: 1px solid var(--color-line);
  border-radius: var(--radius-md);
  background: var(--color-panel);
  padding: var(--space-3);
  cursor: default;
  transition: border-color 0.15s, background 0.15s, box-shadow 0.15s;
  font-family: inherit;
  color: var(--color-ink);
}

.combatant-card--player {
  border-color: color-mix(in oklch, var(--color-frost), transparent 60%);
}

.combatant-card--enemy {
  border-color: color-mix(in oklch, var(--color-blood), transparent 60%);
}

.combatant-card--selectable {
  cursor: pointer;
}

.combatant-card--selectable:hover {
  border-color: var(--color-gold);
  background: color-mix(in oklch, var(--color-gold), transparent 92%);
}

.combatant-card--targeted {
  border-color: var(--color-gold) !important;
  box-shadow: 0 0 0 1px var(--color-gold);
  background: color-mix(in oklch, var(--color-gold), transparent 88%);
}

.combatant-card--active {
  border-color: var(--color-frost) !important;
}

.combatant-card--defeated {
  opacity: 0.4;
  pointer-events: none;
}

.combatant-card__frame {
  display: grid;
  gap: var(--space-2);
}

.combatant-card__archetype {
  font-size: 0.65rem;
  text-transform: uppercase;
  letter-spacing: 0.12em;
  color: var(--color-dim);
}

.combatant-card__name {
  font-weight: 600;
  letter-spacing: 0.04em;
  line-height: 1.2;
}

.combatant-card__bars {
  display: grid;
  gap: var(--space-1);
}

.combatant-card__bar {
  display: grid;
  gap: var(--space-1);
}

.combatant-card__bar-label {
  font-size: 0.7rem;
  color: var(--color-muted);
  font-family: var(--font-mono);
}

.combatant-card__bar-track {
  height: 6px;
  border-radius: var(--radius-sm);
  background: var(--color-panel-soft);
  overflow: hidden;
}

.combatant-card__bar-fill {
  height: 100%;
  border-radius: var(--radius-sm);
  transition: width 0.3s ease;
}

.combatant-card__bar--hp .combatant-card__bar-fill {
  background: var(--color-blood);
}

.combatant-card__bar--guard .combatant-card__bar-fill {
  background: var(--color-frost);
}

.combatant-card__defeated-overlay {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: color-mix(in oklch, var(--color-void), transparent 30%);
  border-radius: var(--radius-md);
}

.combatant-card__defeated-overlay p {
  font-size: 0.75rem;
  letter-spacing: 0.2em;
  color: var(--color-dim);
}

.combatant-card__actor-indicator {
  position: absolute;
  top: var(--space-1);
  right: var(--space-1);
  font-size: 0.6rem;
  letter-spacing: 0.15em;
  color: var(--color-frost);
  border: 1px solid var(--color-frost);
  padding: 1px 5px;
  border-radius: var(--radius-sm);
}
</style>
