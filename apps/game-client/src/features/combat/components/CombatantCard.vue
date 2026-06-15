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

function hpRatio(c: CombatantRuntimeDto): number {
  return c.maxVitality > 0 ? c.currentVitality / c.maxVitality : 0;
}
</script>

<template>
  <button
    class="presence"
    :class="{
      'presence--active': isCurrentActor,
      'presence--selected': isSelectedTarget,
      'presence--selectable': isSelectable,
      'presence--invalid': isInvalidTarget,
      'presence--thinking': isThinking,
      'presence--acting': isActing,
      'presence--damaged': isDamaged,
      'presence--guarded': isGuarded,
      'presence--defeated': combatant.status === 'Defeated',
      'presence--just-defeated': isJustDefeated,
      'presence--ally': combatant.side === 'Player',
      'presence--enemy': combatant.side === 'Enemy',
    }"
    :disabled="combatant.status === 'Defeated' || !isSelectable"
    @click="$emit('select', combatant.id)"
  >
    <span class="presence__archetype">{{ combatant.archetype }}</span>
    <span class="presence__name">{{ combatant.displayName }}</span>

    <!-- Subtle HP gauge -->
    <div class="presence__gauge">
      <div class="presence__gauge-fill" :style="{ width: hpRatio(combatant) * 100 + '%' }" />
    </div>

    <div class="presence__stats">
      <span class="presence__stat presence__stat--hp">{{ combatant.currentVitality }}/{{ combatant.maxVitality }}</span>
      <span v-if="combatant.guard > 0" class="presence__stat presence__stat--guard">🛡 {{ combatant.guard }}</span>
    </div>

    <!-- Defeated overlay -->
    <div v-if="combatant.status === 'Defeated'" class="presence__defeated">
      <span>Abattu</span>
    </div>

    <!-- Actor/thinking indicators -->
    <span v-if="isCurrentActor && combatant.side === 'Player'" class="presence__badge presence__badge--frost">Tour</span>
    <span v-if="isThinking" class="presence__badge presence__badge--gold">…</span>
  </button>
</template>

<style scoped>
.presence {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
  width: 100%;
  min-width: 140px;
  padding: var(--space-2) var(--space-3);
  text-align: left;
  background: var(--card-soft);
  border: 1px solid var(--line-soft);
  border-radius: var(--radius-sm);
  cursor: default;
  font-family: inherit;
  color: var(--ink);
  transition: border-color 0.18s ease, background 0.18s ease, opacity 0.18s ease, transform 0.3s ease, box-shadow 0.3s ease;
}

.presence--ally {
  border-left: 2px solid var(--edge-frost);
  min-height: 80px;
}

.presence--enemy {
  border-right: 2px solid color-mix(in oklch, var(--blood), transparent 50%);
  transform: var(--enemy-jitter, none);
}

.presence--selectable { cursor: pointer; }
.presence--selectable:hover { border-color: var(--edge-gold); background: var(--wash-gold); }

.presence--selected {
  border-color: var(--gold) !important;
  box-shadow: 0 0 12px oklch(0.862 0.098 86 / 0.25);
}

.presence--active {
  border-color: var(--edge-frost) !important;
  box-shadow: 0 0 16px oklch(0.846 0.100 276 / 0.2);
  transform: translateX(12px) scale(1.04);
  z-index: 2;
}

.presence--thinking {
  border-color: var(--edge-gold) !important;
  animation: think-pulse 1.2s ease-in-out infinite;
}

.presence--invalid { opacity: 0.45; }

.presence--defeated {
  opacity: 0.35;
  filter: grayscale(0.8);
  pointer-events: none;
}

.presence--damaged { animation: shake 420ms ease-out; }
.presence--guarded { animation: flare 700ms ease-out; }
.presence--just-defeated { animation: defeat-fade 900ms ease-out; }

.presence__archetype {
  font-family: var(--font-caps);
  font-size: 0.52rem;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.presence__name {
  font-family: var(--font);
  font-size: 0.88rem;
  color: var(--ink-2);
  line-height: 1.2;
}

.presence__gauge {
  height: 3px;
  background: var(--panel);
  border-radius: 2px;
  overflow: hidden;
  margin-top: var(--space-1);
}

.presence__gauge-fill {
  height: 100%;
  background: var(--blood);
  border-radius: 2px;
  transition: width 0.35s ease;
}

.presence--ally .presence__gauge-fill { background: var(--frost-dim); }

.presence__stats {
  display: flex;
  gap: var(--space-2);
  align-items: center;
}

.presence__stat {
  font-family: var(--font-mono);
  font-size: 0.62rem;
  color: var(--ink-4);
}

.presence__stat--guard { color: var(--frost-dim); }

.presence__defeated {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: oklch(0.15 0.03 272 / 0.7);
  border-radius: inherit;
}

.presence__defeated span {
  font-family: var(--font-caps);
  font-size: 0.6rem;
  letter-spacing: 0.2em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.presence__badge {
  position: absolute;
  top: var(--space-1);
  right: var(--space-1);
  font-family: var(--font-caps);
  font-size: 0.5rem;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  padding: 1px 4px;
  border: 1px solid;
  border-radius: 2px;
}

.presence__badge--frost { color: var(--frost); border-color: var(--edge-frost); }
.presence__badge--gold { color: var(--gold); border-color: var(--edge-gold); }

@keyframes think-pulse {
  0%, 100% { filter: brightness(1); }
  50% { filter: brightness(1.15); }
}

@keyframes shake {
  0% { transform: translateX(0); }
  20% { transform: translateX(-4px); }
  45% { transform: translateX(3px); }
  70% { transform: translateX(-1px); }
  100% { transform: translateX(0); }
}

@keyframes flare {
  0% { box-shadow: 0 0 0 oklch(0.846 0.100 276 / 0); }
  40% { box-shadow: 0 0 18px oklch(0.846 0.100 276 / 0.3); }
  100% { box-shadow: 0 0 0 oklch(0.846 0.100 276 / 0); }
}

@keyframes defeat-fade {
  0% { opacity: 1; filter: grayscale(0); }
  100% { opacity: 0.35; filter: grayscale(0.8); }
}

@media (prefers-reduced-motion: reduce) {
  .presence, .presence--selected, .presence--damaged, .presence--guarded, .presence--just-defeated, .presence--thinking {
    animation: none;
    transition: none;
  }
}
</style>
