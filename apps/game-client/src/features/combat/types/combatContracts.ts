export type CombatantStatus = 'Active' | 'Defeated';

export type CombatSide = 'Player' | 'Enemy';

export type SkillType = 'Damage' | 'Guard' | 'Weaken' | 'Disrupt';

export type EmotionalType =
  | 'Neutral' | 'Effroi' | 'Deni' | 'Melancolie' | 'Rupture' | 'Memoire' | 'Silence';

export type TargetingType =
  | 'Self'
  | 'SingleEnemy'
  | 'SingleAlly'
  | 'AllEnemies'
  | 'AllAllies';

export type CombatantSkillRuntimeDto = {
  key: string;
  displayName: string;
  skillType: SkillType;
  targetingType: TargetingType;
  effectType: SkillType;
  manaCost: number;
  chargeCost: number;
  basePower: number;
  tags: string[];
};

export type CombatantRuntimeDto = {
  id: string;
  sourceKey: string;
  displayName: string;
  side: CombatSide;
  archetype: string;
  maxVitality: number;
  currentVitality: number;
  guard: number;
  mana: number;
  charge: number;
  status: CombatantStatus;
  attackType?: EmotionalType;
  weakTo?: EmotionalType[];
  resistantTo?: EmotionalType[];
  immuneTo?: EmotionalType[];
  attackPower?: number;
  defense?: number;
  speed?: number;
  focus?: number;
  atbGauge?: number;
  atbFillPerTick?: number;
  statusEffects?: CombatantStatusEffectDto[];
  skills: CombatantSkillRuntimeDto[];
};

export type StatusEffectKind =
  | 'DamageOverTime'
  | 'HealOverTime'
  | 'StatModifier'
  | 'Stun'
  | 'Silence'
  | 'AtbLock';

export type CombatantStatusEffectDto = {
  key: string;
  displayName: string;
  kind: StatusEffectKind;
  stat: string;
  magnitude: number;
  stacks: number;
};

export type CombatUsableItemDto = {
  itemId: string;
  definitionKey: string;
  displayName: string;
  effectType: string;
  effectAmount: number;
  quantity: number;
  targetingType: 'Self' | 'SingleAlly';
};

export type CombatStatus = 'Active' | 'Completed' | 'Failed';

export type CombatRuntimeDto = {
  id: string;
  status: CombatStatus;
  turnNumber: number;
  currentTick?: number;
  activeCombatantId: string | null;
  allies: CombatantRuntimeDto[];
  enemies: CombatantRuntimeDto[];
  usableBattleItems: CombatUsableItemDto[];
};

export type CombatEncounterDraftDto = {
  encounterTemplateKey: string;
  allies: { sourceKey: string; displayName: string; archetype: string }[];
  enemies: { sourceKey: string; displayName: string; archetype: string }[];
};

export type LogEntryType =
  | 'SkillUsed'
  | 'ItemUsed'
  | 'DamageApplied'
  | 'GuardGained'
  | 'HealApplied'
  | 'TargetDefeated'
  | 'TurnAdvanced'
  | 'EnemyTurnResolved'
  | 'CombatCompleted'
  | 'CombatFailed'
  | 'CriticalHit'
  | 'WeaknessHit'
  | 'ResistedHit'
  | 'ImmuneHit'
  | 'AtbStagger';

export type CombatLogEntryDto = {
  occurredAtUtc: string;
  type: LogEntryType;
  message: string;
  actorId: string | null;
  skillKey: string | null;
  targetIds: string[];
};

export type UseCombatSkillRequest = {
  actorId: string;
  skillKey: string;
  targetIds: string[];
};

export type UseCombatSkillResponse = {
  combatId: string;
  actorId: string;
  skillKey: string;
  targetIds: string[];
  accepted: boolean;
  message: string | null;
  combat: CombatRuntimeDto;
  logEntries: CombatLogEntryDto[];
  combatCompleted: boolean;
  combatFailed: boolean;
  canProgressRun: boolean;
  runStatus: 'Active' | 'Failed';
};

export type UseItemInCombatResponse = {
  combatId: string;
  actorId: string;
  skillKey: string;
  targetIds: string[];
  accepted: boolean;
  message: string | null;
  combat: CombatRuntimeDto;
  logEntries: CombatLogEntryDto[];
  combatCompleted: boolean;
  combatFailed: boolean;
  canProgressRun: boolean;
  runStatus: string;
};