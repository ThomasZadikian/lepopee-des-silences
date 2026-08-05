<script setup lang="ts">
import { computed, ref } from 'vue';
import { useRunStore } from '../runs/stores/runStore';
import StatTooltip from '../../shared/components/StatTooltip.vue';
import PageOverlayModal from '../../shared/components/PageOverlayModal.vue';
import StatutsPage from '../../pages/StatutsPage.vue';
import ManifestationsPage from '../../pages/ManifestationsPage.vue';
import ReputationPage from '../../pages/ReputationPage.vue';
import TutorialPage from '../../pages/TutorialPage.vue';

const runStore = useRunStore();

// Reference pages open as a modal overlaying the map instead of navigating away —
// the player should never have to leave the game board to consult them.
type RefModal = 'statuts' | 'manifestations' | 'reputation' | 'tutoriel';
const activeRefModal = ref<RefModal | null>(null);

// Fold the status bar down to a small tab by default, to give the map as much room
// as possible — expanding it overlays the map rather than pushing it down.
const isCollapsed = ref(true);

const run    = computed(() => runStore.currentRun);
const room   = computed(() => run.value?.currentRoom);

const seed       = computed(() => run.value?.seed ?? '—');
const isCanonRoom = computed(() => Boolean(room.value?.catalogName));
const roomNarrative = computed(() => room.value?.catalogNarrative ?? '');
// A narrative present without a canon name only ever happens for the system's own
// "structureless Palace" fallback notice (see DeterministicRunGenerator) — every real
// canon room carries both together. Surfaced as a visible banner, not just the tooltip.
const systemNotice = computed(() =>
  !isCanonRoom.value && roomNarrative.value ? roomNarrative.value : '');
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

const lawsLabel   = computed(() => String(activeLaws.value).padStart(2, '0'))
const statusColor = computed(() =>
  status.value === 'Failed' ? 'var(--danger-dim)' : 'var(--mint-dim)')
</script>

<template>
  <button
    v-if="isCollapsed"
    type="button"
    class="es-runbar-tab"
    aria-label="Afficher la barre de statut"
    @click="isCollapsed = false"
  >
    <span aria-hidden="true">☰</span>
  </button>

  <header
    v-else
    class="es-runbar es-runbar--overlay"
    style="position: relative; z-index: 8"
  >
    <!-- Palais -->
    <div class="es-seg" style="padding-left: 30px">
      <span class="es-seg__k">Palais</span>
      <span class="es-seg__v" style="letter-spacing: 0.04em; font-weight: 600">
        L'ÉPOPÉE DES SILENCES
      </span>
    </div>

    <!-- Seed -->
    <div class="es-seg">
      <span class="es-seg__k">Seed</span>
      <span class="es-seg__v es-mint">{{ seed }}</span>
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
      <span class="es-seg__v es-mint">● {{ phase }}</span>
    </div>

    <!-- État -->
    <div class="es-seg" style="align-items: flex-end">
      <span class="es-seg__k">État</span>
      <span class="es-seg__v" :style="{ color: statusColor }">● {{ status }}</span>
    </div>

    <!-- Références : statuts, bestiaire, réputation, tutoriel — s'ouvrent en superposition -->
    <div class="es-seg es-runbar__refs" style="padding-right: 30px; border-right: none">
      <button type="button" class="es-runbar__ref-link" @click="activeRefModal = 'statuts'">Statuts</button>
      <button type="button" class="es-runbar__ref-link" @click="activeRefModal = 'manifestations'">Manifestations</button>
      <button v-if="run" type="button" class="es-runbar__ref-link" @click="activeRefModal = 'reputation'">Réputation</button>
      <button type="button" class="es-runbar__ref-link" @click="activeRefModal = 'tutoriel'">Tutoriel</button>
    </div>

    <!-- Slot pour actions supplémentaires (boutons Sauvegarder, Lois, etc.) -->
    <slot />

    <button
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

  <Teleport to="body">
    <PageOverlayModal v-if="activeRefModal" @close="activeRefModal = null">
      <StatutsPage v-if="activeRefModal === 'statuts'" embedded />
      <ManifestationsPage v-else-if="activeRefModal === 'manifestations'" embedded />
      <ReputationPage v-else-if="activeRefModal === 'reputation'" embedded :run-id="run?.id" />
      <TutorialPage v-else-if="activeRefModal === 'tutoriel'" embedded />
    </PageOverlayModal>
  </Teleport>
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
  background: var(--panel);
  color: var(--ink-3);
  cursor: pointer;
  flex-shrink: 0;
}

.es-runbar-tab:hover {
  color: var(--mint-dim);
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
  background: var(--panel);
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
  color: var(--mint-dim);
}

.es-runbar__refs {
  flex-direction: row;
  align-items: center;
  gap: 14px;
}

.es-runbar__ref-link {
  background: none;
  border: none;
  padding: 0;
  cursor: pointer;
  font-family: var(--font-caps);
  font-size: 10px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ink-4);
  text-decoration: none;
  transition: color 0.2s;
}

.es-runbar__ref-link:hover {
  color: var(--mint-dim);
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
