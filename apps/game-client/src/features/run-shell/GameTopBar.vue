<script setup lang="ts">
import { computed } from 'vue';
import { useRunStore } from '../runs/stores/runStore';

const runStore = useRunStore();

const run    = computed(() => runStore.currentRun);
const room   = computed(() => run.value?.currentRoom);

const seed       = computed(() => run.value?.seed ?? '—');
// Prefer the canon room name (e.g. "Le temple de Mounkaanêt") over the abstract theme.
const roomName    = computed(() =>
  room.value?.catalogName || room.value?.theme || room.value?.roomType || '—');
const isCanonRoom = computed(() => Boolean(room.value?.catalogName));
const roomNarrative = computed(() => room.value?.catalogNarrative ?? '');
const depth      = computed(() => (room.value?.currentNodeDepth ?? 0) + 1);
const maxDepth   = computed(() => (room.value?.maxNodeDepth ?? 0) + 1);
const activeLaws = computed(() => run.value?.activePalaceLaws?.length ?? 0);
const phase      = computed(() => {
  const p = runStore.gameplayPhase;
  const map: Record<string, string> = {
    Map: 'EXPLORATION', Combat: 'COMBAT', Reward: 'RÉCOMPENSE',
    EventOutcome: 'ÉVÉNEMENT', EventChoiceResult: 'RÉSOLUTION',
    Interlude: 'INTERLUDE', RoomCleared: 'SALLE LIBÉRÉE',
    Suspended: 'SUSPENDU', Completed: 'TERMINÉ', Loading: 'CHARGEMENT',
  };
  return map[p] ?? p.toUpperCase();
});
const status = computed(() => run.value?.status ?? '—');

const depthLabel  = computed(() =>
  `${String(depth.value).padStart(2, '0')} / ${String(maxDepth.value).padStart(2, '0')}`)
const lawsLabel   = computed(() => String(activeLaws.value).padStart(2, '0'))
const statusColor = computed(() =>
  status.value === 'Failed' ? 'var(--blood)' : 'var(--frost)')
</script>

<template>
  <header class="es-runbar" style="position: relative; z-index: 8">
    <!-- Palais -->
    <div class="es-seg" style="padding-left: 30px">
      <span class="es-seg__k">Palais</span>
      <span class="es-seg__v" style="letter-spacing: 0.04em; font-weight: 600">
        L'ÉPOPÉE DES SILENCES
      </span>
    </div>

    <!-- Salle -->
    <div class="es-seg">
      <span class="es-seg__k">Salle</span>
            <span
        class="es-seg__v"
        :class="{ 'es-room--canon': isCanonRoom }"
        :title="roomNarrative"
      >
        <span v-if="isCanonRoom" class="es-room__sigil" aria-hidden="true">✦</span>{{ roomName }}
      </span>
    </div>

    <!-- Seed -->
    <div class="es-seg">
      <span class="es-seg__k">Seed</span>
      <span class="es-seg__v es-gold">{{ seed }}</span>
    </div>

    <!-- Profondeur -->
    <div class="es-seg">
      <span class="es-seg__k">Profondeur</span>
      <span class="es-seg__v">{{ depthLabel }}</span>
    </div>

    <!-- Lois -->
    <div class="es-seg">
      <span class="es-seg__k">Lois actives</span>
      <span class="es-seg__v">{{ lawsLabel }}</span>
    </div>

    <!-- spacer -->
    <div class="es-seg es-seg--grow" />

    <!-- Phase -->
    <div class="es-seg" style="align-items: flex-end">
      <span class="es-seg__k">Phase</span>
      <span class="es-seg__v es-gold">● {{ phase }}</span>
    </div>

    <!-- État -->
    <div class="es-seg" style="padding-right: 30px; border-right: none; align-items: flex-end">
      <span class="es-seg__k">État</span>
      <span class="es-seg__v" :style="{ color: statusColor }">● {{ status }}</span>
    </div>

    <!-- Slot pour actions supplémentaires (boutons Sauvegarder, Lois, etc.) -->
    <slot />
  </header>
</template>
