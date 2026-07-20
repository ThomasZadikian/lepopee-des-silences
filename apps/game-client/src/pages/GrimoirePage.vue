<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';

import PalaceAtmosphere from '../shared/components/PalaceAtmosphere.vue';
import RuleOrnament from '../shared/components/RuleOrnament.vue';
import { usePlayerStore } from '../features/party/stores/playerStore';
import CharacterPicker from '../features/party/components/CharacterPicker.vue';
import GrimoireTab from '../features/runs/components/team-management/GrimoireTab.vue';

const router = useRouter();
const playerStore = usePlayerStore();
const props = defineProps<{ embedded?: boolean }>();

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
  <main class="grimoire-page" :class="{ 'grimoire-page--embedded': props.embedded }" data-mood="palais">
    <PalaceAtmosphere v-if="!props.embedded" />

    <div class="grimoire-page__content">
      <button v-if="!props.embedded" class="grimoire-page__back" @click="router.back()">← Retour</button>

      <span class="es-kicker">Système · sorts du personnage</span>
      <h1 class="es-h1" style="font-size: clamp(30px, 4.4vw, 52px); margin-top: 12px">Grimoire</h1>
      <RuleOrnament style="width: 150px; margin: 16px 0" />

      <CharacterPicker
        v-if="characters.length"
        v-model="selectedCharacterId"
        :characters="characters"
        style="margin-bottom: 24px"
      />

      <p v-if="playerStore.isLoading && !selectedCharacter" class="grimoire-page__status">Chargement…</p>
      <p v-else-if="!selectedCharacter" class="grimoire-page__status">Aucun personnage disponible.</p>
      <GrimoireTab v-else :character="selectedCharacter" />
    </div>
  </main>
</template>

<style scoped>
.grimoire-page {
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

.grimoire-page--embedded {
  height: auto;
  min-height: 100%;
  overflow: visible;
}

.grimoire-page__content {
  position: relative;
  z-index: 5;
  max-width: 1100px;
  margin: 0 auto;
  padding: 64px 4vw 90px;
}

.grimoire-page--embedded .grimoire-page__content {
  padding: 40px 4vw 48px;
}

.grimoire-page__back {
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
.grimoire-page__back:hover { color: var(--gold); }

.grimoire-page__status {
  margin-top: 40px;
  font-family: var(--font-caps);
  font-size: 12px;
  letter-spacing: 0.08em;
  color: var(--ink-4);
}
</style>
