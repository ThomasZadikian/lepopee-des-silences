import { gameEngineApi } from '../../../shared/api/gameEngineApi';

import type { PalaceProgressView, PlayerProfileView } from '../types/playerTypes';

export const playerApi = {
  getProfile(playerId: string) {
    return gameEngineApi.get<PlayerProfileView>(`/api/v2/players/${playerId}/profile`);
  },

  getPalaceProgress(playerId: string) {
    return gameEngineApi.get<PalaceProgressView>(`/api/v2/players/${playerId}/palace-progress`);
  },

  equipSkill(playerId: string, characterId: string, skillKey: string) {
    return gameEngineApi.post<PlayerProfileView>(
      `/api/v2/players/${playerId}/characters/${characterId}/skills/${skillKey}/equip`,
    );
  },

  unequipSkill(playerId: string, characterId: string, skillKey: string) {
    return gameEngineApi.post<PlayerProfileView>(
      `/api/v2/players/${playerId}/characters/${characterId}/skills/${skillKey}/unequip`,
    );
  },

  equipItem(playerId: string, characterId: string, itemKey: string) {
    return gameEngineApi.post<PlayerProfileView>(
      `/api/v2/players/${playerId}/characters/${characterId}/items/${itemKey}/equip`,
    );
  },

  unequipItem(playerId: string, characterId: string, itemKey: string) {
    return gameEngineApi.post<PlayerProfileView>(
      `/api/v2/players/${playerId}/characters/${characterId}/items/${itemKey}/unequip`,
    );
  },

};
