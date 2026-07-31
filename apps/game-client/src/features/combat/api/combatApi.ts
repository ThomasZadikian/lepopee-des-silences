import { gameEngineApi } from '../../../shared/api/gameEngineApi';
import type {
  TacticalCombatRuntimeDto,
  TacticalCombatResponse,
} from '../types/combatContracts';

export const combatApi = {
  getCurrentTacticalCombat(runId: string) {
    return gameEngineApi.get<TacticalCombatRuntimeDto>(
      `/api/v2/runs/${runId}/tactical-combat`,
    );
  },

  moveTacticalCombatant(runId: string, targetX: number, targetY: number) {
    return gameEngineApi.post<TacticalCombatResponse, { targetX: number; targetY: number }>(
      `/api/v2/runs/${runId}/tactical-combat/move`,
      { targetX, targetY },
    );
  },

  useTacticalSkill(
    runId: string,
    skillKey: string,
    targetX: number,
    targetY: number,
    confirmVitalitySacrifice = false,
  ) {
    return gameEngineApi.post<
      TacticalCombatResponse,
      {
        skillKey: string;
        targetX: number;
        targetY: number;
        confirmVitalitySacrifice: boolean;
      }
    >(`/api/v2/runs/${runId}/tactical-combat/skill`, {
      skillKey,
      targetX,
      targetY,
      confirmVitalitySacrifice,
    });
  },

  useTacticalItem(
    runId: string,
    itemId: string,
    targetX: number,
    targetY: number,
    targetCombatantId?: string,
  ) {
    return gameEngineApi.post<
      TacticalCombatResponse,
      { itemId: string; targetX: number; targetY: number; targetCombatantId?: string }
    >(`/api/v2/runs/${runId}/tactical-combat/item`, {
      itemId,
      targetX,
      targetY,
      targetCombatantId,
    });
  },

  endTacticalTurn(runId: string) {
    return gameEngineApi.post<TacticalCombatResponse>(
      `/api/v2/runs/${runId}/tactical-combat/end-turn`,
      {},
    );
  },
};
