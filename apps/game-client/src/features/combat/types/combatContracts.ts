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
  cooldown: number;
  isUltimate: boolean;
  /** Cost after the active combatant's equipment and statuses have been applied. */
  effectiveManaCost?: number | null;
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
  threatValue?: number;
  hitChanceBonusPercent?: number;
  evasion?: number;
  criticalChanceBonusPercent?: number;
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
  targetingType: 'Self' | 'SingleAlly' | 'DefeatedAlly' | 'SingleEnemy' | 'AllEnemies';
  tacticalRange: number;
  tacticalAreaShape: 'Single' | 'Cross' | 'Diamond' | 'Map';
  requiresLineOfSight: boolean;
};

export type CombatStatus = 'Active' | 'Completed' | 'Failed' | 'Escaped';

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
// Le runtime tactique expose le terrain, les positions et l'ordre d'initiative
// annoncé à l'avance.

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
  facing: 'North' | 'East' | 'South' | 'West';
  /** Remaining owner activations before each skill can be used again. */
  skillCooldowns: Record<string, number>;
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
  usedOnceSkillKeys: string[];
  /** Sortie de fuite, uniquement pour les combats normaux. */
  escape: { x: number; y: number } | null;
  /** Palier figé à l'ouverture ; il ne baisse pas avec les morts. */
  riskTier: 'Calme' | 'Tendu' | 'Dangereux' | 'Perilleux' | 'Fatal';
};

/** Ce qu'un combattant a encaissé, à l'endroit où il l'a encaissé. */
export type TacticalImpactDto = {
  combatantId: string;
  x: number;
  y: number;
  /** Vitalité perdue ; négative pour un soin. */
  vitalityDelta: number;
  defeated: boolean;
  /**
   * Le coup est parti mais n'a pas touché. Sans ce drapeau, une esquive est indistinguable
   * d'une action sans effet chiffré : dans les deux cas la vitalité ne bouge pas.
   */
  missed: boolean;
  /**
   * Ce que la Garde a encaissé à la place de la vitalité. Un coup entièrement absorbé laisse
   * `vitalityDelta` à zéro — sans ce champ, il se lirait comme une action sans effet plutôt
   * que comme la Garde ayant fait exactement son travail. Optionnel : absent (donc 0) pour
   * toute réponse serveur antérieure à ce champ.
   */
  guardAbsorbed?: number;
};

/**
 * Un moment du combat, décrit assez précisément pour être rejoué.
 *
 * Le serveur résout un tour entier d'un coup ; cette chronologie permet d'en jouer la mise en
 * scène — marche case par case, temps de réflexion adverse, chiffres qui s'envolent — sans que
 * la décision, elle, soit rejouée.
 */
export type TacticalCombatEventDto = {
  kind: 'Move' | 'Skill' | 'Item' | 'Tick';
  actorId: string;
  actorName: string;
  path: Array<{ x: number; y: number }>;
  skillKey: string | null;
  skillName: string | null;
  targetX: number | null;
  targetY: number | null;
  impacts: TacticalImpactDto[];
  /**
   * Les cases que ce geste va couvrir, montrées <b>avant</b> qu'il ne parte.
   *
   * C'est la seule information qu'aucun état ne porte : une fois le coup résolu, la zone a
   * disparu et il n'en reste que les conséquences. Pour un déplacement, c'est le trajet.
   */
  telegraphCells: Array<{ x: number; y: number }> | null;
};

export type TacticalCombatResponse = {
  run: unknown;
  combat: TacticalCombatRuntimeDto;
  logEntries: CombatLogEntryDto[];
  events: TacticalCombatEventDto[];
};
