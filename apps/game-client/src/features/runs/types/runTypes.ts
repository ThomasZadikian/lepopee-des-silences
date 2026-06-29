export type NodeDto = {
  id: string;
  type: string;
  row: number;
  lane: number;
  riskLevel: number;
  rewardProfile: string;
  parentNodeIds: string[];
  state: string;
  isBoss: boolean;
  isInitial: boolean;
  hasChosenEventOption: boolean;
};

export type ActivePalaceLawDto = {
  key: string;
  version: string;
  displayName: string;
  description: string;
  domain: string;
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

export type RoomDto = {
  id: string;
  depth: number;
  roomType: string;
  theme: string;
  state: string;
  currentNodeDepth: number;
  maxNodeDepth: number;
  totalNodeCount: number;
  bossPreview: BossPreviewDto;
  nodes: NodeDto[];
  availableNodes: NodeDto[];
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
};

export type RunDto = {
  id: string;
  playerId: string;
  seed: string;
  generatorVersion: string;
  markovMatrixVersion: string;
  status: string;
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
 * Snapshot stored in localStorage when a run is saved and exited.
 * Used to display the resumable run card on ThresholdPage without a backend list endpoint.
 */
export type ResumableRunDto = {
  id: string;
  seed: string;
  savedAt: string;
  currentRoomNumber: number;
  status: string;
};

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
  startedCombat?: CombatInstanceDto | null;
  encounterDraft?: import('../../combat/types/combatContracts').CombatEncounterDraftDto | null;
  combat?: import('../../combat/types/combatContracts').CombatRuntimeDto | null;
  npcDialogue?: NpcDialogueViewDto | null;
};

export type RunResponse =
  | RunDto
  | { run: RunDto }
  | { data: RunDto }
  | { value: RunDto };

export type StartRunResponse = RunResponse;

export type ChooseNodeResponse = RunResponse;

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
