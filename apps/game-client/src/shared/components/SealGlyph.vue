<script setup lang="ts">
/**
 * Le sceau animé — anneau + glyphe + anneau de tampon + coche, purement réactif à `state`.
 * Retravaillé pour "le Palais respire" (mint, plus d'or) : réservé aux moments qui méritent
 * d'être sentis comme un engagement plutôt qu'un simple clic — aujourd'hui, sceller une loi
 * dans la superposition de nœuds.
 *
 * `confirming` joue l'anneau de tampon une fois ; `confirmed` allume le pourtour et la coche.
 */
import { useId } from 'vue';
import SigilIcon from './SigilIcon.vue';

const props = withDefaults(defineProps<{
  kind: string;
  tone?: 'mint' | 'danger' | 'ink';
  size?: number;
  sigilSize?: number;
  sigilStrokeWidth?: number;
  topText?: string;
  bottomText?: string;
  state?: 'idle' | 'confirming' | 'confirmed';
}>(), {
  tone: 'mint',
  size: 96,
  sigilStrokeWidth: 1,
  state: 'idle',
});

const sigilSize = props.sigilSize ?? Math.round(props.size * 0.39);
const arcId = useId();
const topArcId = `sg-arc-top-${arcId}`;
const botArcId = `sg-arc-bot-${arcId}`;
</script>

<template>
  <div
    :class="['sg-seal', `sg-seal--${tone}`, state === 'confirmed' && 'sg-seal--done']"
    :style="{ '--sg-size': size + 'px' }"
  >
    <div class="sg-seal__ring" />

    <div class="sg-seal__sigil" :style="{ opacity: state === 'confirmed' ? 1 : 0.55 }">
      <SigilIcon :kind="kind" :size="sigilSize" :stroke-width="sigilStrokeWidth" />
    </div>

    <div :class="['sg-stamp', state === 'confirming' && 'sg-stamp--go']" />

    <div :class="['sg-check', state === 'confirmed' && 'sg-check--show']">
      <svg width="42%" height="42%" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
        <path d="M3 8l4 4 6-7" />
      </svg>
    </div>

    <svg v-if="topText || bottomText" class="sg-art" viewBox="0 0 218 218" fill="none" aria-hidden="true">
      <path :id="topArcId" d="M 30 109 A 79 79 0 0 1 188 109" fill="none" />
      <path :id="botArcId" d="M 188 109 A 79 79 0 0 1 30 109" fill="none" />
      <text v-if="topText" font-family="var(--font-caps)" font-size="9" letter-spacing="3" fill="currentColor" opacity="0.4" text-anchor="middle">
        <textPath :href="`#${topArcId}`" startOffset="50%">{{ topText }}</textPath>
      </text>
      <text v-if="bottomText" font-family="var(--font-mono)" font-size="7.5" letter-spacing="2" fill="currentColor" opacity="0.3" text-anchor="middle">
        <textPath :href="`#${botArcId}`" startOffset="50%">{{ bottomText }}</textPath>
      </text>
    </svg>
  </div>
</template>

<style scoped>
.sg-seal {
  position: relative;
  width: var(--sg-size);
  height: var(--sg-size);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  border: 1px solid var(--mint-dim);
  background: radial-gradient(circle at 40% 35%, rgba(191, 227, 224, .08), transparent 75%);
  color: var(--mint-dim);
  transition: border-color 0.4s, color 0.4s;
}

.sg-seal--danger { border-color: var(--danger-dim); color: var(--danger-dim); background: radial-gradient(circle at 40% 35%, rgba(192, 114, 104, .08), transparent 75%); }
.sg-seal--ink    { border-color: var(--line-strong); color: var(--ink-3); background: none; }

.sg-seal--done { border-color: currentColor; }
.sg-seal--done.sg-seal--mint   { color: var(--mint); }
.sg-seal--done.sg-seal--danger { color: var(--danger); }
.sg-seal--done.sg-seal--ink    { color: var(--ink-2); }

.sg-seal__ring {
  position: absolute;
  inset: calc(var(--sg-size) * 0.055);
  border-radius: 50%;
  border: 1px dashed currentColor;
  opacity: 0.5;
  pointer-events: none;
}

.sg-seal__sigil {
  position: absolute;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: opacity 0.4s;
}

.sg-stamp {
  position: absolute;
  inset: calc(var(--sg-size) * -0.018);
  border-radius: 50%;
  border: max(1.5px, calc(var(--sg-size) * 0.014)) solid currentColor;
  opacity: 0;
  transform: scale(0.6);
  pointer-events: none;
}

.sg-stamp--go {
  animation: sgStamp 0.46s cubic-bezier(0.5, 0, 0.5, 1) forwards;
}

@keyframes sgStamp {
  0%   { opacity: 0; transform: scale(0.6); }
  50%  { opacity: 1; transform: scale(1.05); }
  100% { opacity: 0; transform: scale(1.18); }
}

.sg-check {
  position: absolute;
  bottom: calc(var(--sg-size) * 0.046);
  right: calc(var(--sg-size) * 0.046);
  width: calc(var(--sg-size) * 0.128);
  height: calc(var(--sg-size) * 0.128);
  min-width: 10px;
  min-height: 10px;
  border-radius: 50%;
  background: currentColor;
  color: var(--void);
  display: flex;
  align-items: center;
  justify-content: center;
  transform: scale(0);
  transition: transform 0.32s cubic-bezier(0.34, 1.56, 0.64, 1);
  pointer-events: none;
}

.sg-check--show { transform: scale(1); }

.sg-art {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  pointer-events: none;
  color: currentColor;
}

@media (prefers-reduced-motion: reduce) {
  .sg-stamp--go { animation: none; }
}
</style>
