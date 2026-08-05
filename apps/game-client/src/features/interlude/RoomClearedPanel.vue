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
      <p class="room-cleared__label">Salle {{ currentRoomIndex + 1 }} · {{ getRoomTypeLabel(room.roomType) }}</p>

      <h2 class="room-cleared__title">Salle terminée</h2>

      <p class="room-cleared__sub">
        Le Palais se replie autour de vous.
      </p>
    </header>

    <div class="room-cleared__boss-summary">
      <p class="room-cleared__label">Gardien vaincu</p>
      <strong>{{ room.bossPreview?.name ?? '—' }}</strong>
    </div>

    <div class="room-cleared__divider" />

    <footer class="room-cleared__actions">
      <button
        class="room-cleared__cta"
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

.room-cleared__label {
  font-family: var(--font-mono);
  font-size: 10px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ink-4);
  margin: 0;
}

.room-cleared__title {
  margin: 0;
  font-family: var(--font-display);
  font-style: italic;
  font-weight: 400;
  color: var(--ink);
  font-size: clamp(1.8rem, 3.5vw, 3rem);
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
  font-family: var(--font-display);
  font-style: italic;
  color: var(--mint-dim);
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
  background: transparent;
  border: 1px solid var(--mint-dim);
  color: var(--mint-dim);
  padding: var(--space-3) var(--space-6);
  font-family: var(--font-mono);
  font-size: 11px;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  cursor: pointer;
  transition: opacity .15s;
}

.room-cleared__cta:hover:not(:disabled) { opacity: .8; }

.room-cleared__cta:disabled {
  color: var(--ink-5);
  border-color: var(--line);
  cursor: not-allowed;
}
</style>
