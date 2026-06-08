import { gameEngineApi } from '../../../shared/api/gameEngineApi';

import type {
  ChooseNodeResponse,
  GenerateNextNodesResponse,
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
};
