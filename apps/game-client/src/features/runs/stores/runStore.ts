import { defineStore } from 'pinia';
import { computed, ref, shallowRef } from 'vue';
import { rewardApi } from '../../rewards/api/rewardApi';
import {
  unwrapRewardOffer,
  unwrapRunFromSelectRewardResponse,
  type RewardOfferDto,
} from '../../rewards/types/rewardTypes';

import { runApi } from '../api/runApi';
import { useTacticalCombatStore } from '../../combat/stores/useTacticalCombatStore';
import { combatApi } from '../../combat/api/combatApi';
import { partyWalkDurationMs } from '../../palace-map/composables/usePartyTokenPath';
import {
  unwrapRunResponse,
  type NarrativeFragmentDto,
  type NpcDialogueViewDto,
  type PermanentItemCandidateDto,
  type ResolveCurrentEventResponse,
  type ResumableRunDto,
  type RunDto,
} from '../types/runTypes';

import { eventChoiceApi } from '../../events/api/eventChoiceApi';
import {
  unwrapChoiceResultFromEventChoiceResponse,
  unwrapNpcDialogueFromEventChoiceResponse,
  unwrapRunFromEventChoiceResponse,
  type CurrentEventChoiceResultDto,
} from '../../events/types/eventTypes';

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

/** Resolves after `ms`, and immediately when there is nothing to wait for. */
function delay(ms: number): Promise<void> {
  return ms > 0 ? new Promise((resolve) => setTimeout(resolve, ms)) : Promise.resolve();
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
  const lastChoiceResult = ref<CurrentEventChoiceResultDto | null>(null);

  const isConfirmingRoomExit = ref(false);

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
  const groundPickupNotice = ref<string | null>(null);
  const pendingGroundPickupIds = ref<string[]>([]);

  const isLoading = ref(false);
  const error = ref<string | null>(null);

  // -------------------------------------------------------------------------
  // Computed
  // -------------------------------------------------------------------------

  const currentRoom = computed(() => currentRun.value?.currentRoom ?? null);

  /** Free-roam grid overlay of the current room. */
  const currentGrid = computed(() => currentRoom.value?.grid ?? null);

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

    if (npcDialogue.value || npcDialogueEnded.value) return 'NpcDialogue';
    if (lastOutcome.value?.primaryEventType === 'Npc') return 'NpcDialogue';
    if (lastChoiceResult.value) return 'EventChoiceResult';
    if (lastOutcome.value) return 'EventOutcome';

    return 'Map';
  });

  // -------------------------------------------------------------------------
  // Helpers
  // -------------------------------------------------------------------------

  function resetNpcDialogue() {
    npcDialogue.value = null;
    npcDialogueEchoes.value = [];
    npcDialogueEnded.value = false;
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

  /**
   * Grants ONE run-found item as a permanent keepsake right away, outside of the end-of-run
   * selection ceremony — the shortcut behind "équiper un objet trouvé cette run" (Besace calls
   * this right before equipItem, since equipping has always required permanent ownership on
   * the server). Deliberately does NOT touch `isPermanentItemSelectionResolved`: unlike
   * `confirmPermanentItemSelection` below, this can fire many times over the course of a run and
   * must never short-circuit the real end-of-run screen for every OTHER eligible item found
   * later. Same underlying endpoint, same idempotent domain rule (no-op if already owned).
   */
  async function grantPermanentItem(itemDefinitionKey: string) {
    if (!currentRun.value) return;

    await execute(async () => {
      await runApi.confirmPermanentItemSelection(currentRun.value!.id, [itemDefinitionKey]);
    });
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

  async function wagerNode(nodeId: string) {
    if (!currentRun.value) return;

    await execute(async () => {
      const response = await runApi.wagerNode(currentRun.value!.id, nodeId);
      currentRun.value = unwrapRunResponse(response);
    });
  }

  async function setRoomRiskTier(tier: string) {
    if (!currentRun.value) return;

    await execute(async () => {
      const response = await runApi.setRoomRiskTier(currentRun.value!.id, tier);
      currentRun.value = unwrapRunResponse(response);
    });
  }

  // -------------------------------------------------------------------------
  // Tactical grid exploration
  // -------------------------------------------------------------------------

  async function movePartyTo(x: number, y: number, plannedSteps = 0) {
    if (!currentRun.value) return;

    await execute(async () => {
      const before = currentRun.value!.currentRoom?.grid;
      const from = { x: before?.partyX ?? 0, y: before?.partyY ?? 0 };

      const response = await runApi.moveParty(currentRun.value!.id, x, y);
      currentRun.value = unwrapRunResponse(response);
      pendingGroundPickupIds.value = response.blockedItemIds;
      groundPickupNotice.value = response.blockedItemIds.length > 0
        ? 'Besace pleine : choisissez l’objet à conserver.'
        : response.collectedItemIds.length > 0
          ? `${response.collectedItemIds.length} objet${response.collectedItemIds.length > 1 ? 's' : ''} ramassé${response.collectedItemIds.length > 1 ? 's' : ''}.`
          : null;

      // Walking onto a contact node selects it server-side mid-move (an ambush does not wait
      // to be clicked), which leaves the room in NodeSelected exactly like enterGridNode does.
      // Without resolving it here the event never fires and the next move is refused by the
      // domain guard — the room would simply appear stuck.
      if (currentRun.value?.currentRoom?.state === 'NodeSelected') {
        // Let the party actually walk there first. Resolving immediately swaps the board out
        // for the event screen before the token has moved a single frame, so an ambush read as
        // a teleport onto the thing that sprang it — losing the approach, which is the only
        // part of a contact node the player gets to watch.
        // Step count from the board when it has one: a route that detours around a wall is
        // longer than the straight-line distance, and waiting only the latter would fire the
        // event while the party is still walking.
        const after = currentRun.value.currentRoom.grid;
        await delay(plannedSteps > 0
          ? partyWalkDurationMs(0, 0, plannedSteps, 0)
          : partyWalkDurationMs(from.x, from.y, after?.partyX ?? from.x, after?.partyY ?? from.y));
      }

      await resolveSelectedNodeIfAny();
    });
  }

  async function swapGroundItem(groundItemId: string, heldItemId: string) {
    if (!currentRun.value) return;

    await execute(async () => {
      const response = await runApi.swapGroundItem(
        currentRun.value!.id,
        groundItemId,
        heldItemId,
      );
      currentRun.value = unwrapRunResponse(response);
      pendingGroundPickupIds.value = pendingGroundPickupIds.value
        .filter((itemId) => itemId !== groundItemId);
      groundPickupNotice.value = pendingGroundPickupIds.value.length > 0
        ? 'Besace pleine : choisissez l’objet à conserver.'
        : 'Objet échangé. L’objet écarté reste au sol.';
    });
  }

  function keepInventoryForGroundItem(groundItemId: string) {
    pendingGroundPickupIds.value = pendingGroundPickupIds.value
      .filter((itemId) => itemId !== groundItemId);
    groundPickupNotice.value = pendingGroundPickupIds.value.length > 0
      ? 'Besace pleine : choisissez l’objet à conserver.'
      : 'Objet laissé au sol.';
  }

  /** Turns a server-side node selection into an actual outcome. Shared by every path that can
   * leave the room in NodeSelected: entering deliberately, challenging the boss remotely, or
   * stepping onto a node that triggers on contact. */
  async function resolveSelectedNodeIfAny() {
    if (currentRun.value?.currentRoom?.state !== 'NodeSelected') return;

    const resolveResponse = await runApi.resolveCurrentEvent(currentRun.value.id);
    currentRun.value = resolveResponse.run;
    lastOutcome.value = resolveResponse.outcome;
    npcDialogue.value = resolveResponse.npcDialogue ?? null;
    npcDialogueEchoes.value = [];
    npcDialogueEnded.value = false;
    if (resolveResponse.tacticalCombat) {
      const tactical = useTacticalCombatStore();
      tactical.setCombat(resolveResponse.tacticalCombat);

      // Quand la créature la plus rapide ouvre le bal, le serveur a déjà joué son tour : il
      // reste à le montrer, sans quoi le joueur reprend la main devant un plateau qui a bougé
      // sans lui.
      void tactical.playOpening(
        resolveResponse.tacticalEvents ?? [], resolveResponse.tacticalCombat);
    }

    await refreshPendingRewardIfNeeded();
  }

  /** Searches around the party for hidden nodes, at the cost of movement budget. Optimistic
   * like every other grid action: the domain rejects it (nothing to find, not enough budget)
   * and the message surfaces through `error`. */
  async function searchParty() {
    if (!currentRun.value) return;

    await execute(async () => {
      const response = await runApi.searchParty(currentRun.value!.id);
      currentRun.value = unwrapRunResponse(response);
    });
  }

  // Both enterGridNode and challengeBossRemotely only *select* the node server-side —
  // resolving it into an outcome is a separate step, so we chain the resolve call
  // immediately after. Without this, the room stays in NodeSelected forever and the
  // next move/action is rejected by the domain guard ("Room is not waiting for party movement.").
  async function enterGridNode(nodeId: string) {
    if (!currentRun.value) return;

    await execute(async () => {
      const enterResponse = await runApi.enterGridNode(currentRun.value!.id, nodeId);
      currentRun.value = unwrapRunResponse(enterResponse);

      await resolveSelectedNodeIfAny();
    });
  }

  async function challengeBossRemotely() {
    if (!currentRun.value) return;

    await execute(async () => {
      const challengeResponse = await runApi.challengeBossRemotely(currentRun.value!.id);
      currentRun.value = unwrapRunResponse(challengeResponse);

      const resolveResponse = await runApi.resolveCurrentEvent(currentRun.value!.id);
      currentRun.value = resolveResponse.run;
      lastOutcome.value = resolveResponse.outcome;
      npcDialogue.value = resolveResponse.npcDialogue ?? null;
      npcDialogueEchoes.value = [];
      npcDialogueEnded.value = false;
      if (resolveResponse.tacticalCombat) {
        const tactical = useTacticalCombatStore();
        tactical.setCombat(resolveResponse.tacticalCombat);
        void tactical.playOpening(
          resolveResponse.tacticalEvents ?? [], resolveResponse.tacticalCombat);
      }

      await refreshPendingRewardIfNeeded();
    });
  }

  async function useCaliceInfini(targetCombatantId?: string | null) {
    if (!currentRun.value) return;

    await execute(async () => {
      const response = await runApi.useCaliceInfini(currentRun.value!.id, targetCombatantId);
      currentRun.value = unwrapRunResponse(response);
    });
  }

  /**
   * Grimoire "Valider les choix": re-applies the freshly-equipped skill selection to
   * the active Run's live combat state (protagonist + companions), so the change is
   * felt in the next combat instead of only on the next Run. No-op outside a Run.
   */
  async function syncPartySkills() {
    if (!currentRun.value) return;

    await execute(async () => {
      const response = await runApi.syncPartySkills(currentRun.value!.id);
      currentRun.value = unwrapRunResponse(response);
    });
  }

  /**
   * Stat-point "Valider les choix": re-applies the freshly-allocated stat points to
   * the active Run's live combat state (protagonist + companions), so the change is
   * felt in the next combat instead of only on the next Run. No-op outside a Run.
   */
  async function syncPartyStats() {
    if (!currentRun.value) return;

    await execute(async () => {
      const response = await runApi.syncPartyStats(currentRun.value!.id);
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
  async function progressRunInlineIfReady() {
    if (!currentRun.value) return;

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
      useTacticalCombatStore().clearCombat();
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
      resetNpcDialogue();

      const tactical = useTacticalCombatStore();
      if (run.activeCombatId) {
        tactical.setCombat(await combatApi.getCurrentTacticalCombat(run.id));
      } else {
        tactical.clearCombat();
      }

      await refreshPendingRewardIfNeeded();
    });
  }

  // -------------------------------------------------------------------------
  // Map / node actions
  // -------------------------------------------------------------------------

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
      useTacticalCombatStore().clearCombat();
      lastOutcome.value = null;

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
      useTacticalCombatStore().clearCombat();
      lastOutcome.value = null;
      pendingRewardOffer.value = null;
    });
  }

  async function handleCombatEscaped() {
    if (!currentRun.value) return;

    await execute(async () => {
      const response = await runApi.getRun(currentRun.value!.id);
      currentRun.value = unwrapRunResponse(response);
      useTacticalCombatStore().clearCombat();
      lastOutcome.value = {
        nodeId: '',
        eventTypes: ['Combat'],
        primaryEventType: 'Combat',
        title: 'Fuite réussie',
        description: 'L’équipe évacue la salle. L’événement est résolu sans récompense.',
        resolutionKind: 'Escaped',
        riskLevel: 0,
        rewardProfile: 'none',
        requiresPlayerChoice: false,
      };
      pendingRewardOffer.value = null;
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

      await refreshPendingRewardIfNeeded();

      // Pour les rewards non-boss, progresser normalement.
      await progressRunInlineIfReady();
    });
  }

  // -------------------------------------------------------------------------
  // Room exit
  // -------------------------------------------------------------------------

  /** "Valider la sortie" — walks the party through a confirmed room exit. No boss dependency. */
  async function confirmRoomExit(nodeId: string) {
    if (!currentRun.value) return;

    isConfirmingRoomExit.value = true;
    error.value = null;

    try {
      const response = await runApi.confirmRoomExit(currentRun.value.id, nodeId);
      const run = unwrapRunResponse(response);

      currentRun.value = run;
      lastOutcome.value = null;
      resetNpcDialogue();
      lastChoiceResult.value = null;
    } catch (caught) {
      error.value = caught instanceof Error
        ? caught.message
        : 'Impossible de franchir cette sortie.';
    } finally {
      isConfirmingRoomExit.value = false;
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
    useTacticalCombatStore().clearCombat();
    lastChoiceResult.value = null;
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
    currentGrid,
    lastOutcome,
    lastChoiceResult,
    npcDialogue,
    npcDialogueEchoes,
    npcDialogueEnded,
    pendingRewardOffer,
    isConfirmingRoomExit,
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
    progressRun,
    generateNextNodes,
    loadPendingReward,
    selectReward,
    handleCombatCompleted,
    handleCombatFailed,
    handleCombatEscaped,
    refreshPendingRewardIfNeeded,
    continueAfterOutcome,
    continueAfterChoiceResult,
    selectCurrentEventChoice,
    selectNpcDialogueChoice,
    continueAfterNpcDialogue,
    confirmRoomExit,
    confirmPermanentItemSelection,
    grantPermanentItem,
    removePalaceLaw,
    wagerNode,
    setRoomRiskTier,
    movePartyTo,
    swapGroundItem,
    keepInventoryForGroundItem,
    searchParty,
    enterGridNode,
    challengeBossRemotely,
    useCaliceInfini,
    syncPartySkills,
    syncPartyStats,

    resumableRun,
    isLoadingResumableRun,
    isSavingAndExiting,
    isExitingMidRoom,
    isAbandoningRun,
    runActionError,
    groundPickupNotice,
    pendingGroundPickupIds,
    loadResumableRun,
    saveAndExitCurrentRun,
    exitMidRoom,
    abandonCurrentRun,
    clearCurrentRun,
  };
});
