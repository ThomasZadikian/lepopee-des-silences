import { gameEngineApi } from '../../../shared/api/gameEngineApi';
import type { GetRunInventoryResponse, UseGrimoireResponse, UseRunItemResponse } from '../types/inventoryTypes';

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

  readGrimoire(runId: string, itemId: string, characterId: string) {
    return gameEngineApi.post<UseGrimoireResponse>(
      `/api/v2/runs/${runId}/inventory/${itemId}/read-grimoire`,
      { characterId },
    );
  },
};
