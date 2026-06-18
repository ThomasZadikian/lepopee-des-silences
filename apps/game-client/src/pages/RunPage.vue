<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';

import GameShellLayout from '../app/layouts/GameShellLayout.vue';
import CombatScene from '../features/combat/components/CombatScene.vue';
import { useCombatStore } from '../features/combat/stores/useCombatStore';
import DecisionDiptych from '../shared/components/DecisionDiptych.vue';
import EliseOverlay from '../features/elise/EliseOverlay.vue';
import EventChoiceResultPanel from '../features/events/components/EventChoiceResultPanel.vue';
import EventOutcomePanel from '../features/events/components/EventOutcomePanel.vue'
import MerchantPanel from '../features/events/components/MerchantPanel.vue'
import LawResolutionPanel from '../features/palace-laws/LawResolutionPanel.vue';
import InterludePanel from '../features/interlude/InterludePanel.vue';
import RoomClearedPanel from '../features/interlude/RoomClearedPanel.vue';
import InventoryDrawer from '../features/inventory/components/InventoryDrawer.vue';
import LawsPopover from '../features/palace-laws/LawsPopover.vue';
import PalaceNodeDrawer from '../features/node-details/PalaceNodeDrawer.vue';
import PalaceMapPlaceholder from '../features/palace-map/PalaceMapPlaceholder.vue';
import RewardOfferPanel from '../features/rewards/components/RewardOfferPanel.vue';
import RoomClimateEffects from '../features/room-climate/RoomClimateEffects.vue';
import RunStatusRibbon from '../features/runs/components/RunStatusRibbon.vue';
import PartyDrawer from '../features/runs/components/PartyDrawer.vue';
import RuntimeDebugPanel from '../shared/components/RuntimeDebugPanel.vue';
import { useRunStore } from '../features/runs/stores/runStore';
import { useGameUiStore } from '../shared/stores/useGameUiStore';
import type { CurrentEventChoiceResultDto } from '../features/events/types/eventTypes';

const route = useRoute();
const router = useRouter();
const runStore = useRunStore();
const combatStore = useCombatStore();
const uiStore = useGameUiStore();

// ── Synthetic "Choix accompli" transition ──────────────────────────────────
const showingTransition = ref(false);
const transitionResult = ref<CurrentEventChoiceResultDto | null>(null);
const transitionAfterChoice = ref(false);

async function handleEventContinue() {
  const outcome = runStore.lastOutcome;
  transitionResult.value = {
    title: outcome?.title ?? 'Résolu',
    description: outcome?.description ?? undefined,
    outcomeKind: outcome?.resolutionKind ?? undefined,
    state: 'Résolu',
  };
  showingTransition.value = true;
  transitionAfterChoice.value = false;
}

async function handleSelectChoice(choiceId: string) {
  const outcome = runStore.lastOutcome;
  await runStore.selectCurrentEventChoice(choiceId);
  if (!runStore.lastChoiceResult) {
    transitionResult.value = {
      title: outcome?.title ?? 'Choix effectué',
      description: outcome?.description ?? undefined,
      outcomeKind: outcome?.resolutionKind ?? undefined,
      state: 'Choix effectué',
    };
    showingTransition.value = true;
    transitionAfterChoice.value = true;
  }
}

async function handleTransitionContinue() {
  showingTransition.value = false;
  transitionResult.value = null;
  if (transitionAfterChoice.value) {
    await runStore.progressRun();
  } else {
    await runStore.continueAfterOutcome();
  }
  transitionAfterChoice.value = false;
}

const isSafePoint = computed(() =>
  runStore.currentRun?.status === 'RoomResolved' ||
  runStore.currentRun?.status === 'Interlude',
);

const showConfirmAbandon = ref(false);

async function handleSaveAndExit() {
  const ok = await runStore.saveAndExitCurrentRun();
  if (ok) await router.replace('/');
}

function requestAbandon() {
  showConfirmAbandon.value = true;
}

async function confirmAbandon() {
  showConfirmAbandon.value = false;
  const ok = await runStore.abandonCurrentRun();
  if (ok) await router.replace('/');
}

async function handleExitMidRoom() {
  const ok = await runStore.exitMidRoom();
  if (ok) await router.replace('/');
}

function clearAllUi() {
  runStore.resetPreviewedNode();
  uiStore.closeAll();
}

async function handleLeaveRun() {
  combatStore.clearCombat();
  runStore.clearCurrentRun();
  await router.replace('/');
}

async function startNewRun() {
  await runStore.startRun();
  const runId = runStore.currentRun?.id;
  if (runId) await router.replace(`/run/${runId}`);
}

const isMapPhase = computed(() => runStore.gameplayPhase === 'Map');
const isCombatPhase = computed(() => runStore.gameplayPhase === 'Combat');
const showNodeDrawer = computed(() => isMapPhase.value && runStore.selectedNode);
const showInventoryDrawer = computed(() => uiStore.activeDrawer === 'besace');
const showPartyDrawer = computed(() => uiStore.activeDrawer === 'party' && !isCombatPhase.value);
const showLaws = computed(() => uiStore.isLawsOpen);
const activeRoomClimate = computed(() =>
  runStore.currentRun?.currentRoom?.activeClimate
  ?? runStore.currentRun?.currentRoom?.climate
  ?? null,
);

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
watch(() => route.params.runId, async () => { await loadRunFromRoute(); });
</script>

<template>
  <GameShellLayout :hide-top-bar="isCombatPhase">
    <template v-if="runStore.currentRun && runStore.currentRun.currentRoom">
      <RoomClimateEffects :climate="activeRoomClimate" />

      <!-- ── Map phase: map dominates ── -->
      <template v-if="isMapPhase">
        <div class="phase-map">
          <!-- Map canvas -->
          <div class="phase-map__canvas">
            <PalaceMapPlaceholder
              :nodes="runStore.allNodes"
              :available-nodes="runStore.availableNodes"
              :selected-node-id="runStore.selectedNode?.id ?? null"
              :current-row="runStore.currentRun.currentRoom.currentNodeDepth"
              :layout-template-key="runStore.currentRun.currentRoom.layoutTemplateKey"
              :layout-template-version="runStore.currentRun.currentRoom.layoutTemplateVersion"
              @choose-node="runStore.previewNode"
            @deselect-node="clearAllUi"
            />
          </div>

          <!-- Bottom ribbon -->
          <RunStatusRibbon
            :run="runStore.currentRun"
            :is-safe-point="isSafePoint"
            @save-and-exit="handleSaveAndExit"
            @abandon="requestAbandon"
            @exit-mid-room="handleExitMidRoom"
            @open-besace="uiStore.toggleBesace"
            @open-party="uiStore.toggleParty"
            @open-influences="uiStore.toggleLaws"
          />

          <!-- Elise overlay -->
          <EliseOverlay :message="runStore.lastOutcome?.description" />

          <!-- Node drawer (right, absolute positioned) -->
          <Transition name="slide">
            <PalaceNodeDrawer
              v-if="showNodeDrawer"
              :node="runStore.selectedNode"
              :is-loading="runStore.isLoading"
              :has-active-combat="Boolean(runStore.currentRun.activeCombatId)"
              :has-pending-reward="Boolean(runStore.pendingRewardOffer || runStore.currentRun.pendingRewardOfferId)"
              @resolve-current-event="runStore.confirmAndResolveNode"
              @generate-next-nodes="runStore.progressRun"
              @choose-and-resolve="runStore.confirmAndResolveNode"
              @close="runStore.resetPreviewedNode"
            />
          </Transition>

          <!-- Inventory drawer (right, absolute positioned) -->
          <Transition name="slide">
            <InventoryDrawer
              v-if="showInventoryDrawer"
              :items="runStore.currentRun.inventoryItems ?? []"
              :run-id="runStore.currentRun.id"
              @close="uiStore.closeDrawer"
            />
          </Transition>

          <!-- Laws / influences popover (right, absolute positioned) -->
          <Transition name="slide">
              <LawsPopover
                v-if="showLaws"
                :laws="runStore.currentRun.activePalaceLaws"
                :curses="runStore.currentRun.activeCurses"
                :modifiers="runStore.currentRun.activeModifiers ?? null"
                :palace-indicators="runStore.currentRun.palaceIndicators ?? null"
                :room-climate="runStore.currentRun.currentRoom.activeClimate ?? runStore.currentRun.currentRoom.climate ?? null"
                show-room-climate
                @close="uiStore.toggleLaws"
              />
          </Transition>

          <!-- Party drawer (right, absolute positioned) -->
          <Transition name="slide">
            <PartyDrawer
              v-if="showPartyDrawer"
              :allies="runStore.currentRun.party?.members ?? null"
              :modifiers="runStore.currentRun.activeModifiers ?? null"
              :laws="runStore.currentRun.activePalaceLaws ?? null"
              :curses="runStore.currentRun.activeCurses ?? null"
              :items="runStore.currentRun.inventoryItems ?? null"
              @close="uiStore.closeDrawer"
            />
          </Transition>
        </div>
      </template>

      <!-- ── Combat phase ── -->
      <template v-else-if="isCombatPhase && runStore.currentRun.activeCombatId">
        <CombatScene
          :run-id="runStore.currentRun.id"
          :combat-id="runStore.currentRun.activeCombatId"
          @combat-completed="runStore.handleCombatCompleted"
          @combat-failed="runStore.handleCombatFailed"
          @leave-run="handleLeaveRun"
        />
      </template>

      <!-- ── Reward phase ── -->
      <template v-else-if="runStore.gameplayPhase === 'Reward'">
        <RewardOfferPanel
          v-if="runStore.pendingRewardOffer"
          :offer="runStore.pendingRewardOffer"
          :is-loading="runStore.isLoading"
          @select-reward="runStore.selectReward"
        />
        <section v-else class="phase-center">
          <p class="es-kicker">Récompense</p>
          <h3 class="es-h2">Une résonance demeure.</h3>
          <p class="es-lede es-dim">Le Palais rassemble ce que tu peux emporter.</p>
        </section>
      </template>

      <!-- ── Interlude phase ── -->
      <template v-else-if="runStore.gameplayPhase === 'Interlude' && runStore.currentInterlude">
        <InterludePanel
          :interlude="runStore.currentInterlude"
          :is-loading="runStore.isEnteringNextRoom"
          :is-saving-and-exiting="runStore.isSavingAndExiting"
          :is-abandoning-run="runStore.isAbandoningRun"
          :run-action-error="runStore.runActionError"
          @enter-next-room="runStore.enterNextRoom"
          @save-and-exit="handleSaveAndExit"
          @abandon="requestAbandon"
        />
      </template>

      <!-- ── Room cleared transition ── -->
      <template v-else-if="runStore.gameplayPhase === 'RoomCleared'">
        <RoomClearedPanel
          :room="runStore.currentRun.currentRoom"
          :current-room-index="runStore.currentRun.currentRoomIndex"
          :is-loading="runStore.isEnteringInterlude"
          @enter-interlude="runStore.enterInterlude"
        />
      </template>

      <!-- ── Synthetic "Choix accompli" transition ── -->
      <template v-else-if="showingTransition && transitionResult">
        <EventChoiceResultPanel
          :result="transitionResult"
          :is-loading="runStore.isLoading"
          @continue="handleTransitionContinue"
        />
      </template>

      <!-- ── Event outcome ── -->
      <template v-else-if="runStore.gameplayPhase === 'EventOutcome' && runStore.lastOutcome">
        <LawResolutionPanel
          v-if="runStore.lastOutcome.resolutionKind === 'PalaceLawOffered'"
          :outcome="runStore.lastOutcome"
          :is-loading="runStore.isLoading"
          :active-laws="runStore.currentRun.activePalaceLaws"
          :active-curses="runStore.currentRun.activeCurses"
          @continue="handleEventContinue"
          @select-choice="handleSelectChoice"
        />
        <MerchantPanel
          v-else-if="runStore.lastOutcome.resolutionKind === 'TradeOffered'"
          :outcome="runStore.lastOutcome"
          :is-loading="runStore.isLoading"
          @continue="handleEventContinue"
          @select-choice="handleSelectChoice"
        />
        <EventOutcomePanel
          v-else
          :outcome="runStore.lastOutcome"
          :is-loading="runStore.isLoading"
          @continue="handleEventContinue"
          @select-choice="handleSelectChoice"
        />
      </template>

      <!-- ── Event choice result (real backend result) ── -->
      <template v-else-if="runStore.gameplayPhase === 'EventChoiceResult' && runStore.lastChoiceResult">
        <EventChoiceResultPanel
          :result="runStore.lastChoiceResult"
          :is-loading="runStore.isLoading"
          @continue="runStore.continueAfterChoiceResult"
        />
      </template>

      <!-- ── Suspended ── -->
      <template v-else-if="runStore.gameplayPhase === 'Suspended'">
        <section class="phase-center">
          <p class="es-kicker">Run suspendue · seed {{ runStore.currentRun.seed }}</p>
          <h3 class="es-h2">Run sauvegardée</h3>
          <p class="es-lede es-dim">
            La reprise de partie est prévue dans une prochaine version du Palais.
            Pour l'instant, tu peux démarrer une seed inédite.
          </p>
          <button
            class="es-btn es-btn--gold es-btn--lg"
            :disabled="runStore.isLoading"
            @click="startNewRun"
          >
            {{ runStore.isLoading ? 'Génération…' : 'Démarrer une nouvelle run →' }}
          </button>
        </section>
      </template>

      <!-- ── Completed / Failed ── -->
      <template v-else>
        <section class="phase-center">
          <p class="es-kicker">Run terminée</p>
          <h3 class="es-h2">{{ runStore.currentRun.status === 'Failed' ? 'Défaite définitive' : 'Le Tome se referme' }}</h3>
          <p class="es-lede es-dim">
            {{ runStore.currentRun.status === 'Failed'
              ? 'Tous les alliés ont été vaincus. Cette run est perdue définitivement.'
              : 'La traversée est terminée. Le bilan détaillé sera intégré dans une prochaine version.' }}
          </p>
          <button class="es-btn es-btn--blood" @click="handleLeaveRun">
            Quitter la run
          </button>
        </section>
      </template>

      <!-- ── Runtime debug (all non-combat phases) ── -->
      <Teleport to="body">
        <div v-if="!isCombatPhase" class="rdp-float">
          <RuntimeDebugPanel
            :data="{
              run: runStore.currentRun,
              phase: runStore.gameplayPhase,
              combatRuntime: runStore.combatRuntime,
              lastOutcome: runStore.lastOutcome,
              pendingReward: runStore.pendingRewardOffer,
            }"
            label="Run state"
          />
        </div>
      </Teleport>

      <!-- ── Abandon confirmation diptych ── -->
      <DecisionDiptych
        v-model="showConfirmAbandon"
        title="Abandonner la run ?"
        description="Ta progression actuelle sera perdue. Le Tome gardera trace de ce qui a été vécu."
        confirm-label="Abandonner"
        cancel-label="Rester"
        danger
        @confirm="confirmAbandon"
      />
    </template>

    <!-- ── Loading / Error ── -->
    <template v-else>
      <section class="phase-center">
        <template v-if="runStore.isLoading">
          <p class="es-kicker">Chargement</p>
          <h3 class="es-h2">Le Palais recompose la pièce…</h3>
        </template>
        <template v-else-if="runStore.error">
          <p class="es-kicker">Erreur</p>
          <h3 class="es-h2">Run introuvable</h3>
          <p class="es-lede es-dim">{{ runStore.error }}</p>
        </template>
        <template v-else>
          <p class="es-kicker">Aucune run</p>
          <h3 class="es-h2">Aucune run chargée</h3>
          <p class="es-lede es-dim">Vérifie l'identifiant dans l'URL ou génère une nouvelle run depuis le seuil.</p>
        </template>
      </section>
    </template>
  </GameShellLayout>
</template>

<style scoped>
.phase-map {
  position: relative;
  height: 100%;
}

.phase-map__canvas {
  height: 100%;
  overflow: hidden;
}

.phase-center {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: var(--space-3);
  height: 100%;
  padding: var(--space-8);
  text-align: center;
}

.phase-center .es-btn {
  margin-top: var(--space-4);
}

.rdp-float {
  position: fixed;
  bottom: 0;
  left: 0;
  width: 480px;
  max-width: 100vw;
  z-index: 9000;
  pointer-events: auto;
}
</style>
