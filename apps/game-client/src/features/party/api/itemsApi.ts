import { gameEngineApi } from '../../../shared/api/gameEngineApi';

import type { ItemDefinitionView } from '../types/itemTypes';

export const itemsApi = {
  listActive() {
    return gameEngineApi.get<{ items: ItemDefinitionView[] }>('/api/v2/items');
  },
};
