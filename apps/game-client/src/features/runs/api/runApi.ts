import { gameEngineApi } from '../../../shared/api/gameEngineApi';
// Si tu as renommé le fichier correctement, utilise plutôt :
// import { gameEngineApi } from '../../../shared/api/gameEngineApi';

import type {
  ChooseNodeResponse,
  GenerateNextNodesResponse,
  ResolveCurrentEventResponse,
  RunResponse,
  StartRunResponse,
} from '../types/runTypes';

export const runApi = {
  startRun(playerId: string) {
    return gameEngineApi.post<StartRunResponse, { playerId: string }>(
      '/api/v2/runs',
      {
        playerId,
      },
    );
  },

  getRun(runId: string) {
    return gameEngineApi.get<RunResponse>(
      `/api/v2/runs/${runId}`,
    );
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
};