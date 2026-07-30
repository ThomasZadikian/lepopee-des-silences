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
export type TacticalAreaShape = 'Single' | 'Cross' | 'Diamond' | 'Map';

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
  /** Server-owned tactical contract. The client must not infer these values independently. */
  tacticalRange: number;
  tacticalAreaShape: TacticalAreaShape;
  requiresLineOfSight: boolean;
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
// ─── Combat tactique ────────────────────────────────────────────────────────
//
// Volontairement distinct de `CombatRuntimeDto` : les deux systèmes n'exposent
// pas la même chose. L'ATB envoie un tick et des jauges ; le tactique envoie un
// terrain, des positions et un ordre d'initiative annoncé à l'avance — cette
// prévisibilité est la contrepartie de l'abandon du tempo.

/** Le terrain de combat : la salle d'exploration vidée de ses nœuds. */
export type TacticalBattlefieldDto = {
  width: number;
  height: number;
  /** Élévations, à plat, rangées en row-major (`y * width + x`). */
  elevation: number[];
  /** Praticabilité, même indexation. Une case fausse porte un obstacle ou est hors salle. */
  walkable: boolean[];
  /** La case appartient-elle à la salle ? C'est elle qui distingue un obstacle du vide. */
  floor: boolean[];
};

export type TacticalCombatantRuntimeDto = {
  combatant: CombatantRuntimeDto;
  x: number;
  y: number;
  /** Les deux sont indépendants : renoncer à l'un ne pénalise pas l'autre. */
  hasMoved: boolean;
  hasActed: boolean;
  /** Nombre de cases atteignables ce tour, à terrain plat. */
  movementBudget: number;
};

export type TacticalCombatRuntimeDto = {
  id: string;
  status: CombatStatus;
  roundNumber: number;
  activeCombatantId: string | null;
  /** Ordre d'action du round, du plus rapide au plus lent. */
  initiativeOrder: string[];
  battlefield: TacticalBattlefieldDto;
  allies: TacticalCombatantRuntimeDto[];
  enemies: TacticalCombatantRuntimeDto[];
  usableBattleItems: CombatUsableItemDto[];
};

/** Ce qu'un combattant a encaissé, à l'endroit où il l'a encaissé. */
export type TacticalImpactDto = {
  combatantId: string;
  x: number;
  y: number;
  /** Vitalité perdue ; négative pour un soin. */
  vitalityDelta: number;
  defeated: boolean;
};

/**
 * Un moment du combat, décrit assez précisément pour être rejoué.
 *
 * Le serveur résout un tour entier d'un coup ; cette chronologie permet d'en jouer la mise en
 * scène — marche case par case, temps de réflexion adverse, chiffres qui s'envolent — sans que
 * la décision, elle, soit rejouée.
 */
export type TacticalCombatEventDto = {
  kind: 'Move' | 'Skill';
  actorId: string;
  actorName: string;
  path: Array<{ x: number; y: number }>;
  skillKey: string | null;
  skillName: string | null;
  impacts: TacticalImpactDto[];
};

export type TacticalCombatResponse = {
  run: unknown;
  combat: TacticalCombatRuntimeDto;
  logEntries: CombatLogEntryDto[];
  events: TacticalCombatEventDto[];
};
