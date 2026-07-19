import { gameEngineApi } from '../../../shared/api/gameEngineApi';

import type {
  ChooseNodeResponse,
  ConfirmPermanentItemSelectionResponse,
  GenerateNextNodesResponse,
  GetPermanentItemCandidatesResponse,
  ResolveCurrentEventResponse,
  RunResponse,
  StartRunResponse,
} from '../types/runTypes';

import type {
  EnterInterludeApiResponse,
  GetInterludeApiResponse,
} from '../../interlude/interludeTypes';

export const runApi = {
  startRun(playerId: string) {
    return gameEngineApi.post<StartRunResponse, { playerId: string }>(
      '/api/v2/runs',
      { playerId },
    );
  },

  getRun(runId: string) {
    return gameEngineApi.get<RunResponse>(`/api/v2/runs/${runId}`);
  },

  chooseNode(runId: string, nodeId: string) {
    return gameEngineApi.post<ChooseNodeResponse>(
      `/api/v2/runs/${runId}/nodes/${nodeId}/choose`,
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

  enterInterlude(runId: string) {
    return gameEngineApi.post<EnterInterludeApiResponse>(
      `/api/v2/runs/${runId}/interlude/enter`,
    );
  },

  getInterlude(runId: string) {
    return gameEngineApi.get<GetInterludeApiResponse>(
      `/api/v2/runs/${runId}/interlude`,
    );
  },

  enterNextRoom(runId: string) {
    return gameEngineApi.post<RunResponse>(
      `/api/v2/runs/${runId}/rooms/next`,
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
