<script setup lang="ts">
import type { RoomDto } from '../runs/types/runTypes';

defineProps<{
  room: RoomDto;
  currentRoomIndex: number;
  isLoading: boolean;
}>();

defineEmits<{
  enterInterlude: [];
}>();

function getRoomTypeLabel(roomType: string): string {
  const labels: Record<string, string> = {
    Threshold:  'Seuil',
    Combat:     'Salle de combat',
    Elite:      'Salle d\'élite',
    Rest:       'Havre',
    Merchant:   'Bazar du Palais',
    Mystery:    'Salle mystère',
  };
  return labels[roomType] ?? roomType;
}
</script>

<template>
  <section class="room-cleared">
    <header class="room-cleared__header">
      <p class="system-label">Salle {{ currentRoomIndex + 1 }} · {{ getRoomTypeLabel(room.roomType) }}</p>

      <h2 class="room-cleared__title">Salle terminée</h2>

      <p class="room-cleared__sub">
        Le Palais se replie autour de vous.
      </p>
    </header>

    <div class="room-cleared__boss-summary">
      <p class="system-label">Gardien vaincu</p>
      <strong>{{ room.bossPreview?.name ?? '—' }}</strong>
    </div>

    <div class="room-cleared__divider" />

    <footer class="room-cleared__actions">
      <button
        class="ghost-button room-cleared__cta"
        :disabled="isLoading"
        @click="$emit('enterInterlude')"
      >
        <span v-if="isLoading">Repli en cours…</span>
        <span v-else>Entrer dans le Repli du Palais</span>
      </button>
    </footer>
  </section>
</template>

<style scoped>
.room-cleared {
  display: grid;
  gap: var(--space-6);
  height: 100%;
  align-content: center;
  max-width: 44rem;
}

.room-cleared__header {
  display: grid;
  gap: var(--space-2);
}

.room-cleared__title {
  margin: 0;
  color: var(--frost);
  font-size: clamp(1.8rem, 3.5vw, 3rem);
  text-transform: uppercase;
  letter-spacing: 0.06em;
}

.room-cleared__sub {
  color: var(--ink-3);
  font-size: 1.05rem;
  line-height: 1.6;
  margin: 0;
}

.room-cleared__boss-summary {
  border-top: 1px solid var(--line);
  padding-top: var(--space-4);
}

.room-cleared__boss-summary strong {
  display: block;
  margin-top: var(--space-1);
  color: var(--gold);
  font-size: 1.1rem;
}

.room-cleared__divider {
  height: 1px;
  background: var(--line);
}

.room-cleared__actions {
  display: flex;
  gap: var(--space-3);
}

.room-cleared__cta {
  border-color: var(--frost);
  color: var(--frost);
  padding: var(--space-3) var(--space-6);
  font-size: 1rem;
}

.room-cleared__cta:hover:not(:disabled) {
  background: color-mix(in oklch, var(--frost), transparent 88%);
}

.room-cleared__cta:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}
</style>
