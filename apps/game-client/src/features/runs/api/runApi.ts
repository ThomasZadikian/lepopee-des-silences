import { gameEngineApi } from '../../../shared/api/gameEnginApi';
import type {
    ChooseNodeResponse,
    GenerateNextNodesResponse,
    ResolveCurrentEventResponse,
    RunDto,
    StartRunResponse,
} from '../types/runTypes';

export const runApi = {
  startRun(playerId: string) {
    return gameEngineApi.post<StartRunResponse>('/api/v2/runs', {
      playerId,
    });
  },

  getRun(runId: string) {
    return gameEngineApi.get<RunDto>(`/api/v2/runs/${runId}`);
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

  generateNextNodes(runId: string) {
    return gameEngineApi.post<GenerateNextNodesResponse>(
      `/api/v2/runs/${runId}/nodes/next`,
    );
  },
};