<script setup lang="ts">
import { computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';

import LivingWalls from '../shared/components/LivingWalls.vue';
import SessionMenu from '../features/account/components/SessionMenu.vue';
import { useRunStore } from '../features/runs/stores/runStore';

const router = useRouter();
const runStore = useRunStore();
const error = computed(() => runStore.runActionError || runStore.error);

onMounted(async () => {
  await runStore.startDeveloperSandbox();
  const runId = runStore.currentRun?.id;
  if (runId) await router.replace(`/run/${runId}`);
});
</script>

<template>
  <main class="developer-island-entry">
    <LivingWalls veins motes />
    <SessionMenu />
    <section class="developer-island-entry__panel">
      <span class="developer-island-entry__kicker">Zone hors canon</span>
      <h1>Île des développeurs</h1>
      <p v-if="!error">Initialisation d’un environnement de test propre…</p>
      <p v-else class="developer-island-entry__error" role="alert">{{ error }}</p>
    </section>
  </main>
</template>

<style scoped>
.developer-island-entry {
  min-height: 100vh;
  display: grid;
  place-items: center;
  position: relative;
  overflow: hidden;
  background: #080d12;
  color: var(--ink, #e7edf0);
}

.developer-island-entry__panel {
  position: relative;
  z-index: 1;
  width: min(560px, calc(100vw - 48px));
  padding: 48px;
  text-align: center;
  border: 1px solid rgba(117, 220, 204, .35);
  background: rgba(9, 18, 24, .9);
  box-shadow: 0 24px 80px rgba(0, 0, 0, .45);
}

.developer-island-entry__kicker {
  color: #75dccc;
  font-size: .75rem;
  letter-spacing: .22em;
  text-transform: uppercase;
}

.developer-island-entry h1 { margin: 12px 0 18px; }
.developer-island-entry__error { color: #ff9b91; }
</style>
