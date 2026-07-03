<script setup lang="ts">
import { computed } from 'vue';

/**
 * Atmosphère vivante du Palais : cœur de chair qui bat, vapeur, rouages,
 * gouttières qui ruissellent les émotions, braises, faille de magma et
 * cristal fracturé. Remplace le lavis froid bleu-violet sur les écrans
 * habillés du mood « palais ». Déterministe via `seed`.
 */
const props = withDefaults(defineProps<{ seed?: number; emberCount?: number }>(), {
  seed: 1337,
  emberCount: 16,
});

type Ember = {
  left: string;
  size: string;
  blur: string;
  duration: string;
  delay: string;
  color: string;
};

const embers = computed<Ember[]>(() => {
  let s = props.seed;
  const rnd = () => {
    s = (s * 9301 + 49297) % 233280;
    return s / 233280;
  };
  const colors = ['var(--ember)', 'var(--gold)', 'var(--gold-hi)', 'var(--blood)'];
  return Array.from({ length: props.emberCount }, () => {
    const r = rnd();
    return {
      left: (8 + rnd() * 84).toFixed(1) + '%',
      size: (1.6 + rnd() * 2.6).toFixed(1) + 'px',
      blur: Math.round(4 + rnd() * 6) + 'px',
      duration: (7 + rnd() * 7).toFixed(1) + 's',
      delay: (rnd() * 9).toFixed(1) + 's',
      color: colors[Math.floor(r * colors.length)],
    };
  });
});
</script>

<template>
  <div class="palace-atmos" aria-hidden="true">
    <!-- pierre taillée -->
    <div class="palace-atmos__stone" />

    <!-- rouages -->
    <div class="palace-atmos__gear palace-atmos__gear--lg" />
    <div class="palace-atmos__gear palace-atmos__gear--md" />
    <div class="palace-atmos__gear palace-atmos__gear--sm" />

    <!-- cristal fracturé -->
    <div class="palace-atmos__crystal" />

    <!-- gouttières -->
    <div class="palace-atmos__gutter palace-atmos__gutter--left">
      <span class="palace-atmos__drop" style="animation-delay: 0s" />
      <span class="palace-atmos__drop" style="animation-delay: 2.4s" />
      <span class="palace-atmos__drop palace-atmos__drop--ember" style="animation-delay: 4.1s" />
    </div>
    <div class="palace-atmos__gutter palace-atmos__gutter--right">
      <span class="palace-atmos__drop palace-atmos__drop--frost" style="animation-delay: 1.2s" />
      <span class="palace-atmos__drop palace-atmos__drop--frost-dim" style="animation-delay: 3.8s" />
    </div>

    <!-- vapeur -->
    <div class="palace-atmos__steam palace-atmos__steam--1" />
    <div class="palace-atmos__steam palace-atmos__steam--2" />
    <div class="palace-atmos__steam palace-atmos__steam--3" />

    <!-- faille de magma -->
    <div class="palace-atmos__magma" />
    <div class="palace-atmos__magma-seam" />

    <!-- cœur de chair -->
    <div class="palace-atmos__heart-glow" />
    <div class="palace-atmos__heart-core" />

    <!-- braises -->
    <span
      v-for="(e, i) in embers"
      :key="i"
      class="palace-atmos__ember"
      :style="{
        left: e.left,
        width: e.size,
        height: e.size,
        '--ember-blur': e.blur,
        '--ember-color': e.color,
        animationDuration: e.duration,
        animationDelay: e.delay,
      }"
    />

    <div class="palace-atmos__vignette" />
    <div class="palace-atmos__grain" />
  </div>
</template>

<style scoped>
.palace-atmos {
  position: absolute;
  inset: 0;
  z-index: 0;
  pointer-events: none;
  overflow: hidden;
}

.palace-atmos__stone {
  position: absolute;
  inset: 0;
  opacity: 0.5;
  mix-blend-mode: overlay;
  background-image:
    repeating-linear-gradient(96deg, oklch(1 0 0 / 0.014) 0 2px, transparent 2px 7px),
    repeating-linear-gradient(4deg, oklch(0 0 0 / 0.05) 0 3px, transparent 3px 11px);
}

.palace-atmos__gear {
  position: absolute;
  border-radius: 46% 54% 51% 49% / 53% 47% 53% 47%;
  filter: blur(2px);
}
.palace-atmos__gear--lg {
  left: -130px;
  bottom: -150px;
  width: 380px;
  height: 380px;
  opacity: 0.085;
  animation: palace-spin 140s linear infinite;
  background:
    radial-gradient(circle, transparent 30%, var(--gold-deep) 34% 38%, transparent 44% 52%, var(--gold-deep) 57% 60%, transparent 65%),
    conic-gradient(
      var(--gold-deep) 0deg 6deg, transparent 6deg 24deg,
      var(--gold-deep) 24deg 27deg, transparent 27deg 52deg,
      var(--gold-deep) 52deg 63deg, transparent 63deg 79deg,
      var(--gold-deep) 79deg 84deg, transparent 84deg 121deg,
      var(--gold-deep) 121deg 129deg, transparent 129deg 150deg,
      var(--gold-deep) 150deg 154deg, transparent 154deg 187deg,
      var(--gold-deep) 187deg 199deg, transparent 199deg 214deg,
      var(--gold-deep) 214deg 219deg, transparent 219deg 253deg,
      var(--gold-deep) 253deg 263deg, transparent 263deg 288deg,
      var(--gold-deep) 288deg 292deg, transparent 292deg 331deg,
      var(--gold-deep) 331deg 340deg, transparent 340deg 360deg
    );
}
.palace-atmos__gear--md {
  right: -90px;
  top: -110px;
  width: 280px;
  height: 280px;
  opacity: 0.07;
  animation: palace-spin-rev 115s linear infinite;
  background:
    radial-gradient(circle, transparent 32%, oklch(0.58 0.06 60) 36% 39%, transparent 45% 55%, oklch(0.58 0.06 60) 59% 62%, transparent 67%),
    conic-gradient(
      oklch(0.55 0.05 58) 0deg 9deg, transparent 9deg 21deg,
      oklch(0.55 0.05 58) 21deg 25deg, transparent 25deg 58deg,
      oklch(0.55 0.05 58) 58deg 68deg, transparent 68deg 83deg,
      oklch(0.55 0.05 58) 83deg 87deg, transparent 87deg 116deg,
      oklch(0.55 0.05 58) 116deg 128deg, transparent 128deg 143deg,
      oklch(0.55 0.05 58) 143deg 146deg, transparent 146deg 182deg,
      oklch(0.55 0.05 58) 182deg 190deg, transparent 190deg 211deg,
      oklch(0.55 0.05 58) 211deg 214deg, transparent 214deg 248deg,
      oklch(0.55 0.05 58) 248deg 260deg, transparent 260deg 277deg,
      oklch(0.55 0.05 58) 277deg 281deg, transparent 281deg 360deg
    );
}
.palace-atmos__gear--sm {
  right: 16%;
  top: 42%;
  width: 120px;
  height: 120px;
  opacity: 0.06;
  animation: palace-spin 78s linear infinite;
  background:
    radial-gradient(circle, transparent 34%, var(--gold-deep) 40% 58%, transparent 63%),
    conic-gradient(
      var(--gold-deep) 0deg 11deg, transparent 11deg 33deg,
      var(--gold-deep) 33deg 39deg, transparent 39deg 70deg,
      var(--gold-deep) 70deg 76deg, transparent 76deg 101deg,
      var(--gold-deep) 101deg 112deg, transparent 112deg 149deg,
      var(--gold-deep) 149deg 154deg, transparent 154deg 189deg,
      var(--gold-deep) 189deg 201deg, transparent 201deg 226deg,
      var(--gold-deep) 226deg 231deg, transparent 231deg 267deg,
      var(--gold-deep) 267deg 279deg, transparent 279deg 360deg
    );
}

.palace-atmos__crystal {
  position: absolute;
  left: 6%;
  top: 14%;
  width: 260px;
  height: 200px;
  opacity: 0.3;
  animation: palace-crackshimmer 9s ease-in-out infinite;
  background:
    linear-gradient(58deg, transparent 49.6%, var(--frost-deep) 49.8%, var(--frost-deep) 50.2%, transparent 50.4%),
    linear-gradient(112deg, transparent 49.7%, var(--frost-deep) 49.9%, var(--frost-deep) 50.1%, transparent 50.3%),
    linear-gradient(16deg, transparent 49.7%, oklch(0.62 0.05 252 / 0.6) 49.9%, oklch(0.62 0.05 252 / 0.6) 50.1%, transparent 50.3%);
  -webkit-mask: radial-gradient(circle at 40% 40%, #000, transparent 72%);
  mask: radial-gradient(circle at 40% 40%, #000, transparent 72%);
}

.palace-atmos__gutter {
  position: absolute;
  top: 62px;
  width: 2px;
  height: 64vh;
  opacity: 0.55;
}
.palace-atmos__gutter--left {
  left: 2.2vw;
  background: linear-gradient(var(--edge-gold), oklch(0.5 0.1 60 / 0.2) 70%, transparent);
}
.palace-atmos__gutter--right {
  right: 2.2vw;
  top: 90px;
  height: 58vh;
  opacity: 0.5;
  background: linear-gradient(var(--edge-frost), oklch(0.5 0.08 252 / 0.18) 72%, transparent);
}
.palace-atmos__drop {
  position: absolute;
  left: -2px;
  top: 0;
  width: 6px;
  height: 9px;
  border-radius: 50% 50% 50% 0;
  transform: rotate(45deg);
  background: var(--gold);
  box-shadow: 0 0 7px var(--ember);
  animation: palace-drip 5.5s linear infinite;
}
.palace-atmos__drop--ember { background: var(--ember); }
.palace-atmos__drop--frost {
  width: 5px;
  height: 8px;
  background: var(--frost);
  box-shadow: 0 0 7px var(--frost-dim);
  animation-duration: 6.4s;
}
.palace-atmos__drop--frost-dim {
  width: 5px;
  height: 8px;
  background: var(--frost-dim);
  box-shadow: 0 0 6px var(--frost-dim);
  animation-duration: 6.4s;
}

.palace-atmos__steam {
  position: absolute;
  bottom: 4%;
  border-radius: 50%;
  filter: blur(20px);
  animation: palace-steam 11s ease-in infinite;
}
.palace-atmos__steam--1 {
  left: 46%;
  width: 120px;
  height: 240px;
  background: radial-gradient(ellipse at center, oklch(0.9 0.02 80 / 0.42), transparent 64%);
}
.palace-atmos__steam--2 {
  left: 54%;
  bottom: 2%;
  width: 90px;
  height: 200px;
  filter: blur(17px);
  background: radial-gradient(ellipse at center, oklch(0.88 0.03 70 / 0.34), transparent 64%);
  animation-duration: 13s;
  animation-delay: 3.5s;
}
.palace-atmos__steam--3 {
  left: 50%;
  bottom: 6%;
  width: 74px;
  height: 170px;
  filter: blur(15px);
  background: radial-gradient(ellipse at center, oklch(0.9 0.02 80 / 0.28), transparent 64%);
  animation-duration: 12s;
  animation-delay: 6.5s;
}

.palace-atmos__magma {
  position: absolute;
  left: 0;
  right: 0;
  bottom: 0;
  height: 120px;
  background: linear-gradient(0deg, var(--wash-blood), transparent);
  animation: palace-flicker 6s ease-in-out infinite;
}
.palace-atmos__magma-seam {
  position: absolute;
  left: 18%;
  right: 18%;
  bottom: 0;
  height: 3px;
  filter: blur(2px);
  background: linear-gradient(90deg, transparent, var(--ember) 30%, var(--gold-hi) 50%, var(--ember) 70%, transparent);
  animation: palace-flicker 4.5s ease-in-out infinite;
}

.palace-atmos__heart-glow {
  position: absolute;
  left: 50%;
  bottom: -12%;
  width: 560px;
  height: 420px;
  transform: translateX(-50%);
  background: radial-gradient(ellipse at center, oklch(0.74 0.16 52 / 0.32), oklch(0.6 0.17 30 / 0.12) 42%, transparent 68%);
  filter: blur(24px);
  animation: palace-beat 4.8s cubic-bezier(0.5, 0, 0.5, 1) infinite;
}
.palace-atmos__heart-core {
  position: absolute;
  left: 50%;
  bottom: 1%;
  width: 130px;
  height: 130px;
  transform: translateX(-50%);
  border-radius: 50% 50% 48% 48%;
  background: radial-gradient(circle at 50% 38%, var(--gold-hi), var(--ember) 44%, var(--blood) 72%, transparent 86%);
  filter: blur(5px);
  opacity: 0.7;
  animation: palace-beat-core 4.8s cubic-bezier(0.5, 0, 0.5, 1) infinite;
}

.palace-atmos__ember {
  position: absolute;
  bottom: -12px;
  border-radius: 50%;
  background: var(--ember-color);
  box-shadow: 0 0 var(--ember-blur) var(--ember-color);
  opacity: 0;
  animation-name: palace-ember-rise;
  animation-timing-function: ease-in;
  animation-iteration-count: infinite;
}

.palace-atmos__vignette {
  position: absolute;
  inset: 0;
  z-index: 1;
  mix-blend-mode: multiply;
  background: radial-gradient(128% 108% at 50% 40%, transparent 42%, oklch(0.12 0.02 50 / 0.5) 80%, oklch(0.08 0.015 48 / 0.86) 100%);
}
.palace-atmos__grain {
  position: absolute;
  inset: 0;
  z-index: 2;
  opacity: 0.45;
  mix-blend-mode: soft-light;
  background-image:
    repeating-linear-gradient(0deg, oklch(1 0 0 / 0.02) 0px, oklch(1 0 0 / 0.02) 1px, transparent 1px, transparent 2px),
    repeating-linear-gradient(90deg, oklch(0 0 0 / 0.05) 0px, oklch(0 0 0 / 0.05) 1px, transparent 1px, transparent 3px);
}

@keyframes palace-spin { to { transform: rotate(360deg); } }
@keyframes palace-spin-rev { to { transform: rotate(-360deg); } }
@keyframes palace-crackshimmer { 0%, 100% { opacity: 0.18; } 50% { opacity: 0.42; } }
@keyframes palace-drip {
  0% { transform: translateY(0) rotate(45deg); opacity: 0; }
  8% { opacity: 0.9; }
  92% { opacity: 0.5; }
  100% { transform: translateY(64vh) rotate(45deg); opacity: 0; }
}
@keyframes palace-steam {
  0% { transform: translateY(40px) scale(0.7); opacity: 0; }
  22% { opacity: 0.4; }
  100% { transform: translateY(-260px) scale(1.8); opacity: 0; }
}
@keyframes palace-flicker { 0%, 100% { opacity: 0.5; } 40% { opacity: 0.78; } 70% { opacity: 0.42; } }
@keyframes palace-beat {
  0%, 42%, 100% { transform: translateX(-50%) scale(1); opacity: 0.5; }
  9% { transform: translateX(-50%) scale(1.16); opacity: 0.92; }
  18% { transform: translateX(-50%) scale(1.02); opacity: 0.62; }
  30% { transform: translateX(-50%) scale(1.10); opacity: 0.78; }
}
@keyframes palace-beat-core {
  0%, 42%, 100% { transform: translateX(-50%) scale(1); }
  9% { transform: translateX(-50%) scale(1.13); }
  18% { transform: translateX(-50%) scale(1.0); }
  30% { transform: translateX(-50%) scale(1.08); }
}
@keyframes palace-ember-rise {
  0% { transform: translateY(0) translateX(0); opacity: 0; }
  16% { opacity: 0.85; }
  70% { opacity: 0.6; }
  100% { transform: translateY(-360px) translateX(22px); opacity: 0; }
}

@media (prefers-reduced-motion: reduce) {
  .palace-atmos__gear,
  .palace-atmos__crystal,
  .palace-atmos__drop,
  .palace-atmos__steam,
  .palace-atmos__magma,
  .palace-atmos__magma-seam,
  .palace-atmos__heart-glow,
  .palace-atmos__heart-core,
  .palace-atmos__ember {
    animation: none;
  }
}
</style>
