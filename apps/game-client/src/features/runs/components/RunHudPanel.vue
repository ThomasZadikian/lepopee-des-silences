<script setup lang="ts">
import type { RunDto } from '../types/runTypes';

const props = defineProps<{
  run: RunDto;
}>();

function dangerClass(dangerHint: string): string {
  switch (dangerHint.toLowerCase()) {
    case 'critical': return 'danger--critical';
    case 'high':     return 'danger--high';
    case 'moderate': return 'danger--moderate';
    default:         return 'danger--low';
  }
}
</script>

<template>
  <section class="panel hud">
    <!-- Main info -->
    <div class="hud__main">
      <div class="hud__row">
        <div class="hud__cell">
          <span class="system-label">SALLE</span>
          <strong class="hud__value hud__salle">{{ run.currentRoomIndex + 1 }}</strong>
        </div>

        <div class="hud__cell">
          <span class="system-label">TYPE</span>
          <strong class="hud__value">{{ run.currentRoom.roomType }}</strong>
        </div>
      </div>

      <div class="hud__row">
        <div class="hud__cell">
          <span class="system-label">PROGRESSION</span>
          <strong class="hud__value">
            {{ run.currentRoom.currentNodeDepth + 1 }} / {{ run.currentRoom.maxNodeDepth + 1 }}
          </strong>
        </div>

        <div class="hud__cell hud__cell--progress">
          <div
            class="hud__progress-bar"
            :style="{
              width: `${((run.currentRoom.currentNodeDepth + 1) / (run.currentRoom.maxNodeDepth + 1)) * 100}%`
            }"
          />
        </div>
      </div>
    </div>

    <!-- Boss preview -->
    <div class="hud__boss">
      <span class="system-label">BOSS</span>
      <div class="hud__boss-info">
        <strong class="hud__value">{{ run.currentRoom.bossPreview.name }}</strong>
        <span
          class="hud__danger"
          :class="dangerClass(run.currentRoom.bossPreview.dangerHint)"
        >{{ run.currentRoom.bossPreview.dangerHint }}</span>
      </div>
    </div>

    <!-- Alpha debug block -->
    <details class="hud__debug">
      <summary class="system-label">ALPHA · DEBUG</summary>

      <dl class="hud__debug-list">
        <div class="hud__debug-row">
          <dt>Seed</dt>
          <dd>{{ run.seed }}</dd>
        </div>

        <div class="hud__debug-row">
          <dt>Générateur</dt>
          <dd>{{ run.generatorVersion }}</dd>
        </div>

        <div class="hud__debug-row">
          <dt>Markov</dt>
          <dd>{{ run.markovMatrixVersion }}</dd>
        </div>

        <div
          v-if="run.currentRoom.layoutTemplateKey"
          class="hud__debug-row"
        >
          <dt>Template</dt>
          <dd>{{ run.currentRoom.layoutTemplateKey }} · {{ run.currentRoom.layoutTemplateVersion }}</dd>
        </div>
      </dl>
    </details>
  </section>
</template>

<style scoped>
.hud {
  padding: var(--space-3) var(--space-4);
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
  border-color: color-mix(in oklch, var(--frost), transparent 65%);
}

/* --- Main block --- */

.hud__main {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.hud__row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--space-3);
  align-items: end;
}

.hud__cell {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
}

.hud__cell--progress {
  justify-content: flex-end;
}

.hud__value {
  font-size: 0.85rem;
  color: var(--frost);
  font-family: var(--font-mono);
  letter-spacing: 0.03em;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.hud__salle {
  font-size: 1.35rem;
  letter-spacing: 0.08em;
}

/* --- Progress bar --- */

.hud__progress-bar {
  height: 2px;
  background: var(--frost);
  border-radius: 1px;
  transition: width 0.3s ease;
  opacity: 0.55;
}

/* --- Boss block --- */

.hud__boss {
  border-top: 1px solid color-mix(in oklch, var(--line), transparent 35%);
  padding-top: var(--space-2);
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
}

.hud__boss-info {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-2);
}

.hud__danger {
  font-size: 0.7rem;
  font-family: var(--font-mono);
  text-transform: uppercase;
  letter-spacing: 0.06em;
  padding: 1px var(--space-2);
  border-radius: var(--radius-sm);
  border: 1px solid currentColor;
  white-space: nowrap;
}

.danger--low      { color: var(--frost); }
.danger--moderate { color: color-mix(in oklch, var(--frost), var(--gold) 35%); }
.danger--high     { color: var(--gold); }
.danger--critical { color: var(--blood); }

/* --- Debug block --- */

.hud__debug {
  border-top: 1px solid color-mix(in oklch, var(--line), transparent 35%);
  padding-top: var(--space-2);
}

.hud__debug summary {
  cursor: pointer;
  user-select: none;
  opacity: 0.6;
  font-size: 0.65rem;
}

.hud__debug summary:hover {
  opacity: 1;
}

.hud__debug-list {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
  margin-top: var(--space-2);
}

.hud__debug-row {
  display: grid;
  grid-template-columns: 5.5rem 1fr;
  gap: var(--space-2);
  font-size: 0.7rem;
  font-family: var(--font-mono);
}

.hud__debug-row dt {
  color: var(--ink-4);
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.hud__debug-row dd {
  color: var(--ink-3);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
