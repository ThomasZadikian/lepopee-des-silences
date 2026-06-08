<template>
  <header class="topbar panel">
    <section class="topbar__brand">
      <span class="system-label">PALACE</span>
      <strong>{{ currentRoom?.theme ?? '—' }}</strong>
    </section>

    <section class="topbar__item">
      <span class="system-label">SEED</span>
      <span class="system-value">{{ currentRun?.seed ?? '—' }}</span>
    </section>

    <section class="topbar__item">
      <span class="system-label">SALLE</span>
      <span class="system-value">{{ currentRun != null ? currentRun.currentRoomIndex + 1 : '—' }}</span>
    </section>

    <section class="topbar__item">
      <span class="system-label">ACTIVE LAWS</span>
      <span class="system-value">{{ currentRun?.activePalaceLaws?.length ?? 0 }}</span>
    </section>

    <section class="topbar__item topbar__score">
      <span class="system-label">SCORE</span>
      <span class="system-value">—</span>
    </section>

    <section class="topbar__state">
      <span class="system-label">STATUS</span>
      <span class="topbar__active">{{ currentRun?.status ?? '—' }}</span>
    </section>
  </header>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRunStore } from '../../features/runs/stores/runStore';

const runStore = useRunStore();
const currentRun = computed(() => runStore.currentRun);
const currentRoom = computed(() => runStore.currentRun?.currentRoom ?? null);
</script>

<style scoped>
.topbar {
  display: grid;
  grid-template-columns: 1.6fr repeat(4, 1fr) auto;
  min-height: 4.5rem;
  margin: var(--space-4);
}

.topbar__brand,
.topbar__item,
.topbar__state {
  display: flex;
  flex-direction: column;
  justify-content: center;
  gap: var(--space-1);
  padding: 0 var(--space-4);
  border-right: 1px solid color-mix(in oklch, var(--color-line), transparent 45%);
}

.topbar__brand strong {
  font-size: 0.95rem;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.topbar__score {
  text-align: right;
}

.topbar__active {
  color: var(--color-frost);
  font-family: var(--font-mono);
  text-transform: uppercase;
}
</style>