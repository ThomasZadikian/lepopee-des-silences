<script setup lang="ts">
defineProps<{
  isVictory: boolean;
  isLoading: boolean;
}>();

defineEmits<{
  continue: [];
  leaveRun: [];
}>();
</script>

<template>
  <section
    class="cop-root"
    :class="isVictory ? 'cop-root--victory' : 'cop-root--defeat'"
  >
    <!-- Atmospheric backdrop -->
    <div class="cop-backdrop" aria-hidden="true" />

    <!-- Particles -->
    <div class="cop-particles" aria-hidden="true">
      <span v-for="n in 12" :key="n" class="cop-particle" :style="`--i:${n}`" />
    </div>

    <!-- Grain overlay -->
    <div class="cop-grain" aria-hidden="true" />

    <!-- Frost corner ornaments -->
    <span class="cop-corner cop-corner--tl" aria-hidden="true" />
    <span class="cop-corner cop-corner--tr" aria-hidden="true" />
    <span class="cop-corner cop-corner--bl" aria-hidden="true" />
    <span class="cop-corner cop-corner--br" aria-hidden="true" />

    <!-- Content -->
    <div class="cop-inner">
      <!-- State kicker -->
      <p class="cop-kicker">
        <svg width="10" height="10" viewBox="0 0 10 10" fill="currentColor" aria-hidden="true">
          <circle cx="5" cy="5" r="4.5"/>
        </svg>
        Combat terminé
      </p>

      <!-- Decorative rule -->
      <div class="cop-rule">
        <span class="cop-rule__line" />
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
          stroke="currentColor" stroke-width="1.2" stroke-linecap="round" stroke-linejoin="round"
          aria-hidden="true">
          <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/>
        </svg>
        <span class="cop-rule__line" />
      </div>

      <!-- Main title -->
      <h2 class="cop-title">
        {{ isVictory ? 'VICTOIRE' : 'DÉFAITE' }}
      </h2>

      <!-- Subtitle -->
      <p class="cop-desc">
        <template v-if="isVictory">
          Les manifestations se sont tues. Le palais retient son souffle.
        </template>
        <template v-else>
          Les voix se sont éteintes. Le silence a gagné.
        </template>
      </p>

      <!-- Action button -->
      <button
        v-if="isVictory"
        class="cop-btn cop-btn--victory"
        :disabled="isLoading"
        @click="$emit('continue')"
      >
        <span v-if="isLoading" class="cop-spinner" aria-hidden="true">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none"
            stroke="currentColor" stroke-width="2.2" stroke-linecap="round" aria-hidden="true">
            <path d="M12 2v4M12 18v4M4.93 4.93l2.83 2.83M16.24 16.24l2.83 2.83M2 12h4M18 12h4M4.93 19.07l2.83-2.83M16.24 7.76l2.83-2.83"/>
          </svg>
        </span>
        <span v-else>Continuer →</span>
      </button>

      <button
        v-else
        class="cop-btn cop-btn--defeat"
        :disabled="isLoading"
        @click="$emit('leaveRun')"
      >
        <span v-if="isLoading" class="cop-spinner" aria-hidden="true">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none"
            stroke="currentColor" stroke-width="2.2" stroke-linecap="round" aria-hidden="true">
            <path d="M12 2v4M12 18v4M4.93 4.93l2.83 2.83M16.24 16.24l2.83 2.83M2 12h4M18 12h4M4.93 19.07l2.83-2.83M16.24 7.76l2.83-2.83"/>
          </svg>
        </span>
        <span v-else>Quitter la run</span>
      </button>
    </div>
  </section>
</template>

<style scoped>
/* ── Root ── */
.cop-root {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 20;
  overflow: hidden;
  animation: cop-enter 320ms ease-out;
}

@keyframes cop-enter {
  from { opacity: 0; }
  to   { opacity: 1; }
}

/* ── Backdrop ── */
.cop-backdrop {
  position: absolute;
  inset: 0;
  pointer-events: none;
}

.cop-root--victory .cop-backdrop {
  background:
    radial-gradient(ellipse 80% 60% at 50% 40%, oklch(.50 .12 84 / .22) 0%, transparent 65%),
    oklch(.14 .03 272 / .88);
}

.cop-root--defeat .cop-backdrop {
  background:
    radial-gradient(ellipse 80% 60% at 50% 45%, oklch(.28 .10 15 / .28) 0%, transparent 65%),
    oklch(.12 .03 268 / .90);
}

/* ── Particles ── */
.cop-particles {
  position: absolute;
  inset: 0;
  pointer-events: none;
  overflow: hidden;
}

.cop-particle {
  position: absolute;
  border-radius: 50%;
  left: calc(var(--i) * 8.5% - 2%);
  bottom: -8px;
  animation: cop-rise calc(5s + var(--i) * 0.9s) ease-in-out infinite;
  animation-delay: calc(var(--i) * -0.7s);
}

.cop-root--victory .cop-particle {
  width: calc(2px + var(--i) * 0.4px);
  height: calc(2px + var(--i) * 0.4px);
  background: var(--gold, oklch(.72 .1 85));
  opacity: calc(.05 + var(--i) * .02);
}

.cop-root--defeat .cop-particle {
  width: calc(1px + var(--i) * 0.3px);
  height: calc(1px + var(--i) * 0.3px);
  background: var(--blood, oklch(.52 .15 20));
  opacity: calc(.04 + var(--i) * .015);
}

@keyframes cop-rise {
  0%   { transform: translateY(0) scale(1);       opacity: 0; }
  10%  { opacity: 1; }
  80%  { opacity: 0.8; }
  100% { transform: translateY(-100vh) scale(0.8); opacity: 0; }
}

/* ── Grain ── */
.cop-grain {
  position: absolute;
  inset: 0;
  pointer-events: none;
  background-image: url("data:image/svg+xml,%3Csvg viewBox='0 0 256 256' xmlns='http://www.w3.org/2000/svg'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.9' numOctaves='4' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23n)' opacity='0.055'/%3E%3C/svg%3E");
  opacity: 0.35;
  mix-blend-mode: multiply;
}

/* ── Corner ornaments ── */
.cop-corner {
  position: absolute;
  width: 20px;
  height: 20px;
  pointer-events: none;
}

.cop-root--victory .cop-corner {
  border-color: oklch(.72 .1 85 / .5);
}
.cop-root--defeat .cop-corner {
  border-color: oklch(.52 .15 20 / .4);
}

.cop-corner--tl {
  top: 0; left: 0;
  border-top: 1px solid;
  border-left: 1px solid;
}
.cop-corner--tr {
  top: 0; right: 0;
  border-top: 1px solid;
  border-right: 1px solid;
}
.cop-corner--bl {
  bottom: 0; left: 0;
  border-bottom: 1px solid;
  border-left: 1px solid;
}
.cop-corner--br {
  bottom: 0; right: 0;
  border-bottom: 1px solid;
  border-right: 1px solid;
}

/* ── Inner content ── */
.cop-inner {
  position: relative;
  z-index: 2;
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  gap: 0;
  animation: cop-content-in 400ms 80ms ease-out both;
}

@keyframes cop-content-in {
  from { opacity: 0; transform: translateY(12px); }
  to   { opacity: 1; transform: translateY(0); }
}

/* ── Kicker ── */
.cop-kicker {
  display: inline-flex;
  align-items: center;
  gap: 7px;
  font-family: var(--font-caps, var(--font));
  font-size: 10px;
  letter-spacing: 0.2em;
  text-transform: uppercase;
  margin-bottom: 20px;
}

.cop-root--victory .cop-kicker { color: oklch(.72 .1 85 / .75); }
.cop-root--defeat  .cop-kicker { color: oklch(.52 .15 20 / .75); }

/* ── Decorative rule ── */
.cop-rule {
  display: flex;
  align-items: center;
  gap: 12px;
  width: 240px;
  margin-bottom: 22px;
}

.cop-rule__line {
  flex: 1;
  height: 1px;
  opacity: .4;
}

.cop-root--victory .cop-rule__line {
  background: linear-gradient(90deg, transparent, var(--gold, oklch(.72 .1 85)), transparent);
}
.cop-root--victory .cop-rule svg { color: var(--gold, oklch(.72 .1 85)); }

.cop-root--defeat .cop-rule__line {
  background: linear-gradient(90deg, transparent, var(--blood, oklch(.52 .15 20)), transparent);
}
.cop-root--defeat .cop-rule svg { color: var(--blood, oklch(.52 .15 20)); opacity: .7; }

/* ── Title ── */
.cop-title {
  font-family: var(--font-display, var(--font));
  font-size: clamp(3rem, 8vw, 5.5rem);
  font-weight: 500;
  letter-spacing: 0.18em;
  line-height: 1;
  margin: 0 0 22px;
}

.cop-root--victory .cop-title {
  color: var(--gold, oklch(.72 .1 85));
  text-shadow:
    0 0 60px oklch(.72 .1 85 / .55),
    0 0 120px oklch(.72 .1 85 / .25);
}

.cop-root--defeat .cop-title {
  color: var(--blood, oklch(.52 .15 20));
  text-shadow:
    0 0 50px oklch(.52 .15 20 / .50),
    0 0 100px oklch(.52 .15 20 / .20);
}

/* ── Description ── */
.cop-desc {
  font-family: var(--font, serif);
  font-style: italic;
  font-size: 14px;
  line-height: 1.65;
  color: var(--ink-3, oklch(.62 .02 275));
  max-width: 380px;
  margin: 0 0 36px;
}

/* ── Button ── */
.cop-btn {
  min-width: 200px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 12px 32px;
  border-radius: 4px;
  font-family: var(--font-caps, var(--font));
  font-size: 11px;
  letter-spacing: 0.2em;
  text-transform: uppercase;
  cursor: pointer;
  transition: box-shadow 0.2s, opacity 0.2s, transform 0.15s;
}

.cop-btn:disabled {
  opacity: .4;
  pointer-events: none;
}

.cop-btn:not(:disabled):hover {
  transform: translateY(-1px);
}

.cop-btn--victory {
  border: 1px solid var(--gold, oklch(.72 .1 85));
  color: oklch(.12 .03 268);
  background: var(--gold, oklch(.72 .1 85));
  box-shadow: 0 0 28px -8px oklch(.72 .1 85 / .65);
}

.cop-btn--victory:not(:disabled):hover {
  box-shadow: 0 0 40px -6px oklch(.72 .1 85 / .80);
}

.cop-btn--defeat {
  border: 1px solid var(--blood, oklch(.52 .15 20));
  color: var(--blood, oklch(.52 .15 20));
  background: oklch(.52 .15 20 / .14);
  box-shadow: 0 0 28px -10px oklch(.52 .15 20 / .5);
}

.cop-btn--defeat:not(:disabled):hover {
  background: oklch(.52 .15 20 / .22);
  box-shadow: 0 0 38px -8px oklch(.52 .15 20 / .65);
}

/* ── Spinner ── */
.cop-spinner {
  display: flex;
  align-items: center;
  animation: cop-spin 1s linear infinite;
}

@keyframes cop-spin {
  to { transform: rotate(360deg); }
}

@media (prefers-reduced-motion: reduce) {
  .cop-root,
  .cop-inner,
  .cop-particle,
  .cop-spinner {
    animation: none;
  }
}
</style>
