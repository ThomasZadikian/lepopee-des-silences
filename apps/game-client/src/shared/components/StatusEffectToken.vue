<script setup lang="ts">
import { computed } from 'vue';

import type { StatusEffectKind } from '../../features/combat/types/combatContracts';

const props = withDefaults(
  defineProps<{
    kind: StatusEffectKind | string;
    /** Sign of the stat change — only meaningful for StatModifier (buff vs debuff). */
    magnitude?: number;
    stacks?: number;
    px?: number;
    /** Shows the effect label/duration underneath the sigil. */
    meta?: boolean;
    durLabel?: string;
  }>(),
  { magnitude: 0, stacks: 1, px: 40, meta: false, durLabel: '' },
);

type Shape = 'drop' | 'plus' | 'chevUp' | 'chevDown' | 'aster' | 'silence' | 'lock';

const KIND_META: Record<string, { color: string; label: string; shape: Shape }> = {
  DamageOverTime: { color: 'oklch(0.78 0.16 145)', label: 'Dégât continu', shape: 'drop' },
  HealOverTime: { color: 'oklch(0.82 0.13 200)', label: 'Soin continu', shape: 'plus' },
  'StatModifier+': { color: 'oklch(0.86 0.10 86)', label: 'Renforcement', shape: 'chevUp' },
  'StatModifier-': { color: 'oklch(0.78 0.16 25)', label: 'Affaiblissement', shape: 'chevDown' },
  Stun: { color: 'oklch(0.80 0.13 300)', label: 'Étourdissement', shape: 'aster' },
  Silence: { color: 'oklch(0.80 0.13 300)', label: 'Silence', shape: 'silence' },
  AtbLock: { color: 'oklch(0.80 0.13 300)', label: 'Jauge bloquée', shape: 'lock' },
};

const kindMeta = computed(() => {
  const key = props.kind === 'StatModifier' ? `StatModifier${props.magnitude >= 0 ? '+' : '-'}` : props.kind;
  return KIND_META[key] ?? KIND_META.DamageOverTime;
});

const glow = computed(() => `color-mix(in oklch, ${kindMeta.value.color}, transparent 64%)`);
const sigPx = computed(() => Math.round(props.px * 0.52));
const bar = computed(() => Math.max(2, Math.round(sigPx.value * 0.15)));
const showStacks = computed(() => props.stacks > 1);
const label = computed(() => props.durLabel || kindMeta.value.label);

const dropD = computed(() => Math.round(sigPx.value * 0.56));
const plusLen = computed(() => Math.round(sigPx.value * 0.74));
const chevLen = computed(() => Math.round(sigPx.value * 0.46));
const asterLen = computed(() => Math.round(sigPx.value * 0.92));
const ringD = computed(() => Math.round(sigPx.value * 0.62));
const slashLen = computed(() => Math.round(sigPx.value * 0.92));
const tubeW = computed(() => Math.round(sigPx.value * 0.44));
const tubeH = computed(() => Math.round(sigPx.value * 0.74));
const lockBar = computed(() => Math.round(sigPx.value * 0.66));
</script>

<template>
  <div class="sigil" :style="{ '--sigil-px': px + 'px' }" :title="stacks > 1 ? `${kindMeta.label} ×${stacks}` : kindMeta.label">
    <div class="sigil__chip">
      <div class="sigil__glow" :style="{ background: `radial-gradient(circle at 50% 38%, ${glow}, transparent 72%)` }" />
      <div class="sigil__hairline" :style="{ background: kindMeta.color }" />

      <div class="sigil__mark" :style="{ width: sigPx + 'px', height: sigPx + 'px' }">
        <div
          v-if="kindMeta.shape === 'drop'"
          class="sigil__drop"
          :style="{ width: dropD + 'px', height: dropD + 'px', background: kindMeta.color, boxShadow: `inset 0 0 5px oklch(0.10 0.03 60 / 0.4), 0 0 7px ${glow}` }"
        />

        <template v-else-if="kindMeta.shape === 'plus'">
          <div class="sigil__bar sigil__bar--h" :style="{ width: plusLen + 'px', height: bar + 'px', background: kindMeta.color }" />
          <div class="sigil__bar sigil__bar--v" :style="{ width: bar + 'px', height: plusLen + 'px', background: kindMeta.color }" />
        </template>

        <template v-else-if="kindMeta.shape === 'chevUp'">
          <span class="sigil__chev sigil__chev--ul" :style="{ width: chevLen + 'px', height: bar + 'px', background: kindMeta.color }" />
          <span class="sigil__chev sigil__chev--ur" :style="{ width: chevLen + 'px', height: bar + 'px', background: kindMeta.color }" />
          <span class="sigil__chev sigil__chev--ll" :style="{ width: chevLen + 'px', height: bar + 'px', background: kindMeta.color, opacity: 0.55 }" />
          <span class="sigil__chev sigil__chev--lr" :style="{ width: chevLen + 'px', height: bar + 'px', background: kindMeta.color, opacity: 0.55 }" />
        </template>

        <template v-else-if="kindMeta.shape === 'chevDown'">
          <span class="sigil__chev sigil__chev--dul" :style="{ width: chevLen + 'px', height: bar + 'px', background: kindMeta.color }" />
          <span class="sigil__chev sigil__chev--dur" :style="{ width: chevLen + 'px', height: bar + 'px', background: kindMeta.color }" />
          <span class="sigil__chev sigil__chev--dll" :style="{ width: chevLen + 'px', height: bar + 'px', background: kindMeta.color, opacity: 0.55 }" />
          <span class="sigil__chev sigil__chev--dlr" :style="{ width: chevLen + 'px', height: bar + 'px', background: kindMeta.color, opacity: 0.55 }" />
        </template>

        <template v-else-if="kindMeta.shape === 'aster'">
          <span class="sigil__aster" :style="{ width: asterLen + 'px', height: bar + 'px', background: kindMeta.color, transform: 'translate(-50%,-50%) rotate(0deg)' }" />
          <span class="sigil__aster" :style="{ width: asterLen + 'px', height: bar + 'px', background: kindMeta.color, transform: 'translate(-50%,-50%) rotate(60deg)' }" />
          <span class="sigil__aster" :style="{ width: asterLen + 'px', height: bar + 'px', background: kindMeta.color, transform: 'translate(-50%,-50%) rotate(120deg)' }" />
        </template>

        <template v-else-if="kindMeta.shape === 'silence'">
          <div class="sigil__ring" :style="{ width: ringD + 'px', height: ringD + 'px', borderWidth: bar + 'px', borderColor: kindMeta.color }" />
          <span class="sigil__slash" :style="{ width: slashLen + 'px', height: bar + 'px', background: kindMeta.color }" />
        </template>

        <template v-else-if="kindMeta.shape === 'lock'">
          <div class="sigil__tube" :style="{ width: tubeW + 'px', height: tubeH + 'px', borderColor: kindMeta.color }">
            <div class="sigil__tube-fill" :style="{ background: kindMeta.color }" />
          </div>
          <span class="sigil__lock-bar" :style="{ width: lockBar + 'px', height: bar + 'px', background: kindMeta.color }" />
        </template>
      </div>

      <div v-if="showStacks" class="sigil__stacks" :style="{ background: kindMeta.color }">{{ stacks }}</div>
    </div>

    <div v-if="meta" class="sigil__meta">{{ label }}</div>
  </div>
</template>

<style scoped>
.sigil {
  display: inline-flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
  font-family: var(--font);
}

.sigil__chip {
  position: relative;
  width: var(--sigil-px);
  height: var(--sigil-px);
  border-radius: 5px;
  background: linear-gradient(150deg, var(--raise), var(--void));
  border: 1px solid var(--edge-gold);
  box-shadow:
    inset 0 1px 0 oklch(1 0 0 / 0.05),
    inset 0 -3px 7px oklch(0.10 0.018 50 / 0.6),
    0 2px 5px oklch(0.08 0.015 48 / 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}

.sigil__glow { position: absolute; inset: 0; }
.sigil__hairline { position: absolute; left: 0; right: 0; top: 0; height: 1px; opacity: 0.45; }

.sigil__mark { position: relative; display: flex; align-items: center; justify-content: center; }

.sigil__drop { border-radius: 50% 50% 50% 0; transform: rotate(45deg); }

.sigil__bar { position: absolute; border-radius: 2px; }

.sigil__chev { position: absolute; border-radius: 2px; transform-origin: left center; }
.sigil__chev--ul { left: 10%; top: 32%; transform: rotate(42deg); }
.sigil__chev--ur { right: 10%; top: 32%; transform-origin: right center; transform: rotate(-42deg); }
.sigil__chev--ll { left: 10%; top: 64%; transform: rotate(42deg); }
.sigil__chev--lr { right: 10%; top: 64%; transform-origin: right center; transform: rotate(-42deg); }
.sigil__chev--dul { left: 10%; top: 30%; transform: rotate(-42deg); }
.sigil__chev--dur { right: 10%; top: 30%; transform-origin: right center; transform: rotate(42deg); }
.sigil__chev--dll { left: 10%; top: 62%; transform: rotate(-42deg); }
.sigil__chev--dlr { right: 10%; top: 62%; transform-origin: right center; transform: rotate(42deg); }

.sigil__aster { position: absolute; left: 50%; top: 50%; border-radius: 2px; }

.sigil__ring { border-style: solid; border-radius: 50%; }
.sigil__slash { position: absolute; left: 50%; top: 50%; border-radius: 2px; transform: translate(-50%, -50%) rotate(-38deg); }

.sigil__tube { position: relative; border: 1.5px solid; border-radius: 4px; overflow: hidden; }
.sigil__tube-fill { position: absolute; left: 0; right: 0; bottom: 0; height: 38%; opacity: 0.55; }
.sigil__lock-bar { position: absolute; left: 50%; top: 50%; border-radius: 2px; transform: translate(-50%, -50%); box-shadow: 0 0 4px oklch(0.10 0.03 60 / 0.9); }

.sigil__stacks {
  position: absolute;
  right: -2px;
  bottom: -2px;
  min-width: 14px;
  height: 14px;
  padding: 0 3px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-family: var(--font-mono);
  font-size: 9px;
  font-weight: 500;
  color: oklch(0.16 0.02 50);
  border-radius: 4px;
  box-shadow: 0 1px 3px oklch(0.08 0.015 48 / 0.5);
}

.sigil__meta {
  font-family: var(--font-mono);
  font-size: 9px;
  letter-spacing: 0.06em;
  color: var(--ink-4);
  white-space: nowrap;
}
</style>
