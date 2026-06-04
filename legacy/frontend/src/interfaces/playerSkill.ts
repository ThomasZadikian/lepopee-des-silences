export interface PlayerSkill {
  id: number;
  playerId: number;
  skillId: number;
  unlockedAt: string;
  skill: Skill;
}

export interface Skill {
  id: number;
  name: string;
  mpCost: number;
  baseDamage: number | null;
  healAmount: number | null;
  effectType: string;
  elementType: string | null;
  description: string | null;
}
