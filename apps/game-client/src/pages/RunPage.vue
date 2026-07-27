<script setup lang="ts">
import { computed, defineAsyncComponent, onMounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';

import GameShellLayout from '../app/layouts/GameShellLayout.vue';
import CombatScene from '../features/combat/components/CombatScene.vue';
import TacticalCombatScene from '../features/combat/components/TacticalCombatScene.vue';
import { useTacticalCombatStore } from '../features/combat/stores/useTacticalCombatStore';
import { useCombatStore } from '../features/combat/stores/useCombatStore';
import EliseOverlay from '../features/elise/EliseOverlay.vue';
import EventChoiceResultPanel from '../features/events/components/EventChoiceResultPanel.vue';
import EventOutcomePanel from '../features/events/components/EventOutcomePanel.vue';
import MerchantPanel from '../features/events/components/MerchantPanel.vue';
import NpcDialoguePanel from '../features/events/components/NpcDialoguePanel.vue';
import ReputationEffectPopup from '../features/events/components/ReputationEffectPopup.vue';
import type { CurrentEventChoiceResultDto } from '../features/events/types/eventTypes';
import InterludePanel from '../features/interlude/InterludePanel.vue';
import RoomClearedPanel from '../features/interlude/RoomClearedPanel.vue';
import InventoryDrawer from '../features/inventory/components/InventoryDrawer.vue';
import JournalModal from '../features/runs/components/JournalModal.vue';
import PermanentItemSelectionPanel from '../features/runs/components/PermanentItemSelectionPanel.vue';
import LawResolutionPanel from '../features/palace-laws/LawResolutionPanel.vue';
import LawsPopover from '../features/palace-laws/LawsPopover.vue';
import TacticalGridMap from '../features/palace-map/TacticalGridMap.vue';
import { usePlayerStore } from '../features/party/stores/playerStore';
import RewardOfferPanel from '../features/rewards/components/RewardOfferPanel.vue';
import RoomClimateEffects from '../features/room-climate/RoomClimateEffects.vue';
import PartyDrawer from '../features/runs/components/PartyDrawer.vue';
import RunStatusRibbon from '../features/runs/components/RunStatusRibbon.vue';
import TeamMicroMenu from '../features/runs/components/TeamMicroMenu.vue';
import { useRunStore } from '../features/runs/stores/runStore';
import DecisionDiptych from '../shared/components/DecisionDiptych.vue';
import { useGameUiStore } from '../shared/stores/useGameUiStore';

const route = useRoute();
const router = useRouter();
const runStore = useRunStore();
const combatStore = useCombatStore();
const tacticalCombatStore = useTacticalCombatStore();
const uiStore = useGameUiStore();
const playerStore = usePlayerStore();
const devToolsEnabled = import.meta.env.DEV === true &&
  import.meta.env.VITE_GAME_CLIENT_DEVTOOLS_ENABLED === 'true';
const DevToolsPanel = devToolsEnabled
  ? defineAsyncComponent(() => import('../features/devtools/components/DevToolsPanel.vue'))
  : null;

// ── Synthetic "Choix accompli" transition ──────────────────────────────────
const showingTransition = ref(false);
const transitionResult = ref<CurrentEventChoiceResultDto | null>(null);
const transitionAfterChoice = ref(false);
const showDevTools = ref(false);

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

// Clicking anywhere outside the open drawers closes them — except on the
// map itself (its own click handling for choosing/deselecting nodes stays
// authoritative) and on the status ribbon (whose buttons already toggle
// their own drawer; letting this listener also fire there would reopen a
// drawer the same click just closed).
const drawersRef = ref<HTMLElement | null>(null);

async function handleLeaveRun() {
  combatStore.clearCombat();
  tacticalCombatStore.clearCombat();
  runStore.clearCurrentRun();
  await router.replace('/');
}

async function startNewRun() {
  await runStore.startRun();
  const runId = runStore.currentRun?.id;
  if (runId) await router.replace(`/run/${runId}`);
}

const isMapPhase = computed(() => runStore.gameplayPhase === 'Map');
// activeModifiers deliberately excluded — a law's mechanical effect is mirrored
// there under the hood, so counting it too double-counts the same influence
// (LawsPopover no longer displays modifiers separately for the same reason).
const totalInfluenceCount = computed(() => {
  const run = runStore.currentRun;
  if (!run) return 0;
  return (run.activePalaceLaws?.length ?? 0) + (run.activeCurses?.length ?? 0);
});
const isCombatPhase = computed(() => runStore.gameplayPhase === 'Combat');
const showInventoryDrawer = computed(() => uiStore.activeDrawer === 'besace');
const showPartyDrawer = computed(() => uiStore.activeDrawer === 'party' && !isCombatPhase.value);
const showJournalModal = computed(() => uiStore.activeDrawer === 'journal');

watch(showPartyDrawer, (isOpen) => {
  if (isOpen) void playerStore.loadProfile();
});
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

      <Teleport to="body">
        <ReputationEffectPopup
          :effects="runStore.reputationEffects"
          @dismiss="runStore.dismissReputationEffect"
        />
      </Teleport>

      <!-- ── Map phase: map dominates ── -->
      <template v-if="isMapPhase">
        <div class="phase-map">
          <!-- Map canvas -->
          <div class="phase-map__canvas">
            <TacticalGridMap
              :room="runStore.currentRun.currentRoom"
              :influence-count="totalInfluenceCount"
              @move-request="runStore.movePartyTo"
              @enter-node="runStore.enterGridNode"
              @wager-node="runStore.wagerNode"
              @challenge-boss="runStore.challengeBossRemotely"
              @search="runStore.searchParty"
              @toggle-laws="uiStore.toggleLaws"
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
            @open-journal="uiStore.toggleJournal"
          />

          <!-- Elise overlay -->
          <EliseOverlay :message="runStore.lastOutcome?.description" />

          <!-- Drawers (right, absolute positioned) — a click outside all of them closes whichever is open -->
          <div ref="drawersRef">
            <!-- Inventory drawer -->
            <Transition name="slide">
              <InventoryDrawer
                v-if="showInventoryDrawer"
                :items="runStore.currentRun.inventoryItems ?? []"
                :run-id="runStore.currentRun.id"
                :capacity="runStore.currentRun.runItemCapacity ?? null"
                @close="uiStore.closeDrawer"
              />
            </Transition>

            <!-- Laws / influences popover -->
            <Transition name="slide">
                <LawsPopover
                  v-if="showLaws"
                  :laws="runStore.currentRun.activePalaceLaws"
                  :curses="runStore.currentRun.activeCurses"
                  :room-climate="runStore.currentRun.currentRoom.activeClimate ?? runStore.currentRun.currentRoom.climate ?? null"
                  show-room-climate
                  :law-denial-enabled="runStore.currentRun.lawDenialEnabled ?? false"
                  :can-use-law-denial="runStore.currentRun.canUseLawDenial ?? false"
                  @close="uiStore.toggleLaws"
                  @revoke-law="runStore.removePalaceLaw"
                />
            </Transition>

            <!-- Party drawer -->
            <Transition name="slide">
              <PartyDrawer
                v-if="showPartyDrawer"
                :allies="runStore.currentRun.party?.members ?? null"
                :modifiers="runStore.currentRun.activeModifiers ?? null"
                :laws="runStore.currentRun.activePalaceLaws ?? null"
                :curses="runStore.currentRun.activeCurses ?? null"
                :items="runStore.currentRun.inventoryItems ?? null"
                :calice-infini-enabled="runStore.currentRun.caliceInfiniEnabled ?? false"
                :can-use-calice-infini="runStore.currentRun.canUseCaliceInfini ?? false"
                @close="uiStore.closeDrawer"
              />
            </Transition>
          </div>

          <Teleport to="body">
            <JournalModal v-if="showJournalModal" @close="uiStore.closeDrawer" />
          </Teleport>
        </div>
      </template>

      <!-- ── Combat phase ── -->
      <!-- Le mode de combat est fixé pour toute la run : une seule des deux scènes existe. -->
      <template v-else-if="isCombatPhase && tacticalCombatStore.combat">
        <TacticalCombatScene
          :run-id="runStore.currentRun.id"
          :theme="runStore.currentRun.currentRoom?.theme"
          @combat-completed="runStore.handleCombatCompleted"
          @combat-failed="runStore.handleCombatFailed"
        />
      </template>

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

            <!-- ── NPC dialogue ── -->
      <template v-else-if="runStore.gameplayPhase === 'NpcDialogue'">
        <NpcDialoguePanel
          :dialogue="runStore.npcDialogue"
          :echoes="runStore.npcDialogueEchoes"
          :ended="runStore.npcDialogueEnded"
          :is-loading="runStore.isLoading"
          @select-choice="runStore.selectNpcDialogueChoice"
          @continue="runStore.continueAfterNpcDialogue"
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

      <!-- ── End-of-run permanent item selection ── -->
      <template v-else-if="runStore.gameplayPhase === 'ItemSelection'">
        <PermanentItemSelectionPanel
          :candidates="runStore.permanentItemCandidates"
          :is-loading="runStore.isLoading"
          @confirm="runStore.confirmPermanentItemSelection"
        />
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

      <!-- ── Micro-menu (Équipe/Statistiques/Grimoire/Équipement) ── -->
      <Teleport to="body">
        <TeamMicroMenu v-if="!isCombatPhase" />
      </Teleport>

      <!-- Devtools are development-only and require VITE_GAME_CLIENT_DEVTOOLS_ENABLED=true. -->
      <Teleport v-if="devToolsEnabled" to="body">
        <button
          type="button"
          class="devtools-float-toggle"
          :aria-expanded="showDevTools"
          @click="showDevTools = !showDevTools"
        >
          DevTools
        </button>
        <component
          :is="DevToolsPanel"
          v-if="showDevTools && DevToolsPanel"
          :run-id="runStore.currentRun.id"
          :combat="combatStore.combat"
          @close="showDevTools = false"
        />
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

.devtools-float-toggle {
  position: fixed;
  right: 18px;
  bottom: 18px;
  z-index: 9090;
  border: 1px solid oklch(0.72 0.12 85 / 0.55);
  border-radius: 999px;
  padding: 9px 13px;
  background: oklch(0.16 0.03 270 / 0.92);
  color: var(--gold, #d7b56d);
  font-family: var(--caps, monospace);
  font-size: 11px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  cursor: pointer;
  box-shadow: 0 8px 30px oklch(0 0 0 / 0.4);
}

.devtools-float-toggle:hover {
  background: oklch(0.23 0.04 270 / 0.96);
}
</style>
