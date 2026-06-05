import { gameEngineApi } from '../../../shared/api/gameEngineApi';
import type {
    CombatInstanceDto,
    SubmitCombatActionRequest,
    SubmitCombatActionResponse,
} from '../types/combatTypes';

export const combatApi = {
  getCombat(runId: string, combatId: string) {
    return gameEngineApi.get<CombatInstanceDto | { combat: CombatInstanceDto }>(
      `/api/v2/runs/${runId}/combats/${combatId}`,
    );
  },

  submitAction(
    runId: string,
    combatId: string,
    body: SubmitCombatActionRequest,
  ) {
    return gameEngineApi.post<SubmitCombatActionResponse>(
      `/api/v2/runs/${runId}/combats/${combatId}/actions`,
      body,
    );
  },
};