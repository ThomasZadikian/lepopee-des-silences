export type PlayerCharacterSkillView = {
  skillKey: string;
  unlockedAtUtc: string;
  source: string | null;
  isEquipped: boolean;
};

export type PlayerCharacterStatsView = {
  maxVitality: number;
  attackPower: number;
  defense: number;
  startingGuard: number;
  speed: number;
  initiative: number;
  recovery: number;
  focus: number;
  mana: number;
  charge: number;
};

export type PlayerStatKind =
  | 'MaxVitality'
  | 'AttackPower'
  | 'Defense'
  | 'StartingGuard'
  | 'Speed'
  | 'Initiative'
  | 'Recovery'
  | 'Focus'
  | 'Mana'
  | 'Charge';

export type PlayerCharacterView = {
  id: string;
  definitionKey: string;
  displayName: string;
  skills: PlayerCharacterSkillView[];
  stats: PlayerCharacterStatsView;
  maxEquippedSkills: number;
};

export type PlayerProgressionView = {
  unspentStatPoints: number;
  totalStatPointsEarned: number;
};

export type PlayerProfileView = {
  id: string;
  displayName: string;
  characters: PlayerCharacterView[];
  progression: PlayerProgressionView;
};
