<script setup lang="ts">
import { computed } from 'vue';
import { RouterLink } from 'vue-router';
import { useRunStore } from '../runs/stores/runStore';
import StatTooltip from '../../shared/components/StatTooltip.vue';

const runStore = useRunStore();

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
  </header>

  <div v-if="systemNotice" class="es-system-notice" role="status">
    {{ systemNotice }}
  </div>
</template>

<style scoped>
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
