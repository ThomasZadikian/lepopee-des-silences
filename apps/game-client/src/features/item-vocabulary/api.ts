import { gameEngineApi } from '../../shared/api/gameEngineApi';
import type { ItemEffectTypeCatalog, ItemRarityCatalog, ItemTypeCatalog } from './types';

export const itemVocabularyApi = {
  getItemTypes() {
    return gameEngineApi.get<ItemTypeCatalog>('/api/v2/item-types');
  },
  getItemRarities() {
    return gameEngineApi.get<ItemRarityCatalog>('/api/v2/item-rarities');
  },
  getItemEffectTypes() {
    return gameEngineApi.get<ItemEffectTypeCatalog>('/api/v2/item-effect-types');
  },
};
