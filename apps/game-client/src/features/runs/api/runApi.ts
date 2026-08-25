import { gameEngineApi } from '../../../shared/api/gameEngineApi';

import type {
  ActorAdvanceMode,
  AdvanceRoomActorsResponse,
  ConfirmPermanentItemSelectionResponse,
  GenerateNextNodesResponse,
  GetOpenRunResponse,
  GetPermanentItemCandidatesResponse,
  InteractWithRoomNpcResponse,
  MovePartyResponse,
  ResolveCurrentEventResponse,
  RunResponse,
  StartRunResponse,
  SwapGroundItemResponse,
} from '../types/runTypes';

export const runApi = {
  /** Lance une run avec le système de combat tactique. */
  startRun(playerId: string, difficultyLevel?: number) {
    return gameEngineApi.post<StartRunResponse, { playerId: string; difficultyLevel?: number }>(
      '/api/v2/runs',
      { playerId, ...(difficultyLevel === undefined ? {} : { difficultyLevel }) },
    );
  },

  getRun(runId: string) {
    return gameEngineApi.get<RunResponse>(`/api/v2/runs/${runId}`);
  },

  getOpenRun(playerId: string) {
    return gameEngineApi.get<GetOpenRunResponse>(
      `/api/v2/runs/open?playerId=${encodeURIComponent(playerId)}`,
    );
  },

  resolveCurrentEvent(runId: string) {
    return gameEngineApi.post<ResolveCurrentEventResponse>(
      `/api/v2/runs/${runId}/current-event/resolve`,
    );
  },

  progressRun(runId: string) {
    return gameEngineApi.post<GenerateNextNodesResponse>(
      `/api/v2/runs/${runId}/progress`,
    );
  },

  generateNextNodes(runId: string) {
    return gameEngineApi.post<GenerateNextNodesResponse>(
      `/api/v2/runs/${runId}/nodes/next`,
    );
  },

  /** "Valider la sortie" — walks the party through a confirmed room exit (see NodeDto's
   * exitDestinationRoomKey/exitDestinationDisplayName). No boss dependency anymore. */
  confirmRoomExit(runId: string, nodeId: string) {
    return gameEngineApi.post<RunResponse>(
      `/api/v2/runs/${runId}/nodes/${nodeId}/exit`,
    );
  },

  saveAndExitRun(runId: string) {
    return gameEngineApi.post<RunResponse>(
      `/api/v2/runs/${runId}/save-and-exit`,
    );
  },

  resumeRun(runId: string) {
    return gameEngineApi.post<RunResponse>(
      `/api/v2/runs/${runId}/resume`,
    );
  },

  exitMidRoom(runId: string) {
    return gameEngineApi.post<RunResponse>(
      `/api/v2/runs/${runId}/exit-mid-room`,
    );
  },

  abandonRun(runId: string) {
    return gameEngineApi.post<RunResponse>(
      `/api/v2/runs/${runId}/abandon`,
    );
  },

  getPermanentItemCandidates(runId: string) {
    return gameEngineApi.get<GetPermanentItemCandidatesResponse>(
      `/api/v2/runs/${runId}/permanent-item-candidates`,
    );
  },

  confirmPermanentItemSelection(runId: string, itemDefinitionKeys: string[]) {
    return gameEngineApi.post<ConfirmPermanentItemSelectionResponse, { itemDefinitionKeys: string[] }>(
      `/api/v2/runs/${runId}/permanent-items/confirm`,
      { itemDefinitionKeys },
    );
  },

  removePalaceLaw(runId: string, lawKey: string) {
    return gameEngineApi.post<RunResponse>(
      `/api/v2/runs/${runId}/palace-laws/${encodeURIComponent(lawKey)}/revoke`,
    );
  },

  wagerNode(runId: string, nodeId: string) {
    return gameEngineApi.post<RunResponse>(
      `/api/v2/runs/${runId}/nodes/${nodeId}/wager`,
    );
  },

  setRoomRiskTier(runId: string, tier: string) {
    return gameEngineApi.post<RunResponse, { tier: string }>(
      `/api/v2/runs/${runId}/rooms/current/risk-tier`,
      { tier },
    );
  },

  moveParty(runId: string, targetX: number, targetY: number) {
    return gameEngineApi.post<MovePartyResponse, { targetX: number; targetY: number }>(
      `/api/v2/runs/${runId}/party/move`,
      { targetX, targetY },
    );
  },

  advanceRoomActors(runId: string, mode: ActorAdvanceMode) {
    return gameEngineApi.post<AdvanceRoomActorsResponse, { mode: ActorAdvanceMode }>(
      `/api/v2/runs/${runId}/rooms/current/actors/advance`,
      { mode },
    );
  },

  interactWithRoomNpc(runId: string, roomNpcId: string) {
    return gameEngineApi.post<InteractWithRoomNpcResponse>(
      `/api/v2/runs/${runId}/rooms/current/npcs/${roomNpcId}/interact`,
    );
  },

  swapGroundItem(runId: string, groundItemId: string, heldItemId: string) {
    return gameEngineApi.post<SwapGroundItemResponse, { heldItemId: string }>(
      `/api/v2/runs/${runId}/ground-items/${groundItemId}/swap`,
      { heldItemId },
    );
  },

  /** Searches the ground around the party for hidden nodes. No body — the party can only
   * ever search where it already stands. */
  searchParty(runId: string) {
    return gameEngineApi.post<RunResponse>(`/api/v2/runs/${runId}/party/search`);
  },

  enterGridNode(runId: string, nodeId: string) {
    return gameEngineApi.post<RunResponse>(
      `/api/v2/runs/${runId}/nodes/${nodeId}/enter`,
    );
  },

  syncPartySkills(runId: string) {
    return gameEngineApi.post<RunResponse>(
      `/api/v2/runs/${runId}/sync-skills`,
    );
  },

  syncPartyStats(runId: string) {
    return gameEngineApi.post<RunResponse>(
      `/api/v2/runs/${runId}/sync-stats`,
    );
  },

  useCaliceInfini(runId: string, targetCombatantId?: string | null) {
    return gameEngineApi.post<RunResponse, { targetCombatantId?: string | null }>(
      `/api/v2/runs/${runId}/calice-infini/use`,
      { targetCombatantId: targetCombatantId ?? null },
    );
  },
};
