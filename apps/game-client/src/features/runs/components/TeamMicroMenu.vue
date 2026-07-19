<script setup lang="ts">
import SigilIcon from '../../../shared/components/SigilIcon.vue';

const entries: { to: string; label: string; icon: string }[] = [
  { to: '/equipe', label: 'Équipe', icon: 'equipe' },
  { to: '/statistiques', label: 'Statistiques', icon: 'statistiques' },
  { to: '/grimoire', label: 'Grimoire', icon: 'grimoire' },
  { to: '/equipement', label: 'Équipement', icon: 'equipement' },
];
</script>

<template>
  <nav class="micro-menu" aria-label="Menu de gestion du personnage">
    <RouterLink
      v-for="entry in entries"
      :key="entry.to"
      :to="entry.to"
      class="micro-menu__btn"
      :title="entry.label"
    >
      <SigilIcon :kind="entry.icon" :size="20" />
    </RouterLink>
  </nav>
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
  border-radius: 6px;
  border: 1px solid var(--line-soft);
  background: oklch(0.24 0.015 283 / 0.5);
  color: var(--ink-3);
  text-decoration: none;
  transition: color 0.15s, border-color 0.15s, background 0.15s;
}

.micro-menu__btn:hover {
  color: var(--ink-2);
  border-color: var(--ink-3);
}

.micro-menu__btn.router-link-active {
  color: var(--gold);
  border-color: var(--gold);
  background: oklch(0.55 0.08 85 / 0.16);
}
</style>
