<script setup lang="ts">
import { computed, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';

import GameShellLayout from '../app/layouts/GameShellLayout.vue';
import CombatScene from '../features/combat/components/CombatScene.vue';
import EliseOverlay from '../features/elise/EliseOverlay.vue';
import EventChoiceResultPanel from '../features/events/components/EventChoiceResultPanel.vue';
import EventOutcomePanel from '../features/events/components/EventOutcomePanel.vue';
import InterludePanel from '../features/interlude/InterludePanel.vue';
import RoomClearedPanel from '../features/interlude/RoomClearedPanel.vue';
import NodeDetailPanel from '../features/node-details/NodeDetailPanel.vue';
import PalaceLawPanel from '../features/palace-laws/PalaceLawPanel.vue';
import PalaceMapPlaceholder from '../features/palace-map/PalaceMapPlaceholder.vue';
import PartyPanel from '../features/party/PartyPanel.vue';
import RewardOfferPanel from '../features/rewards/components/RewardOfferPanel.vue';
import RunDangerActions from '../features/runs/components/RunDangerActions.vue';
import RunHudPanel from '../features/runs/components/RunHudPanel.vue';
import { useRunStore } from '../features/runs/stores/runStore';

const route = useRoute();
const router = useRouter();
const runStore = useRunStore();

const isSafePoint = computed(() =>
  runStore.currentRun?.status === 'RoomResolved' ||
  runStore.currentRun?.status === 'Interlude',
);

async function handleSaveAndExit() {
  const ok = await runStore.saveAndExitCurrentRun();
  if (ok) await router.replace('/');
}

async function handleAbandon() {
  const ok = await runStore.abandonCurrentRun();
  if (ok) await router.replace('/');
}

async function handleExitMidRoom() {
  const ok = await runStore.exitMidRoom();
  if (ok) await router.replace('/');
}

async function startNewRun() {
  await runStore.startRun();
  const runId = runStore.currentRun?.id;
  if (runId) await router.replace(`/run/${runId}`);
}

const shouldDisplayRightPanel = computed(() => runStore.gameplayPhase === 'Map');
const shouldUseWideCenterPanel = computed(() => runStore.gameplayPhase !== 'Map');

function getRouteRunId(): string | null {
  const rawRunId = route.params.runId;
  if (typeof rawRunId !== 'string') return null;
  const runId = rawRunId.trim();
  if (runId.length === 0 || runId === 'undefined' || runId === 'null') return null;
  return runId;
}

async function loadRunFromRoute() {
  const runId = getRouteRunId();
  if (!runId) return;

  if (runStore.currentRun?.id === runId) {
    await runStore.refreshPendingRewardIfNeeded();
    return;
  }

  await runStore.loadRun(runId);
}

onMounted(loadRunFromRoute);

watch(
  () => route.params.runId,
  async () => { await loadRunFromRoute(); },
);
</script>

<template>
  <GameShellLayout>
    <template v-if="runStore.currentRun && runStore.currentRun.currentRoom">
      <section
        class="run-grid"
        :class="{ 'run-grid--wide-center': shouldUseWideCenterPanel }"
      >
        <aside class="run-grid__left">
          <RunHudPanel :run="runStore.currentRun" />
          <PartyPanel />
          <PalaceLawPanel :laws="runStore.currentRun.activePalaceLaws" />
          <RunDangerActions
            :is-safe-point="isSafePoint"
            :is-saving-and-exiting="runStore.isSavingAndExiting"
            :is-abandoning-run="runStore.isAbandoningRun"
            :is-exiting-mid-room="runStore.isExitingMidRoom"
            :error="runStore.runActionError"
            @save-and-exit="handleSaveAndExit"
            @abandon="handleAbandon"
            @exit-mid-room="handleExitMidRoom"
          />
        </aside>

        <section class="run-grid__center panel">

          <!-- 1. Reward (prioritaire) -->
          <RewardOfferPanel
            v-if="runStore.gameplayPhase === 'Reward' && runStore.pendingRewardOffer"
            :offer="runStore.pendingRewardOffer"
            :is-loading="runStore.isLoading"
            @select-reward="runStore.selectReward"
          />

          <!-- 2. Combat (prioritaire) -->
          <CombatScene
            v-else-if="runStore.gameplayPhase === 'Combat' && runStore.currentRun.activeCombatId"
            :run-id="runStore.currentRun.id"
            :combat-id="runStore.currentRun.activeCombatId"
            @combat-completed="runStore.handleCombatCompleted"
          />

          <!-- 3. Interlude hub -->
          <InterludePanel
            v-else-if="runStore.gameplayPhase === 'Interlude' && runStore.currentInterlude"
            :interlude="runStore.currentInterlude"
            :is-loading="runStore.isEnteringNextRoom"
            :is-saving-and-exiting="runStore.isSavingAndExiting"
            :is-abandoning-run="runStore.isAbandoningRun"
            :run-action-error="runStore.runActionError"
            @enter-next-room="runStore.enterNextRoom"
            @save-and-exit="handleSaveAndExit"
            @abandon="handleAbandon"
          />

          <!-- 4. Transition RoomCleared -->
          <RoomClearedPanel
            v-else-if="runStore.gameplayPhase === 'RoomCleared'"
            :room="runStore.currentRun.currentRoom"
            :current-room-index="runStore.currentRun.currentRoomIndex"
            :is-loading="runStore.isEnteringInterlude"
            @enter-interlude="runStore.enterInterlude"
          />

          <!-- 5. Event outcome -->
          <EventOutcomePanel
            v-else-if="runStore.gameplayPhase === 'EventOutcome' && runStore.lastOutcome"
            :outcome="runStore.lastOutcome"
            :is-loading="runStore.isLoading"
            @continue="runStore.continueAfterOutcome"
            @select-choice="runStore.selectCurrentEventChoice"
          />

          <!-- 6. Event choice result -->
          <EventChoiceResultPanel
            v-else-if="runStore.gameplayPhase === 'EventChoiceResult' && runStore.lastChoiceResult"
            :result="runStore.lastChoiceResult"
            :is-loading="runStore.isLoading"
            @continue="runStore.continueAfterChoiceResult"
          />

          <!-- 7. Map -->
          <template v-else-if="runStore.gameplayPhase === 'Map'">
            <PalaceMapPlaceholder
              :nodes="runStore.allNodes"
              :available-nodes="runStore.availableNodes"
              :selected-node-id="runStore.selectedNode?.id ?? null"
              :current-row="runStore.currentRun.currentRoom.currentNodeDepth"
              :layout-template-key="runStore.currentRun.currentRoom.layoutTemplateKey"
              :layout-template-version="runStore.currentRun.currentRoom.layoutTemplateVersion"
              @choose-node="runStore.previewNode"
            />
            <EliseOverlay :message="runStore.lastOutcome?.description" />
          </template>

          <!-- 8. Run suspendue (chargée directement via URL) -->
          <section
            v-else-if="runStore.gameplayPhase === 'Suspended'"
            class="run-suspended panel"
          >
            <p class="system-label">Run suspendue · seed {{ runStore.currentRun.seed }}</p>
            <h3>Run sauvegardée</h3>
            <p>
              La reprise de partie est prévue dans une prochaine version du Palais.
              Pour l'instant, tu peux démarrer une seed inédite.
            </p>
            <div class="run-suspended__actions">
              <button
                class="ghost-button run-suspended__cta"
                :disabled="runStore.isLoading"
                @click="startNewRun"
              >
                {{ runStore.isLoading ? 'Génération…' : 'Démarrer une nouvelle run →' }}
              </button>
            </div>
          </section>

          <!-- 9. Run terminée -->
          <section v-else class="run-grid__outcome panel">
            <p class="system-label">Run terminée</p>
            <h3>Le Tome se referme</h3>
            <p>La traversée est terminée. Le bilan détaillé sera intégré dans une prochaine version.</p>
          </section>

        </section>

        <aside v-if="shouldDisplayRightPanel" class="run-grid__right">
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

        <template v-if="runStore.isLoading">
          <h2>Le Palais recompose la pièce...</h2>
          <p>Le backend reconstitue l'état de la run.</p>
        </template>

        <template v-else-if="runStore.error">
          <h2>Run introuvable ou indisponible</h2>
          <p>{{ runStore.error }}</p>
          <p class="run-loading__hint">
            En environnement local, les runs sont encore stockées en mémoire.
            Si le backend a redémarré, l'identifiant présent dans l'URL n'existe plus.
          </p>
        </template>

        <template v-else>
          <h2>Aucune run chargée</h2>
          <p>Vérifie l'identifiant dans l'URL ou génère une nouvelle run depuis le seuil.</p>
        </template>
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

.run-grid--wide-center {
  grid-template-columns: 17rem minmax(42rem, 1fr);
}

.run-grid--wide-center .run-grid__center {
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

.run-loading h2 {
  margin: var(--space-2) 0;
  color: var(--color-frost);
  text-transform: uppercase;
  letter-spacing: 0.08em;
}

.run-loading p {
  color: var(--color-muted);
  line-height: 1.55;
}

.run-loading__hint {
  margin-top: var(--space-4);
  color: var(--color-gold);
  font-family: var(--font-mono);
  font-size: 0.8rem;
}


.run-suspended {
  display: grid;
  gap: var(--space-4);
  padding: var(--space-6);
  align-content: start;
}

.run-suspended h3 {
  margin: 0;
  color: var(--color-frost);
  text-transform: uppercase;
  letter-spacing: 0.08em;
}

.run-suspended p {
  color: var(--color-muted);
  line-height: 1.55;
  margin: 0;
}

.run-suspended__actions {
  display: flex;
  gap: var(--space-3);
}

.run-suspended__cta {
  border-color: var(--color-gold);
  color: var(--color-gold);
}

.run-suspended__cta:hover:not(:disabled) {
  background: color-mix(in oklch, var(--color-gold), transparent 88%);
}

.run-suspended__cta:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}
</style>
