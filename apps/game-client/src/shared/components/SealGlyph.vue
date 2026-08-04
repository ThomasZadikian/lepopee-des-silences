<script setup lang="ts">
/**
 * Le sceau animé — extrait du panneau de loi (« apposer le sceau au Tome »), généralisé pour
 * tout moment du jeu qui mérite d'être senti comme un engagement plutôt qu'un simple clic :
 * choisir une récompense, équiper un objet trouvé en run, sceller un verdict de réputation.
 *
 * Le composant est purement réactif à `state` — c'est à l'appelant de décider quand une
 * confirmation est en cours (`confirming`, joue l'anneau de tampon une fois) et quand elle est
 * acquise (`confirmed`, allume le pourtour et la coche).
 */
import { useId } from 'vue';
import SigilIcon from './SigilIcon.vue';

const props = withDefaults(defineProps<{
  kind: string;
  tone?: 'gold' | 'frost' | 'blood' | 'sap' | 'ink';
  size?: number;
  sigilSize?: number;
  sigilStrokeWidth?: number;
  topText?: string;
  bottomText?: string;
  state?: 'idle' | 'confirming' | 'confirmed';
}>(), {
  tone: 'gold',
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
      <text v-if="topText" font-family="var(--caps, 'Marcellus SC', serif)" font-size="9" letter-spacing="3" fill="currentColor" opacity="0.4" text-anchor="middle">
        <textPath :href="`#${topArcId}`" startOffset="50%">{{ topText }}</textPath>
      </text>
      <text v-if="bottomText" font-family="var(--mono, 'JetBrains Mono', monospace)" font-size="7.5" letter-spacing="2" fill="currentColor" opacity="0.3" text-anchor="middle">
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
  border: 1px solid var(--gold-dim);
  background: radial-gradient(circle at 40% 35%, oklch(0.72 0.12 84 / 0.18), oklch(0.55 0.08 84 / 0.08) 55%, transparent 80%);
  color: var(--gold);
  transition: border-color 0.4s, box-shadow 0.4s;
}

.sg-seal--frost { border-color: var(--frost-dim); color: var(--frost); background: radial-gradient(circle at 40% 35%, var(--wash-frost), transparent 80%); }
.sg-seal--blood { border-color: var(--blood-dim); color: var(--blood); background: radial-gradient(circle at 40% 35%, var(--wash-blood), transparent 80%); }
.sg-seal--sap   { border-color: var(--sap); color: var(--sap); background: radial-gradient(circle at 40% 35%, var(--wash-sap), transparent 80%); }
.sg-seal--ink   { border-color: var(--line-strong); color: var(--ink-3); background: radial-gradient(circle at 40% 35%, oklch(0.6 0.02 272 / 0.14), transparent 80%); }

.sg-seal--done { box-shadow: 0 0 calc(var(--sg-size) * 0.22) calc(var(--sg-size) * -0.055) currentColor; }
.sg-seal--done.sg-seal--gold  { border-color: var(--gold); }
.sg-seal--done.sg-seal--frost { border-color: var(--frost); }
.sg-seal--done.sg-seal--blood { border-color: var(--blood); }
.sg-seal--done.sg-seal--sap   { border-color: var(--sap); }
.sg-seal--done.sg-seal--ink   { border-color: var(--ink-3); }

.sg-seal__ring {
  position: absolute;
  inset: calc(var(--sg-size) * 0.055);
  border-radius: 50%;
  border: 1px dashed currentColor;
  opacity: 0.55;
  pointer-events: none;
}

.sg-seal--done .sg-seal__ring { opacity: 0.5; }

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
  animation: sgStamp 0.46s cubic-bezier(0.22, 0.8, 0.4, 1) forwards;
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
  min-width: 14px;
  min-height: 14px;
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
</style>
