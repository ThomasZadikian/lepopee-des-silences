<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { useRouter } from 'vue-router';

import LivingWalls from '../shared/components/LivingWalls.vue';
import CharacterPicker from '../features/party/components/CharacterPicker.vue';
import TeamOverviewTab from '../features/runs/components/team-management/TeamOverviewTab.vue';
import CharacterStatsTab from '../features/runs/components/team-management/CharacterStatsTab.vue';
import GrimoireTab from '../features/runs/components/team-management/GrimoireTab.vue';
import ItemManagementTab from '../features/runs/components/team-management/ItemManagementTab.vue';
import BesacePanel from '../shared/components/BesacePanel.vue';
import { usePlayerStore } from '../features/party/stores/playerStore';
import { useRunStore } from '../features/runs/stores/runStore';
import type { RunItemDto } from '../features/runs/types/runTypes';

type TabKey = 'equipe' | 'statistiques' | 'grimoire' | 'equipement' | 'besace';

const props = defineProps<{
  embedded?: boolean;
  initialTab?: TabKey;
}>();

const tabs: { key: TabKey; label: string }[] = [
  { key: 'equipe', label: 'Équipe' },
  { key: 'statistiques', label: 'Statistiques' },
  { key: 'grimoire', label: 'Grimoire' },
  { key: 'equipement', label: 'Équipement' },
  { key: 'besace', label: 'Besace' },
];

const router = useRouter();
const playerStore = usePlayerStore();
const runStore = useRunStore();

const activeTab = ref<TabKey>(props.initialTab ?? 'equipe');
watch(() => props.initialTab, (tab) => { if (tab) activeTab.value = tab; });

const characters = computed(() => playerStore.profile?.characters ?? []);
const needsCharacter = computed(() => activeTab.value === 'statistiques' || activeTab.value === 'grimoire' || activeTab.value === 'equipement');

const selectedCharacterId = ref<string | null>(null);
const selectedCharacter = computed(() =>
  characters.value.find((c) => c.id === selectedCharacterId.value) ?? characters.value[0] ?? null,
);

const runItems = computed<RunItemDto[]>(() => runStore.currentRun?.inventoryItems ?? []);
const runCapacity = computed(() => runStore.currentRun?.runItemCapacity ?? null);

onMounted(async () => {
  // Toujours rafraîchi à l'ouverture : un profil déjà en cache (visite précédente, autre
  // page) peut dater d'avant qu'un compagnon n'ait été recruté en cours de run — s'arrêter
  // au premier chargement laissait la liste des personnages figée sur son état d'alors.
  await playerStore.loadProfile();
  selectedCharacterId.value = characters.value[0]?.id ?? null;
});
</script>

<template>
  <main class="team-hub" :class="{ 'team-hub--embedded': props.embedded }">
    <LivingWalls v-if="!props.embedded" />

    <div class="team-hub__content">
      <button v-if="!props.embedded" class="team-hub__back" @click="router.back()">← sommaire</button>

      <h1 class="team-hub__title">Équipe</h1>

      <nav class="team-hub__tabs" aria-label="Sections de l'équipe">
        <button
          v-for="tab in tabs"
          :key="tab.key"
          type="button"
          class="team-hub__tab"
          :class="{ 'team-hub__tab--active': activeTab === tab.key }"
          @click="activeTab = tab.key"
        >
          {{ tab.label }}
        </button>
      </nav>

      <CharacterPicker
        v-if="needsCharacter && characters.length"
        v-model="selectedCharacterId"
        :characters="characters"
        class="team-hub__picker"
      />

      <p v-if="playerStore.isLoading && !characters.length && activeTab !== 'besace'" class="team-hub__status">Chargement…</p>
      <p v-else-if="needsCharacter && !selectedCharacter" class="team-hub__status">Aucun personnage disponible.</p>
      <p v-else-if="activeTab === 'equipe' && !characters.length" class="team-hub__status">Aucun personnage disponible.</p>

      <TeamOverviewTab v-else-if="activeTab === 'equipe'" :characters="characters" />
      <CharacterStatsTab v-else-if="activeTab === 'statistiques'" :character="selectedCharacter!" />
      <GrimoireTab v-else-if="activeTab === 'grimoire'" :character="selectedCharacter!" />
      <ItemManagementTab v-else-if="activeTab === 'equipement'" :character="selectedCharacter!" />

      <template v-else-if="activeTab === 'besace'">
        <p v-if="!runStore.currentRun" class="team-hub__status">Aucune run active.</p>
        <BesacePanel v-else :items="runItems" :run-id="runStore.currentRun.id" :capacity="runCapacity" />
      </template>
    </div>
  </main>
</template>

<style scoped>
.team-hub {
  position: relative;
  min-height: 100dvh;
  background: var(--void);
  color: var(--ink);
  font-family: var(--font);
}

.team-hub--embedded { min-height: 0; }

.team-hub__content {
  position: relative;
  z-index: 2;
  max-width: 900px;
  margin: 0 auto;
  padding: 48px 40px 96px;
}

.team-hub--embedded .team-hub__content {
  padding: 0;
  max-width: none;
}

.team-hub__back {
  all: unset;
  cursor: pointer;
  display: block;
  margin-bottom: 24px;
  font-family: var(--font-mono);
  font-size: 11px;
  letter-spacing: 0.08em;
  color: var(--ink-4);
  transition: color .3s;
}
.team-hub__back:hover { color: var(--mint-dim); }

.team-hub__title {
  margin: 0 0 20px;
  font-family: var(--font-display);
  font-style: italic;
  font-weight: 400;
  font-size: 38px;
  color: var(--ink);
}

.team-hub--embedded .team-hub__title { margin-top: 0; }

.team-hub__tabs {
  display: flex;
  gap: 4px;
  margin-bottom: 24px;
  border-bottom: 1px solid var(--line-soft);
}

.team-hub__tab {
  all: unset;
  cursor: pointer;
  padding: 8px 14px;
  font-family: var(--font-mono);
  font-size: 11px;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--ink-4);
  border-bottom: 2px solid transparent;
  transition: color .15s, border-color .15s;
}

.team-hub__tab:hover { color: var(--ink-2); }

.team-hub__tab--active {
  color: var(--mint-dim);
  border-color: var(--mint-dim);
}

.team-hub__picker {
  margin-bottom: 20px;
}

.team-hub__status {
  margin-top: 24px;
  font-family: var(--font-mono);
  font-size: 12px;
  letter-spacing: .08em;
  color: var(--ink-4);
}
</style>
