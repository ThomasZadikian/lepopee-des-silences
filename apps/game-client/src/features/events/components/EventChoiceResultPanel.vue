<script setup lang="ts">
import type { CurrentEventChoiceResultDto } from '../types/eventTypes';

const props = defineProps<{ result: CurrentEventChoiceResultDto; isLoading: boolean }>();
defineEmits<{ continue: [] }>();

function outcomeTone(kind: string | undefined | null): 'blood' | 'frost' | 'gold' | null {
  if (!kind) return null;
  const k = kind.toLowerCase();
  if (k.includes('fail') || k.includes('death') || k.includes('loss') || k.includes('bad') || k.includes('malus')) return 'blood';
  if (k.includes('success') || k.includes('gain') || k.includes('reward') || k.includes('gold') || k.includes('win')) return 'gold';
  return 'frost';
}

function outcomeLabel(kind: string | undefined | null, state: string | undefined | null): string {
  return kind ?? state ?? 'Résolu';
}
</script>

<template>
  <div class="ecr-root">
    <!-- Ambiance atmosphérique : particules CSS -->
    <div class="ecr-atmos" aria-hidden="true">
      <span v-for="n in 9" :key="n" class="ecr-atmos__dot" :style="`--i:${n}`" />
    </div>

    <!-- Vignette de bord -->
    <div class="ecr-vignette" aria-hidden="true" />

    <!-- Coin frost haut-gauche -->
    <span class="es-corner es-corner--frost es-corner--tl" aria-hidden="true" />
    <!-- Coin frost bas-droite -->
    <span class="es-corner es-corner--frost es-corner--br" aria-hidden="true" />

    <!-- Contenu central -->
    <div class="ecr-inner">
      <!-- Chip d'état -->
      <div class="ecr-chip-row">
        <span
          class="es-chip ecr-chip"
          :class="{
            'es-chip--blood': outcomeTone(result.outcomeKind) === 'blood',
            'es-chip--gold':  outcomeTone(result.outcomeKind) === 'gold',
            'es-chip--frost': outcomeTone(result.outcomeKind) === 'frost' || outcomeTone(result.outcomeKind) === null,
          }"
        >
          <!-- Outcome icon: small inline circle indicator -->
          <svg width="8" height="8" viewBox="0 0 8 8" fill="currentColor" aria-hidden="true">
            <circle cx="4" cy="4" r="3.5"/>
          </svg>
          Choix résolu · {{ outcomeLabel(result.outcomeKind, result.state) }}
        </span>
      </div>

      <!-- Titre -->
      <h2 class="es-h2 ecr-title">{{ result.title ?? 'CHOICE_DONE' }}</h2>

      <!-- Séparateur décoratif -->
      <div class="ecr-rule">
        <span class="ecr-rule__line" />
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none"
          stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"
          aria-hidden="true" style="color:var(--frost); flex:0 0 auto;">
          <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/>
        </svg>
        <span class="ecr-rule__line" />
      </div>

      <!-- Description -->
      <p class="es-body ecr-desc">
        {{ result.description ?? result.message ?? 'CHOICE_REGISTERED' }}
      </p>

      <!-- Bouton Continuer -->
      <button
        class="es-btn es-btn--frost es-btn--lg ecr-continue"
        :disabled="isLoading"
        :class="{ 'ecr-continue--loading': isLoading }"
        @click="$emit('continue')"
      >
        <span v-if="isLoading" class="ecr-spinner" aria-hidden="true">
          <!-- Spinner inline SVG -->
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none"
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
/* ── Racine : pleine surface, centrée, ambiance nocturne ── */
.ecr-root {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  min-height: 400px;
  padding: 48px 32px;
  overflow: hidden;
  background: linear-gradient(
    160deg,
    oklch(.24 .034 268 / .92) 0%,
    oklch(.19 .026 270 / .96) 55%,
    oklch(.22 .03 268 / .88) 100%
  );
  border: 1px solid var(--line);
  border-radius: 6px;
}

/* ── Atmosphère : particules flottantes ── */
.ecr-atmos {
  position: absolute;
  inset: 0;
  pointer-events: none;
  overflow: hidden;
}

.ecr-atmos__dot {
  position: absolute;
  width: calc(1px + var(--i) * 0.4px);
  height: calc(1px + var(--i) * 0.4px);
  border-radius: 50%;
  background: var(--frost, oklch(.70 .07 232));
  opacity: calc(.06 + var(--i) * .018);
  left: calc(var(--i) * 11% - 2%);
  top: calc(var(--i) * 9% + 5%);
  animation: ecr-float calc(8s + var(--i) * 2.3s) ease-in-out infinite;
  animation-delay: calc(var(--i) * -1.1s);
}

@keyframes ecr-float {
  0%, 100% { transform: translateY(0) scale(1); opacity: calc(.06 + var(--i) * .018); }
  50%       { transform: translateY(-14px) scale(1.12); opacity: calc(.12 + var(--i) * .022); }
}

/* ── Vignette ── */
.ecr-vignette {
  position: absolute;
  inset: 0;
  pointer-events: none;
  background: radial-gradient(ellipse at 50% 50%, transparent 38%, oklch(.12 .02 268 / .65) 100%);
}

/* ── Coins décoratifs (fallback si es-corner non stylé globalement) ── */
.es-corner {
  position: absolute;
  width: 18px;
  height: 18px;
  pointer-events: none;
}
.es-corner--frost { --c-corner: var(--frost, oklch(.70 .07 232)); opacity: .55; }
.es-corner--tl { top: 0; left: 0; border-top: 1px solid var(--c-corner); border-left: 1px solid var(--c-corner); }
.es-corner--br { bottom: 0; right: 0; border-bottom: 1px solid var(--c-corner); border-right: 1px solid var(--c-corner); }

/* ── Contenu central ── */
.ecr-inner {
  position: relative;
  z-index: 2;
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  max-width: 680px;
  width: 100%;
  gap: 0;
}

/* ── Chip d'état ── */
.ecr-chip-row {
  margin-bottom: 20px;
}

.ecr-chip {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

/* ── Titre ── */
.ecr-title {
  font-size: 38px;
  line-height: 1.06;
  margin: 0 0 18px;
  max-width: 600px;
}

/* ── Séparateur décoratif ── */
.ecr-rule {
  display: flex;
  align-items: center;
  gap: 12px;
  width: 100%;
  max-width: 340px;
  margin: 0 auto 22px;
}

.ecr-rule__line {
  flex: 1;
  height: 1px;
  background: linear-gradient(90deg, transparent, var(--frost, oklch(.70 .07 232)), transparent);
  opacity: .35;
}

/* ── Description ── */
.ecr-desc {
  font-size: 15px;
  line-height: 1.65;
  color: var(--ink-2);
  max-width: 560px;
  margin: 0 0 32px;
}

/* ── Bouton Continuer ── */
.ecr-continue {
  min-width: 220px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  transition: opacity .2s, box-shadow .2s;
}

.ecr-continue:disabled {
  opacity: .45;
  pointer-events: none;
}

.ecr-continue--loading {
  pointer-events: none;
}

/* ── Spinner ── */
.ecr-spinner {
  display: flex;
  align-items: center;
  animation: ecr-spin 1s linear infinite;
}

@keyframes ecr-spin {
  to { transform: rotate(360deg); }
}
</style>
