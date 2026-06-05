<script setup lang="ts">
import { onMounted } from 'vue';
import { useRoute } from 'vue-router';

import GameShellLayout from '../app/layouts/GameShellLayout.vue';
import EliseOverlay from '../features/elise/EliseOverlay.vue';
import NodeDetailPanel from '../features/node-details/NodeDetailPanel.vue';
import PalaceLawPanel from '../features/palace-laws/PalaceLawPanel.vue';
import PalaceMapPlaceholder from '../features/palace-map/PalaceMapPlaceholder.vue';
import PartyPanel from '../features/party/PartyPanel.vue';
import { useRunStore } from '../features/runs/stores/runStore';

const route = useRoute();
const runStore = useRunStore();

onMounted(async () => {
  const runId = route.params.runId;

  if (typeof runId !== 'string' || runId.length === 0 || runId === 'undefined') {
    return;
  }

  if (runStore.currentRun?.id === runId) {
    return;
  }

  await runStore.loadRun(runId);
});
</script>

<template>
  <GameShellLayout>
    <template v-if="runStore.currentRun && runStore.currentRun.currentRoom">
      <section class="run-grid">
        <aside class="run-grid__left">
          <PartyPanel />
          <PalaceLawPanel :laws="runStore.currentRun.activePalaceLaws" />
        </aside>

        <section class="run-grid__center panel">
          <PalaceMapPlaceholder
            :nodes="runStore.allNodes"
            :available-nodes="runStore.availableNodes"
            :selected-node-id="runStore.selectedNode?.id ?? null"
            @choose-node="runStore.chooseNode"
          />

          <EliseOverlay
            :message="runStore.lastOutcome?.description"
          />

          <div v-if="runStore.lastOutcome" class="run-grid__outcome panel">
            <p class="system-label">
              {{ runStore.lastOutcome.resolutionKind }}
            </p>
            <h3>{{ runStore.lastOutcome.title }}</h3>
            <p>{{ runStore.lastOutcome.description }}</p>
          </div>

          <div v-if="runStore.activeCombat" class="run-grid__combat panel">
            <p class="system-label">Combat démarré</p>
            <h3>
              {{ runStore.activeCombat.state }} · Tour {{ runStore.activeCombat.round }}
            </h3>
            <p>
              Combat ID :
              <span class="system-value">{{ runStore.activeCombat.id }}</span>
            </p>
          </div>
        </section>

        <aside class="run-grid__right">
          <NodeDetailPanel
            :node="runStore.selectedNode"
            :is-loading="runStore.isLoading"
            @resolve-current-event="runStore.resolveCurrentEvent"
            @generate-next-nodes="runStore.generateNextNodes"
          />
        </aside>
      </section>
    </template>

    <template v-else>
      <section class="run-loading panel">
        <p class="system-label">Chargement run</p>
        <p v-if="runStore.error">{{ runStore.error }}</p>
        <p v-else>Le Palais recompose la pièce...</p>
      </section>
    </template>
  </GameShellLayout>
</template>

<style scoped>
.run-grid {
  display: grid;
  grid-template-columns: 18rem minmax(36rem, 1fr) 20rem;
  gap: var(--space-4);
  height: calc(100vh - 7rem);
}

.run-grid__left,
.run-grid__right {
  display: grid;
  gap: var(--space-4);
  align-content: start;
}

.run-grid__center {
  position: relative;
  overflow: hidden;
  padding: var(--space-6);
}

.run-grid__outcome,
.run-grid__combat {
  position: absolute;
  right: var(--space-6);
  bottom: var(--space-6);
  width: min(28rem, 45%);
  padding: var(--space-4);
}

.run-grid__combat {
  bottom: 11rem;
  border-color: color-mix(in oklch, var(--color-blood), transparent 30%);
}

.run-loading {
  margin: var(--space-4);
  padding: var(--space-6);
}
</style>