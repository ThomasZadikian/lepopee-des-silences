<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';

import PalaceAtmosphere from '../shared/components/PalaceAtmosphere.vue';
import RuleOrnament from '../shared/components/RuleOrnament.vue';
import { usePlayerStore } from '../features/party/stores/playerStore';
import CharacterPicker from '../features/party/components/CharacterPicker.vue';
import ItemManagementTab from '../features/runs/components/team-management/ItemManagementTab.vue';

const router = useRouter();
const playerStore = usePlayerStore();

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
  <main class="equipment-page" data-mood="palais">
    <PalaceAtmosphere />

    <div class="equipment-page__content">
      <button class="equipment-page__back" @click="router.back()">← Retour</button>

      <span class="es-kicker">Système · équipement du personnage</span>
      <h1 class="es-h1" style="font-size: clamp(30px, 4.4vw, 52px); margin-top: 12px">Équipement</h1>
      <RuleOrnament style="width: 150px; margin: 16px 0" />

      <CharacterPicker
        v-if="characters.length"
        v-model="selectedCharacterId"
        :characters="characters"
        style="margin-bottom: 24px"
      />

      <p v-if="playerStore.isLoading && !selectedCharacter" class="equipment-page__status">Chargement…</p>
      <p v-else-if="!selectedCharacter" class="equipment-page__status">Aucun personnage disponible.</p>
      <ItemManagementTab v-else :character="selectedCharacter" />
    </div>
  </main>
</template>

<style scoped>
.equipment-page {
  position: relative;
  height: 100dvh;
  overflow-y: auto;
  overflow-x: hidden;
  background:
    radial-gradient(70% 52% at 20% 12%, var(--wash-frost), transparent 60%),
    radial-gradient(64% 56% at 86% 80%, var(--wash-blood), transparent 58%),
    radial-gradient(58% 50% at 60% 26%, var(--wash-sap), transparent 60%),
    radial-gradient(56% 50% at 12% 92%, var(--wash-gold), transparent 60%),
    radial-gradient(150% 130% at 50% -10%, var(--bg) 0%, var(--bg-2) 48%, var(--void) 100%);
  color: var(--ink);
  font-family: var(--font);
}

.equipment-page__content {
  position: relative;
  z-index: 5;
  max-width: 1100px;
  margin: 0 auto;
  padding: 64px 4vw 90px;
}

.equipment-page__back {
  all: unset;
  cursor: pointer;
  display: block;
  margin-bottom: 24px;
  font-family: var(--font-caps);
  font-size: 11px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ink-4);
  transition: color 0.2s;
}
.equipment-page__back:hover { color: var(--gold); }

.equipment-page__status {
  margin-top: 40px;
  font-family: var(--font-caps);
  font-size: 12px;
  letter-spacing: 0.08em;
  color: var(--ink-4);
}
</style>
