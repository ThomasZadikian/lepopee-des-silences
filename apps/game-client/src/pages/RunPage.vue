<script setup lang="ts">
import { onMounted, watch } from 'vue';
import { useRoute } from 'vue-router';

import GameShellLayout from '../app/layouts/GameShellLayout.vue';
import CombatRuntimePanel from '../features/combats/components/CombatRuntimePanel.vue';
import EliseOverlay from '../features/elise/EliseOverlay.vue';
import NodeDetailPanel from '../features/node-details/NodeDetailPanel.vue';
import PalaceLawPanel from '../features/palace-laws/PalaceLawPanel.vue';
import PalaceMapPlaceholder from '../features/palace-map/PalaceMapPlaceholder.vue';
import PartyPanel from '../features/party/PartyPanel.vue';
import RewardOfferPanel from '../features/rewards/components/RewardOfferPanel.vue';
import { useRunStore } from '../features/runs/stores/runStore';

const route = useRoute();
const runStore = useRunStore();

function getRouteRunId(): string | null {
  const rawRunId = route.params.runId;

  if (typeof rawRunId !== 'string') {
    return null;
  }

  const runId = rawRunId.trim();

  if (runId.length === 0 || runId === 'undefined' || runId === 'null') {
    return null;
  }

  return runId;
}

async function loadRunFromRoute() {
  const runId = getRouteRunId();

  if (!runId) {
    return;
  }

  if (runStore.currentRun?.id === runId) {
    await runStore.refreshPendingRewardIfNeeded();
    return;
  }

  await runStore.loadRun(runId);
}

onMounted(loadRunFromRoute);

watch(
  () => route.params.runId,
  async () => {
    await loadRunFromRoute();
  },
);
</script>

<template>
  <GameShellLayout>
    <template v-if="runStore.currentRun && runStore.currentRun.currentRoom">
      <section
        class="run-grid"
        :class="{ 'run-grid--combat': runStore.currentRun.activeCombatId }"
      >
        <aside class="run-grid__left">
          <PartyPanel />

          <PalaceLawPanel
            :laws="runStore.currentRun.activePalaceLaws"
          />
        </aside>

        <section class="run-grid__center panel">
            <RewardOfferPanel
              v-if="runStore.pendingRewardOffer"
              :offer="runStore.pendingRewardOffer"
              :is-loading="runStore.isLoading"
              @select-reward="runStore.selectReward"
            />

            <CombatRuntimePanel
              v-else-if="runStore.currentRun.activeCombatId"
              :run-id="runStore.currentRun.id"
              :combat-id="runStore.currentRun.activeCombatId"
              @combat-completed="runStore.handleCombatCompleted"
            />

          <template v-else>
            <PalaceMapPlaceholder
              :nodes="runStore.allNodes"
              :available-nodes="runStore.availableNodes"
              :selected-node-id="runStore.selectedNode?.id ?? null"
              @choose-node="runStore.previewNode"
            />

            <EliseOverlay
              :message="runStore.lastOutcome?.description"
            />

            <div
              v-if="runStore.lastOutcome"
              class="run-grid__outcome panel"
            >
              <p class="system-label">
                {{ runStore.lastOutcome.resolutionKind }}
              </p>

              <h3>{{ runStore.lastOutcome.title }}</h3>

              <p>{{ runStore.lastOutcome.description }}</p>
            </div>
          </template>
        </section>

        <aside
          v-if="!runStore.currentRun.activeCombatId"
          class="run-grid__right"
        >
          <NodeDetailPanel
            :node="runStore.selectedNode"
            :is-loading="runStore.isLoading"
            :has-active-combat="Boolean(runStore.currentRun.activeCombatId)"
            :has-pending-reward="Boolean(runStore.pendingRewardOffer || runStore.currentRun.pendingRewardOfferId)"
            @resolve-current-event="runStore.confirmAndResolveNode"
            @generate-next-nodes="runStore.progressRun"
          />
        </aside>
      </section>
    </template>

    <template v-else>
      <section class="run-loading panel">
        <p class="system-label">Chargement run</p>

        <p v-if="runStore.isLoading">
          Le Palais recompose la pièce...
        </p>

        <p v-else-if="runStore.error">
          {{ runStore.error }}
        </p>

        <p v-else>
          Aucune run chargée. Vérifie l’identifiant dans l’URL ou génère une nouvelle run.
        </p>
      </section>
    </template>
  </GameShellLayout>
</template>

<style scoped>
.run-grid {
  display: grid;
  grid-template-columns: 17rem minmax(36rem, 1fr) 20rem;
  gap: var(--space-4);
  height: calc(100vh - 7rem);
}

.run-grid--combat {
  grid-template-columns: 17rem minmax(42rem, 1fr);
}

.run-grid--combat .run-grid__center {
  grid-column: 2 / 3;
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

.run-grid__outcome {
  position: absolute;
  right: var(--space-6);
  bottom: var(--space-6);
  width: min(28rem, 45%);
  padding: var(--space-4);
}

.run-grid__outcome h3 {
  margin: var(--space-2) 0;
  color: var(--color-frost);
}

.run-grid__outcome p:last-child {
  color: var(--color-muted);
  line-height: 1.55;
}

.run-loading {
  margin: var(--space-4);
  padding: var(--space-6);
}
</style>