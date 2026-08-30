<script setup lang="ts">
import { computed, defineAsyncComponent, ref } from 'vue';
import SigilIcon from '../../../shared/components/SigilIcon.vue';
import PageOverlayModal from '../../../shared/components/PageOverlayModal.vue';
import TeamHubPage from '../../../pages/TeamHubPage.vue';
import { useRunStore } from '../stores/runStore';
import { useGameUiStore } from '../../../shared/stores/useGameUiStore';
import { isDevToolsEnabled } from '../../devtools/utils/isDevToolsEnabled';

type MenuKey = 'equipe' | 'statistiques' | 'grimoire' | 'equipement' | 'besace';

const entries: { key: MenuKey; label: string; icon: string }[] = [
  { key: 'equipe', label: 'Équipe', icon: 'equipe' },
  { key: 'statistiques', label: 'Statistiques', icon: 'statistiques' },
  { key: 'grimoire', label: 'Grimoire', icon: 'grimoire' },
  { key: 'equipement', label: 'Équipement', icon: 'equipement' },
  { key: 'besace', label: 'La Besace', icon: 'besace' },
];

// These open as a modal overlaying the game board instead of navigating away — the
// player should never have to leave the map to check their character.
const activeModal = ref<MenuKey | null>(null);

// DevTools used to float on its own in the opposite corner, overlapping the combat scene.
// Folded into the same menu (development-only, requires VITE_GAME_CLIENT_DEVTOOLS_ENABLED=true).
const runStore = useRunStore();
const devToolsEnabled = isDevToolsEnabled();
const DevToolsPanel = devToolsEnabled
  ? defineAsyncComponent(() => import('../../devtools/components/DevToolsPanel.vue'))
  : null;
const showDevTools = ref(false);

// "Équipe" above opens the persistent character sheet (TeamHubPage — base stats, loadout).
// This opens the run's own live vitals (current HP/mana/guard, active modifiers —
// see PartyDrawer.vue, rendered by RunPage.vue) — the window players were missing during
// exploration. It already existed behind RunStatusRibbon's collapsed tab; this just puts it
// somewhere always visible. PartyDrawer is deliberately combat-only-excluded (portraits cover
// that during combat), so the entry hides there rather than opening a dead drawer.
const uiStore = useGameUiStore();
const isExploring = computed(() => runStore.gameplayPhase !== 'Combat');
</script>

<template>
  <nav class="micro-menu" aria-label="Menu de gestion du personnage">
    <button
      v-if="isExploring"
      type="button"
      class="micro-menu__btn"
      :class="{ 'micro-menu__btn--active': uiStore.activeDrawer === 'party' }"
      title="État de l'équipe"
      @click="uiStore.toggleParty()"
    >
      <SigilIcon kind="vitalite" :size="20" />
    </button>

    <button
      v-for="entry in entries"
      :key="entry.key"
      type="button"
      class="micro-menu__btn"
      :class="{ 'micro-menu__btn--active': activeModal === entry.key }"
      :title="entry.label"
      @click="activeModal = entry.key"
    >
      <SigilIcon :kind="entry.icon" :size="20" />
    </button>

    <button
      v-if="devToolsEnabled"
      type="button"
      class="micro-menu__btn micro-menu__btn--devtools"
      :class="{ 'micro-menu__btn--active': showDevTools }"
      :aria-expanded="showDevTools"
      title="DevTools"
      @click="showDevTools = !showDevTools"
    >
      <SigilIcon kind="statistiques" :size="20" />
    </button>
  </nav>

  <Teleport to="body">
    <PageOverlayModal v-if="activeModal" @close="activeModal = null">
      <TeamHubPage embedded :initial-tab="activeModal" />
    </PageOverlayModal>

    <component
      :is="DevToolsPanel"
      v-if="devToolsEnabled && showDevTools && DevToolsPanel && runStore.currentRun"
      :run-id="runStore.currentRun.id"
      @close="showDevTools = false"
    />
  </Teleport>
</template>

<style scoped>
.micro-menu {
  position: fixed;
  bottom: 0;
  left: 0;
  z-index: 9000;
  display: flex;
  gap: 4px;
  padding: 10px;
  background: var(--panel);
  border-top: 1px solid var(--line-soft);
  border-right: 1px solid var(--line-soft);
}

.micro-menu__btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  padding: 0;
  border: 1px solid var(--line-soft);
  background: var(--panel-2);
  color: var(--ink-3);
  cursor: pointer;
  text-decoration: none;
  transition: color 0.15s, border-color 0.15s, background 0.15s;
}

.micro-menu__btn:hover {
  color: var(--ink-2);
  border-color: var(--ink-3);
}

.micro-menu__btn--active {
  color: var(--mint-dim);
  border-color: var(--mint-dim);
  background: var(--panel-2);
}

/* Distinct from the character-management entries (gold) — a clear "this is a dev-only
   control" tell, and a small gap sets it apart from the regular menu group. */
.micro-menu__btn--devtools {
  margin-left: 6px;
  border-color: oklch(0.62 0.19 35 / 0.55);
  color: oklch(0.68 0.19 35);
}

.micro-menu__btn--devtools:hover {
  color: oklch(0.75 0.2 35);
  border-color: oklch(0.75 0.2 35);
}

.micro-menu__btn--devtools.micro-menu__btn--active {
  color: oklch(0.75 0.2 35);
  border-color: oklch(0.75 0.2 35);
  background: oklch(0.55 0.19 35 / 0.2);
}
</style>
