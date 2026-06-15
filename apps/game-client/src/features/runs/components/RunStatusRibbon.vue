<script setup lang="ts">
import type { RunDto } from '../../runs/types/runTypes';

defineProps<{
  run: RunDto;
  isSafePoint: boolean;
}>();

const emit = defineEmits<{
  saveAndExit: [];
  abandon: [];
  exitMidRoom: [];
  openBesace: [];
}>();
</script>

<template>
  <div class="status-ribbon">
    <div class="status-ribbon__info">
      <span class="es-chip es-chip--gold">Salle {{ run.currentRoomIndex + 1 }}</span>
      <span class="es-chip">{{ run.currentRoom?.roomType ?? '—' }}</span>
    </div>

    <div class="status-ribbon__actions">
      <button class="es-btn es-btn--ghost" @click="emit('openBesace')">
        La Besace
      </button>

      <button
        v-if="!isSafePoint"
        class="es-btn es-btn--ghost"
        @click="emit('exitMidRoom')"
      >
        Quitter la salle
      </button>

      <button
        class="es-btn es-btn--ghost"
        :disabled="!isSafePoint"
        @click="emit('saveAndExit')"
      >
        Sauvegarder
      </button>

      <button
        class="es-btn es-btn--ghost"
        :disabled="!isSafePoint"
        @click="emit('abandon')"
      >
        Abandonner
      </button>
    </div>
  </div>
</template>

<style scoped>
.status-ribbon {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-4);
  padding: var(--space-2) var(--space-4);
  background: oklch(0.20 0.04 272 / 0.7);
  backdrop-filter: blur(8px);
  border-top: 1px solid var(--line-soft);
  z-index: var(--z-panel);
}

.status-ribbon__info {
  display: flex;
  gap: var(--space-2);
  align-items: center;
}

.status-ribbon__actions {
  display: flex;
  gap: var(--space-2);
  align-items: center;
}
</style>
