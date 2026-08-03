<script setup lang="ts">
import { computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';

import PalaceAtmosphere from '../shared/components/PalaceAtmosphere.vue';
import RuleOrnament from '../shared/components/RuleOrnament.vue';
import { usePlayerStore } from '../features/party/stores/playerStore';
import TeamOverviewTab from '../features/runs/components/team-management/TeamOverviewTab.vue';

const router = useRouter();
const playerStore = usePlayerStore();
const props = defineProps<{ embedded?: boolean }>();

const characters = computed(() => playerStore.profile?.characters ?? []);

onMounted(async () => {
  // Toujours rafraîchi à l'ouverture : un profil déjà en cache (visite précédente, autre
  // page) peut dater d'avant qu'un compagnon n'ait été recruté en cours de run — s'arrêter
  // au premier chargement laissait la liste des personnages figée sur son état d'alors.
  await playerStore.loadProfile();
});
</script>

<template>
  <main class="team-page" :class="{ 'team-page--embedded': props.embedded }" data-mood="palais">
    <PalaceAtmosphere v-if="!props.embedded" />

    <div class="team-page__content">
      <button v-if="!props.embedded" class="team-page__back" @click="router.back()">← Retour</button>

      <span class="es-kicker">Système · votre équipe</span>
      <h1 class="es-h1" style="font-size: clamp(30px, 4.4vw, 52px); margin-top: 12px">Équipe</h1>
      <RuleOrnament style="width: 150px; margin: 16px 0" />

      <p v-if="playerStore.isLoading && !characters.length" class="team-page__status">Chargement…</p>
      <p v-else-if="!characters.length" class="team-page__status">Aucun personnage disponible.</p>
      <TeamOverviewTab v-else :characters="characters" />
    </div>
  </main>
</template>

<style scoped>
.team-page {
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

.team-page--embedded {
  height: auto;
  min-height: 100%;
  overflow: visible;
}

.team-page__content {
  position: relative;
  z-index: 5;
  max-width: 1100px;
  margin: 0 auto;
  padding: 64px 4vw 90px;
}

.team-page--embedded .team-page__content {
  padding: 40px 4vw 48px;
}

.team-page__back {
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
.team-page__back:hover { color: var(--gold); }

.team-page__status {
  margin-top: 40px;
  font-family: var(--font-caps);
  font-size: 12px;
  letter-spacing: 0.08em;
  color: var(--ink-4);
}
</style>
