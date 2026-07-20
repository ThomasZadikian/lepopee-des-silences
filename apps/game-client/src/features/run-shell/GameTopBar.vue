<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { RouterLink } from 'vue-router';
import { useRunStore } from '../runs/stores/runStore';
import StatTooltip from '../../shared/components/StatTooltip.vue';

const runStore = useRunStore();

// Tactical mode only: fold the status bar down to a small tab by default, to give the
// map as much room as possible — expanding it overlays the map rather than pushing it
// down. Classic mode keeps today's always-expanded, in-flow behavior untouched.
const isCollapsed = ref(false);
watch(
  () => runStore.isTacticalMode,
  (tactical) => {
    if (tactical) isCollapsed.value = true;
  },
  { immediate: true },
);

const run    = computed(() => runStore.currentRun);
const room   = computed(() => run.value?.currentRoom);

const seed       = computed(() => run.value?.seed ?? '—');
// Prefer the canon room name (e.g. "Le temple de Mounkaanêt") over the abstract theme.
const roomName    = computed(() =>
  room.value?.catalogName || room.value?.theme || room.value?.roomType || '—');
const isCanonRoom = computed(() => Boolean(room.value?.catalogName));
const roomNarrative = computed(() => room.value?.catalogNarrative ?? '');
// A narrative present without a canon name only ever happens for the system's own
// "structureless Palace" fallback notice (see DeterministicRunGenerator) — every real
// canon room carries both together. Surfaced as a visible banner, not just the tooltip.
const systemNotice = computed(() =>
  !isCanonRoom.value && roomNarrative.value ? roomNarrative.value : '');
const depth      = computed(() => (room.value?.currentNodeDepth ?? 0) + 1);
const maxDepth   = computed(() => (room.value?.maxNodeDepth ?? 0) + 1);
const activeLaws = computed(() => run.value?.activePalaceLaws?.length ?? 0);
const activeLawsTooltip = computed(() => {
  const laws = run.value?.activePalaceLaws ?? [];
  if (laws.length === 0) return 'Aucune loi active.';
  return laws.map((law) => `${law.displayName || law.key} (${law.rarity})`).join(' · ');
});
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
  <button
    v-if="runStore.isTacticalMode && isCollapsed"
    type="button"
    class="es-runbar-tab"
    aria-label="Afficher la barre de statut"
    @click="isCollapsed = false"
  >
    <span aria-hidden="true">☰</span>
  </button>

  <header
    v-else
    class="es-runbar"
    :class="{ 'es-runbar--overlay': runStore.isTacticalMode }"
    style="position: relative; z-index: 8"
  >
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

    <!-- Profondeur (sans objet en mode Tactique — pas de structure en lignes à traverser) -->
    <div v-if="!runStore.isTacticalMode" class="es-seg">
      <span class="es-seg__k">Profondeur</span>
      <span class="es-seg__v">{{ depthLabel }}</span>
    </div>

    <!-- Lois -->
    <div class="es-seg">
      <span class="es-seg__k">Lois actives</span>
      <StatTooltip :text="activeLawsTooltip">
        <span class="es-seg__v">{{ lawsLabel }}</span>
      </StatTooltip>
    </div>

    <!-- spacer -->
    <div class="es-seg es-seg--grow" />

    <!-- Phase -->
    <div class="es-seg" style="align-items: flex-end">
      <span class="es-seg__k">Phase</span>
      <span class="es-seg__v es-gold">● {{ phase }}</span>
    </div>

    <!-- État -->
    <div class="es-seg" style="align-items: flex-end">
      <span class="es-seg__k">État</span>
      <span class="es-seg__v" :style="{ color: statusColor }">● {{ status }}</span>
    </div>

    <!-- Références : statuts, bestiaire, réputation, tutoriel -->
    <div class="es-seg es-runbar__refs" style="padding-right: 30px; border-right: none">
      <RouterLink class="es-runbar__ref-link" to="/statuts">Statuts</RouterLink>
      <RouterLink class="es-runbar__ref-link" to="/manifestations">Manifestations</RouterLink>
      <RouterLink v-if="run" class="es-runbar__ref-link" :to="`/reputation/${run.id}`">Réputation</RouterLink>
      <RouterLink class="es-runbar__ref-link" to="/tutoriel">Tutoriel</RouterLink>
    </div>

    <!-- Slot pour actions supplémentaires (boutons Sauvegarder, Lois, etc.) -->
    <slot />

    <button
      v-if="runStore.isTacticalMode"
      type="button"
      class="es-runbar__collapse"
      aria-label="Réduire la barre de statut"
      @click="isCollapsed = true"
    >
      ▴
    </button>
  </header>

  <div v-if="systemNotice" class="es-system-notice" role="status">
    {{ systemNotice }}
  </div>
</template>

<style scoped>
.es-runbar-tab {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 22px;
  margin: 4px;
  border: 1px solid var(--line-soft);
  border-radius: 4px;
  background: oklch(0.20 0.04 272 / 0.85);
  color: var(--ink-3);
  cursor: pointer;
  flex-shrink: 0;
}

.es-runbar-tab:hover {
  color: var(--frost);
}

.es-runbar--overlay {
  /* The inline style="position: relative; z-index: 8" on the element pre-dates this
     class and still wins on specificity for position — !important is required here to
     switch it to an overlay. z-index:8 (inline, unchanged) already clears
     .game-shell__main's z-index:1, so no override needed there. */
  position: absolute !important;
  top: 0;
  left: 0;
  right: 0;
  background: oklch(0.16 0.03 272 / 0.94);
  backdrop-filter: blur(8px);
}

.es-runbar__collapse {
  border: none;
  background: transparent;
  color: var(--ink-4);
  cursor: pointer;
  padding: 4px 14px;
  font-size: 0.85rem;
}

.es-runbar__collapse:hover {
  color: var(--frost);
}

.es-runbar__refs {
  flex-direction: row;
  align-items: center;
  gap: 14px;
}

.es-runbar__ref-link {
  font-family: var(--font-caps);
  font-size: 10px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ink-4);
  text-decoration: none;
  transition: color 0.2s;
}

.es-runbar__ref-link:hover {
  color: var(--gold);
}

.es-system-notice {
  padding: 6px 30px;
  font-family: var(--font-caps);
  font-size: 11px;
  font-style: italic;
  letter-spacing: 0.03em;
  color: var(--ink-4);
  background: rgba(0, 0, 0, 0.15);
  border-bottom: 1px solid var(--line, rgba(255, 255, 255, 0.08));
  text-align: center;
}
</style>
