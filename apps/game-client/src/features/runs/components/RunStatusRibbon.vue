<script setup lang="ts">
import { ref } from 'vue';
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
  openJournal: [];
}>();

// Folded down to a small tab by default to declutter the map view.
const isCollapsed = ref(true);
</script>

<template>
  <button
    v-if="isCollapsed"
    type="button"
    class="status-ribbon-tab"
    aria-label="Afficher la barre d'actions"
    @click="isCollapsed = false"
  >
    <span aria-hidden="true">☰</span>
  </button>

  <div v-else class="status-ribbon">
    <div class="status-ribbon__info">
      <span class="sr-chip sr-chip--mint">Salle {{ run.currentRoomNumber }}</span>
      <span class="sr-chip">{{ run.currentRoom?.roomType ?? '—' }}</span>
      <RoomClimateBadge
        :climate="run.currentRoom?.activeClimate ?? run.currentRoom?.climate ?? null"
        @open="emit('openInfluences')"
      />
      <button
        v-if="run.activePalaceLaws?.length"
        class="sr-chip sr-chip--mint sr-chip-btn"
        :title="`${run.activePalaceLaws.length} loi(s) active(s) — cliquer pour voir`"
        @click="emit('openInfluences')"
      >
        {{ run.activePalaceLaws.length }} loi{{ run.activePalaceLaws.length > 1 ? 's' : '' }}
      </button>
      <button
        v-if="run.activeModifiers?.length"
        class="sr-chip sr-chip-btn"
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
        class="es-btn es-btn--ghost"
        :disabled="!run.journalEnabled"
        :title="!run.journalEnabled ? 'Nécessite l\'objet permanent : Carnet de bord' : 'Ouvrir le Carnet de bord'"
        @click="emit('openJournal')"
      >
        Carnet de bord
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

      <button
        type="button"
        class="es-btn es-btn--ghost status-ribbon__collapse"
        aria-label="Réduire la barre d'actions"
        @click="isCollapsed = true"
      >
        ▾
      </button>
    </div>
  </div>
</template>

<style scoped>
.status-ribbon-tab {
  position: absolute;
  bottom: 0;
  left: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 22px;
  margin: 4px;
  border: 1px solid var(--line-soft);
  background: var(--panel);
  color: var(--ink-3);
  cursor: pointer;
  z-index: var(--z-panel);
}

.status-ribbon-tab:hover {
  color: var(--mint-dim);
}

.status-ribbon__collapse {
  padding: 3px 8px !important;
}

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
  background: var(--panel);
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

.sr-chip {
  display: inline-flex;
  align-items: center;
  font-family: var(--font-mono);
  font-size: 10px;
  letter-spacing: .06em;
  text-transform: uppercase;
  padding: 2px 7px;
  border: 1px solid var(--ink-5);
  color: var(--ink-4);
}

.sr-chip--mint { border-color: var(--mint-dim); color: var(--mint-dim); }

.sr-chip-btn {
  cursor: pointer;
  background: transparent;
  font: inherit;
  transition: opacity .15s;
}

.sr-chip-btn:hover { opacity: .8; }
</style>
