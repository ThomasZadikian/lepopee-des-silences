<script setup lang="ts">
import { computed } from 'vue';
import type { CSSProperties } from 'vue';

import type { RoomClimateStateDto } from '../runs/types/runTypes';
import { resolveRoomClimateDisplay } from './roomClimateDisplay';

type ParticleStyle = CSSProperties & Record<`--${string}`, string>;

const props = defineProps<{
  climate?: RoomClimateStateDto | null;
}>();

const display = computed(() => resolveRoomClimateDisplay(props.climate));
const knownEffectTypes = new Set(['rain', 'hail', 'heatwave', 'grey']);
const effectType = computed(() => {
  const normalized = display.value?.type?.toLowerCase() ?? null;
  return normalized && knownEffectTypes.has(normalized) ? normalized : null;
});
const effectClass = computed(() => effectType.value ? `rce-root--${effectType.value}` : null);

const rainDrops = Array.from({ length: 42 }, (_, index) => index);
const hailStones = Array.from({ length: 28 }, (_, index) => index);
const heatMotes = Array.from({ length: 18 }, (_, index) => index);
const greyWisps = Array.from({ length: 9 }, (_, index) => index);

function particleStyle(index: number, count: number, durationBase: number, durationSpread: number): ParticleStyle {
  const x = (index * 37) % 101;
  const y = (index * 19) % 101;
  const delay = -((index * 0.37) % durationBase);
  const duration = durationBase + ((index * 0.23) % durationSpread);

  return {
    '--x': `${x}%`,
    '--y': `${y}%`,
    '--delay': `${delay}s`,
    '--duration': `${duration}s`,
    '--scale': `${0.72 + ((index % count) / count) * 0.58}`,
  };
}
</script>

<template>
  <div
    v-if="display && effectClass"
    class="rce-root"
    :class="effectClass"
    :data-climate-effect="display.type"
    aria-hidden="true"
  >
    <template v-if="effectType === 'rain'">
      <span
        v-for="drop in rainDrops"
        :key="`rain-${drop}`"
        class="rce-rain-drop"
        :style="particleStyle(drop, rainDrops.length, 1.8, 1.4)"
      />
    </template>

    <template v-else-if="effectType === 'hail'">
      <span
        v-for="stone in hailStones"
        :key="`hail-${stone}`"
        class="rce-hail-stone"
        :style="particleStyle(stone, hailStones.length, 1.25, 1.1)"
      />
    </template>

    <template v-else-if="effectType === 'heatwave'">
      <span class="rce-sun" />
      <span class="rce-heat-shimmer" />
      <span
        v-for="mote in heatMotes"
        :key="`heat-${mote}`"
        class="rce-heat-mote"
        :style="particleStyle(mote, heatMotes.length, 5.6, 4.2)"
      />
    </template>

    <template v-else-if="effectType === 'grey'">
      <span
        v-for="wisp in greyWisps"
        :key="`grey-${wisp}`"
        class="rce-grey-wisp"
        :style="particleStyle(wisp, greyWisps.length, 9.5, 6.2)"
      />
    </template>
  </div>
</template>

<style scoped>
.rce-root {
  position: absolute;
  inset: 0;
  z-index: 2;
  overflow: hidden;
  pointer-events: none;
  mix-blend-mode: screen;
  contain: layout paint;
}

.rce-root::before,
.rce-root::after {
  content: '';
  position: absolute;
  inset: 0;
  pointer-events: none;
}

.rce-root--rain::before {
  background:
    linear-gradient(180deg, oklch(0.62 0.08 238 / 0.11), transparent 42%),
    radial-gradient(70% 55% at 30% 0%, oklch(0.66 0.09 238 / 0.18), transparent 64%);
}

.rce-rain-drop {
  position: absolute;
  top: -18%;
  left: var(--x);
  width: 1px;
  height: calc(34px * var(--scale));
  border-radius: 999px;
  background: linear-gradient(180deg, transparent, oklch(0.86 0.06 232 / 0.68));
  opacity: 0.62;
  transform: rotate(12deg);
  animation: rceRainFall var(--duration) linear var(--delay) infinite;
}

.rce-root--hail::before {
  background:
    radial-gradient(80% 60% at 50% 0%, oklch(0.72 0.07 230 / 0.13), transparent 62%),
    linear-gradient(180deg, oklch(0.34 0.04 248 / 0.16), transparent 52%);
}

.rce-root--hail::after {
  background: linear-gradient(110deg, transparent 0 46%, oklch(0.88 0.04 232 / 0.1) 48%, transparent 51% 100%);
  animation: rceColdFlash 5.8s ease-in-out infinite;
}

.rce-hail-stone {
  position: absolute;
  top: -12%;
  left: var(--x);
  width: calc(3px * var(--scale));
  height: calc(9px * var(--scale));
  border-radius: 999px;
  background: oklch(0.92 0.035 231 / 0.72);
  box-shadow: 0 0 10px oklch(0.84 0.08 228 / 0.24);
  transform: rotate(24deg);
  animation: rceHailFall var(--duration) linear var(--delay) infinite;
}

.rce-root--heatwave {
  mix-blend-mode: soft-light;
}

.rce-root--heatwave::before {
  background:
    radial-gradient(38% 45% at 78% 16%, oklch(0.78 0.17 70 / 0.27), transparent 68%),
    radial-gradient(60% 40% at 50% 110%, oklch(0.68 0.16 42 / 0.17), transparent 68%),
    linear-gradient(180deg, oklch(0.65 0.12 52 / 0.12), transparent 64%);
}

.rce-sun {
  position: absolute;
  top: 5%;
  right: 9%;
  width: min(22vw, 230px);
  aspect-ratio: 1;
  border-radius: 50%;
  background: radial-gradient(circle, oklch(0.88 0.16 78 / 0.26), oklch(0.68 0.14 54 / 0.1) 48%, transparent 70%);
  filter: blur(2px);
  animation: rceSunPulse 6.8s ease-in-out infinite;
}

.rce-heat-shimmer {
  position: absolute;
  inset: -10% -8%;
  opacity: 0.22;
  background: repeating-linear-gradient(
    90deg,
    transparent 0 22px,
    oklch(0.84 0.13 74 / 0.12) 24px 28px,
    transparent 31px 58px
  );
  filter: blur(8px);
  animation: rceShimmer 7s ease-in-out infinite alternate;
}

.rce-heat-mote {
  position: absolute;
  left: var(--x);
  top: var(--y);
  width: calc(3px * var(--scale));
  height: calc(3px * var(--scale));
  border-radius: 50%;
  background: oklch(0.84 0.14 72 / 0.52);
  box-shadow: 0 0 18px oklch(0.78 0.16 62 / 0.32);
  animation: rceHeatFloat var(--duration) ease-in-out var(--delay) infinite alternate;
}

.rce-root--grey {
  mix-blend-mode: multiply;
}

.rce-root--grey::before {
  background:
    linear-gradient(180deg, oklch(0.5 0.018 250 / 0.19), oklch(0.24 0.02 258 / 0.18)),
    radial-gradient(80% 80% at 50% 50%, transparent 40%, oklch(0.18 0.02 260 / 0.26));
}

.rce-grey-wisp {
  position: absolute;
  left: var(--x);
  top: var(--y);
  width: calc(180px * var(--scale));
  height: calc(54px * var(--scale));
  border-radius: 999px;
  background: radial-gradient(ellipse, oklch(0.72 0.014 250 / 0.16), transparent 68%);
  filter: blur(14px);
  animation: rceGreyDrift var(--duration) ease-in-out var(--delay) infinite alternate;
}

@keyframes rceRainFall {
  from { transform: translate3d(0, -16vh, 0) rotate(12deg); }
  to { transform: translate3d(-10vw, 122vh, 0) rotate(12deg); }
}

@keyframes rceHailFall {
  from { transform: translate3d(0, -14vh, 0) rotate(24deg); }
  to { transform: translate3d(-18vw, 118vh, 0) rotate(24deg); }
}

@keyframes rceColdFlash {
  0%, 72%, 100% { opacity: 0; }
  76% { opacity: 0.65; }
  80% { opacity: 0.12; }
}

@keyframes rceSunPulse {
  0%, 100% { transform: scale(0.96); opacity: 0.72; }
  50% { transform: scale(1.06); opacity: 1; }
}

@keyframes rceShimmer {
  from { transform: translate3d(-2%, 0, 0) skewX(-4deg); }
  to { transform: translate3d(3%, 0, 0) skewX(4deg); }
}

@keyframes rceHeatFloat {
  from { transform: translate3d(0, 10px, 0); opacity: 0.16; }
  to { transform: translate3d(18px, -24px, 0); opacity: 0.72; }
}

@keyframes rceGreyDrift {
  from { transform: translate3d(-22px, -8px, 0); opacity: 0.22; }
  to { transform: translate3d(34px, 16px, 0); opacity: 0.5; }
}

@media (prefers-reduced-motion: reduce) {
  .rce-rain-drop,
  .rce-hail-stone,
  .rce-sun,
  .rce-heat-shimmer,
  .rce-heat-mote,
  .rce-grey-wisp,
  .rce-root--hail::after {
    animation: none;
  }
}
</style>
