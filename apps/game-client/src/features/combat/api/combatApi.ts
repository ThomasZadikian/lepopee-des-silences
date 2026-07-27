import { gameEngineApi } from '../../../shared/api/gameEngineApi';
import { HttpError } from '../../../shared/api/httpClient';
import type {
  CombatRuntimeDto,
  TacticalCombatResponse,
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

    hold(runId: string, combatId: string, deltaTicks = 200) {
    return gameEngineApi.post<UseCombatSkillResponse, { deltaTicks: number }>(
      `/api/v2/runs/${runId}/combats/${combatId}/hold`,
      { deltaTicks },
    );
  },

    advanceCombat(runId: string, combatId: string) {
    return gameEngineApi.post<UseCombatSkillResponse>(
      `/api/v2/runs/${runId}/combats/${combatId}/advance`,
      {},
    );
  },

  // ── Combat tactique ──
  // Une seule route par action, sans identifiant de combat dans l'URL : la run
  // ne porte qu'un combat à la fois, le serveur sait déjà lequel.

  moveTacticalCombatant(runId: string, targetX: number, targetY: number) {
    return gameEngineApi.post<TacticalCombatResponse, { targetX: number; targetY: number }>(
      `/api/v2/runs/${runId}/tactical-combat/move`,
      { targetX, targetY },
    );
  },

  useTacticalSkill(runId: string, skillKey: string, targetX: number, targetY: number) {
    return gameEngineApi.post<
      TacticalCombatResponse,
      { skillKey: string; targetX: number; targetY: number }
    >(`/api/v2/runs/${runId}/tactical-combat/skill`, { skillKey, targetX, targetY });
  },

  endTacticalTurn(runId: string) {
    return gameEngineApi.post<TacticalCombatResponse>(
      `/api/v2/runs/${runId}/tactical-combat/end-turn`,
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
