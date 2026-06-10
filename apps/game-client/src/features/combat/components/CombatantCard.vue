<script setup lang="ts">
import type { CombatantRuntimeDto } from '../types/combatContracts';

defineProps<{
  combatant: CombatantRuntimeDto;
  isCurrentActor: boolean;
  isSelectedTarget: boolean;
  isSelectable: boolean;
  isTargetable: boolean;
  isInvalidTarget: boolean;
  isActivePlayer: boolean;
  isThinking: boolean;
  isDamaged: boolean;
  isGuarded: boolean;
  isJustDefeated: boolean;
  isActing: boolean;
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
      'combatant-card--selected': isSelectedTarget,
      'combatant-card--targeted': isSelectedTarget,
      'combatant-card--selectable': isSelectable,
      'combatant-card--targetable': isTargetable,
      'combatant-card--invalid-target': isInvalidTarget,
      'combatant-card--thinking': isThinking,
      'combatant-card--enemy-thinking': isThinking,
      'combatant-card--acting': isActing,
      'combatant-card--damaged': isDamaged,
      'combatant-card--guarded': isGuarded,
      'combatant-card--just-defeated': isJustDefeated,
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

      <div
        v-if="isThinking"
        class="combatant-card__thinking-indicator"
      >
        <span>CHOIX...</span>
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
  transition: border-color 0.18s ease, background 0.18s ease, box-shadow 0.18s ease, opacity 0.18s ease, filter 0.18s ease;
  font-family: inherit;
  color: var(--color-ink);
}

.combatant-card--player {
  border-color: color-mix(in oklch, var(--color-frost), transparent 60%);
}

.combatant-card--enemy {
  border-color: color-mix(in oklch, var(--color-blood), transparent 60%);
}

.combatant-card--selectable,
.combatant-card--targetable {
  cursor: pointer;
}

.combatant-card--selectable:hover,
.combatant-card--targetable:hover {
  border-color: var(--color-gold);
  background: color-mix(in oklch, var(--color-gold), transparent 92%);
}

.combatant-card--invalid-target {
  opacity: 0.58;
  filter: saturate(0.72);
}

.combatant-card--selected,
.combatant-card--targeted {
  border-color: var(--color-gold) !important;
  box-shadow:
    0 0 0 1px color-mix(in oklch, var(--color-gold), transparent 45%),
    0 0 18px color-mix(in oklch, var(--color-gold), transparent 78%);
  background: color-mix(in oklch, var(--color-gold), transparent 90%);
}

.combatant-card--selected::after {
  content: "";
  position: absolute;
  inset: 0;
  border: 1px solid color-mix(in oklch, var(--color-gold), transparent 58%);
  border-radius: inherit;
  pointer-events: none;
  animation: target-aura-pulse 1.8s ease-in-out infinite;
}

.combatant-card--active {
  border-color: var(--color-frost) !important;
  box-shadow:
    0 0 0 1px color-mix(in oklch, var(--color-frost), transparent 48%),
    0 0 24px color-mix(in oklch, var(--color-frost), transparent 84%);
}

.combatant-card--thinking,
.combatant-card--enemy-thinking {
  border-color: var(--color-gold) !important;
  box-shadow: 0 0 0 1px var(--color-gold), 0 0 1.5rem color-mix(in oklch, var(--color-gold), transparent 70%);
  background: color-mix(in oklch, var(--color-gold), transparent 90%);
  animation: enemy-thinking-pulse 1.2s ease-in-out infinite;
}

.combatant-card--acting {
  border-color: color-mix(in oklch, var(--color-gold), transparent 25%) !important;
  box-shadow: 0 0 20px color-mix(in oklch, var(--color-gold), transparent 80%);
}

.combatant-card--damaged {
  animation: combat-damage-shake 420ms ease-out;
}

.combatant-card--guarded {
  animation: combat-guard-flare 700ms ease-out;
}

.combatant-card--defeated {
  opacity: 0.42;
  filter: grayscale(0.75);
  transform: translateY(4px);
  pointer-events: none;
}

.combatant-card--just-defeated {
  animation: combat-defeat-fade 900ms ease-out;
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

.combatant-card__thinking-indicator {
  position: absolute;
  top: var(--space-1);
  right: var(--space-1);
  font-size: 0.6rem;
  letter-spacing: 0.15em;
  color: var(--color-gold);
  border: 1px solid var(--color-gold);
  padding: 1px 5px;
  border-radius: var(--radius-sm);
}

@keyframes target-aura-pulse {
  0%, 100% {
    opacity: 0.45;
    transform: scale(1);
  }
  50% {
    opacity: 0.9;
    transform: scale(1.025);
  }
}

@keyframes enemy-thinking-pulse {
  0%, 100% { filter: brightness(1); }
  50% { filter: brightness(1.12); }
}

@keyframes combat-damage-shake {
  0% {
    transform: translateX(0);
    filter: brightness(1);
  }
  20% {
    transform: translateX(-5px);
    filter: brightness(1.22) saturate(1.08);
  }
  45% { transform: translateX(4px); }
  70% { transform: translateX(-2px); }
  100% {
    transform: translateX(0);
    filter: brightness(1);
  }
}

@keyframes combat-guard-flare {
  0% { box-shadow: 0 0 0 color-mix(in oklch, var(--color-frost), transparent 100%); }
  40% { box-shadow: 0 0 22px color-mix(in oklch, var(--color-frost), transparent 72%); }
  100% { box-shadow: 0 0 0 color-mix(in oklch, var(--color-frost), transparent 100%); }
}

@keyframes combat-defeat-fade {
  0% {
    opacity: 1;
    filter: grayscale(0);
    transform: translateY(0);
  }
  100% {
    opacity: 0.42;
    filter: grayscale(0.75);
    transform: translateY(4px);
  }
}

@media (prefers-reduced-motion: reduce) {
  .combatant-card,
  .combatant-card--selected::after,
  .combatant-card--damaged,
  .combatant-card--guarded,
  .combatant-card--just-defeated,
  .combatant-card--enemy-thinking {
    animation: none;
    transition: none;
  }
}
</style>
