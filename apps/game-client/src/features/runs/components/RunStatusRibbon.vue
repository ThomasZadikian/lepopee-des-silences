<script setup lang="ts">
import RoomClimateBadge from '../../room-climate/RoomClimateBadge.vue';
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
  openInfluences: [];
}>();
</script>

<template>
  <div class="status-ribbon">
    <div class="status-ribbon__info">
      <span class="es-chip es-chip--gold">Salle {{ run.currentRoomNumber }}</span>
      <span class="es-chip">{{ run.currentRoom?.roomType ?? '—' }}</span>
      <RoomClimateBadge
        :climate="run.currentRoom?.activeClimate ?? run.currentRoom?.climate ?? null"
        @open="emit('openInfluences')"
      />
      <button
        v-if="run.activePalaceLaws?.length"
        class="es-chip es-chip--frost sr-chip-btn"
        :title="`${run.activePalaceLaws.length} loi(s) active(s) — cliquer pour voir`"
        @click="emit('openInfluences')"
      >
        {{ run.activePalaceLaws.length }} loi{{ run.activePalaceLaws.length > 1 ? 's' : '' }}
      </button>
      <button
        v-if="run.activeCurses?.length"
        class="es-chip es-chip--blood sr-chip-btn"
        :title="`${run.activeCurses.length} malédiction(s) active(s) — cliquer pour voir`"
        @click="emit('openInfluences')"
      >
        {{ run.activeCurses.length }} malédiction{{ run.activeCurses.length > 1 ? 's' : '' }}
      </button>
      <button
        v-if="run.activeModifiers?.length"
        class="es-chip sr-chip-btn"
        :title="`${run.activeModifiers.length} modificateur(s) actif(s) — cliquer pour voir`"
        @click="emit('openInfluences')"
      >
        {{ run.activeModifiers.length }} mod.
      </button>
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
  margin-left:45%; 
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

.sr-chip-btn {
  cursor: pointer;
  border: none;
  background: inherit;
  font: inherit;
  transition: opacity .15s;
}

.sr-chip-btn:hover { opacity: .8; }
</style>
