import { gameEngineApi } from '../../../shared/api/gameEngineApi';
import type { GetRunInventoryResponse, UseRunItemResponse } from '../types/inventoryTypes';

export const inventoryApi = {
  getRunInventory(runId: string) {
    return gameEngineApi.get<GetRunInventoryResponse>(
      `/api/v2/runs/${runId}/inventory`,
    );
  },

  useItem(runId: string, itemId: string) {
    return gameEngineApi.post<UseRunItemResponse>(
      `/api/v2/runs/${runId}/inventory/${itemId}/use`,
    );
  },
};