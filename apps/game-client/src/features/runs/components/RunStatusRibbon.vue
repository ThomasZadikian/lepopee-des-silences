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
  openParty: [];
}>();
</script>

<template>
  <div class="status-ribbon">
    <div class="status-ribbon__info">
      <span class="es-chip es-chip--gold">Salle {{ run.currentRoomNumber }}</span>
      <span class="es-chip">{{ run.currentRoom?.roomType ?? '—' }}</span>
      <span
        v-if="run.activePalaceLaws?.length"
        class="es-chip es-chip--frost"
        :title="`${run.activePalaceLaws.length} loi(s) active(s)`"
      >
        {{ run.activePalaceLaws.length }} loi{{ run.activePalaceLaws.length > 1 ? 's' : '' }}
      </span>
      <span
        v-if="run.activeModifiers?.length"
        class="es-chip"
        :title="`${run.activeModifiers.length} modificateur(s) actif(s)`"
      >
        {{ run.activeModifiers.length }} mod.
      </span>
    </div>

    <div class="status-ribbon__actions">
      <button class="es-btn es-btn--ghost" @click="emit('openParty')">
        Équipe
      </button>

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
  flex-wrap: nowrap;
  gap: var(--space-3);
  padding: var(--space-1) var(--space-3);
  background: oklch(0.20 0.04 272 / 0.85);
  backdrop-filter: blur(8px);
  border-top: 1px solid var(--line-soft);
  z-index: var(--z-panel);
  min-height: 0;
}

.status-ribbon__info {
  display: flex;
  gap: var(--space-2);
  align-items: center;
  flex-shrink: 0;
  flex-wrap: nowrap;
}

.status-ribbon__actions {
  display: flex;
  gap: var(--space-1);
  align-items: center;
  flex-shrink: 0;
  flex-wrap: nowrap;
}

.status-ribbon__actions .es-btn {
  padding: 3px 10px;
  font-size: 11px;
  height: auto;
}
</style>
