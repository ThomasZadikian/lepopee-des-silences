import { gameEngineApi } from '../../../shared/api/gameEngineApi';
import type { GetRunInventoryResponse } from '../types/inventoryTypes';

export const inventoryApi = {
  getRunInventory(runId: string) {
    return gameEngineApi.get<GetRunInventoryResponse>(
      `/api/v2/runs/${runId}/inventory`,
    );
  },
};
