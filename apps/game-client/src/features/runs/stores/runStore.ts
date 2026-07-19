import { defineStore } from 'pinia';
import { computed, ref, shallowRef } from 'vue';
import { rewardApi } from '../../rewards/api/rewardApi';
import {
  unwrapRewardOffer,
  unwrapRunFromSelectRewardResponse,
  type RewardOfferDto,
} from '../../rewards/types/rewardTypes';

import { runApi } from '../api/runApi';
import {
  unwrapRunResponse,
  type CombatInstanceDto,
  type NarrativeFragmentDto,
  type NodeDto,
  type NpcDialogueViewDto,
  type PermanentItemCandidateDto,
  type ResolveCurrentEventResponse,
  type ResumableRunDto,
  type RunDto,
} from '../types/runTypes';

import type { CombatRuntimeDto } from '../../combat/types/combatContracts';

import { eventChoiceApi } from '../../events/api/eventChoiceApi';
import {
  unwrapChoiceResultFromEventChoiceResponse,
  unwrapNpcDialogueFromEventChoiceResponse,
  unwrapRunFromEventChoiceResponse,
  type CurrentEventChoiceResultDto,
} from '../../events/types/eventTypes';

import {
  unwrapInterludeResponse,
  type InterludeDto,
} from '../../interlude/interludeTypes';

export const demoPlayerId = '00000000-0000-0000-0000-000000000001';

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

  const currentRun = shallowRef<RunDto | null>(null);
  const pendingRewardOffer = ref<RewardOfferDto | null>(null);
  const lastOutcome = ref<ResolveCurrentEventResponse['outcome'] | null>(null);
  const npcDialogue = ref<NpcDialogueViewDto | null>(null);
  const npcDialogueEchoes = ref<NarrativeFragmentDto[]>([]);
  const npcDialogueEnded = ref(false);
  const activeCombat = ref<CombatInstanceDto | null>(null);
  const combatRuntime = shallowRef<CombatRuntimeDto | null>(null);
  const previewedNodeId = ref<string | null>(null);
  const lastChoiceResult = ref<CurrentEventChoiceResultDto | null>(null);

  const currentInterlude = ref<InterludeDto | null>(null);
  const isEnteringInterlude = ref(false);
  const isEnteringNextRoom = ref(false);

  type ReputationEffect = { id: number; amount: number; npcName: string };
  const reputationEffects = ref<ReputationEffect[]>([]);
  let reputationEffectSeq = 0;

  const permanentItemCandidates = ref<PermanentItemCandidateDto[]>([]);
  const isPermanentItemSelectionResolved = ref(false);
  const isLoadingPermanentItemCandidates = ref(false);

  const resumableRun = shallowRef<ResumableRunDto | null>(null);
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

  const shouldShowRunFailedPanel = computed(() =>
    currentRun.value?.status === 'Failed',
  );

  const shouldShowCombatScene = computed(() =>
    Boolean(currentRun.value?.activeCombatId),
  );

  const shouldShowRewardPanel = computed(() =>
    Boolean(pendingRewardOffer.value || currentRun.value?.pendingRewardOfferId),
  );

  const shouldShowRunMap = computed(() =>
    Boolean(currentRun.value) &&
    !shouldShowRunFailedPanel.value &&
    !shouldShowCombatScene.value &&
    !shouldShowRewardPanel.value,
  );

  const gameplayPhase = computed(() => {
    if (!currentRun.value) return 'Loading';

    if (shouldShowRunFailedPanel.value || currentRun.value.status === 'Completed') {
      if (permanentItemCandidates.value.length > 0 && !isPermanentItemSelectionResolved.value) {
        return 'ItemSelection';
      }
      return 'Completed';
    }

    if (currentRun.value.status === 'Suspended') {
      return 'Suspended';
    }

    if (shouldShowCombatScene.value) {
      return 'Combat';
    }

    if (shouldShowRewardPanel.value) {
      return 'Reward';
    }

    if (currentInterlude.value || currentRun.value.status === 'Interlude') {
      return 'Interlude';
    }

    if (isRoomCleared.value) {
      return 'RoomCleared';
    }

    if (npcDialogue.value || npcDialogueEnded.value) return 'NpcDialogue';
    if (lastOutcome.value?.primaryEventType === 'Npc') return 'NpcDialogue';
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

  function resetNpcDialogue() {
    npcDialogue.value = null;
    npcDialogueEchoes.value = [];
    npcDialogueEnded.value = false;
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
      await refreshPermanentItemCandidatesIfNeeded();
    } catch (caught) {
      error.value = caught instanceof Error
        ? caught.message
        : 'Une erreur inconnue est survenue.';
    } finally {
      isLoading.value = false;
    }
  }

  // Fetches permanent-item candidates once a run has actually ended (Completed/Failed).
  // Guarded so it only fires the network call the first time a terminal state is seen —
  // every subsequent execute() call after that is a cheap no-op.
  async function refreshPermanentItemCandidatesIfNeeded() {
    if (!currentRun.value) return;
    if (isPermanentItemSelectionResolved.value) return;
    if (permanentItemCandidates.value.length > 0) return;

    const status = currentRun.value.status;
    if (status !== 'Completed' && status !== 'Failed') return;

    isLoadingPermanentItemCandidates.value = true;
    try {
      const response = await runApi.getPermanentItemCandidates(currentRun.value.id);
      permanentItemCandidates.value = response.candidates;
      if (response.candidates.length === 0) {
        isPermanentItemSelectionResolved.value = true;
      }
    } finally {
      isLoadingPermanentItemCandidates.value = false;
    }
  }

  async function confirmPermanentItemSelection(itemDefinitionKeys: string[]) {
    if (!currentRun.value) return;

    await execute(async () => {
      await runApi.confirmPermanentItemSelection(currentRun.value!.id, itemDefinitionKeys);
      isPermanentItemSelectionResolved.value = true;
    });
  }

  async function removePalaceLaw(lawKey: string) {
    if (!currentRun.value) return;

    await execute(async () => {
      const response = await runApi.removePalaceLaw(currentRun.value!.id, lawKey);
      currentRun.value = unwrapRunResponse(response);
    });
  }

  async function useCaliceInfini(targetCombatantId?: string | null) {
    if (!currentRun.value) return;

    await execute(async () => {
      const response = await runApi.useCaliceInfini(currentRun.value!.id, targetCombatantId);
      currentRun.value = unwrapRunResponse(response);
    });
  }

  function pushReputationEffects(
    effects?: { kind: string; amount: number; label: string }[] | null,
  ) {
    if (!effects) return;
    for (const effect of effects) {
      if (effect.kind !== 'reputation' || effect.amount === 0) continue;
      reputationEffectSeq += 1;
      reputationEffects.value.push({ id: reputationEffectSeq, amount: effect.amount, npcName: effect.label });
    }
  }

  function dismissReputationEffect(id: number) {
    reputationEffects.value = reputationEffects.value.filter((e) => e.id !== id);
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
      resetNpcDialogue();
      activeCombat.value = null;
      combatRuntime.value = null;
      currentInterlude.value = null;
      resetPreviewedNode();
      permanentItemCandidates.value = [];
      isPermanentItemSelectionResolved.value = false;

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
      resetNpcDialogue();
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
      npcDialogue.value = resolveResponse.npcDialogue ?? null;
      npcDialogueEchoes.value = [];
      npcDialogueEnded.value = false;
      activeCombat.value = resolveResponse.startedCombat ?? null;
      combatRuntime.value = resolveResponse.combat ?? null;

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
      npcDialogue.value = response.npcDialogue ?? null;
      npcDialogueEchoes.value = [];
      npcDialogueEnded.value = false;
      activeCombat.value = response.startedCombat ?? null;
      combatRuntime.value = response.combat ?? null;
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
      resetNpcDialogue();
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
      resetNpcDialogue();
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
      combatRuntime.value = null;
      lastOutcome.value = null;
      resetPreviewedNode();

      await refreshPendingRewardIfNeeded();
      await progressRunInlineIfReady();
    });
  }

  async function handleCombatFailed() {
    if (!currentRun.value) return;

    await execute(async () => {
      const response = await runApi.getRun(currentRun.value!.id);
      const run = unwrapRunResponse(response);

      lastChoiceResult.value = null;
      currentRun.value = run;
      activeCombat.value = null;
      combatRuntime.value = null;
      lastOutcome.value = null;
      pendingRewardOffer.value = null;
      resetPreviewedNode();
    });
  }

  // -------------------------------------------------------------------------
  // Rewards
  // -------------------------------------------------------------------------

  async function selectReward(optionId: string) {
    if (!currentRun.value || !pendingRewardOffer.value) return;

    await execute(async () => {
      const selectResponse = await rewardApi.selectReward(currentRun.value!.id, {
        choiceId: optionId,
      });

      pendingRewardOffer.value = null;

      const runFromResponse = unwrapRunFromSelectRewardResponse(selectResponse);
      if (runFromResponse) {
        currentRun.value = runFromResponse;
      } else {
        const runResponse = await runApi.getRun(currentRun.value!.id);
        currentRun.value = unwrapRunResponse(runResponse);
      }

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
      resetNpcDialogue();
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
    resetNpcDialogue();
    activeCombat.value = null;
    combatRuntime.value = null;
    previewedNodeId.value = null;
    lastChoiceResult.value = null;
    currentInterlude.value = null;
    error.value = null;
    permanentItemCandidates.value = [];
    isPermanentItemSelectionResolved.value = false;
    reputationEffects.value = [];
  }

  // -------------------------------------------------------------------------
  // Event choices / outcome
  // -------------------------------------------------------------------------

  async function continueAfterOutcome() {
    if (!lastOutcome.value) return;

    // Him'Lit (FinalBoss) starts combat in the SAME resolveCurrentEvent response as
    // his taunt lines — unlike every other node type, where narrative and combat are
    // two separate steps. When that's the case, "Continue" just needs to dismiss the
    // narrative outcome to reveal the already-active combat; calling progressRun()
    // here would hit a command that assumes no combat is running yet and does
    // nothing, leaving the button appearing broken.
    if (currentRun.value?.activeCombatId) {
      lastOutcome.value = null;
      return;
    }

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
      pushReputationEffects(choiceResult?.appliedEffects);

      await refreshPendingRewardIfNeeded();
    });
  }

  // -------------------------------------------------------------------------
  // NPC dialogue
  // -------------------------------------------------------------------------

  async function selectNpcDialogueChoice(choiceId: string) {
    if (!currentRun.value) return;

    await execute(async () => {
      const response = await eventChoiceApi.chooseCurrentEventOption(
        currentRun.value!.id,
        { choiceId, optionId: choiceId, eventChoiceId: choiceId },
      );

      const run = unwrapRunFromEventChoiceResponse(response);
      const choiceResult = unwrapChoiceResultFromEventChoiceResponse(response);
      const dialogue = unwrapNpcDialogueFromEventChoiceResponse(response);

      if (run) {
        currentRun.value = run;
      } else {
        const runResponse = await runApi.getRun(currentRun.value!.id);
        currentRun.value = unwrapRunResponse(runResponse);
      }

      npcDialogueEchoes.value = choiceResult?.narrativeFragments ?? [];
      pushReputationEffects(choiceResult?.appliedEffects);

      if (dialogue) {
        npcDialogue.value = dialogue;
        npcDialogueEnded.value = false;
      } else {
        npcDialogue.value = null;
        npcDialogueEnded.value = true;
      }

      await refreshPendingRewardIfNeeded();
    });
  }

  async function continueAfterNpcDialogue() {
    resetNpcDialogue();
    await progressRun();
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
    npcDialogue,
    npcDialogueEchoes,
    npcDialogueEnded,
    activeCombat,
    combatRuntime,
    pendingRewardOffer,
    currentInterlude,
    isEnteringInterlude,
    isEnteringNextRoom,
    isRoomCleared,
    shouldShowCombatScene,
    shouldShowRewardPanel,
    shouldShowRunMap,
    shouldShowRunFailedPanel,
    gameplayPhase,
    isLoading,
    error,
    permanentItemCandidates,
    isPermanentItemSelectionResolved,
    isLoadingPermanentItemCandidates,
    reputationEffects,
    dismissReputationEffect,

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
    handleCombatFailed,
    refreshPendingRewardIfNeeded,
    continueAfterOutcome,
    continueAfterChoiceResult,
    selectCurrentEventChoice,
    selectNpcDialogueChoice,
    continueAfterNpcDialogue,
    enterInterlude,
    loadInterlude,
    enterNextRoom,
    confirmPermanentItemSelection,
    removePalaceLaw,
    useCaliceInfini,

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
    resetPreviewedNode,
  };
});