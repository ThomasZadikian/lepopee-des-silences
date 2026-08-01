<script setup lang="ts">
import type { DevToolsRunPsycheResponse } from '../types/devToolsTypes';

const props = defineProps<{
  disabled: boolean;
  isLoading: boolean;
  psyche: DevToolsRunPsycheResponse | null;
}>();

const emit = defineEmits<{ refresh: [] }>();

function entries(distribution: Record<string, number>): Array<[string, number]> {
  return Object.entries(distribution).sort((a, b) => b[1] - a[1]);
}

function pct(value: number): string {
  return `${Math.round(value * 100)}%`;
}
</script>

<template>
  <div class="devtools-window">
    <header class="devtools-window__head">
      <h2>Psyché du Palais</h2>
      <p>Lecture seule : distribution émotionnelle courante et trajectoire par salle.</p>
    </header>

    <div class="devtools-window__body">
      <button class="devtools-btn" :disabled="props.disabled || props.isLoading" @click="emit('refresh')">
        Rafraîchir
      </button>

      <p v-if="!props.psyche" class="devtools-muted">
        Aucune donnée. Lance « Rafraîchir » (token requis).
      </p>

      <template v-else>
        <p class="devtools-psyche__dominant">
          Dominante : <strong>{{ props.psyche.dominant }}</strong>
        </p>

        <ul class="devtools-psyche__bars">
          <li v-for="[emotion, value] in entries(props.psyche.current)" :key="emotion">
            <span class="devtools-psyche__label">{{ emotion }}</span>
            <span class="devtools-psyche__track">
              <span class="devtools-psyche__fill" :style="{ width: pct(value) }" />
            </span>
            <span class="devtools-psyche__value">{{ pct(value) }}</span>
          </li>
        </ul>

        <h4 class="devtools-psyche__subtitle">Trajectoire (par salle)</h4>
        <ol class="devtools-psyche__trajectory">
          <li v-for="step in props.psyche.trajectory" :key="step.depth">
            <span class="devtools-psyche__depth">#{{ step.depth }}</span>
            <span class="devtools-psyche__step-dominant">{{ step.dominant }}</span>
            <span class="devtools-psyche__spark">
              <span
                v-for="[emotion, value] in entries(step.distribution)"
                :key="emotion"
                class="devtools-psyche__spark-seg"
                :title="`${emotion} ${pct(value)}`"
                :style="{ flexGrow: value }"
                :data-emotion="emotion"
              />
            </span>
          </li>
        </ol>
      </template>
    </div>
  </div>
</template>

<style scoped>
.devtools-psyche__dominant { font-size: 13px; }
.devtools-psyche__bars { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 6px; }
.devtools-psyche__bars li { display: grid; grid-template-columns: 120px 1fr 48px; align-items: center; gap: 8px; }
.devtools-psyche__label { font-size: 12px; color: var(--ink-4, #a49a88); }
.devtools-psyche__track { height: 12px; border-radius: 6px; background: oklch(0.11 0.025 270 / 0.9); overflow: hidden; }
.devtools-psyche__fill { display: block; height: 100%; background: var(--gold, #d7b56d); }
.devtools-psyche__value { font-size: 12px; text-align: right; }
.devtools-psyche__subtitle { font-size: 11px; letter-spacing: 0.12em; text-transform: uppercase; color: var(--ink-4, #a49a88); margin: 8px 0 0; }
.devtools-psyche__trajectory { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 5px; }
.devtools-psyche__trajectory li { display: grid; grid-template-columns: 36px 120px 1fr; align-items: center; gap: 8px; }
.devtools-psyche__depth { font-size: 11px; color: var(--ink-4, #a49a88); }
.devtools-psyche__step-dominant { font-size: 12px; }
.devtools-psyche__spark { display: flex; height: 10px; border-radius: 5px; overflow: hidden; background: oklch(0.11 0.025 270 / 0.9); }
.devtools-psyche__spark-seg { height: 100%; min-width: 2px; }
.devtools-psyche__spark-seg[data-emotion='Calm'] { background: oklch(0.78 0.12 145); }
.devtools-psyche__spark-seg[data-emotion='Wary'] { background: oklch(0.80 0.13 85); }
.devtools-psyche__spark-seg[data-emotion='Withdrawn'] { background: oklch(0.62 0.06 270); }
.devtools-psyche__spark-seg[data-emotion='Dissociated'] { background: oklch(0.60 0.10 300); }
.devtools-psyche__spark-seg[data-emotion='Fragmented'] { background: oklch(0.66 0.17 25); }
.devtools-psyche__spark-seg[data-emotion='Lucid'] { background: oklch(0.82 0.12 200); }
</style>
