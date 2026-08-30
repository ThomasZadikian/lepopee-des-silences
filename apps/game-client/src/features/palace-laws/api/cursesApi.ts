import { gameEngineApi } from '../../../shared/api/gameEngineApi';

import type { CurseDefinitionView } from '../types/curseTypes';

export const cursesApi = {
  listAvailable() {
    return gameEngineApi.get<{ curses: CurseDefinitionView[] }>('/api/v2/curses');
  },
};
