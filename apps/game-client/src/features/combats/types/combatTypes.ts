export type CombatantSnapshotDto = {
  id: string;
  templateKey: string;
  displayName: string;
  side: string;
  maxHealth: number;
  currentHealth: number;
  attack: number;
  defense: number;
  speed: number;
  isDefeated: boolean;
};

export type CombatInstanceDto = {
  id: string;
  state: string;
  round: number;
  currentActorId?: string | null;
  combatants: CombatantSnapshotDto[];
  turnOrder: string[];
};

export type SubmitCombatActionRequest = {
  actorId: string;
  targetId: string;
  actionType: 'BasicAttack';
};

export type CombatActionResultDto = {
  combatId?: string;
  actorId?: string;
  targetId?: string;
  actionType?: string;
  damage?: number;
  targetRemainingHealth?: number;
  targetDefeated?: boolean;
  combatState?: string;
  winningSide?: string | null;
  nextActorId?: string | null;
  round?: number;
};

export type SubmitCombatActionResponse = {
  run?: unknown;
  combat?: CombatInstanceDto;
  result?: CombatActionResultDto;
  rewardOffer?: unknown | null;
};

export function unwrapCombatResponse(
  response: CombatInstanceDto | { combat: CombatInstanceDto },
): CombatInstanceDto {
  if ('combat' in response) {
    return response.combat;
  }

  return response;
}