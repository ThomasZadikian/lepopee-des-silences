<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';

import LivingWalls from '../shared/components/LivingWalls.vue';
import SessionMenu from '../features/account/components/SessionMenu.vue';
import { useRunStore } from '../features/runs/stores/runStore';

const router = useRouter();
const runStore = useRunStore();
const error = computed(() => runStore.runActionError || runStore.error);
const isChecking = ref(true);
const confirmingReset = ref(false);

onMounted(async () => {
  await runStore.findDeveloperSandbox();
  isChecking.value = false;
});

async function openSandbox() {
  const runId = runStore.currentRun?.id;
  if (runId) await router.replace(`/run/${runId}`);
}

async function resetSandbox() {
  if (runStore.currentRun && !confirmingReset.value) {
    confirmingReset.value = true;
    return;
  }

  await runStore.resetDeveloperSandbox();
  await openSandbox();
}
</script>

<template>
  <main class="developer-island-entry">
    <LivingWalls veins motes />
    <SessionMenu />
    <section class="developer-island-entry__panel">
      <span class="developer-island-entry__kicker">Zone hors canon</span>
      <h1>Île des développeurs</h1>
      <p v-if="isChecking && !error">Recherche d’un environnement de test existant…</p>
      <p v-else-if="error" class="developer-island-entry__error" role="alert">{{ error }}</p>
      <template v-if="!isChecking && !error">
        <p v-if="runStore.currentRun">Une sandbox existante est disponible.</p>
        <p v-else>Aucune sandbox active pour ce compte.</p>
        <div class="developer-island-entry__actions">
          <button
            v-if="runStore.currentRun"
            data-testid="resume-sandbox"
            type="button"
            @click="openSandbox"
          >
            Reprendre
          </button>
          <button data-testid="reset-sandbox" type="button" @click="resetSandbox">
            {{ runStore.currentRun
              ? (confirmingReset ? 'Confirmer la réinitialisation' : 'Réinitialiser')
              : 'Créer l’environnement' }}
          </button>
          <button
            v-if="confirmingReset"
            type="button"
            class="developer-island-entry__cancel"
            @click="confirmingReset = false"
          >
            Annuler
          </button>
        </div>
      </template>
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

.developer-island-entry__actions {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 12px;
  margin-top: 28px;
}

.developer-island-entry__actions button {
  padding: 10px 18px;
  border: 1px solid rgba(117, 220, 204, .55);
  background: rgba(117, 220, 204, .12);
  color: inherit;
  cursor: pointer;
}

.developer-island-entry__actions .developer-island-entry__cancel {
  border-color: rgba(231, 237, 240, .28);
  background: transparent;
}
</style>
