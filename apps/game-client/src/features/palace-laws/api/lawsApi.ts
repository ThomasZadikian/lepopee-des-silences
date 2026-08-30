import { gameEngineApi } from '../../../shared/api/gameEngineApi';

import type { PalaceLawDefinitionView } from '../types/lawTypes';

export const lawsApi = {
  listActive() {
    return gameEngineApi.get<{ laws: PalaceLawDefinitionView[] }>('/api/v2/laws');
  },
};
