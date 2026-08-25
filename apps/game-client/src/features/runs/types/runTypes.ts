/** The 5 named combat danger tiers (see backend RiskTier). Only meaningful for
 * combat-flavored nodes (Combat/Elite/Rare/RoomBoss/FinalBoss) — null otherwise. */
export type CombatRiskTier = 'Calme' | 'Tendu' | 'Dangereux' | 'Perilleux' | 'Fatal';

export type NodeDto = {
  id: string;
  type: string;
  row: number;
  lane: number;
  /** Raw 0-100 generation roll. Still drives Item/Merchant reward generosity —
   * unrelated to combat danger, which is now carried separately by combatRiskTier. */
  riskLevel: number;
  /** Combat danger tier (Calme..Fatal). Null for non-combat node types. */
  combatRiskTier?: CombatRiskTier | null;
  rewardProfile: string;
  parentNodeIds: string[];
  state: string;
  isBoss: boolean;
  isInitial: boolean;
  hasChosenEventOption: boolean;
  /** What walking onto this node's cell does. 'Blocking' cells cannot be crossed in transit. */
  contactBehavior?: ContactBehavior;
  /** The warning it gives off before contact. 'None' on a contact node IS the ambush. */
  dangerTell?: DangerTell;
  /** 'Revealed' once a cache has been searched out. An unfound cache is not sent as a node at
   * all — only its cell appears in `RoomGridDto.hintCells`. */
  hiddenState?: 'None' | 'Hint' | 'Revealed';
  /** Exit nodes only (type === 'Exit') — the catalog room this exit leads to. Null means the
   * destination is only decided when the exit is confirmed (legacy content, no reachability
   * graph at placement time). */
  exitDestinationRoomKey?: string | null;
  /** Exit nodes only — cached display name for exitDestinationRoomKey, "???" when unresolved. */
  exitDestinationDisplayName?: string | null;
};

/** Mirrors the backend ContactBehavior enum. */
export type ContactBehavior = 'None' | 'TriggerOnEnter' | 'Blocking';

/** Mirrors the backend DangerTell enum. */
export type DangerTell = 'None' | 'Tracks' | 'Glow' | 'Blight';

export type ActivePalaceLawDto = {
  key: string;
  version: string;
  displayName: string;
  description: string;
  rarity: string;
  polarity: string;
  domains: string[];
};

export type PalacePublicIndicatorDto = {
  key: string;
  label: string;
  description?: string | null;
  category?: string | null;
  level?: string | null;
  tone?: string | null;
  source?: string | null;
};

export type BossPreviewDto = {
  bossId: string;
  name: string;
  roomType: string;
  dangerHint: string;
};

export type RoomClimateDto = {
  key?: string | null;
  type?: string | null;
  displayName?: string | null;
  description?: string | null;
  source?: string | null;
  expiresAt?: string | null;
  expiresWhen?: string | null;
  roomId?: string | null;
};

export type RoomClimateStateDto = string | RoomClimateDto;

/** Free-roam grid overlay for a room. */
export type RoomGridDto = {
  width: number;
  height: number;
  partyX: number;
  partyY: number;
  /** Fog-of-war revealed cells, each as [x, y]. */
  revealedCells: [number, number][];
  /** Flat, row-major (index = y*width+x), one value 0..3 per cell. Sent for the whole grid,
   * not gated by fog of war — real server-authoritative terrain, not a cosmetic client hash. */
  elevation: number[];
  /** Impassable cells, each as [x, y]. Sent for the whole grid, same rationale as elevation. */
  obstacleCells: [number, number][];
  /** Flat, row-major like elevation: which cells are part of the room at all. False = a hole in
   * the bounding rectangle — what gives a room its shape and where cliff faces get painted. */
  floorCells: boolean[];
  /** Threshold of a sub-room, each as [x, y]. Never a wall, never gated by fog of war — same
   * rationale as elevation/floorCells. Belongs to no enceinte: useRoomRegions treats a door cell
   * as a boundary rather than assigning it to either side. Empty for a room without sub-rooms. */
  doorCells: [number, number][];
  /** Whether searching from where the party stands would turn something up. Deliberately not
   * the cache's position: the player is told a search is worth trying, never what or where. */
  canSearch: boolean;
  /** Cells holding an unfound cache, each as [x, y]. Position only — deliberately no id, type
   * or reward: the player sees a slab that rings hollow and decides whether to spend budget
   * finding out what is under it. */
  hintCells: [number, number][];
  /** Objects physically present in the room. They are collected by crossing their cell. */
  groundItems: GroundItemDto[];
  /** Authored per-cell floor-material overrides (e.g. the Hall's tapis band) — never gated by
   * fog of war, same rationale as elevation/floorCells. Empty for a procedurally-generated room. */
  surfaceOverrides: CellDecorDto[];
  /** Authored per-cell decor placements (e.g. the Hall's four marble pillars) — independent from
   * obstacleCells; decor and collision are separate concepts. */
  decorPlacements: CellDecorDto[];
};

/** One authored surface/decor placement — see RoomGridDto.surfaceOverrides/decorPlacements. */
export type CellDecorDto = {
  x: number;
  y: number;
  actorKind: 'Npc';
  key: string;
};

export type GroundItemDto = {
  id: string;
  definitionKey: string;
  displayName: string;
  rarity: string;
  quantity: number;
  x: number;
  y: number;
};

/** A physically present, positioned NPC in the room's exploration grid — distinct from a
 * dialogue/event NPC. Only sent once its cell has been revealed by fog of war (see
 * RoomDto.FromDomain server-side). */
export type RoomNpcDto = {
  id: string;
  catalogNpcKey: string;
  x: number;
  y: number;
  /** Fixed | Guardian | Patrol | Hunter | Passive */
  behavior: string;
  /** Unaware | Aware | Alert */
  awareness: string;
};

export type RoomDto = {
  id: string;
  depth: number;
  roomType: string;
  theme: string;
  state: string;
  currentNodeDepth: number;
  maxNodeDepth: number;
  totalNodeCount: number;
  bossPreview: BossPreviewDto | null;
  nodes: NodeDto[];
  availableNodes: NodeDto[];
  /** Positioned NPCs currently visible in this room — empty until a room generator actually
   * populates one (the Hall d'entrée is the first: see HallEntreeCasting.cs). */
  roomNpcs: RoomNpcDto[];
  layoutTemplateKey: string | null;
  layoutTemplateVersion: string | null;
  activeClimate?: RoomClimateStateDto | null;
  climate?: RoomClimateStateDto | null;
    /** Catalog room this room was drawn from (e.g. "canon.room.mounkaanet"). Null = procedural. */
  catalogRoomKey?: string | null;
  /** Canon room name to display (e.g. "Le temple de Mounkaanêt"). Null = procedural. */
  catalogName?: string | null;
  /** Canon room narrative/flavour text. */
  catalogNarrative?: string | null;
  grid: RoomGridDto;
};

export type RunDto = {
  id: string;
  playerId: string;
  seed: string;
  generatorVersion: string;
  markovMatrixVersion: string;
  status: string;
  progressionMode?: 'Story' | 'Standard';
  storyDifficulty?: 'Canonical' | null;
  difficultyLevel?: number | null;
  story?: {
    sequenceKey?: string | null;
    sequenceVersion?: string | null;
    stepKey?: string | null;
    checkpointKey?: string | null;
  } | null;
  activeCombatId?: string | null;
  currentDepth: number;
  /** Zero-based index of the current room in the infinite run sequence. Use currentRoomIndex + 1 for display. */
  currentRoomIndex: number;
  /** One-based room number for player display ("Salle 1"). Equals currentRoomIndex + 1. */
  currentRoomNumber: number;
  currentRoom: RoomDto;
  rooms: RoomDto[];
  pendingRewardOfferId?: string | null;
  activePalaceLaws: ActivePalaceLawDto[];
  inventoryItems: RunItemDto[];
  /** Run-bag capacity — distinct item slots (SFD "Système d'équipement et sac permanent" § 5). */
  runItemCapacity: number;
  /** true when status === 'Suspended' — run paused at a safe point, can be resumed. */
  canResume?: boolean;
  /** ISO timestamp set by SaveAndExit. null if the run was never suspended. */
  savedAt?: string | null;
  /** ISO timestamp set when status === 'Abandoned'. Equals EndedAt on the domain object. */
  abandonedAt?: string | null;
  /** Active (unconsumed) RunModifiers on this run. Null/absent when none are active. */
  activeModifiers?: RunModifierDto[] | null;
  /** Active curses on this run (alpha-0.7.8+). Null/absent when none are active. */
  activeCurses?: ActiveCurseDto[] | null;
  /** Public Palace indicators exposed by the server. Empty/absent when none are available. */
  palaceIndicators?: PalacePublicIndicatorDto[] | null;
  /** Party snapshot — available hors combat depuis alpha-0.8.1. Null pour les runs antérieures. */
  party?: RunPartySnapshotDto | null;
  /** true when the player owns the "Carnet de bord" permanent item — gates the journal UI. */
  journalEnabled?: boolean;
  /** Auto-written journal entries for this run, one per event, tagged with the room they happened in. */
  journalEntries?: RunJournalEntryDto[] | null;
  /** true when the player owns the "Déni permanent" permanent item — gates the law-revoke UI. */
  lawDenialEnabled?: boolean;
  /** true when law denial is currently usable: owned, and cooldown (10 rooms) has elapsed. */
  canUseLawDenial?: boolean;
  /** true when the player owns the "Calice infini" permanent item — gates the Calice infini UI. */
  caliceInfiniEnabled?: boolean;
  /** true when Calice infini is currently usable: owned, and at least 1 room has elapsed since last use. */
  canUseCaliceInfini?: boolean;
};

export type RunJournalEntryDto = {
  roomIndex: number;
  roomNumber: number;
  roomDisplayName: string | null;
  text: string;
};

export type RunItemDto = {
  id: string;
  definitionKey: string;
  displayName: string;
  description: string;
  type: string;
  rarity: string;
  quantity: number;
  effectType: string;
  effectAmount: number;
  isUsable?: boolean;
  tacticalRange?: number;
  tacticalAreaShape?: 'Single' | 'Cross' | 'Diamond' | 'Map';
  requiresLineOfSight?: boolean;
  /** Weapon/Accessory/Relic, or null when not equippable — resolved server-side. */
  equipSlot?: string | null;
};

export type MovePartyResponse = {
  run: RunDto;
  collectedItemIds: string[];
  blockedItemIds: string[];
};

export type ActorAdvanceMode = 'All' | 'HostilesOnly';

export type ActorMovementDto = {
  actorId: string;
  actorKind: 'Npc' | 'Enemy';
  fromX: number;
  fromY: number;
  toX: number;
  toY: number;
};

export type AdvanceRoomActorsResponse = {
  run: RunDto;
  movements: ActorMovementDto[];
  triggeredNodeId?: string | null;
};

export type InteractWithRoomNpcResponse = {
  run: RunDto;
  actor: RoomNpcDto;
  localRuleNotices: {
    ruleKey: string;
    ruleName: string;
    outcome: string;
    message?: string | null;
  }[];
};

export type SwapGroundItemResponse = {
  run: RunDto;
};

export type RunPartyMemberSkillDto = {
  key: string;
  displayName: string;
  skillType: string;
  targetingMode: string;
  effectType: string;
  manaCost: number;
  chargeCost: number;
  basePower: number;
  temporarySlot?: 'Permanent' | 'Temporary1' | 'Temporary2' | 'Grimoire';
};

export type RunPartyMemberDto = {
  id: string;
  definitionKey: string;
  displayName: string;
  maxVitality: number;
  currentVitality: number;
  guard: number;
  mana: number;
  charge: number;
  movement?: number;
  isActive: boolean;
  isDefeated: boolean;
  skills: RunPartyMemberSkillDto[];
};

export type RunPartySnapshotDto = {
  members: RunPartyMemberDto[];
};

export type RunModifierDto = {
  id: string;
  type: string;
  value: number;
  duration: string;
  sourceType: string;
  sourceKey: string;
};

export type ActiveCurseDto = {
  id: string;
  curseDefinitionKey: string;
  displayName?: string | null;
  description?: string | null;
  severity?: string | null;
  duration?: string | null;
  consumedAtUtc?: string | null;
};

/**
 * Lightweight projection of an Active or Suspended run displayed on ThresholdPage.
 * The backend is authoritative; localStorage is only a compatibility cache for saved runs.
 */
export type ResumableRunDto = {
  id: string;
  seed: string;
  savedAt: string;
  currentRoomNumber: number;
  status: string;
};

export type NarrativeFragmentDto = {
  speaker: string;
  text: string;
};

export type NpcDialogueChoiceDto = {
  choiceId: string;
  label: string;
  consequencePreview: string;
};

export type NpcDialogueViewDto = {
  npcKey: string;
  speaker: string;
  nodeKey: string;
  lines: string[];
  choices: NpcDialogueChoiceDto[];
  aggregateState: string;
  encounterActive: boolean;
};

export type ResolvedNodeEventOutcomeDto = {
  nodeId: string;
  eventTypes: string[];
  primaryEventType: string;
  resolutionKind: string;
  riskLevel: number;
  rewardProfile: string;
  title: string;
  description: string;
  requiresPlayerChoice: boolean;
  choices?: unknown[];
  narrativeFragments?: NarrativeFragmentDto[];
};

export type ResolveCurrentEventResponse = {
  run: RunDto;
  outcome: ResolvedNodeEventOutcomeDto;
  encounterDraft?: import('../../combat/types/combatContracts').CombatEncounterDraftDto | null;
  tacticalCombat?:
    | import('../../combat/types/combatContracts').TacticalCombatRuntimeDto
    | null;
  /** Tours ennemis déjà joués à l'ouverture, quand l'initiative revient à l'adversaire. */
  tacticalEvents?:
    | import('../../combat/types/combatContracts').TacticalCombatEventDto[]
    | null;
  npcDialogue?: NpcDialogueViewDto | null;
};

export type PermanentItemCandidateDto = {
  itemDefinitionKey: string;
  displayName: string;
  description: string;
  rarity: string;
};

export type GetPermanentItemCandidatesResponse = {
  runId: string;
  candidates: PermanentItemCandidateDto[];
};

export type ConfirmPermanentItemSelectionResponse = {
  runId: string;
  confirmedItemDefinitionKeys: string[];
};

export type RunResponse =
  | RunDto
  | { run: RunDto }
  | { data: RunDto }
  | { value: RunDto };

export type StartRunResponse = RunResponse;

export type GetOpenRunResponse = { run: RunDto | null };

export type GenerateNextNodesResponse = RunResponse;

function isRunDto(value: unknown): value is RunDto {
  return (
    typeof value === 'object' &&
    value !== null &&
    'id' in value &&
    'currentRoom' in value
  );
}

export function unwrapRunResponse(response: RunResponse): RunDto {
  if (isRunDto(response)) {
    return response;
  }

  if ('run' in response && isRunDto(response.run)) {
    return response.run;
  }

  if ('data' in response && isRunDto(response.data)) {
    return response.data;
  }

  if ('value' in response && isRunDto(response.value)) {
    return response.value;
  }

  throw new Error('Unable to unwrap RunDto from API response.');
}
