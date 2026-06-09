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

export type BossPreviewDto = {
  bossId: string;
  name: string;
  roomType: string;
  dangerHint: string;
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
  bossPreview: BossPreviewDto;
  nodes: NodeDto[];
  availableNodes: NodeDto[];
  layoutTemplateKey: string | null;
  layoutTemplateVersion: string | null;
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
  /** true when status === 'Suspended' — run paused at a safe point, can be resumed. */
  canResume?: boolean;
  /** ISO timestamp set by SaveAndExit. null if the run was never suspended. */
  savedAt?: string | null;
  /** ISO timestamp set when status === 'Abandoned'. Equals EndedAt on the domain object. */
  abandonedAt?: string | null;
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