import { gameEngineApi } from '../../../shared/api/gameEnginApi';

import type {
  PendingRewardOfferResponse,
  SelectRewardRequest,
  SelectRewardResponse,
} from '../types/rewardTypes';

export const rewardApi = {
  getPendingReward(runId: string) {
    return gameEngineApi.get<PendingRewardOfferResponse>(
      `/api/v2/runs/${runId}/rewards/pending`,
    );
  },

  selectReward(runId: string, body: SelectRewardRequest) {
    return gameEngineApi.post<SelectRewardResponse, SelectRewardRequest>(
      `/api/v2/runs/${runId}/rewards/select`,
      body,
    );
  },
};