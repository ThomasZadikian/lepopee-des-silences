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
      <span class="rcp-kicker">Climat de Room</span>
      <span class="rcp-status" :class="{ 'rcp-status--active': display }">{{ display ? 'Actif' : 'Aucun' }}</span>
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
  padding: 12px 14px;
  background: var(--panel-2);
  border: 1px solid var(--line-soft);
  margin-bottom: 22px;
}

.rcp-head,
.rcp-card__head,
.rcp-meta div {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.rcp-kicker {
  font-family: var(--font-mono);
  font-size: 10px;
  letter-spacing: .14em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.rcp-status {
  font-family: var(--font-mono);
  font-size: 10px;
  letter-spacing: .1em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.rcp-status--active { color: var(--mint-dim); }

.rcp-card {
  display: grid;
  gap: 6px;
  margin-top: 8px;
}

.rcp-card__head strong {
  font-family: var(--font-display);
  font-style: italic;
  color: var(--ink);
  font-size: 14px;
}

.rcp-card__type,
.rcp-meta {
  font-family: var(--font-mono);
  font-size: 10px;
  color: var(--ink-4);
}

.rcp-card__desc,
.rcp-empty {
  margin: 0;
  color: var(--ink-3);
  font-size: 12px;
  line-height: 1.5;
}

.rcp-meta {
  display: grid;
  gap: 4px;
  margin: 4px 0 0;
}

.rcp-meta dt,
.rcp-meta dd { margin: 0; }

.rcp-meta dt {
  color: var(--ink-5);
  text-transform: uppercase;
  letter-spacing: .08em;
}

.rcp-meta dd {
  color: var(--ink-3);
  text-align: right;
}
</style>
