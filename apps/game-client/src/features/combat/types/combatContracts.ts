export type CombatantStatus = 'Active' | 'Defeated';

export type CombatSide = 'Player' | 'Enemy';

export type SkillType =
  | 'Damage' | 'Guard' | 'Heal' | 'Buff' | 'Debuff' | 'Status'
  | 'CopySkills' | 'ExtendDotDuration'
  // Legacy/unused values — no seeded skill produces them today, kept for safety.
  | 'Weaken' | 'Disrupt';

export type EmotionalType =
  | 'Neutral' | 'Effroi' | 'Deni' | 'Melancolie' | 'Rupture' | 'Memoire' | 'Silence' | 'Folie';

export type TargetingType =
  | 'Self'
  | 'SingleEnemy'
  | 'SingleAlly'
  | 'AllEnemies'
  | 'AllAllies';

export type SkillCategory = 'Physical' | 'Magic';

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
  category: SkillCategory;
  /** The skill's OWN "élément" — null for basic attacks / untyped skills. */
  emotionalType?: EmotionalType | null;
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
  magicAttack?: number;
  magicDefense?: number;
  atbGauge?: number;
  atbFillPerTick?: number;
  threatValue?: number;
  statusEffects?: CombatantStatusEffectDto[];
  skills: CombatantSkillRuntimeDto[];
};

export type StatusEffectKind =
  | 'DamageOverTime'
  | 'HealOverTime'
  | 'GuardOverTime'
  | 'StatModifier'
  | 'Stun'
  | 'Silence'
  | 'AtbLock'
  | 'SkillGrant';

export type CombatantStatusEffectDto = {
  key: string;
  displayName: string;
  kind: StatusEffectKind;
  stat: string;
  magnitude: number;
  stacks: number;
  /** True when `magnitude` is a percentage of the base stat, not a flat delta. */
  isMagnitudePercentOfBaseStat: boolean;
  /** What one tick deals/heals/guards right now (0 for non-periodic kinds). */
  perTickAmount: number;
  /** Ticks remaining from the combat's current tick — null when permanent. */
  ticksRemaining: number | null;
  isPermanent: boolean;
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
  | 'ActionAccepted'
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
  | 'AttackMissed'
  | 'CriticalHit'
  | 'WeaknessHit'
  | 'ResistedHit'
  | 'ImmuneHit'
  | 'AtbStagger'
  | 'StatusApplied';

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