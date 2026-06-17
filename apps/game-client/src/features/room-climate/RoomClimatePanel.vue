<script setup lang="ts">
import { computed } from 'vue';

import type { RoomClimateStateDto } from '../runs/types/runTypes';
import { resolveRoomClimateDisplay } from './roomClimateDisplay';

const props = defineProps<{
  climate?: RoomClimateStateDto | null;
}>();

const display = computed(() => resolveRoomClimateDisplay(props.climate));
</script>

<template>
  <section class="rcp-root" aria-label="Climat de Room">
    <div class="rcp-head">
      <span class="es-kicker">Climat de Room</span>
      <span v-if="display" class="es-chip es-chip--frost">Actif</span>
      <span v-else class="es-chip">Aucun</span>
    </div>

    <div v-if="display" class="rcp-card">
      <div class="rcp-card__head">
        <strong>{{ display.displayName }}</strong>
        <span v-if="display.type" class="rcp-card__type">{{ display.type }}</span>
      </div>

      <p v-if="display.description" class="rcp-card__desc">{{ display.description }}</p>

      <dl class="rcp-meta">
        <div v-if="display.source">
          <dt>Source</dt>
          <dd>{{ display.source }}</dd>
        </div>
        <div>
          <dt>Durée</dt>
          <dd>{{ display.duration }}</dd>
        </div>
        <div v-if="display.roomId">
          <dt>Room</dt>
          <dd>{{ display.roomId }}</dd>
        </div>
      </dl>
    </div>

    <p v-else class="rcp-empty">Aucun climat actif dans cette Room.</p>
  </section>
</template>

<style scoped>
.rcp-root {
  padding: var(--space-3);
  border: 1px solid color-mix(in oklch, var(--frost), transparent 68%);
  border-radius: var(--radius-sm);
  background: oklch(0.16 0.035 272 / 0.64);
}

.rcp-head,
.rcp-card__head,
.rcp-meta div {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-2);
}

.rcp-card {
  display: grid;
  gap: var(--space-2);
  margin-top: var(--space-2);
}

.rcp-card__head strong {
  font-family: var(--font-display);
  color: var(--ink);
  font-size: 1rem;
}

.rcp-card__type,
.rcp-meta {
  font-family: var(--font-mono);
  font-size: 0.68rem;
  color: var(--ink-4);
}

.rcp-card__desc,
.rcp-empty {
  margin: 0;
  color: var(--ink-3);
  font-size: 0.78rem;
  line-height: 1.45;
}

.rcp-meta {
  display: grid;
  gap: 4px;
  margin: 0;
}

.rcp-meta dt,
.rcp-meta dd {
  margin: 0;
}

.rcp-meta dt {
  color: var(--ink-5);
  text-transform: uppercase;
  letter-spacing: 0.08em;
}

.rcp-meta dd {
  color: var(--frost-dim);
  text-align: right;
}
</style>
