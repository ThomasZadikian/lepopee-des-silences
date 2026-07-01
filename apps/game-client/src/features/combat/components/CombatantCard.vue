<script setup lang="ts">
import { ref } from 'vue';
import type { CombatantRuntimeDto } from '../types/combatContracts';
import AtbGauge from './AtbGauge.vue';
import EmotionalTypeBadge from './EmotionalTypeBadge.vue';
import StatusEffectToken from '../../../shared/components/StatusEffectToken.vue';

const showDetails = ref(false);

function toggleDetails(event: Event) {
  event.stopPropagation();
  showDetails.value = !showDetails.value;
}

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
      'presence--active-player': isActivePlayer,
    }"
    :disabled="combatant.status === 'Defeated' || !isSelectable"
    @click="$emit('select', combatant.id)"
  >
    <div class="presence__atb-tube" aria-hidden="true">
      <AtbGauge :gauge="combatant.atbGauge ?? 0" :fill-per-tick="combatant.atbFillPerTick ?? 10" :active="isCurrentActor" />
    </div>

    <span class="presence__portrait" aria-hidden="true">
      <span
        class="presence__details-trigger"
        role="button"
        tabindex="0"
        title="Détails des statistiques"
        @click="toggleDetails"
        @keydown.enter="toggleDetails"
        @keydown.space.prevent="toggleDetails"
      >◎</span>

      <div v-if="showDetails" class="presence__details-popover" @click.stop>
        <button class="presence__details-close" @click="toggleDetails" aria-label="Fermer">✕</button>
        <div class="presence__details-row"><span>⚔</span><b>{{ combatant.attackPower ?? 0 }}</b><small>Attaque</small></div>
        <div class="presence__details-row"><span>⛨</span><b>{{ combatant.defense ?? 0 }}</b><small>Défense</small></div>
        <div class="presence__details-row"><span>⚡</span><b>{{ combatant.speed ?? 0 }}</b><small>Vitesse</small></div>
        <div class="presence__details-row"><span>◎</span><b>{{ combatant.focus ?? 0 }}</b><small>Focus</small></div>
      </div>
    </span>

    <div class="presence__topline">
      <EmotionalTypeBadge :type="combatant.attackType ?? 'Neutral'" />
      <span v-if="isActivePlayer" class="presence__state presence__state--ready">PRÊT</span>
      <span v-else-if="isSelectedTarget" class="presence__state presence__state--target">cible</span>
      <span v-else-if="combatant.status === 'Defeated'" class="presence__state presence__state--dead">abattu</span>
    </div>

    <span class="presence__name">{{ combatant.displayName }}</span>

    <div class="presence__tags" aria-hidden="true">
      <span v-if="combatant.guard > 0" class="presence__tag presence__tag--guard">Garde</span>
    </div>

     <div v-if="(combatant.statusEffects?.length ?? 0) > 0" class="presence__fx">
      <StatusEffectToken
        v-for="fx in combatant.statusEffects"
        :key="fx.key"
        class="presence__fx-badge"
        :kind="fx.kind"
        :magnitude="fx.magnitude"
        :stacks="fx.stacks"
        :px="26"
      />
    </div>

    <div class="presence__gauge">
      <div class="presence__gauge-fill" :style="{ width: hpRatio(combatant) * 100 + '%' }" />
    </div>

    <div class="presence__stats">
      <span class="presence__stat presence__stat--hp">PV {{ combatant.currentVitality }} / {{ combatant.maxVitality }}</span>
      <span v-if="combatant.guard > 0" class="presence__stat presence__stat--guard">{{ combatant.guard }}</span>
      <span v-if="combatant.side === 'Player'" class="presence__stat presence__stat--breath">{{ combatant.mana }} souffle</span>
    </div>

    <slot />
  </button>
</template>

<style scoped>
.presence {
  position: relative;
  display: grid;
  grid-template-columns: 18px 3.5rem minmax(0, 1fr);
  align-items: center;
  gap: var(--space-1);
  width: 100%;
  min-width: 18rem;
  min-height: 4.7rem;
  padding: var(--space-2);
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
}

.presence__atb-tube {
  grid-row: 1 / span 6;
  grid-column: 1;
  align-self: stretch;
  min-height: 5.6rem;
}

.presence__portrait {
  position: relative;
  grid-row: 1 / span 5;
  grid-column: 2;
  width: 3.2rem;
  height: 3.2rem;
  border: 1px solid var(--edge-frost);
  border-radius: 4px;
  background:
    radial-gradient(circle at 65% 20%, var(--gold), transparent 8%),
    radial-gradient(circle at 45% 36%, oklch(0.5 0.06 252 / 0.38), transparent 48%),
    linear-gradient(145deg, var(--void), var(--raise));
  box-shadow: inset 0 0 20px oklch(0 0 0 / 0.45);
}

.presence__details-trigger {
  position: absolute;
  right: -5px;
  bottom: -5px;
  width: 16px;
  height: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  border: 1px solid var(--edge-gold);
  background: var(--raise);
  color: var(--gold);
  font-size: 9px;
  line-height: 1;
  cursor: pointer;
  z-index: 3;
}

.presence__details-trigger:hover,
.presence__details-trigger:focus-visible {
  background: var(--wash-gold);
  color: var(--gold-hi);
  outline: none;
}

.presence__details-popover {
  position: absolute;
  top: calc(100% + 8px);
  left: 0;
  z-index: 6;
  display: flex;
  flex-direction: column;
  gap: 6px;
  min-width: 8rem;
  padding: 10px 12px;
  border: 1px solid var(--edge-gold);
  border-radius: var(--radius-sm);
  background: linear-gradient(150deg, var(--raise), var(--void));
  box-shadow: var(--shadow-deep);
  cursor: default;
}

.presence__details-close {
  position: absolute;
  top: 4px;
  right: 6px;
  border: none;
  background: none;
  color: var(--ink-4);
  font-size: 10px;
  cursor: pointer;
  padding: 2px;
}

.presence__details-close:hover { color: var(--ink); }

.presence__details-row {
  display: flex;
  align-items: baseline;
  gap: 7px;
  font-family: var(--font-mono);
  white-space: nowrap;
}

.presence__details-row span {
  color: var(--gold);
  font-size: 0.85rem;
  width: 1rem;
}

.presence__details-row b {
  color: var(--ink);
  font-size: 0.95rem;
  font-weight: 600;
}

.presence__details-row small {
  color: var(--ink-4);
  font-size: 0.6rem;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.presence--enemy {
  border-right: 2px solid color-mix(in oklch, var(--blood), transparent 50%);
  transform: var(--enemy-jitter, none);
}

.presence--selectable { cursor: pointer; }
.presence--selectable:hover { border-color: var(--edge-gold); background: var(--wash-gold); }

.presence--selected {
  border-color: var(--gold) !important;
  background: var(--wash-gold);
  box-shadow: 0 0 10px oklch(0.862 0.098 86 / 0.2);
}

.presence--active {
  border-color: var(--edge-frost) !important;
  box-shadow: 0 0 12px oklch(0.846 0.100 276 / 0.16);
  transform: translateX(5px);
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

.presence__topline {
  grid-column: 3;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-1);
  min-width: 0;
}

.presence__archetype {
  font-family: var(--font-caps);
  font-size: 0.48rem;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--ink-4);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.presence__state {
  flex: 0 0 auto;
  font-family: var(--font-caps);
  font-size: 0.42rem;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  padding: 1px 4px;
  border: 1px solid var(--line-soft);
  border-radius: 999px;
}

.presence__state--ready { color: var(--frost); border-color: var(--edge-frost); }
.presence__state--target { color: var(--gold); border-color: var(--edge-gold); }
.presence__state--dead { color: var(--blood); border-color: color-mix(in oklch, var(--blood), transparent 50%); }

.presence__name {
  grid-column: 3;
  font-family: var(--font);
  font-size: 0.82rem;
  color: var(--ink-2);
  line-height: 1.2;
}

.presence__tags {
  grid-column: 3;
  display: flex;
  justify-content: flex-end;
  flex-wrap: wrap;
  gap: 3px;
  min-height: 0.75rem;
}

.presence__tag {
  font-family: var(--font-caps);
  font-size: 0.44rem;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--ink-4);
  border: 1px solid var(--line-soft);
  border-radius: 999px;
  padding: 0 4px;
}

.presence__tag--guard { color: var(--frost-dim); border-color: color-mix(in oklch, var(--frost), transparent 62%); }
.presence__tag--blood { color: var(--blood); border-color: color-mix(in oklch, var(--blood), transparent 58%); }
.presence__tag--gold { color: var(--gold); border-color: color-mix(in oklch, var(--gold), transparent 58%); }

.presence__gauge {
  grid-column: 3;
  height: 7px;
  background: var(--panel);
  border-radius: 3px;
  overflow: hidden;
}

.presence__gauge-fill {
  height: 100%;
  background: var(--blood);
  border-radius: 2px;
  transition: width 0.35s ease;
}

.presence--ally .presence__gauge-fill { background: var(--frost-dim); }

.presence__stats {
  grid-column: 3;
  display: flex;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: var(--space-2);
  align-items: center;
}

.presence__stat {
  font-family: var(--font-mono);
  font-size: 0.62rem;
  color: var(--ink-4);
}

.presence__stat--guard { color: var(--frost-dim); }
.presence__stat--breath { color: var(--ink-5); }

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

/* ── Status effect badges ───────────────────────────────────────────────── */
.presence__fx {
  display: flex;
  flex-wrap: wrap;
  gap: 3px;
  justify-content: center;
  min-height: 1.1rem;
  margin-top: 2px;
}

.presence__fx-badge {
  flex: 0 0 auto;
}
</style>
