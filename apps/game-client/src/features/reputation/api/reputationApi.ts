import { gameEngineApi } from '../../../shared/api/gameEngineApi';
import type { GetRunReputationResponse } from '../types/reputationTypes';

export const reputationApi = {
  getRunReputation(runId: string) {
    return gameEngineApi.get<GetRunReputationResponse>(
      `/api/v2/runs/${runId}/reputation`,
    );
  },
};
