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
  focus: number;
  mana: number;
  charge: number;
  magicAttack?: number;
  magicDefense?: number;
  movement?: number;
};

export type PlayerStatKind =
  | 'MaxVitality'
  | 'AttackPower'
  | 'Defense'
  | 'StartingGuard'
  | 'Speed'
  | 'Initiative'
  | 'Focus'
  | 'Mana'
  | 'MagicAttack'
  | 'MagicDefense';

export type PlayerCharacterItemView = {
  itemKey: string;
  acquiredAtUtc: string;
  source: string | null;
  isEquipped: boolean;
  slot?: string;
  itemInstanceId?: string;
  position?: EquipmentPosition | null;
};

export type PlayerCharacterView = {
  id: string;
  definitionKey: string;
  displayName: string;
  skills: PlayerCharacterSkillView[];
  stats: PlayerCharacterStatsView;
  maxEquippedSkills: number;
  items: PlayerCharacterItemView[];
  maxEquippedItems: number;
  characterType: 'Standard' | 'Companion';
  archetypeKey?: string | null;
  baseStats?: PlayerCharacterStatsView | null;
};

export type PlayerProgressionView = {
  palaceShardCount: number;
  himLitShardCount?: number;
};

export type PlayerPermanentItemView = {
  itemDefinitionKey: string;
  sourceRunId: string | null;
  acquiredAtUtc: string;
  containedLiquidDefinitionKey?: string | null;
  itemInstanceId?: string;
};

export type EquipmentPosition =
  | 'Head' | 'Neck' | 'Shoulders' | 'Cape' | 'Chest' | 'Wrist' | 'Hand'
  | 'Waist' | 'Legs' | 'Feet' | 'Ring1' | 'Ring2' | 'Relic' | 'MainWeapon' | 'OffWeapon';

export type EquipmentStatsView = PlayerCharacterStatsView & { movement: number };

export type EquipmentChangePlanView = {
  targetPosition: EquipmentPosition;
  candidateItem: { itemInstanceId: string; definitionKey: string; displayName: string };
  currentlyEquippedItem: { itemInstanceId: string; definitionKey: string; displayName: string } | null;
  canEquip: boolean;
  blockingReasons: string[];
  currentEffectiveStats: EquipmentStatsView;
  projectedEffectiveStats: EquipmentStatsView;
  statDeltas: Array<{ stat: string; current: number; projected: number; delta: number }>;
  currentTemporarySkills: string[];
  projectedTemporarySkills: string[];
  gainedTemporarySkills: string[];
  lostTemporarySkills: string[];
  currentVitality: number;
  projectedCurrentVitality: number;
  currentMana: number;
  projectedCurrentMana: number;
  allowedSlots: string[];
  proficiencyTags: string[];
};

export type PalaceProgressView = {
  sequenceKey?: string | null;
  sequenceVersion?: string | null;
  stepKey?: string | null;
  checkpointKey?: string | null;
  isCompleted: boolean;
  highestDifficultyLevelUnlocked: number;
  unlockedRoomKeys: string[];
  visibleRoomKeys: string[];
};

export type PlayerProfileView = {
  id: string;
  displayName: string;
  characters: PlayerCharacterView[];
  progression: PlayerProgressionView;
  mainStory?: PalaceProgressView;
  permanentItems: PlayerPermanentItemView[];
};
