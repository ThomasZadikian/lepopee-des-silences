<template>
  <header class="runbar">
    <div class="runbar__seg">
      <span class="runbar__k">Palais</span>
      <span class="runbar__v">{{ currentRoom?.theme ?? '—' }}</span>
    </div>

    <div class="runbar__seg">
      <span class="runbar__k">Salle</span>
      <span class="runbar__v runbar__v--gold">{{ currentRun != null ? currentRun.currentRoomIndex + 1 : '—' }}</span>
    </div>

    <div class="runbar__seg">
      <span class="runbar__k">Seed</span>
      <span class="runbar__v runbar__v--mono">{{ currentRun?.seed ?? '—' }}</span>
    </div>

    <div v-if="(currentRun?.activePalaceLaws?.length ?? 0) > 0" class="runbar__seg runbar__seg--clickable" @click="uiStore.toggleLaws">
      <span class="runbar__k">Lois</span>
      <span class="runbar__v runbar__v--frost">{{ currentRun!.activePalaceLaws!.length }}</span>
    </div>

    <div class="runbar__seg runbar__seg--grow" />

    <div class="runbar__seg">
      <span class="runbar__v runbar__v--dim">{{ statusLabel }}</span>
    </div>
  </header>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRunStore } from '../../features/runs/stores/runStore';
import { useGameUiStore } from '../../shared/stores/useGameUiStore';

const runStore = useRunStore();
const uiStore = useGameUiStore();
const currentRun = computed(() => runStore.currentRun);
const currentRoom = computed(() => runStore.currentRun?.currentRoom ?? null);

const statusLabel = computed(() => {
  const s = currentRun.value?.status;
  if (!s) return '—';
  const map: Record<string, string> = {
    Active: 'Exploration',
    RoomResolved: 'Salle résolue',
    Interlude: 'Repli',
    BossReached: 'Boss',
    Completed: 'Achèvement',
    Failed: 'Défaite',
    Abandoned: 'Abandonnée',
    Suspended: 'Suspendue',
  };
  return map[s] ?? s;
});
</script>

<style scoped>
.runbar {
  display: flex;
  align-items: center;
  gap: var(--space-6);
  padding: var(--space-2) var(--space-4);
  border-bottom: 1px solid var(--line-soft);
  background: oklch(0.20 0.04 272 / 0.6);
  backdrop-filter: blur(8px);
  z-index: var(--z-panel);
  flex-shrink: 0;
}

.runbar__seg {
  display: flex;
  flex-direction: column;
  gap: 1px;
}

.runbar__seg--grow {
  flex: 1;
}

.runbar__k {
  font-family: var(--font-caps);
  font-size: 0.56rem;
  letter-spacing: 0.2em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.runbar__v {
  font-family: var(--font);
  font-size: 0.82rem;
  color: var(--ink-2);
}

.runbar__v--gold { color: var(--gold); }
.runbar__v--frost { color: var(--frost); }
.runbar__v--dim { color: var(--ink-4); font-size: 0.72rem; }
.runbar__v--mono { font-family: var(--font-mono); font-size: 0.72rem; color: var(--ink-3); }

.runbar__seg--clickable {
  cursor: pointer;
  padding: var(--space-1) var(--space-2);
  border-radius: var(--radius-sm);
  transition: background 0.2s ease;
}

.runbar__seg--clickable:hover {
  background: var(--card-soft);
}
</style>
