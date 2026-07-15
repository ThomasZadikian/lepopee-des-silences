<script setup lang="ts">
import { computed, ref } from 'vue';
import type { CombatantRuntimeDto } from '../types/combatContracts';
import { useClickOutside } from '../../../shared/composables/useClickOutside';
import { statDescriptions } from '../../party/constants/statDescriptions';
import AtbGauge from './AtbGauge.vue';
import EmotionalTypeBadge from './EmotionalTypeBadge.vue';
import StatTooltip from '../../../shared/components/StatTooltip.vue';
import StatusEffectToken from '../../../shared/components/StatusEffectToken.vue';

const showDetails = ref(false);
const detailsPopoverRef = ref<HTMLElement | null>(null);

function toggleDetails(event: Event) {
  event.stopPropagation();
  showDetails.value = !showDetails.value;
}

useClickOutside(detailsPopoverRef, () => { showDetails.value = false; }, {
  ignoreSelectors: ['.presence__details-trigger'],
});

const props = defineProps<{
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
  isMagicHit: boolean;
  isCriticalHit: boolean;
  isMissed: boolean;
  /** Highest threat value among living allies — this card's bar renders relative to it. */
  maxThreat?: number;
}>();

// Net Speed StatModifier currently active on this combatant — drives the ATB
// gauge's halo (task: "les effets qui influencent la jauge ATB soient visibles").
const speedEffect = computed<'boosted' | 'slowed' | null>(() => {
  const net = (props.combatant.statusEffects ?? [])
    .filter((effect) => effect.kind === 'StatModifier' && effect.stat === 'Speed')
    .reduce((sum, effect) => sum + effect.magnitude * effect.stacks, 0);
  if (net > 0) return 'boosted';
  if (net < 0) return 'slowed';
  return null;
});

// Net % StatModifier per stat — only percent-of-base effects are meaningful as a
// "%" badge (a flat-delta StatModifier wouldn't read correctly as a percentage).
function statModifierPercent(
  stat: 'AttackPower' | 'Defense' | 'Speed' | 'Focus' | 'MagicAttack' | 'MagicDefense',
): number {
  return (props.combatant.statusEffects ?? [])
    .filter((effect) => effect.kind === 'StatModifier' && effect.stat === stat && effect.isMagnitudePercentOfBaseStat)
    .reduce((sum, effect) => sum + effect.magnitude * effect.stacks, 0);
}

const attackModifierPercent = computed(() => statModifierPercent('AttackPower'));
const defenseModifierPercent = computed(() => statModifierPercent('Defense'));
const speedModifierPercent = computed(() => statModifierPercent('Speed'));
const focusModifierPercent = computed(() => statModifierPercent('Focus'));
const magicAttackModifierPercent = computed(() => statModifierPercent('MagicAttack'));
const magicDefenseModifierPercent = computed(() => statModifierPercent('MagicDefense'));

defineEmits<{
  select: [combatantId: string];
}>();

function hpRatio(c: CombatantRuntimeDto): number {
  return c.maxVitality > 0 ? c.currentVitality / c.maxVitality : 0;
}

// The guard segment sits right after the HP fill, both sized relative to
// maxVitality. When their sum would exceed 100% (e.g. a combatant at or
// near full HP still carrying guard), scale both down proportionally
// instead of clamping guard alone — clamping guard alone made it vanish
// visually any time HP was already full, even with a large guard value.
function barSegments(c: CombatantRuntimeDto): { hp: number; guard: number } {
  const hp = hpRatio(c);
  const guard = c.maxVitality > 0 ? c.guard / c.maxVitality : 0;
  const total = hp + guard;
  const scale = total > 1 ? 1 / total : 1;
  return { hp: hp * scale * 100, guard: guard * scale * 100 };
}

// Threat only means anything on the player side (enemies never accrue it —
// only player actions build aggro). Bar fills relative to the highest threat
// currently held among living allies; the ally holding it gets flagged as the
// most likely enemy target.
const showThreat = computed(() =>
  props.combatant.side === 'Player' && (props.maxThreat ?? 0) > 0,
);

const threatRatio = computed(() => {
  const max = props.maxThreat ?? 0;
  if (max <= 0) return 0;
  return Math.min(1, (props.combatant.threatValue ?? 0) / max);
});

const hasAggro = computed(() =>
  showThreat.value && (props.combatant.threatValue ?? 0) >= (props.maxThreat ?? 0),
);
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
      'presence--critical-hit': isCriticalHit,
      'presence--missed': isMissed,
      'presence--ally': combatant.side === 'Player',
      'presence--enemy': combatant.side === 'Enemy',
      'presence--active-player': isActivePlayer,
    }"
    :disabled="combatant.status === 'Defeated' || !isSelectable"
    @click="$emit('select', combatant.id)"
  >
    <span
      v-if="isDamaged"
      class="presence__hit-fx"
      :class="{ 'presence__hit-fx--magic': isMagicHit, 'presence__hit-fx--critical': isCriticalHit }"
      aria-hidden="true"
    >{{ isMagicHit ? '✦' : '⚔' }}</span>
    <span v-if="isGuarded" class="presence__guard-fx" aria-hidden="true">⛨</span>
    <span v-if="isMissed" class="presence__miss-fx" aria-hidden="true">Manqué</span>

    <div class="presence__atb-tube" aria-hidden="true">
      <AtbGauge
        :gauge="combatant.atbGauge ?? 0"
        :fill-per-tick="combatant.atbFillPerTick ?? 10"
        :active="isCurrentActor"
        :speed-effect="speedEffect"
      />
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

      <div v-if="showDetails" ref="detailsPopoverRef" class="presence__details-popover" @click.stop>
        <button class="presence__details-close" @click="toggleDetails" aria-label="Fermer">✕</button>
        <div class="presence__details-row">
          <span>⚔</span><b>{{ combatant.attackPower ?? 0 }}</b>
          <StatTooltip :text="statDescriptions.AttackPower"><small>Attaque</small></StatTooltip>
          <span
            v-if="attackModifierPercent !== 0"
            class="presence__stat-mod"
            :class="attackModifierPercent > 0 ? 'presence__stat-mod--up' : 'presence__stat-mod--down'"
          >{{ attackModifierPercent > 0 ? '▲' : '▼' }} {{ Math.abs(attackModifierPercent) }}%</span>
        </div>
        <div class="presence__details-row">
          <span>⛨</span><b>{{ combatant.defense ?? 0 }}</b>
          <StatTooltip :text="statDescriptions.Defense"><small>Défense</small></StatTooltip>
          <span
            v-if="defenseModifierPercent !== 0"
            class="presence__stat-mod"
            :class="defenseModifierPercent > 0 ? 'presence__stat-mod--up' : 'presence__stat-mod--down'"
          >{{ defenseModifierPercent > 0 ? '▲' : '▼' }} {{ Math.abs(defenseModifierPercent) }}%</span>
        </div>
        <div class="presence__details-row">
          <span>⚡</span><b>{{ combatant.speed ?? 0 }}</b>
          <StatTooltip :text="statDescriptions.Speed"><small>Vitesse</small></StatTooltip>
          <span
            v-if="speedModifierPercent !== 0"
            class="presence__stat-mod"
            :class="speedModifierPercent > 0 ? 'presence__stat-mod--up' : 'presence__stat-mod--down'"
          >{{ speedModifierPercent > 0 ? '▲' : '▼' }} {{ Math.abs(speedModifierPercent) }}%</span>
        </div>
        <div class="presence__details-row">
          <span>◎</span><b>{{ combatant.focus ?? 0 }}</b>
          <StatTooltip :text="statDescriptions.Focus"><small>Focus</small></StatTooltip>
          <span
            v-if="focusModifierPercent !== 0"
            class="presence__stat-mod"
            :class="focusModifierPercent > 0 ? 'presence__stat-mod--up' : 'presence__stat-mod--down'"
          >{{ focusModifierPercent > 0 ? '▲' : '▼' }} {{ Math.abs(focusModifierPercent) }}%</span>
        </div>
        <div class="presence__details-row">
          <span>✨</span><b>{{ combatant.magicAttack ?? 0 }}</b>
          <StatTooltip :text="statDescriptions.MagicAttack"><small>Attaque magique</small></StatTooltip>
          <span
            v-if="magicAttackModifierPercent !== 0"
            class="presence__stat-mod"
            :class="magicAttackModifierPercent > 0 ? 'presence__stat-mod--up' : 'presence__stat-mod--down'"
          >{{ magicAttackModifierPercent > 0 ? '▲' : '▼' }} {{ Math.abs(magicAttackModifierPercent) }}%</span>
        </div>
        <div class="presence__details-row">
          <span>🔮</span><b>{{ combatant.magicDefense ?? 0 }}</b>
          <StatTooltip :text="statDescriptions.MagicDefense"><small>Défense magique</small></StatTooltip>
          <span
            v-if="magicDefenseModifierPercent !== 0"
            class="presence__stat-mod"
            :class="magicDefenseModifierPercent > 0 ? 'presence__stat-mod--up' : 'presence__stat-mod--down'"
          >{{ magicDefenseModifierPercent > 0 ? '▲' : '▼' }} {{ Math.abs(magicDefenseModifierPercent) }}%</span>
        </div>
      </div>
    </span>

    <div class="presence__topline">
      <EmotionalTypeBadge :type="combatant.attackType ?? 'Neutral'" />
      <span
        v-if="hasAggro"
        class="presence__state presence__state--aggro"
        title="Cible la plus menacée : les ennemis viseront probablement ici"
      >⚠ menace</span>
      <span v-if="isActivePlayer" class="presence__state presence__state--ready">PRÊT</span>
      <span v-else-if="isSelectedTarget" class="presence__state presence__state--target">cible</span>
      <span v-else-if="combatant.status === 'Defeated'" class="presence__state presence__state--dead">abattu</span>
    </div>

    <span class="presence__name">{{ combatant.displayName }}</span>

     <div v-if="(combatant.statusEffects?.length ?? 0) > 0" class="presence__fx">
      <StatusEffectToken
        v-for="fx in combatant.statusEffects"
        :key="fx.key"
        class="presence__fx-badge"
        :kind="fx.kind"
        :magnitude="fx.magnitude"
        :stacks="fx.stacks"
        :per-tick-amount="fx.perTickAmount"
        :ticks-remaining="fx.ticksRemaining"
        :is-permanent="fx.isPermanent"
        :px="26"
      />
    </div>

    <div class="presence__gauge">
      <div class="presence__gauge-fill" :style="{ width: barSegments(combatant).hp + '%' }" />
      <div v-if="combatant.guard > 0" class="presence__gauge-guard" :style="{ width: barSegments(combatant).guard + '%' }" />
    </div>

    <div class="presence__stats">
      <span class="presence__stat presence__stat--hp">PV {{ combatant.currentVitality }} / {{ combatant.maxVitality }}</span>
      <span v-if="combatant.guard > 0" class="presence__stat presence__stat--guard">⛨ {{ combatant.guard }}</span>
      <span v-if="combatant.side === 'Player'" class="presence__stat presence__stat--breath">{{ combatant.mana }} PP</span>
    </div>

    <div
      v-if="showThreat"
      class="presence__threat"
      :class="{ 'presence__threat--aggro': hasAggro }"
      :title="`Menace : ${Math.round(combatant.threatValue ?? 0)}`"
    >
      <div class="presence__threat-fill" :style="{ width: (threatRatio * 100) + '%' }" />
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

.presence__stat-mod {
  font-family: var(--font-mono);
  font-size: 0.58rem;
  font-weight: 600;
  letter-spacing: 0.02em;
  white-space: nowrap;
}

.presence__stat-mod--up { color: var(--frost); }
.presence__stat-mod--down { color: var(--blood); }

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
.presence--critical-hit { animation: shake 420ms ease-out, crit-flare 550ms ease-out; }
.presence--missed { animation: dodge 500ms ease-out; }

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

.presence__gauge {
  grid-column: 3;
  display: flex;
  height: 7px;
  background: var(--panel);
  border-radius: 3px;
  overflow: hidden;
}

.presence__gauge-fill {
  flex: 0 0 auto;
  height: 100%;
  background: var(--blood);
  transition: width 0.35s ease;
}

.presence--ally .presence__gauge-fill { background: var(--frost-dim); }

.presence__gauge-guard {
  flex: 0 0 auto;
  height: 100%;
  background: var(--gold);
  box-shadow: inset 0 0 4px oklch(0.08 0.015 48 / 0.4);
  transition: width 0.35s ease;
}

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

.presence__stat--guard {
  font-size: 0.78rem;
  font-weight: 700;
  color: var(--gold);
  text-shadow: 0 0 8px color-mix(in oklch, var(--gold), transparent 60%);
}
.presence__stat--breath { color: var(--ink-5); }

/* ── Threat / aggro ──────────────────────────────────────────────────────── */
.presence__state--aggro {
  color: var(--blood);
  border-color: color-mix(in oklch, var(--blood), transparent 40%);
  background: color-mix(in oklch, var(--blood), transparent 88%);
}

.presence__threat {
  grid-column: 3;
  height: 4px;
  margin-top: 2px;
  background: var(--panel);
  border-radius: 2px;
  overflow: hidden;
}

.presence__threat-fill {
  height: 100%;
  background: color-mix(in oklch, var(--blood), var(--ink-4) 35%);
  transition: width 0.35s ease, background 0.25s ease;
}

.presence__threat--aggro .presence__threat-fill {
  background: var(--blood);
  box-shadow: 0 0 6px color-mix(in oklch, var(--blood), transparent 40%);
}

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

/* ── Hit / guard strike effects ──────────────────────────────────────────── */
.presence__hit-fx,
.presence__guard-fx {
  position: absolute;
  top: 45%;
  left: 62%;
  transform: translate(-50%, -50%);
  z-index: 7;
  font-size: 1.7rem;
  pointer-events: none;
}

.presence__hit-fx {
  color: var(--blood);
  text-shadow: 0 0 14px color-mix(in oklch, var(--blood), transparent 30%);
  animation: hit-pop 450ms ease-out forwards;
}

.presence__guard-fx {
  color: var(--gold);
  text-shadow: 0 0 14px color-mix(in oklch, var(--gold), transparent 30%);
  animation: guard-pop 550ms ease-out forwards;
}

/* Magic-category hit: frost/violet glyph instead of the physical blade. */
.presence__hit-fx--magic {
  color: var(--frost);
  text-shadow:
    0 0 14px color-mix(in oklch, var(--frost), transparent 25%),
    0 0 24px color-mix(in oklch, oklch(0.62 0.19 300), transparent 50%);
}

/* Critical hit: bigger, hotter glow layered on top of either variant. */
.presence__hit-fx--critical {
  font-size: 2.3rem;
  text-shadow:
    0 0 16px color-mix(in oklch, var(--gold), transparent 15%),
    0 0 28px color-mix(in oklch, var(--blood), transparent 40%);
}

.presence__miss-fx {
  position: absolute;
  top: 45%;
  left: 62%;
  transform: translate(-50%, -50%);
  z-index: 7;
  font-family: var(--font-caps);
  font-size: 0.62rem;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ink-4);
  pointer-events: none;
  animation: miss-fade 500ms ease-out forwards;
}

@keyframes hit-pop {
  0% { opacity: 0; transform: translate(-50%, -50%) scale(0.4) rotate(-18deg); }
  30% { opacity: 1; transform: translate(-50%, -50%) scale(1.2) rotate(10deg); }
  100% { opacity: 0; transform: translate(-50%, -50%) scale(1.35) rotate(10deg); }
}

@keyframes guard-pop {
  0% { opacity: 0; transform: translate(-50%, -50%) scale(0.5); }
  35% { opacity: 1; transform: translate(-50%, -50%) scale(1.3); }
  100% { opacity: 0; transform: translate(-50%, -50%) scale(1.05); }
}

@keyframes miss-fade {
  0% { opacity: 0; transform: translate(-50%, -50%) translateX(-8px); }
  30% { opacity: 1; transform: translate(-50%, -50%) translateX(0); }
  100% { opacity: 0; transform: translate(-50%, -50%) translateX(8px); }
}

@keyframes crit-flare {
  0% { box-shadow: 0 0 0 color-mix(in oklch, var(--gold), transparent 100%); }
  35% { box-shadow: 0 0 22px color-mix(in oklch, var(--gold), transparent 15%); }
  100% { box-shadow: 0 0 0 color-mix(in oklch, var(--gold), transparent 100%); }
}

@keyframes dodge {
  0% { transform: translateX(0); }
  30% { transform: translateX(8px); }
  60% { transform: translateX(-4px); }
  100% { transform: translateX(0); }
}

@media (prefers-reduced-motion: reduce) {
  .presence, .presence--selected, .presence--damaged, .presence--guarded, .presence--just-defeated, .presence--thinking,
  .presence--critical-hit, .presence--missed {
    animation: none;
    transition: none;
  }

  .presence__hit-fx, .presence__guard-fx, .presence__miss-fx {
    display: none;
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
