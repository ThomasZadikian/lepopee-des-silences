<script setup lang="ts">
import { computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';

import LivingWalls from '../shared/components/LivingWalls.vue';
import BesacePanel from '../shared/components/BesacePanel.vue';
import { useRunStore } from '../features/runs/stores/runStore';
import { usePlayerStore } from '../features/party/stores/playerStore';
import type { RunItemDto } from '../features/runs/types/runTypes';

const props = defineProps<{ embedded?: boolean }>();

const router = useRouter();
const runStore = useRunStore();
const playerStore = usePlayerStore();

const items = computed<RunItemDto[]>(() => runStore.currentRun?.inventoryItems ?? []);
const capacity = computed(() => runStore.currentRun?.runItemCapacity ?? null);

onMounted(() => {
  // Toujours rafraîchi à l'ouverture : un profil déjà en cache peut dater d'avant qu'un
  // objet trouvé plus tôt cette run n'ait rejoint le sac permanent (voir grantPermanentItem).
  void playerStore.loadProfile();
});
</script>

<template>
  <main class="besace-page" :class="{ 'besace-page--embedded': props.embedded }">
    <LivingWalls v-if="!props.embedded" />

    <div class="besace-page__content">
      <button v-if="!props.embedded" class="besace-page__back" @click="router.back()">← sommaire</button>
      <h1 class="besace-page__title">Besace</h1>

      <p v-if="!runStore.currentRun" class="besace-page__status">Aucune run active.</p>
      <BesacePanel v-else :items="items" :run-id="runStore.currentRun.id" :capacity="capacity" />
    </div>
  </main>
</template>

<style scoped>
.besace-page {
  position: relative;
  min-height: 100dvh;
  background: var(--void);
  color: var(--ink);
  font-family: var(--font);
}

.besace-page--embedded { min-height: 0; }

.besace-page__content {
  position: relative;
  z-index: 2;
  max-width: 640px;
  margin: 0 auto;
  padding: 48px 40px 96px;
}

.besace-page--embedded .besace-page__content {
  padding: 0;
  max-width: none;
}

.besace-page__back {
  all: unset;
  cursor: pointer;
  font-family: var(--font-mono);
  font-size: 11px;
  color: var(--ink-4);
  transition: color .3s;
}

.besace-page__back:hover { color: var(--mint-dim); }

.besace-page__title {
  margin: 18px 0 24px;
  font-family: var(--font-display);
  font-style: italic;
  font-weight: 400;
  font-size: 38px;
  color: var(--ink);
}

.besace-page--embedded .besace-page__title { margin-top: 0; }

.besace-page__status {
  font-family: var(--font-mono);
  font-size: 12px;
  letter-spacing: .08em;
  color: var(--ink-4);
}
</style>
