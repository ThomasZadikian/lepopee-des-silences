import { gameEngineApi } from '../../../shared/api/gameEngineApi';
import type { CombatRuntimeDto, UseCombatSkillRequest, UseCombatSkillResponse } from '../types/combatContracts';

export const combatApi = {
  getCurrentCombat(runId: string) {
    return gameEngineApi.get<CombatRuntimeDto>(
      `/api/v2/runs/${runId}/current-combat`,
    );
  },

  useSkillAction(
    runId: string,
    combatId: string,
    body: UseCombatSkillRequest,
  ) {
    return gameEngineApi.post<UseCombatSkillResponse>(
      `/api/v2/runs/${runId}/combats/${combatId}/skill-actions`,
      body,
    );
  },
};
