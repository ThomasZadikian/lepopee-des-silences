<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { usePlayerStore } from '../../party/stores/playerStore';
import TeamOverviewTab from './team-management/TeamOverviewTab.vue';
import StatManagementTab from './team-management/StatManagementTab.vue';
import SkillManagementTab from './team-management/SkillManagementTab.vue';

defineEmits<{ close: [] }>();

const playerStore = usePlayerStore();

type TabKey = 'overview' | 'stats' | 'skills';

const tabs: { key: TabKey; label: string }[] = [
  { key: 'overview', label: 'Équipe' },
  { key: 'stats', label: 'Statistiques' },
  { key: 'skills', label: 'Compétences' },
];

const activeTab = ref<TabKey>('overview');

const characters = computed(() => playerStore.profile?.characters ?? []);
const selectedCharacterId = ref<string | null>(null);

const selectedCharacter = computed(() =>
  characters.value.find((c) => c.id === selectedCharacterId.value) ?? characters.value[0] ?? null,
);

onMounted(async () => {
  if (!playerStore.profile) await playerStore.loadProfile();
  selectedCharacterId.value = characters.value[0]?.id ?? null;
});
</script>

<template>
  <div class="tmm-backdrop" @click.self="$emit('close')">
    <div class="tmm-panel">
      <button class="tmm-close" @click="$emit('close')" aria-label="Fermer">✕</button>

      <header class="tmm-header">
        <h2 class="tmm-title">Gestion de l'équipe</h2>
        <nav class="tmm-tabs">
          <button
            v-for="tab in tabs"
            :key="tab.key"
            type="button"
            class="tmm-tab"
            :class="{ 'tmm-tab--active': activeTab === tab.key }"
            @click="activeTab = tab.key"
          >
            {{ tab.label }}
          </button>
        </nav>

        <div v-if="characters.length > 1" class="tmm-character-picker">
          <button
            v-for="character in characters"
            :key="character.id"
            type="button"
            class="tmm-character-chip"
            :class="{ 'tmm-character-chip--active': selectedCharacter?.id === character.id }"
            @click="selectedCharacterId = character.id"
          >
            {{ character.displayName }}
          </button>
        </div>
      </header>

      <div class="tmm-body">
        <p v-if="playerStore.isLoading && !selectedCharacter" class="tmm-empty">Chargement…</p>
        <p v-else-if="!selectedCharacter" class="tmm-empty">Aucun personnage disponible.</p>
        <template v-else>
          <TeamOverviewTab v-if="activeTab === 'overview'" :characters="characters" />
          <StatManagementTab v-else-if="activeTab === 'stats'" :character="selectedCharacter" />
          <SkillManagementTab v-else-if="activeTab === 'skills'" :character="selectedCharacter" />
        </template>
      </div>
    </div>
  </div>
</template>

<style scoped>
.tmm-backdrop {
  position: fixed;
  inset: 0;
  z-index: var(--z-modal);
  display: flex;
  align-items: center;
  justify-content: center;
  background: oklch(0.08 0.02 60 / 0.72);
  backdrop-filter: blur(4px);
  padding: var(--space-4);
}

.tmm-panel {
  position: relative;
  width: 80vw;
  height: 80vh;
  max-width: 1400px;
  display: flex;
  flex-direction: column;
  box-shadow: var(--shadow-deep);
  background: var(--panel);
  border: 1px solid var(--line);
  border-radius: 8px;
  overflow: hidden;
}

.tmm-close {
  position: absolute;
  top: 14px;
  right: 14px;
  z-index: 1;
  width: 30px;
  height: 30px;
  border-radius: 50%;
  border: 1px solid var(--edge-gold);
  background: var(--panel);
  color: var(--ink-2);
  cursor: pointer;
  font-size: 0.8rem;
}

.tmm-close:hover {
  border-color: var(--gold);
  color: var(--gold);
}

.tmm-header {
  flex: 0 0 auto;
  padding: 20px 24px 0;
  border-bottom: 1px solid var(--line-soft);
}

.tmm-title {
  font-family: var(--font-display, var(--font));
  font-size: 22px;
  font-weight: 500;
  color: var(--gold);
  margin: 0 0 14px;
}

.tmm-tabs {
  display: flex;
  gap: 4px;
}

.tmm-tab {
  padding: 8px 16px;
  border: 1px solid transparent;
  border-bottom: none;
  border-radius: 4px 4px 0 0;
  background: transparent;
  color: var(--ink-4);
  font-family: var(--font-caps, var(--font));
  font-size: 11px;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  cursor: pointer;
  transition: color 0.15s, background 0.15s;
}

.tmm-tab:hover { color: var(--ink-2); }

.tmm-tab--active {
  color: var(--gold);
  background: var(--bg);
  border-color: var(--line-soft);
}

.tmm-character-picker {
  display: flex;
  gap: 6px;
  padding: 10px 0;
}

.tmm-character-chip {
  padding: 4px 12px;
  border-radius: 999px;
  border: 1px solid var(--line-soft);
  background: transparent;
  color: var(--ink-3);
  font-size: 12px;
  cursor: pointer;
}

.tmm-character-chip--active {
  border-color: var(--frost);
  color: var(--frost);
}

.tmm-body {
  flex: 1;
  overflow-y: auto;
  padding: 20px 24px;
}

.tmm-empty {
  font-size: 13px;
  color: var(--ink-4);
  font-style: italic;
}
</style>
