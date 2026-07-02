<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';

const props = withDefaults(
  defineProps<{ gauge?: number; fillPerTick?: number; active?: boolean; vertical?: boolean }>(),
  { gauge: 0, fillPerTick: 10, active: false, vertical: true },
);

const READY = 21_500;
// Matches the backend's AtbConstants.MaxChargeOverflow — the single source of
// truth for how far a gauge can bank past READY. (Previously this file had
// two disagreeing copies: a 50_000 used by the bar's own overflow ratio and
// a correct 10_000 used only by the charge-multiplier label — unified here.)
const MAX_OVERFLOW = 25_000;

const displayed = ref(props.gauge);

const baseRatio = computed(() => Math.min(displayed.value, READY) / READY);
const overflow = computed(() => Math.max(0, displayed.value - READY));
const chargeRatio = computed(() => Math.min(overflow.value, MAX_OVERFLOW) / MAX_OVERFLOW);
const isReady = computed(() => displayed.value >= READY);
const isCharging = computed(() => overflow.value > 0 && displayed.value < READY + MAX_OVERFLOW);
const isMax = computed(() => displayed.value >= READY + MAX_OVERFLOW);

const fillStyle = computed(() => (props.vertical ? { height: baseRatio.value * 100 + '%' } : { width: baseRatio.value * 100 + '%' }));
const chargeStyle = computed(() => (props.vertical ? { height: chargeRatio.value * 100 + '%' } : { width: chargeRatio.value * 100 + '%' }));

const justReady = ref(false);
const staggered = ref(false);
const timers: number[] = [];

const chargeMult = computed(() => {
  const over = Math.min(Math.max(displayed.value - READY, 0), MAX_OVERFLOW);
  if (over <= 0) return null;
  const r = over / MAX_OVERFLOW;
  return r < 0.34 ? '×1.15' : r < 0.67 ? '×1.30' : '×1.5';
});

const chargeMultLabel = computed(() =>
  chargeMult.value ? `Surcharge : dégâts ${chargeMult.value} tant que la jauge reste chargée` : '',
);

function flash(target: { value: boolean }, ms = 600) {
  target.value = true;
  const id = window.setTimeout(() => { target.value = false; }, ms);
  timers.push(id);
}

// Visual fill rate (gauge units / second), derived from the combatant's real
// AtbFillPerTick so fast vs. slow combatants visibly animate at different
// speeds. Must be fast enough to fully close the gap to a new server target
// before the NEXT snapshot arrives, or the bar permanently lags behind the
// real gauge that actually gates the turn (useCombatStore's runCombatClock
// advances TICK_DELTA=340 ticks every TICK_INTERVAL=480ms, i.e. the real
// average rate is fillPerTick * 340 / 0.48 ≈ fillPerTick * 708/s — the old
// ×220 multiplier was ~3x too slow and could leave the bar visibly "not
// full yet" long after the real gauge — and the turn — had already moved
// on). ×900 gives headroom to always catch up within one tick interval.
const fillRate = computed(() => Math.max(2_000, props.fillPerTick * 900));

let raf = 0;
let last = 0;
function frame(now: number) {
  const dt = last ? Math.min(0.1, (now - last) / 1000) : 0;
  last = now;
  const target = props.gauge;
  if (displayed.value < target) {
    displayed.value = Math.min(target, displayed.value + fillRate.value * dt); // animate up
  } else if (displayed.value > target) {
    displayed.value = target; // snap down (acted / interrupted)
  }
  raf = requestAnimationFrame(frame);
}

watch(() => props.gauge, (next) => {
  if (next < displayed.value - 800) flash(staggered);                 // big drop → interrupted
  if (displayed.value < READY && next >= READY) flash(justReady);     // about to be ready
  if (next < displayed.value) displayed.value = next;                 // snap down only
});

onMounted(() => { last = 0; raf = requestAnimationFrame(frame); });
onBeforeUnmount(() => {
  cancelAnimationFrame(raf);
  for (const id of timers) window.clearTimeout(id);
});
</script>

<template>
  <div
    class="atb"
    :class="{
      'atb--ready': isReady,
      'atb--charging': isCharging,
      'atb--max': isMax,
      'atb--active': active,
      'atb--just-ready': justReady,
      'atb--staggered': staggered,
      'atb--vertical': vertical,
      'atb--horizontal': !vertical,
    }"
    aria-hidden="true"
  >
    <template v-if="vertical">
      <span class="atb__tick" style="top: 10%" />
      <span class="atb__tick" style="top: 42%" />
      <span class="atb__tick" style="top: 72%" />
    </template>
    <div class="atb__fill" :style="fillStyle" />
    <span
      v-if="chargeMult"
      class="atb-gauge__charge"
      :class="{ 'atb-gauge__charge--max': chargeMult === '×1.5' }"
      :title="chargeMultLabel"
    >⚡ {{ chargeMult }}</span>
    <div v-if="overflow > 0" class="atb__charge" :style="chargeStyle" />
    <span v-if="isReady" class="atb__spark" />
  </div>
</template>

<style scoped>
.atb {
  position: relative;
  border-radius: 999px;
  background: linear-gradient(var(--void), oklch(0.13 0.016 52));
  overflow: hidden;
  isolation: isolate;
  box-shadow: inset 0 0 7px oklch(0.08 0.015 48 / 0.85);
}

.atb--horizontal {
  height: 4px;
  border-radius: 999px;
}

.atb--vertical {
  width: 100%;
  height: 100%;
  min-height: 3.6rem;
  border-radius: 9px;
  border: 1px solid var(--edge-frost);
}

.atb--vertical.atb--ready { border-color: var(--edge-gold); }

.atb__tick {
  position: absolute;
  left: 0;
  right: 0;
  height: 1px;
  background: oklch(1 0 0 / 0.06);
}

.atb-gauge__charge {
  position: absolute; right: 4px; top: -2px;
  font-family: var(--font-mono); font-size: 0.58rem;
  color: var(--surge); text-shadow: 0 0 8px color-mix(in oklch, var(--surge), transparent 40%);
  cursor: help;
}

.atb-gauge__charge--max {
  animation: atb-charge-label-pulse 0.6s ease-in-out infinite;
}

.atb--vertical .atb-gauge__charge {
  top: -16px;
  right: 50%;
  transform: translateX(50%);
  white-space: nowrap;
}

.atb__fill {
  position: absolute;
  border-radius: inherit;
  background: linear-gradient(90deg, var(--frost-dim), var(--frost));
  transition: width 0.18s linear, height 0.18s linear, background 0.25s ease;
}

.atb--horizontal .atb__fill { inset: 0 auto 0 0; }
.atb--vertical .atb__fill { inset: auto 0 0 0; background: linear-gradient(0deg, var(--frost-deep), var(--frost-dim) 50%, var(--frost)); }

.atb__charge {
  position: absolute;
  border-radius: inherit;
  background: linear-gradient(90deg, var(--surge-dim), var(--surge));
  box-shadow: 0 0 8px color-mix(in oklch, var(--surge), transparent 50%);
  transition: width 0.18s linear, height 0.18s linear;
  mix-blend-mode: screen;
}

.atb--horizontal .atb__charge { inset: 0 auto 0 0; }
.atb--vertical .atb__charge { inset: auto 0 0 0; background: linear-gradient(0deg, var(--surge-dim), var(--surge) 50%, var(--surge-hi)); }

.atb--ready .atb__fill {
  background: linear-gradient(90deg, var(--gold-dim), var(--gold));
  animation: atb-ready-pulse 1.1s ease-in-out infinite;
}
.atb--vertical.atb--ready .atb__fill {
  background: linear-gradient(0deg, var(--gold-deep), var(--gold) 45%, var(--gold-hi));
  box-shadow: 0 0 12px oklch(0.74 0.14 60 / 0.7);
}
.atb--ready { box-shadow: inset 0 0 7px oklch(0.08 0.015 48 / 0.85), 0 0 10px color-mix(in oklch, var(--gold), transparent 62%); }
.atb--active.atb--ready { box-shadow: inset 0 0 7px oklch(0.08 0.015 48 / 0.85), 0 0 16px color-mix(in oklch, var(--gold), transparent 42%); }

.atb--charging .atb__charge {
  animation: atb-charge-shimmer 0.9s linear infinite;
  background-size: 200% 100%;
  background-image: linear-gradient(90deg, var(--surge-dim), var(--surge), var(--surge-dim));
}

.atb--max .atb__charge {
  animation: atb-max-pulse 0.6s ease-in-out infinite;
  box-shadow: 0 0 18px color-mix(in oklch, var(--surge-hi), transparent 15%);
}
.atb--max { box-shadow: inset 0 0 7px oklch(0.08 0.015 48 / 0.85), 0 0 20px color-mix(in oklch, var(--surge), transparent 22%); }

.atb__spark {
  position: absolute;
  top: 50%;
  right: 1px;
  width: 5px;
  height: 5px;
  border-radius: 999px;
  transform: translateY(-50%);
  background: var(--gold);
  box-shadow: 0 0 8px var(--gold);
  animation: atb-spark 1.1s ease-in-out infinite;
}
.atb--vertical .atb__spark { top: auto; bottom: 2px; right: 50%; transform: translateX(50%); }

.atb--just-ready { animation: atb-flash 0.5s ease-out; }
.atb--staggered { animation: atb-stagger 0.55s ease-out; }
.atb--staggered .atb__fill {
  background: linear-gradient(90deg, var(--blood), color-mix(in oklch, var(--blood), transparent 40%));
}
.atb--vertical.atb--staggered .atb__fill {
  background: linear-gradient(0deg, color-mix(in oklch, var(--blood), transparent 40%), var(--blood));
}

@keyframes atb-ready-pulse { 0%, 100% { filter: brightness(1); } 50% { filter: brightness(1.35); } }
@keyframes atb-charge-shimmer { 0% { background-position: 0% 0; } 100% { background-position: 200% 0; } }
@keyframes atb-max-pulse { 0%, 100% { filter: brightness(1.1); } 50% { filter: brightness(1.6); } }
@keyframes atb-charge-label-pulse { 0%, 100% { transform: scale(1); } 50% { transform: scale(1.18); } }
@keyframes atb-spark { 0%, 100% { opacity: 0.6; transform: translateY(-50%) scale(0.85); } 50% { opacity: 1; transform: translateY(-50%) scale(1.2); } }
@keyframes atb-flash {
  0% { box-shadow: 0 0 0 color-mix(in oklch, var(--gold), transparent 0%); }
  30% { box-shadow: 0 0 18px color-mix(in oklch, var(--gold), transparent 20%); }
  100% { box-shadow: 0 0 0 color-mix(in oklch, var(--gold), transparent 100%); }
}
@keyframes atb-stagger {
  0% { box-shadow: 0 0 0 color-mix(in oklch, var(--blood), transparent 0%); transform: translateX(0); }
  25% { box-shadow: 0 0 16px color-mix(in oklch, var(--blood), transparent 25%); transform: translateX(-2px); }
  60% { transform: translateX(2px); }
  100% { box-shadow: 0 0 0 color-mix(in oklch, var(--blood), transparent 100%); transform: translateX(0); }
}

@media (prefers-reduced-motion: reduce) {
  .atb__fill, .atb__charge, .atb__spark,
  .atb--ready .atb__fill, .atb--charging .atb__charge, .atb--max .atb__charge,
  .atb--just-ready, .atb--staggered, .atb-gauge__charge--max { animation: none; transition: none; }
}
</style>
