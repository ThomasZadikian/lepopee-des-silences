import { gameEngineApi } from '../../../shared/api/gameEngineApi';
import { HttpError } from '../../../shared/api/httpClient';
import type {
  CombatRuntimeDto,
  UseCombatSkillRequest,
  UseCombatSkillResponse,
  UseItemInCombatResponse,
} from '../types/combatContracts';

export const combatApi = {
  async getCurrentCombat(runId: string): Promise<CombatRuntimeDto | null> {
    try {
      return await gameEngineApi.get<CombatRuntimeDto>(
        `/api/v2/runs/${runId}/current-combat`,
      );
    } catch (error) {
      if (error instanceof HttpError && error.status === 404) {
        return null;
      }
      throw error;
    }
  },

  useSkillAction(runId: string, combatId: string, body: UseCombatSkillRequest) {
    return gameEngineApi.post<UseCombatSkillResponse, UseCombatSkillRequest>(
      `/api/v2/runs/${runId}/combats/${combatId}/skill-actions`,
      body,
    );
  },

    advanceCombat(runId: string, combatId: string) {
    return gameEngineApi.post<UseCombatSkillResponse>(
      `/api/v2/runs/${runId}/combats/${combatId}/advance`,
      {},
    );
  },

  useItemAction(
    runId: string,
    combatId: string,
    body: { itemId: string; targetIds: string[] },
  ) {
    return gameEngineApi.post<UseItemInCombatResponse>(
      `/api/v2/runs/${runId}/combats/${combatId}/item-actions`,
      body,
    );
  },
};
