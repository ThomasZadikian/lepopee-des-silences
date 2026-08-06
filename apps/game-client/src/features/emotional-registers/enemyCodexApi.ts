import { gameEngineApi } from '../../shared/api/gameEngineApi';

export type BossCodexEntry = {
  key: string;
  displayName: string;
  description: string;
  emotionalRegister: string;
  compatibleRoomTypes: string[];
  threat: number;
};

export const enemyCodexApi = {
  listBosses() {
    return gameEngineApi.get<{ bosses: BossCodexEntry[] }>('/api/v2/enemy-codex/bosses');
  },
};
