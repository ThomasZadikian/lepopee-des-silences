<script setup lang="ts">
import { ref } from 'vue';
import SigilIcon from '../../../shared/components/SigilIcon.vue';
import PageOverlayModal from '../../../shared/components/PageOverlayModal.vue';
import TeamPage from '../../../pages/TeamPage.vue';
import StatsPage from '../../../pages/StatsPage.vue';
import GrimoirePage from '../../../pages/GrimoirePage.vue';
import EquipmentPage from '../../../pages/EquipmentPage.vue';

type MenuKey = 'equipe' | 'statistiques' | 'grimoire' | 'equipement';

const entries: { key: MenuKey; label: string; icon: string }[] = [
  { key: 'equipe', label: 'Équipe', icon: 'equipe' },
  { key: 'statistiques', label: 'Statistiques', icon: 'statistiques' },
  { key: 'grimoire', label: 'Grimoire', icon: 'grimoire' },
  { key: 'equipement', label: 'Équipement', icon: 'equipement' },
];

// These open as a modal overlaying the game board instead of navigating away — the
// player should never have to leave the map to check their character.
const activeModal = ref<MenuKey | null>(null);
</script>

<template>
  <nav class="micro-menu" aria-label="Menu de gestion du personnage">
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
  </nav>

  <Teleport to="body">
    <PageOverlayModal v-if="activeModal" @close="activeModal = null">
      <TeamPage v-if="activeModal === 'equipe'" embedded />
      <StatsPage v-else-if="activeModal === 'statistiques'" embedded />
      <GrimoirePage v-else-if="activeModal === 'grimoire'" embedded />
      <EquipmentPage v-else-if="activeModal === 'equipement'" embedded />
    </PageOverlayModal>
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
  background: oklch(0.16 0.03 270 / 0.92);
  border-top: 1px solid var(--line-soft);
  border-right: 1px solid var(--line-soft);
  border-radius: 0 8px 0 0;
  box-shadow: 0 8px 30px oklch(0 0 0 / 0.4);
}

.micro-menu__btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  padding: 0;
  border-radius: 6px;
  border: 1px solid var(--line-soft);
  background: oklch(0.24 0.015 283 / 0.5);
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
  color: var(--gold);
  border-color: var(--gold);
  background: oklch(0.55 0.08 85 / 0.16);
}
</style>
