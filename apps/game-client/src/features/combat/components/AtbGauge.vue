<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';

const props = withDefaults(
  defineProps<{ gauge?: number; fillPerTick?: number; active?: boolean }>(),
  { gauge: 0, fillPerTick: 10, active: false },
);

const READY = 50_000;
const MAX_OVERFLOW = 50_000;
// Visual fill rate (gauge units / second). Decoupled from the tiny server
// fillPerTick so the bar reads well; relative speed shows via turn frequency.
const FILL_RATE = 6_500;

const displayed = ref(props.gauge);

const baseRatio = computed(() => Math.min(displayed.value, READY) / READY);
const overflow = computed(() => Math.max(0, displayed.value - READY));
const chargeRatio = computed(() => Math.min(overflow.value, MAX_OVERFLOW) / MAX_OVERFLOW);
const isReady = computed(() => displayed.value >= READY);
const isCharging = computed(() => overflow.value > 0 && displayed.value < READY + MAX_OVERFLOW);
const isMax = computed(() => displayed.value >= READY + MAX_OVERFLOW);

const justReady = ref(false);
const staggered = ref(false);
const timers: number[] = [];
function flash(target: { value: boolean }, ms = 600) {
  target.value = true;
  const id = window.setTimeout(() => { target.value = false; }, ms);
  timers.push(id);
}

let raf = 0;
let last = 0;
function frame(now: number) {
  const dt = last ? Math.min(0.1, (now - last) / 1000) : 0;
  last = now;
  const target = props.gauge;
  if (displayed.value < target) {
    displayed.value = Math.min(target, displayed.value + FILL_RATE * dt); // animate up
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
    }"
    aria-hidden="true"
  >
    <div class="atb__fill" :style="{ width: baseRatio * 100 + '%' }" />
    <div v-if="overflow > 0" class="atb__charge" :style="{ width: chargeRatio * 100 + '%' }" />
    <span v-if="isReady" class="atb__spark" />
  </div>
</template>

<style scoped>
.atb {
  position: relative;
  height: 4px;
  border-radius: 999px;
  background: oklch(0.14 0.03 272 / 0.9);
  overflow: hidden;
  isolation: isolate;
}

.atb__fill {
  position: absolute;
  inset: 0 auto 0 0;
  height: 100%;
  border-radius: inherit;
  background: linear-gradient(90deg, var(--frost-dim), var(--frost));
  transition: width 0.18s linear, background 0.25s ease;
}

.atb__charge {
  position: absolute;
  inset: 0 auto 0 0;
  height: 100%;
  border-radius: inherit;
  background: linear-gradient(90deg, var(--gold-dim), var(--gold));
  box-shadow: 0 0 8px color-mix(in oklch, var(--gold), transparent 50%);
  transition: width 0.18s linear;
  mix-blend-mode: screen;
}

.atb--ready .atb__fill {
  background: linear-gradient(90deg, var(--gold-dim), var(--gold));
  animation: atb-ready-pulse 1.1s ease-in-out infinite;
}
.atb--ready { box-shadow: 0 0 10px color-mix(in oklch, var(--gold), transparent 62%); }
.atb--active.atb--ready { box-shadow: 0 0 16px color-mix(in oklch, var(--gold), transparent 42%); }

.atb--charging .atb__charge {
  animation: atb-charge-shimmer 0.9s linear infinite;
  background-size: 200% 100%;
  background-image: linear-gradient(90deg, var(--gold-dim), var(--gold), var(--gold-dim));
}

.atb--max .atb__charge {
  animation: atb-max-pulse 0.6s ease-in-out infinite;
  box-shadow: 0 0 14px color-mix(in oklch, var(--gold), transparent 25%);
}
.atb--max { box-shadow: 0 0 18px color-mix(in oklch, var(--gold), transparent 30%); }

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

.atb--just-ready { animation: atb-flash 0.5s ease-out; }
.atb--staggered { animation: atb-stagger 0.55s ease-out; }
.atb--staggered .atb__fill {
  background: linear-gradient(90deg, var(--blood), color-mix(in oklch, var(--blood), transparent 40%));
}

@keyframes atb-ready-pulse { 0%, 100% { filter: brightness(1); } 50% { filter: brightness(1.35); } }
@keyframes atb-charge-shimmer { 0% { background-position: 0% 0; } 100% { background-position: 200% 0; } }
@keyframes atb-max-pulse { 0%, 100% { filter: brightness(1.1); } 50% { filter: brightness(1.6); } }
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
  .atb--just-ready, .atb--staggered { animation: none; transition: none; }
}
</style>