<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';

const props = withDefaults(
  defineProps<{
    gauge?: number;
    fillPerTick?: number;
    active?: boolean;
    vertical?: boolean;
    /** Net Speed StatModifier currently active — drives the gauge's halo. */
    speedEffect?: 'boosted' | 'slowed' | null;
  }>(),
  { gauge: 0, fillPerTick: 10, active: false, vertical: true, speedEffect: null },
);

const READY = 50_000;

const displayed = ref(props.gauge);

const fillRatio = computed(() => Math.min(displayed.value, READY) / READY);
const isReady = computed(() => displayed.value >= READY);

const fillStyle = computed(() => (props.vertical ? { height: fillRatio.value * 100 + '%' } : { width: fillRatio.value * 100 + '%' }));

const justReady = ref(false);
const staggered = ref(false);
const timers: number[] = [];

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
// average rate is fillPerTick * 340 / 0.48 ≈ fillPerTick * 708/s). ×900 gives
// headroom to always catch up within one tick interval.
const fillRate = computed(() => Math.max(2_000, props.fillPerTick * 900));

let raf = 0;
let last = 0;
function frame(now: number) {
  const dt = last ? Math.min(0.1, (now - last) / 1000) : 0;
  last = now;
  const target = props.gauge;
  if (displayed.value < target) {
    displayed.value = Math.min(target, displayed.value + fillRate.value * dt); // animate up
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
      'atb--active': active,
      'atb--just-ready': justReady,
      'atb--staggered': staggered,
      'atb--vertical': vertical,
      'atb--horizontal': !vertical,
      'atb--boosted': speedEffect === 'boosted',
      'atb--slowed': speedEffect === 'slowed',
    }"
    aria-hidden="true"
  >
    <template v-if="vertical">
      <span class="atb__tick" style="top: 10%" />
      <span class="atb__tick" style="top: 42%" />
      <span class="atb__tick" style="top: 72%" />
    </template>
    <div class="atb__fill" :style="fillStyle" />
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

.atb__fill {
  position: absolute;
  border-radius: inherit;
  background: linear-gradient(90deg, var(--frost-dim), var(--frost));
  transition: width 0.18s linear, height 0.18s linear, background 0.25s ease;
}

.atb--horizontal .atb__fill { inset: 0 auto 0 0; }
.atb--vertical .atb__fill { inset: auto 0 0 0; background: linear-gradient(0deg, var(--frost-deep), var(--frost-dim) 50%, var(--frost)); }

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

/* Persistent halo while a Speed StatModifier is active — reflects an ongoing
   state (not a one-shot event), so it loops for as long as the effect lasts.
   Orange/red = sped up, blue/violet = slowed down. */
.atb--boosted {
  border-color: color-mix(in oklch, oklch(0.68 0.19 42), transparent 25%);
  animation: atb-halo-boosted 1.3s ease-in-out infinite;
}
.atb--slowed {
  border-color: color-mix(in oklch, oklch(0.62 0.17 288), transparent 25%);
  animation: atb-halo-slowed 1.3s ease-in-out infinite;
}

@keyframes atb-halo-boosted {
  0%, 100% { box-shadow: inset 0 0 7px oklch(0.08 0.015 48 / 0.85), 0 0 8px color-mix(in oklch, oklch(0.68 0.19 42), transparent 55%); }
  50% { box-shadow: inset 0 0 7px oklch(0.08 0.015 48 / 0.85), 0 0 16px color-mix(in oklch, oklch(0.68 0.19 42), transparent 30%); }
}

@keyframes atb-halo-slowed {
  0%, 100% { box-shadow: inset 0 0 7px oklch(0.08 0.015 48 / 0.85), 0 0 8px color-mix(in oklch, oklch(0.62 0.17 288), transparent 55%); }
  50% { box-shadow: inset 0 0 7px oklch(0.08 0.015 48 / 0.85), 0 0 16px color-mix(in oklch, oklch(0.62 0.17 288), transparent 30%); }
}

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
  .atb__fill, .atb__spark,
  .atb--ready .atb__fill,
  .atb--just-ready, .atb--staggered,
  .atb--boosted, .atb--slowed { animation: none; transition: none; }
}
</style>
