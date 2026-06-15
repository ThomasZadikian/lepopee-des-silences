import { HttpError, httpRequest } from '../../../shared/api/httpClient';
import type {
  CombatRuntimeDto,
  UseCombatSkillRequest,
  UseCombatSkillResponse,
  UseItemInCombatResponse,
} from '../types/combatContracts';

export const combatApi = {
  async getCurrentCombat(runId: string): Promise<CombatRuntimeDto | null> {
    try {
      return await httpRequest<CombatRuntimeDto>(
        `/api/v2/runs/${runId}/current-combat`,
      );
    } catch (error) {
      if (error instanceof HttpError && error.status === 404) {
        return null;
      }
      throw error;
    }
  },

  useSkillAction(
    runId: string,
    combatId: string,
    body: UseCombatSkillRequest,
  ) {
    return httpRequest<UseCombatSkillResponse>(
      `/api/v2/runs/${runId}/combats/${combatId}/skill-actions`,
      {
        method: 'POST',
        body: JSON.stringify(body),
      },
    );
  },

  useItemAction(
    runId: string,
    combatId: string,
    body: { itemId: string; targetIds: string[] },
  ) {
    return httpRequest<UseItemInCombatResponse>(
      `/api/v2/runs/${runId}/combats/${combatId}/item-actions`,
      {
        method: 'POST',
        body: JSON.stringify(body),
      },
    );
  },
};