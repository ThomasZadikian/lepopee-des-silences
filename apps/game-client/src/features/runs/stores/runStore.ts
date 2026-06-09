import { defineStore } from 'pinia';
import { computed, ref } from 'vue';

import { rewardApi } from '../../rewards/api/rewardApi';
import {
  unwrapRewardOffer,
  type RewardOfferDto,
} from '../../rewards/types/rewardTypes';

import { runApi } from '../api/runApi';
import {
  unwrapRunResponse,
  type CombatInstanceDto,
  type NodeDto,
  type ResolveCurrentEventResponse,
  type ResumableRunDto,
  type RunDto,
} from '../types/runTypes';

import { eventChoiceApi } from '../../events/api/eventChoiceApi';
import {
  unwrapChoiceResultFromEventChoiceResponse,
  unwrapRunFromEventChoiceResponse,
  type CurrentEventChoiceResultDto,
} from '../../events/types/eventTypes';

import {
  unwrapInterludeResponse,
  type InterludeDto,
} from '../../interlude/interludeTypes';

const demoPlayerId = '00000000-0000-0000-0000-000000000001';

// ---------------------------------------------------------------------------
// localStorage — suspended run persistence
// ---------------------------------------------------------------------------

const SUSPENDED_RUN_KEY = 'rpg:suspended_run_id';

function getSuspendedRunId(): string | null {
  try { return localStorage.getItem(SUSPENDED_RUN_KEY); } catch { return null; }
}

function setSuspendedRunId(runId: string): void {
  try { localStorage.setItem(SUSPENDED_RUN_KEY, runId); } catch {}
}

function clearSuspendedRunId(): void {
  try { localStorage.removeItem(SUSPENDED_RUN_KEY); } catch {}
}

export const useRunStore = defineStore('run', () => {
  // -------------------------------------------------------------------------
  // State
  // -------------------------------------------------------------------------

  const currentRun = ref<RunDto | null>(null);
  const pendingRewardOffer = ref<RewardOfferDto | null>(null);
  const lastOutcome = ref<ResolveCurrentEventResponse['outcome'] | null>(null);
  const activeCombat = ref<CombatInstanceDto | null>(null);
  const previewedNodeId = ref<string | null>(null);
  const lastChoiceResult = ref<CurrentEventChoiceResultDto | null>(null);

  const currentInterlude = ref<InterludeDto | null>(null);
  const isEnteringInterlude = ref(false);
  const isEnteringNextRoom = ref(false);

  const resumableRun = ref<ResumableRunDto | null>(null);
  const isLoadingResumableRun = ref(false);
  const isSavingAndExiting = ref(false);
  const isExitingMidRoom = ref(false);
  const isAbandoningRun = ref(false);
  const runActionError = ref<string | null>(null);

  const isLoading = ref(false);
  const error = ref<string | null>(null);

  // -------------------------------------------------------------------------
  // Computed
  // -------------------------------------------------------------------------

  const currentRoom = computed(() => currentRun.value?.currentRoom ?? null);

  const allNodes = computed<NodeDto[]>(() => {
    return currentRoom.value?.nodes ?? [];
  });

  const previewedNode = computed<NodeDto | null>(() => {
    if (!previewedNodeId.value) return null;
    return allNodes.value.find((n) => n.id === previewedNodeId.value) ?? null;
  });

  const selectedNode = computed<NodeDto | null>(() => {
    const room = currentRoom.value;
    if (!room) return null;
    if (previewedNode.value) return previewedNode.value;
    const selected = allNodes.value.find((n) => n.state === 'Selected');
    if (selected) return selected;
    return null;
  });

  const availableNodes = computed(() => currentRoom.value?.availableNodes ?? []);

  /**
   * True when the current room is fully cleared (boss defeated, reward selected)
   * and the run is waiting to enter the Interlude.
   * run.status === 'RoomResolved' is the backend's cleared state.
   */
  const isRoomCleared = computed(() =>
    currentRun.value?.status === 'RoomResolved' &&
    !pendingRewardOffer.value &&
    !currentRun.value?.pendingRewardOfferId,
  );

  const gameplayPhase = computed(() => {
    if (!currentRun.value) return 'Loading';

    if (pendingRewardOffer.value || currentRun.value.pendingRewardOfferId) {
      return 'Reward';
    }

    if (currentRun.value.activeCombatId) {
      return 'Combat';
    }

    if (currentRun.value.status === 'Suspended') {
      return 'Suspended';
    }

    if (currentRun.value.status === 'Completed' || currentRun.value.status === 'Failed') {
      return 'Completed';
    }

    if (currentInterlude.value || currentRun.value.status === 'Interlude') {
      return 'Interlude';
    }

    if (isRoomCleared.value) {
      return 'RoomCleared';
    }

    if (lastChoiceResult.value) return 'EventChoiceResult';
    if (lastOutcome.value) return 'EventOutcome';

    return 'Map';
  });

  // -------------------------------------------------------------------------
  // Helpers
  // -------------------------------------------------------------------------

  function resetPreviewedNode() {
    previewedNodeId.value = null;
  }

  function previewNode(nodeId: string) {
    const node = allNodes.value.find((n) => n.id === nodeId);
    if (!node || node.state !== 'Available') return;
    previewedNodeId.value = nodeId;
  }

  async function execute(action: () => Promise<void>) {
    isLoading.value = true;
    error.value = null;
    try {
      await action();
    } catch (caught) {
      error.value = caught instanceof Error
        ? caught.message
        : 'Une erreur inconnue est survenue.';
    } finally {
      isLoading.value = false;
    }
  }

  async function refreshPendingRewardIfNeeded() {
    if (!currentRun.value?.pendingRewardOfferId) {
      pendingRewardOffer.value = null;
      return;
    }
    const response = await rewardApi.getPendingReward(currentRun.value.id);
    pendingRewardOffer.value = unwrapRewardOffer(response);
  }

  // Avance la room si l'état le permet (non-boss reward selected → continue).
  // RoomResolved is handled explicitly by the RoomClearedPanel, not here.
  async function progressRunInlineIfReady() {
    if (!currentRun.value) return;

    // RoomResolved = salle terminée → RoomClearedPanel prend le relai.
    if (currentRun.value.status === 'RoomResolved') return;

    if (pendingRewardOffer.value || currentRun.value.pendingRewardOfferId) return;

    const response = await runApi.progressRun(currentRun.value.id);
    currentRun.value = unwrapRunResponse(response);
  }

  // -------------------------------------------------------------------------
  // Run lifecycle
  // -------------------------------------------------------------------------

  async function startRun() {
    await execute(async () => {
      // Vider currentRun avant l'appel pour que, si l'API échoue,
      // le composant appelant ne navigue pas vers l'ancienne run.
      currentRun.value = null;

      const response = await runApi.startRun(demoPlayerId);
      const run = unwrapRunResponse(response);

      lastChoiceResult.value = null;
      currentRun.value = run;
      pendingRewardOffer.value = null;
      lastOutcome.value = null;
      activeCombat.value = null;
      currentInterlude.value = null;
      resetPreviewedNode();

      await refreshPendingRewardIfNeeded();
    });
  }

  async function loadRun(runId: string) {
    await execute(async () => {
      // Auto-resume suspended runs
      const initialResponse = await runApi.getRun(runId);
      const initialRun = unwrapRunResponse(initialResponse);

      if (initialRun.status === 'Suspended') {
        await runApi.resumeRun(runId);
      }

      const response = await runApi.getRun(runId);
      const run = unwrapRunResponse(response);

      lastChoiceResult.value = null;
      currentRun.value = run;
      currentInterlude.value = null;
      resetPreviewedNode();

      if (!run.activeCombatId) {
        activeCombat.value = null;
      }

      await refreshPendingRewardIfNeeded();

      if (run.status === 'Interlude') {
        const interludeResponse = await runApi.getInterlude(run.id);
        currentInterlude.value = unwrapInterludeResponse(interludeResponse);
      }
    });
  }

  // -------------------------------------------------------------------------
  // Map / node actions
  // -------------------------------------------------------------------------

  function chooseNode(nodeId: string) {
    previewNode(nodeId);
  }

  async function confirmAndResolveNode() {
    if (!currentRun.value || !selectedNode.value) return;

    lastChoiceResult.value = null;
    const node = selectedNode.value;

    if (node.state === 'Resolved') {
      await progressRun();
      return;
    }

    if (node.state === 'Selected') {
      await resolveCurrentEvent();
      return;
    }

    if (node.state !== 'Available') return;

    await execute(async () => {
      const chooseResponse = await runApi.chooseNode(currentRun.value!.id, node.id);
      currentRun.value = unwrapRunResponse(chooseResponse);
      resetPreviewedNode();

      const resolveResponse = await runApi.resolveCurrentEvent(currentRun.value!.id);
      currentRun.value = resolveResponse.run;
      lastOutcome.value = resolveResponse.outcome;
      activeCombat.value = resolveResponse.startedCombat ?? null;

      await refreshPendingRewardIfNeeded();
    });
  }

  async function resolveCurrentEvent() {
    if (!currentRun.value) return;

    await execute(async () => {
      const response = await runApi.resolveCurrentEvent(currentRun.value!.id);

      lastChoiceResult.value = null;
      currentRun.value = response.run;
      lastOutcome.value = response.outcome;
      activeCombat.value = response.startedCombat ?? null;
      resetPreviewedNode();

      await refreshPendingRewardIfNeeded();
    });
  }

  async function progressRun() {
    if (!currentRun.value) return;

    await execute(async () => {
      await refreshPendingRewardIfNeeded();

      if (pendingRewardOffer.value || currentRun.value?.pendingRewardOfferId) {
        throw new Error('Une récompense est en attente : choisis une faveur avant de progresser.');
      }

      const response = await runApi.progressRun(currentRun.value!.id);

      lastChoiceResult.value = null;
      currentRun.value = unwrapRunResponse(response);
      lastOutcome.value = null;
      activeCombat.value = null;
      resetPreviewedNode();

      await refreshPendingRewardIfNeeded();
    });
  }

  async function generateNextNodes() {
    if (!currentRun.value) return;

    await execute(async () => {
      const response = await runApi.generateNextNodes(currentRun.value!.id);
      currentRun.value = unwrapRunResponse(response);
      lastOutcome.value = null;
      resetPreviewedNode();
      await refreshPendingRewardIfNeeded();
    });
  }

  // -------------------------------------------------------------------------
  // Combat
  // -------------------------------------------------------------------------

  async function handleCombatCompleted() {
    if (!currentRun.value) return;

    await execute(async () => {
      const response = await runApi.getRun(currentRun.value!.id);
      const run = unwrapRunResponse(response);

      lastChoiceResult.value = null;
      currentRun.value = run;
      activeCombat.value = null;
      lastOutcome.value = null;
      resetPreviewedNode();

      await refreshPendingRewardIfNeeded();
      await progressRunInlineIfReady();
    });
  }

  // -------------------------------------------------------------------------
  // Rewards
  // -------------------------------------------------------------------------

  async function selectReward(optionId: string) {
    if (!currentRun.value || !pendingRewardOffer.value) return;

    await execute(async () => {
      await rewardApi.selectReward(currentRun.value!.id, {
        rewardOfferId: pendingRewardOffer.value!.id,
        rewardChoiceId: optionId,
        choiceId: optionId,
        rewardOptionId: optionId,
        optionId,
      });

      pendingRewardOffer.value = null;

      const response = await runApi.getRun(currentRun.value!.id);
      currentRun.value = unwrapRunResponse(response);

      lastChoiceResult.value = null;
      lastOutcome.value = null;
      activeCombat.value = null;
      resetPreviewedNode();

      await refreshPendingRewardIfNeeded();

      // Pour les rewards non-boss, progresser normalement.
      // RoomResolved (boss) → RoomClearedPanel prend le relai.
      await progressRunInlineIfReady();
    });
  }

  // -------------------------------------------------------------------------
  // Interlude
  // -------------------------------------------------------------------------

  async function enterInterlude() {
    if (!currentRun.value) return;

    isEnteringInterlude.value = true;
    error.value = null;

    try {
      const response = await runApi.enterInterlude(currentRun.value.id);
      const interlude = unwrapInterludeResponse(response);

      currentInterlude.value = interlude;

      // Mettre à jour le run avec le nouveau status Interlude
      const runResponse = await runApi.getRun(currentRun.value.id);
      currentRun.value = unwrapRunResponse(runResponse);
    } catch (caught) {
      error.value = caught instanceof Error
        ? caught.message
        : 'Impossible d\'entrer dans le Repli du Palais.';
    } finally {
      isEnteringInterlude.value = false;
    }
  }

  async function loadInterlude() {
    if (!currentRun.value) return;

    await execute(async () => {
      const response = await runApi.getInterlude(currentRun.value!.id);
      currentInterlude.value = unwrapInterludeResponse(response);
    });
  }

  async function enterNextRoom() {
    if (!currentRun.value) return;

    isEnteringNextRoom.value = true;
    error.value = null;

    try {
      const response = await runApi.enterNextRoom(currentRun.value.id);
      const run = unwrapRunResponse(response);

      currentRun.value = run;
      currentInterlude.value = null;
      lastOutcome.value = null;
      activeCombat.value = null;
      lastChoiceResult.value = null;
      resetPreviewedNode();
    } catch (caught) {
      error.value = caught instanceof Error
        ? caught.message
        : 'Impossible d\'entrer dans la prochaine salle.';
    } finally {
      isEnteringNextRoom.value = false;
    }
  }

  // -------------------------------------------------------------------------
  // Resumable run (localStorage-backed)
  // -------------------------------------------------------------------------

  async function loadResumableRun() {
    const runId = getSuspendedRunId();
    if (!runId) return;

    isLoadingResumableRun.value = true;
    try {
      const response = await runApi.getRun(runId);
      const run = unwrapRunResponse(response);
      if (run.canResume) {
        resumableRun.value = {
          id: run.id,
          seed: run.seed,
          savedAt: run.savedAt ?? new Date().toISOString(),
          currentRoomNumber: run.currentRoomNumber,
          status: run.status,
        };
      } else {
        clearSuspendedRunId();
        resumableRun.value = null;
      }
    } catch {
      // Backend restarted or run no longer exists — clear stale ref
      clearSuspendedRunId();
      resumableRun.value = null;
    } finally {
      isLoadingResumableRun.value = false;
    }
  }

  async function saveAndExitCurrentRun(): Promise<boolean> {
    if (!currentRun.value) return false;
    const runId = currentRun.value.id;

    isSavingAndExiting.value = true;
    runActionError.value = null;

    try {
      const response = await runApi.saveAndExitRun(runId);
      const run = unwrapRunResponse(response);

      setSuspendedRunId(run.id);
      resumableRun.value = {
        id: run.id,
        seed: run.seed,
        savedAt: run.savedAt ?? new Date().toISOString(),
        currentRoomNumber: run.currentRoomNumber,
        status: run.status,
      };

      clearCurrentRun();
      return true;
    } catch (caught) {
      runActionError.value = caught instanceof Error
        ? caught.message
        : 'Impossible de sauvegarder la run.';
      return false;
    } finally {
      isSavingAndExiting.value = false;
    }
  }

  async function exitMidRoom(): Promise<boolean> {
    if (!currentRun.value) return false;
    const runId = currentRun.value.id;

    isExitingMidRoom.value = true;
    runActionError.value = null;

    try {
      const response = await runApi.exitMidRoom(runId);
      const run = unwrapRunResponse(response);

      setSuspendedRunId(run.id);
      resumableRun.value = {
        id: run.id,
        seed: run.seed,
        savedAt: run.savedAt ?? new Date().toISOString(),
        currentRoomNumber: run.currentRoomNumber,
        status: run.status,
      };

      clearCurrentRun();
      return true;
    } catch (caught) {
      runActionError.value = caught instanceof Error
        ? caught.message
        : 'Impossible de quitter la salle.';
      return false;
    } finally {
      isExitingMidRoom.value = false;
    }
  }

  async function abandonCurrentRun(): Promise<boolean> {
    if (!currentRun.value) return false;
    const runId = currentRun.value.id;

    isAbandoningRun.value = true;
    runActionError.value = null;

    try {
      await runApi.abandonRun(runId);

      if (getSuspendedRunId() === runId) {
        clearSuspendedRunId();
      }
      if (resumableRun.value?.id === runId) {
        resumableRun.value = null;
      }

      clearCurrentRun();
      return true;
    } catch (caught) {
      runActionError.value = caught instanceof Error
        ? caught.message
        : 'Impossible d\'abandonner la run.';
      return false;
    } finally {
      isAbandoningRun.value = false;
    }
  }

  function clearCurrentRun() {
    currentRun.value = null;
    pendingRewardOffer.value = null;
    lastOutcome.value = null;
    activeCombat.value = null;
    previewedNodeId.value = null;
    lastChoiceResult.value = null;
    currentInterlude.value = null;
    error.value = null;
  }

  // -------------------------------------------------------------------------
  // Event choices / outcome
  // -------------------------------------------------------------------------

  async function continueAfterOutcome() {
    if (!lastOutcome.value) return;
    await progressRun();
  }

  async function continueAfterChoiceResult() {
    if (!lastChoiceResult.value) return;
    await progressRun();
  }

  async function selectCurrentEventChoice(choiceId: string) {
    if (!currentRun.value || !lastOutcome.value) return;

    await execute(async () => {
      const response = await eventChoiceApi.chooseCurrentEventOption(
        currentRun.value!.id,
        { choiceId, optionId: choiceId, eventChoiceId: choiceId },
      );

      const run = unwrapRunFromEventChoiceResponse(response);
      const choiceResult = unwrapChoiceResultFromEventChoiceResponse(response);

      if (run) {
        currentRun.value = run;
      } else {
        const runResponse = await runApi.getRun(currentRun.value!.id);
        currentRun.value = unwrapRunResponse(runResponse);
      }

      lastChoiceResult.value = choiceResult;
      lastOutcome.value = null;
      activeCombat.value = null;
      resetPreviewedNode();

      await refreshPendingRewardIfNeeded();
    });
  }

  // -------------------------------------------------------------------------
  // Loads
  // -------------------------------------------------------------------------

  async function loadPendingReward() {
    if (!currentRun.value) return;
    await execute(async () => {
      await refreshPendingRewardIfNeeded();
    });
  }

  // -------------------------------------------------------------------------
  // Exports
  // -------------------------------------------------------------------------

  return {
    currentRun,
    currentRoom,
    allNodes,
    selectedNode,
    availableNodes,
    previewedNodeId,
    lastOutcome,
    lastChoiceResult,
    activeCombat,
    pendingRewardOffer,
    currentInterlude,
    isEnteringInterlude,
    isEnteringNextRoom,
    isRoomCleared,
    gameplayPhase,
    isLoading,
    error,

    startRun,
    loadRun,
    chooseNode,
    previewNode,
    confirmAndResolveNode,
    progressRun,
    resolveCurrentEvent,
    generateNextNodes,
    loadPendingReward,
    selectReward,
    handleCombatCompleted,
    refreshPendingRewardIfNeeded,
    continueAfterOutcome,
    continueAfterChoiceResult,
    selectCurrentEventChoice,
    enterInterlude,
    loadInterlude,
    enterNextRoom,

    resumableRun,
    isLoadingResumableRun,
    isSavingAndExiting,
    isExitingMidRoom,
    isAbandoningRun,
    runActionError,
    loadResumableRun,
    saveAndExitCurrentRun,
    exitMidRoom,
    abandonCurrentRun,
    clearCurrentRun,
  };
});
