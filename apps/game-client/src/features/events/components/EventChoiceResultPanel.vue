<script setup lang="ts">
import type { CurrentEventChoiceResultDto } from '../types/eventTypes';

const props = defineProps<{ result: CurrentEventChoiceResultDto; isLoading: boolean }>()
defineEmits<{ continue: [] }>()

function outcomeTone(kind: string | undefined | null): 'blood' | 'frost' | 'gold' | null {
  if (!kind) return null
  const k = kind.toLowerCase()
  if (k.includes('fail') || k.includes('death') || k.includes('loss') || k.includes('bad') || k.includes('malus')) return 'blood'
  if (k.includes('success') || k.includes('gain') || k.includes('reward') || k.includes('gold') || k.includes('win')) return 'gold'
  return 'frost'
}

function outcomeLabel(kind: string | undefined | null, state: string | undefined | null): string {
  return kind ?? state ?? 'Résolu'
}

function accentVar(tone: 'blood' | 'frost' | 'gold' | null): string {
  if (tone === 'blood') return 'var(--blood)'
  if (tone === 'gold')  return 'var(--gold)'
  return 'var(--frost)'
}

function accentWash(tone: 'blood' | 'frost' | 'gold' | null): string {
  if (tone === 'blood') return 'var(--wash-blood, oklch(.52 .15 20 / .10))'
  if (tone === 'gold')  return 'var(--wash-gold, oklch(.72 .1 85 / .10))'
  return 'var(--wash-frost, oklch(.6 .1 195 / .10))'
}
</script>

<template>
  <div class="ecr-root">
    <!-- Atmospheric backdrop -->
    <div class="ecr-bg" aria-hidden="true" />

    <!-- Floating particles -->
    <div class="ecr-particles" aria-hidden="true">
      <span
        v-for="n in 14"
        :key="n"
        class="ecr-particle"
        :style="`--i:${n}`"
      />
    </div>

    <!-- Grain overlay -->
    <div class="ecr-grain" aria-hidden="true" />

    <!-- Corner ornaments -->
    <span class="ecr-corner ecr-corner--tl" aria-hidden="true" />
    <span class="ecr-corner ecr-corner--tr" aria-hidden="true" />
    <span class="ecr-corner ecr-corner--bl" aria-hidden="true" />
    <span class="ecr-corner ecr-corner--br" aria-hidden="true" />

    <!-- Content -->
    <div class="ecr-inner" :style="{ '--accent': accentVar(outcomeTone(result.outcomeKind)), '--accent-wash': accentWash(outcomeTone(result.outcomeKind)) }">
      <!-- Kicker -->
      <p class="ecr-kicker">
        <svg width="8" height="8" viewBox="0 0 8 8" fill="currentColor" aria-hidden="true">
          <circle cx="4" cy="4" r="3.5"/>
        </svg>
        Choix résolu
      </p>

      <!-- Chip -->
      <span
        class="es-chip ecr-chip"
        :class="{
          'es-chip--blood': outcomeTone(result.outcomeKind) === 'blood',
          'es-chip--gold':  outcomeTone(result.outcomeKind) === 'gold',
          'es-chip--frost': outcomeTone(result.outcomeKind) === 'frost' || outcomeTone(result.outcomeKind) === null,
        }"
        style="margin-bottom: 22px;"
      >
        {{ outcomeLabel(result.outcomeKind, result.state) }}
      </span>

      <!-- Decorative rule -->
      <div class="ecr-rule" style="margin-bottom: 22px;">
        <span class="ecr-rule__line" />
        <svg
          width="16" height="16" viewBox="0 0 24 24" fill="none"
          stroke="currentColor" stroke-width="1.2"
          stroke-linecap="round" stroke-linejoin="round"
          aria-hidden="true"
        >
          <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/>
        </svg>
        <span class="ecr-rule__line" />
      </div>

      <!-- Main title -->
      <h2 class="ecr-title">{{ result.title ?? 'Choix accompli' }}</h2>

      <!-- Description -->
      <p class="ecr-desc">
        {{ result.description ?? result.message ?? 'Ton geste a été retenu par le Palais.' }}
      </p>

      <!-- Continue button -->
      <button
        class="ecr-btn"
        :disabled="isLoading"
        @click="$emit('continue')"
      >
        <span v-if="isLoading" class="ecr-spinner" aria-hidden="true">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none"
            stroke="currentColor" stroke-width="2.2" stroke-linecap="round" aria-hidden="true">
            <path d="M12 2v4M12 18v4M4.93 4.93l2.83 2.83M16.24 16.24l2.83 2.83M2 12h4M18 12h4M4.93 19.07l2.83-2.83M16.24 7.76l2.83-2.83"/>
          </svg>
        </span>
        <span v-else>Continuer →</span>
      </button>
    </div>
  </div>
</template>

<style scoped>
.ecr-root {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 100%;
  overflow: hidden;
  animation: ecr-enter 320ms ease-out;
}

@keyframes ecr-enter {
  from { opacity: 0; }
  to   { opacity: 1; }
}

/* Backdrop */
.ecr-bg {
  position: absolute;
  inset: 0;
  pointer-events: none;
  background:
    radial-gradient(ellipse 80% 60% at 50% 35%, oklch(.32 .06 232 / .20) 0%, transparent 60%),
    oklch(.15 .028 270 / .96);
}

/* Particles */
.ecr-particles {
  position: absolute;
  inset: 0;
  pointer-events: none;
  overflow: hidden;
}
.ecr-particle {
  position: absolute;
  border-radius: 50%;
  left: calc(var(--i) * 7.2% - 1%);
  bottom: -8px;
  background: var(--accent, var(--frost));
  width: calc(1.5px + var(--i) * 0.3px);
  height: calc(1.5px + var(--i) * 0.3px);
  opacity: calc(.04 + var(--i) * .014);
  animation: ecr-rise calc(6s + var(--i) * 0.8s) ease-in-out infinite;
  animation-delay: calc(var(--i) * -0.6s);
}
@keyframes ecr-rise {
  0%   { transform: translateY(0) scale(1);        opacity: 0; }
  10%  { opacity: 1; }
  80%  { opacity: 0.7; }
  100% { transform: translateY(-100vh) scale(0.7); opacity: 0; }
}

/* Grain */
.ecr-grain {
  position: absolute;
  inset: 0;
  pointer-events: none;
  background-image: url("data:image/svg+xml,%3Csvg viewBox='0 0 256 256' xmlns='http://www.w3.org/2000/svg'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.9' numOctaves='4' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23n)' opacity='0.055'/%3E%3C/svg%3E");
  opacity: 0.28;
  mix-blend-mode: multiply;
}

/* Corners */
.ecr-corner {
  position: absolute;
  width: 20px;
  height: 20px;
  pointer-events: none;
  border-color: var(--accent, var(--frost));
  opacity: .45;
}
.ecr-corner--tl { top: 0; left: 0;     border-top: 1px solid; border-left: 1px solid; }
.ecr-corner--tr { top: 0; right: 0;    border-top: 1px solid; border-right: 1px solid; }
.ecr-corner--bl { bottom: 0; left: 0;  border-bottom: 1px solid; border-left: 1px solid; }
.ecr-corner--br { bottom: 0; right: 0; border-bottom: 1px solid; border-right: 1px solid; }

/* Inner content */
.ecr-inner {
  position: relative;
  z-index: 2;
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  max-width: 600px;
  width: 100%;
  padding: 0 32px;
  animation: ecr-content-in 400ms 80ms ease-out both;
}
@keyframes ecr-content-in {
  from { opacity: 0; transform: translateY(12px); }
  to   { opacity: 1; transform: translateY(0); }
}

/* Kicker */
.ecr-kicker {
  display: inline-flex;
  align-items: center;
  gap: 7px;
  font-family: var(--caps);
  font-size: 10px;
  letter-spacing: 0.2em;
  text-transform: uppercase;
  color: var(--accent, var(--frost));
  opacity: .75;
  margin-bottom: 16px;
}

.ecr-chip {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

/* Decorative rule */
.ecr-rule {
  display: flex;
  align-items: center;
  gap: 12px;
  width: 240px;
}
.ecr-rule__line {
  flex: 1;
  height: 1px;
  background: linear-gradient(90deg, transparent, var(--accent, var(--frost)), transparent);
  opacity: .35;
}
.ecr-rule svg { color: var(--accent, var(--frost)); opacity: .7; }

/* Title */
.ecr-title {
  font-family: var(--display);
  font-size: clamp(2.2rem, 6vw, 3.6rem);
  font-weight: 400;
  letter-spacing: 0.02em;
  line-height: 1.06;
  color: var(--ink);
  margin: 0 0 20px;
  text-shadow: 0 0 60px var(--accent, var(--frost));
}

/* Description */
.ecr-desc {
  font-family: var(--font);
  font-style: italic;
  font-size: 15px;
  line-height: 1.65;
  color: var(--ink-3);
  max-width: 480px;
  margin: 0 0 36px;
}

/* Button */
.ecr-btn {
  min-width: 200px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 12px 32px;
  border-radius: 4px;
  border: 1px solid var(--accent, var(--frost));
  color: var(--accent, var(--frost));
  background: var(--accent-wash, oklch(.6 .1 195 / .10));
  font-family: var(--caps);
  font-size: 11px;
  letter-spacing: 0.2em;
  text-transform: uppercase;
  cursor: pointer;
  transition: box-shadow .2s, opacity .2s, transform .15s, background .18s;
  box-shadow: 0 0 28px -10px var(--accent, var(--frost));
}
.ecr-btn:not(:disabled):hover {
  transform: translateY(-1px);
  background: oklch(from var(--accent, var(--frost)) l c h / 0.18);
  box-shadow: 0 0 38px -8px var(--accent, var(--frost));
}
.ecr-btn:disabled {
  opacity: .4;
  pointer-events: none;
}

/* Spinner */
.ecr-spinner {
  display: flex;
  align-items: center;
  animation: ecr-spin 1s linear infinite;
}
@keyframes ecr-spin {
  to { transform: rotate(360deg); }
}

@media (prefers-reduced-motion: reduce) {
  .ecr-root, .ecr-inner, .ecr-particle, .ecr-spinner {
    animation: none;
  }
}
</style>
