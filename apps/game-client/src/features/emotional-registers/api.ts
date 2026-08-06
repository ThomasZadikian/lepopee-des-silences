import { gameEngineApi } from '../../shared/api/gameEngineApi';
import type { EmotionalRegisterCatalog } from './types';

export const emotionalRegistersApi = {
  getCatalog() {
    return gameEngineApi.get<EmotionalRegisterCatalog>('/api/v2/emotional-registers');
  },
};
