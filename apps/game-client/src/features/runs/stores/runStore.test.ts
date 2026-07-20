import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { useRunStore } from './runStore';
import { runApi } from '../api/runApi';
import { rewardApi } from '../../rewards/api/rewardApi';
import { eventChoiceApi } from '../../events/api/eventChoiceApi';

vi.mock('../api/runApi', () => ({
  runApi: {
    startRun: vi.fn(),
    getRun: vi.fn(),
    chooseNode: vi.fn(),
    resolveCurrentEvent: vi.fn(),
    progressRun: vi.fn(),
    generateNextNodes: vi.fn(),
    enterInterlude: vi.fn(),
    getInterlude: vi.fn(),
    enterNextRoom: vi.fn(),
    saveAndExitRun: vi.fn(),
    resumeRun: vi.fn(),
    exitMidRoom: vi.fn(),
    abandonRun: vi.fn(),
    getPermanentItemCandidates: vi.fn(),
    confirmPermanentItemSelection: vi.fn(),
    removePalaceLaw: vi.fn(),
    wagerNode: vi.fn(),
    moveParty: vi.fn(),
    enterGridNode: vi.fn(),
    challengeBossRemotely: vi.fn(),
    useCaliceInfini: vi.fn(),
    syncPartySkills: vi.fn(),
    syncPartyStats: vi.fn(),
  },
}));

vi.mock('../../rewards/api/rewardApi', () => ({
  rewardApi: {
    getPendingReward: vi.fn(),
    selectReward: vi.fn(),
  },
}));

vi.mock('../../events/api/eventChoiceApi', () => ({
  eventChoiceApi: {
    chooseCurrentEventOption: vi.fn(),
  },
}));

describe('useRunStore computed properties', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  it('currentRoom returns null when no run', () => {
    const store = useRunStore();
    expect(store.currentRoom).toBeNull();
  });

  it('allNodes returns empty array when no room', () => {
    const store = useRunStore();
    expect(store.allNodes).toEqual([]);
  });

  it('previewedNode returns null when no preview', () => {
    const store = useRunStore();
    expect(store.previewedNode == null).toBe(true);
  });

  it('isRoomCleared is false when no run', () => {
    const store = useRunStore();
    expect(store.isRoomCleared).toBe(false);
  });

  it('shouldShowRunFailedPanel is false when no run', () => {
    const store = useRunStore();
    expect(store.shouldShowRunFailedPanel).toBe(false);
  });

  it('shouldShowCombatScene is false when no active combat', () => {
    const store = useRunStore();
    expect(store.shouldShowCombatScene).toBe(false);
  });

  it('shouldShowRewardPanel is false when no pending reward', () => {
    const store = useRunStore();
    expect(store.shouldShowRewardPanel).toBe(false);
  });

  it('shouldShowRunMap is true when run exists and no blocking states', () => {
    const store = useRunStore();
    store.currentRun = {
      id: 'run-1',
      seed: 'abc',
      status: 'Active',
      currentRoomIndex: 0,
      currentRoomNumber: 1,
      currentRoom: {
        id: 'room-1',
        roomType: 'Combat',
        currentNodeDepth: 0,
        maxNodeDepth: 3,
        nodes: [],
        availableNodes: [],
        bossPreview: { name: 'Boss', dangerHint: 'High' },
      },
    } as any;
    expect(store.shouldShowRunMap).toBe(true);
  });

  it('gameplayPhase returns Loading when no run', () => {
    const store = useRunStore();
    expect(store.gameplayPhase).toBe('Loading');
  });

  it('gameplayPhase returns Map when run is active', () => {
    const store = useRunStore();
    store.currentRun = {
      id: 'run-1',
      seed: 'abc',
      status: 'Active',
      currentRoomIndex: 0,
      currentRoomNumber: 1,
      currentRoom: {
        id: 'room-1',
        roomType: 'Combat',
        currentNodeDepth: 0,
        maxNodeDepth: 3,
        nodes: [],
        availableNodes: [],
        bossPreview: { name: 'Boss', dangerHint: 'High' },
      },
    } as any;
    expect(store.gameplayPhase).toBe('Map');
  });

  it('gameplayPhase returns Completed when run is Completed', () => {
    const store = useRunStore();
    store.currentRun = { id: 'run-1', status: 'Completed' } as any;
    expect(store.gameplayPhase).toBe('Completed');
  });

  it('gameplayPhase returns ItemSelection when the run just ended with unresolved candidates', () => {
    const store = useRunStore();
    store.currentRun = { id: 'run-1', status: 'Completed' } as any;
    store.permanentItemCandidates = [
      { itemDefinitionKey: 'item.a', displayName: 'A', description: '', rarity: 'Common' },
    ];
    expect(store.gameplayPhase).toBe('ItemSelection');
  });

  it('gameplayPhase falls back to Completed once item selection is resolved', () => {
    const store = useRunStore();
    store.currentRun = { id: 'run-1', status: 'Completed' } as any;
    store.permanentItemCandidates = [
      { itemDefinitionKey: 'item.a', displayName: 'A', description: '', rarity: 'Common' },
    ];
    store.isPermanentItemSelectionResolved = true;
    expect(store.gameplayPhase).toBe('Completed');
  });

  it('gameplayPhase returns Suspended when run is Suspended', () => {
    const store = useRunStore();
    store.currentRun = { id: 'run-1', status: 'Suspended' } as any;
    expect(store.gameplayPhase).toBe('Suspended');
  });

  it('gameplayPhase returns Combat when active combat exists', () => {
    const store = useRunStore();
    store.currentRun = { id: 'run-1', status: 'Active', activeCombatId: 'combat-1' } as any;
    expect(store.gameplayPhase).toBe('Combat');
  });

  it('gameplayPhase returns Reward when pending reward exists', () => {
    const store = useRunStore();
    store.currentRun = { id: 'run-1', status: 'Active' } as any;
    store.pendingRewardOffer = { id: 'offer-1', choices: [] } as any;
    expect(store.gameplayPhase).toBe('Reward');
  });

  it('gameplayPhase returns Interlude when interlude exists', () => {
    const store = useRunStore();
    store.currentRun = { id: 'run-1', status: 'Active' } as any;
    store.currentInterlude = { id: 'interlude-1' } as any;
    expect(store.gameplayPhase).toBe('Interlude');
  });

  it('gameplayPhase returns NpcDialogue when dialogue exists', () => {
    const store = useRunStore();
    store.currentRun = { id: 'run-1', status: 'Active' } as any;
    store.npcDialogue = { nodeKey: 'npc-1' } as any;
    expect(store.gameplayPhase).toBe('NpcDialogue');
  });

  it('gameplayPhase returns EventChoiceResult when choice result exists', () => {
    const store = useRunStore();
    store.currentRun = { id: 'run-1', status: 'Active' } as any;
    store.lastChoiceResult = { id: 'result-1' } as any;
    expect(store.gameplayPhase).toBe('EventChoiceResult');
  });

  it('gameplayPhase returns EventOutcome when outcome exists', () => {
    const store = useRunStore();
    store.currentRun = { id: 'run-1', status: 'Active' } as any;
    store.lastOutcome = { id: 'outcome-1' } as any;
    expect(store.gameplayPhase).toBe('EventOutcome');
  });
});

describe('useRunStore actions', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  it('startRun forwards the exploration mode to the API', async () => {
    const store = useRunStore();

    vi.mocked(runApi.startRun).mockResolvedValue({
      run: { id: 'run-1', status: 'Active', explorationMode: 'Tactical', currentRoom: {} },
    } as any);

    await store.startRun('Tactical');

    expect(runApi.startRun).toHaveBeenCalledWith(expect.any(String), 'Tactical');
    expect(store.currentRun?.explorationMode).toBe('Tactical');
  });

  it('startRun defaults to no exploration mode when none is given', async () => {
    const store = useRunStore();

    vi.mocked(runApi.startRun).mockResolvedValue({
      run: { id: 'run-1', status: 'Active', explorationMode: 'Classic', currentRoom: {} },
    } as any);

    await store.startRun();

    expect(runApi.startRun).toHaveBeenCalledWith(expect.any(String), undefined);
  });

  it('previewNode sets previewedNodeId for available node', () => {
    const store = useRunStore();
    store.currentRun = {
      id: 'run-1',
      status: 'Active',
      currentRoomIndex: 0,
      currentRoomNumber: 1,
      currentRoom: {
        id: 'room-1',
        roomType: 'Combat',
        currentNodeDepth: 0,
        maxNodeDepth: 3,
        nodes: [{ id: 'node-1', state: 'Available', type: 'Combat', riskLevel: 50, row: 0, lane: 0, parentNodeIds: [], rewardProfile: 'combat-common', isBoss: false }],
        availableNodes: [{ id: 'node-1', state: 'Available' }],
        bossPreview: { name: 'Boss', dangerHint: 'High' },
      },
    } as any;

    store.previewNode('node-1');
    expect(store.previewedNodeId).toBe('node-1');
  });

  it('previewNode does nothing for non-available node', () => {
    const store = useRunStore();
    store.currentRun = {
      id: 'run-1',
      status: 'Active',
      currentRoomIndex: 0,
      currentRoomNumber: 1,
      currentRoom: {
        id: 'room-1',
        roomType: 'Combat',
        currentNodeDepth: 0,
        maxNodeDepth: 3,
        nodes: [{ id: 'node-1', state: 'Locked', type: 'Combat', riskLevel: 50, row: 0, lane: 0, parentNodeIds: [], rewardProfile: 'combat-common', isBoss: false }],
        availableNodes: [],
        bossPreview: { name: 'Boss', dangerHint: 'High' },
      },
    } as any;

    store.previewNode('node-1');
    expect(store.previewedNodeId).toBeNull();
  });

  it('resetPreviewedNode clears preview', () => {
    const store = useRunStore();
    store.previewedNodeId = 'some-node';
    store.resetPreviewedNode();
    expect(store.previewedNodeId).toBeNull();
  });

  it('clearCurrentRun resets all run state', () => {
    const store = useRunStore();
    store.currentRun = { id: 'run-1' } as any;
    store.pendingRewardOffer = { id: 'offer-1' } as any;
    store.lastOutcome = { id: 'outcome-1' } as any;
    store.npcDialogue = { nodeKey: 'npc-1' } as any;
    store.activeCombat = { id: 'combat-1' } as any;
    store.combatRuntime = { id: 'combat-1' } as any;
    store.previewedNodeId = 'node-1';
    store.lastChoiceResult = { id: 'result-1' } as any;
    store.currentInterlude = { id: 'interlude-1' } as any;
    store.error = 'error';

    store.clearCurrentRun();

    expect(store.currentRun).toBeNull();
    expect(store.pendingRewardOffer).toBeNull();
    expect(store.lastOutcome).toBeNull();
    expect(store.npcDialogue).toBeNull();
    expect(store.activeCombat).toBeNull();
    expect(store.combatRuntime).toBeNull();
    expect(store.previewedNodeId).toBeNull();
    expect(store.lastChoiceResult).toBeNull();
    expect(store.currentInterlude).toBeNull();
    expect(store.error).toBeNull();
  });

  it('chooseNode delegates to previewNode', () => {
    const store = useRunStore();
    store.currentRun = {
      id: 'run-1',
      status: 'Active',
      currentRoomIndex: 0,
      currentRoomNumber: 1,
      currentRoom: {
        id: 'room-1',
        roomType: 'Combat',
        currentNodeDepth: 0,
        maxNodeDepth: 3,
        nodes: [{ id: 'node-1', state: 'Available', type: 'Combat', riskLevel: 50, row: 0, lane: 0, parentNodeIds: [], rewardProfile: 'combat-common', isBoss: false }],
        availableNodes: [{ id: 'node-1', state: 'Available' }],
        bossPreview: { name: 'Boss', dangerHint: 'High' },
      },
    } as any;

    store.chooseNode('node-1');
    expect(store.previewedNodeId).toBe('node-1');
  });

  it('selectedNode returns previewed node when set', () => {
    const store = useRunStore();
    store.currentRun = {
      id: 'run-1',
      status: 'Active',
      currentRoomIndex: 0,
      currentRoomNumber: 1,
      currentRoom: {
        id: 'room-1',
        roomType: 'Combat',
        currentNodeDepth: 0,
        maxNodeDepth: 3,
        nodes: [{ id: 'node-1', state: 'Available', type: 'Combat', riskLevel: 50, row: 0, lane: 0, parentNodeIds: [], rewardProfile: 'combat-common', isBoss: false }],
        availableNodes: [{ id: 'node-1', state: 'Available' }],
        bossPreview: { name: 'Boss', dangerHint: 'High' },
      },
    } as any;
    store.previewedNodeId = 'node-1';

    expect(store.selectedNode?.id).toBe('node-1');
  });

  it('selectedNode returns selected node when no preview', () => {
    const store = useRunStore();
    store.currentRun = {
      id: 'run-1',
      status: 'Active',
      currentRoomIndex: 0,
      currentRoomNumber: 1,
      currentRoom: {
        id: 'room-1',
        roomType: 'Combat',
        currentNodeDepth: 0,
        maxNodeDepth: 3,
        nodes: [{ id: 'node-1', state: 'Selected', type: 'Combat', riskLevel: 50, row: 0, lane: 0, parentNodeIds: [], rewardProfile: 'combat-common', isBoss: false }],
        availableNodes: [],
        bossPreview: { name: 'Boss', dangerHint: 'High' },
      },
    } as any;

    expect(store.selectedNode?.id).toBe('node-1');
  });

  it('selectReward surfaces the specific bag-full message when the run bag is full', async () => {
    const store = useRunStore();
    store.currentRun = { id: 'run-1', status: 'RoomResolved' } as any;
    store.pendingRewardOffer = { id: 'offer-1', choices: [] } as any;

    vi.mocked(rewardApi.selectReward).mockRejectedValue(
      new Error('Le sac est plein — il n\'y a plus de place pour cet objet.'),
    );

    await store.selectReward('choice-1');

    expect(store.error).toBe('Le sac est plein — il n\'y a plus de place pour cet objet.');
    expect(store.pendingRewardOffer).not.toBeNull();
  });

  it('fetches permanent item candidates once a run action lands on Completed', async () => {
    const store = useRunStore();
    store.currentRun = { id: 'run-1', status: 'Active' } as any;

    vi.mocked(runApi.progressRun).mockResolvedValue({
      id: 'run-1',
      status: 'Completed',
      currentRoom: {},
    } as any);
    vi.mocked(runApi.getPermanentItemCandidates).mockResolvedValue({
      runId: 'run-1',
      candidates: [
        { itemDefinitionKey: 'item.relic.tome', displayName: 'Tome-38', description: '', rarity: 'Epic' },
      ],
    });

    await store.progressRun();

    expect(runApi.getPermanentItemCandidates).toHaveBeenCalledWith('run-1');
    expect(store.permanentItemCandidates).toHaveLength(1);
    expect(store.gameplayPhase).toBe('ItemSelection');
  });

  it('does not fetch permanent item candidates again once already resolved', async () => {
    const store = useRunStore();
    store.currentRun = { id: 'run-1', status: 'Active' } as any;
    store.isPermanentItemSelectionResolved = true;

    vi.mocked(runApi.progressRun).mockResolvedValue({
      id: 'run-1',
      status: 'Completed',
      currentRoom: {},
    } as any);

    await store.progressRun();

    expect(runApi.getPermanentItemCandidates).not.toHaveBeenCalled();
  });

  it('confirmPermanentItemSelection sends the choice and marks selection resolved', async () => {
    const store = useRunStore();
    store.currentRun = { id: 'run-1', status: 'Completed' } as any;
    store.permanentItemCandidates = [
      { itemDefinitionKey: 'item.relic.tome', displayName: 'Tome-38', description: '', rarity: 'Epic' },
    ];

    vi.mocked(runApi.confirmPermanentItemSelection).mockResolvedValue({
      runId: 'run-1',
      confirmedItemDefinitionKeys: ['item.relic.tome'],
    });

    await store.confirmPermanentItemSelection(['item.relic.tome']);

    expect(runApi.confirmPermanentItemSelection).toHaveBeenCalledWith('run-1', ['item.relic.tome']);
    expect(store.isPermanentItemSelectionResolved).toBe(true);
    expect(store.gameplayPhase).toBe('Completed');
  });

  it('continueAfterOutcome dismisses the outcome without calling progressRun when combat is already active', async () => {
    const store = useRunStore();
    // Him'Lit (FinalBoss) starts combat in the same response as his taunt lines —
    // "Continue" here must just reveal the already-active combat.
    store.currentRun = { id: 'run-1', status: 'Active', activeCombatId: 'combat-1' } as any;
    store.lastOutcome = { narrativeFragments: [{ speaker: "Him'Lit", text: 'Tiens.' }] } as any;

    await store.continueAfterOutcome();

    expect(runApi.progressRun).not.toHaveBeenCalled();
    expect(store.lastOutcome).toBeNull();
    expect(store.gameplayPhase).toBe('Combat');
  });

  it('continueAfterOutcome calls progressRun when no combat is active', async () => {
    const store = useRunStore();
    store.currentRun = { id: 'run-1', status: 'Active' } as any;
    store.lastOutcome = { narrativeFragments: [{ speaker: 'Narrateur', text: 'Un couloir silencieux.' }] } as any;

    vi.mocked(runApi.progressRun).mockResolvedValue({
      id: 'run-1',
      status: 'Active',
      currentRoom: {},
    } as any);

    await store.continueAfterOutcome();

    expect(runApi.progressRun).toHaveBeenCalledWith('run-1');
  });

  it('removePalaceLaw sends the law key and refreshes the run', async () => {
    const store = useRunStore();
    store.currentRun = {
      id: 'run-1',
      status: 'Active',
      activePalaceLaws: [{ key: 'law-echo-v1', version: '1.0', displayName: 'Loi', description: '', domain: 'Combat' }],
    } as any;

    vi.mocked(runApi.removePalaceLaw).mockResolvedValue({
      run: { id: 'run-1', status: 'Active', currentRoom: {}, activePalaceLaws: [] },
    } as any);

    await store.removePalaceLaw('law-echo-v1');

    expect(runApi.removePalaceLaw).toHaveBeenCalledWith('run-1', 'law-echo-v1');
    expect(store.currentRun?.activePalaceLaws).toEqual([]);
  });

  it('wagerNode sends the node id and refreshes the run', async () => {
    const store = useRunStore();
    store.currentRun = {
      id: 'run-1',
      status: 'Active',
      currentRoom: { nodes: [{ id: 'node-1', combatRiskTier: 'Tendu' }] },
    } as any;

    vi.mocked(runApi.wagerNode).mockResolvedValue({
      run: {
        id: 'run-1',
        status: 'Active',
        currentRoom: { nodes: [{ id: 'node-1', combatRiskTier: 'Dangereux' }] },
      },
    } as any);

    await store.wagerNode('node-1');

    expect(runApi.wagerNode).toHaveBeenCalledWith('run-1', 'node-1');
    expect(store.currentRun?.currentRoom.nodes[0].combatRiskTier).toBe('Dangereux');
  });

  it('movePartyTo sends the target cell and refreshes the run', async () => {
    const store = useRunStore();
    store.currentRun = {
      id: 'run-1',
      status: 'Active',
      explorationMode: 'Tactical',
      currentRoom: { grid: { partyX: 0, partyY: 0 } },
    } as any;

    vi.mocked(runApi.moveParty).mockResolvedValue({
      run: {
        id: 'run-1',
        status: 'Active',
        explorationMode: 'Tactical',
        currentRoom: { grid: { partyX: 1, partyY: 0 } },
      },
    } as any);

    await store.movePartyTo(1, 0);

    expect(runApi.moveParty).toHaveBeenCalledWith('run-1', 1, 0);
    expect(store.currentRun?.currentRoom.grid.partyX).toBe(1);
  });

  it('enterGridNode sends the node id and refreshes the run', async () => {
    const store = useRunStore();
    store.currentRun = { id: 'run-1', status: 'Active', explorationMode: 'Tactical' } as any;

    vi.mocked(runApi.enterGridNode).mockResolvedValue({
      run: { id: 'run-1', status: 'Active', explorationMode: 'Tactical', currentRoom: {} },
    } as any);

    await store.enterGridNode('node-1');

    expect(runApi.enterGridNode).toHaveBeenCalledWith('run-1', 'node-1');
  });

  it('challengeBossRemotely calls the API and refreshes the run', async () => {
    const store = useRunStore();
    store.currentRun = { id: 'run-1', status: 'Active', explorationMode: 'Tactical' } as any;

    vi.mocked(runApi.challengeBossRemotely).mockResolvedValue({
      run: { id: 'run-1', status: 'Active', explorationMode: 'Tactical', currentRoom: {} },
    } as any);

    await store.challengeBossRemotely();

    expect(runApi.challengeBossRemotely).toHaveBeenCalledWith('run-1');
  });

  it('useCaliceInfini calls the API and refreshes the run', async () => {
    const store = useRunStore();
    store.currentRun = {
      id: 'run-1',
      status: 'Active',
      caliceInfiniEnabled: true,
      canUseCaliceInfini: true,
    } as any;

    vi.mocked(runApi.useCaliceInfini).mockResolvedValue({
      run: { id: 'run-1', status: 'Active', currentRoom: {}, canUseCaliceInfini: false },
    } as any);

    await store.useCaliceInfini();

    expect(runApi.useCaliceInfini).toHaveBeenCalledWith('run-1', undefined);
    expect(store.currentRun?.canUseCaliceInfini).toBe(false);
  });

  it('syncPartySkills calls the API and refreshes the run', async () => {
    const store = useRunStore();
    store.currentRun = { id: 'run-1', status: 'Active' } as any;

    vi.mocked(runApi.syncPartySkills).mockResolvedValue({
      run: { id: 'run-1', status: 'Active', currentRoom: {} },
    } as any);

    await store.syncPartySkills();

    expect(runApi.syncPartySkills).toHaveBeenCalledWith('run-1');
  });

  it('syncPartySkills is a no-op when there is no active run', async () => {
    const store = useRunStore();
    store.currentRun = null;

    await store.syncPartySkills();

    expect(runApi.syncPartySkills).not.toHaveBeenCalled();
  });

  it('syncPartyStats calls the API and refreshes the run', async () => {
    const store = useRunStore();
    store.currentRun = { id: 'run-1', status: 'Active' } as any;

    vi.mocked(runApi.syncPartyStats).mockResolvedValue({
      run: { id: 'run-1', status: 'Active', currentRoom: {} },
    } as any);

    await store.syncPartyStats();

    expect(runApi.syncPartyStats).toHaveBeenCalledWith('run-1');
  });

  it('syncPartyStats is a no-op when there is no active run', async () => {
    const store = useRunStore();
    store.currentRun = null;

    await store.syncPartyStats();

    expect(runApi.syncPartyStats).not.toHaveBeenCalled();
  });

  it('selectNpcDialogueChoice queues a reputation popup from a reputation applied effect', async () => {
    const store = useRunStore();
    store.currentRun = { id: 'run-1', status: 'Active' } as any;

    vi.mocked(eventChoiceApi.chooseCurrentEventOption).mockResolvedValue({
      run: { id: 'run-1', status: 'Active', currentRoom: {} },
      result: {
        choiceId: 'choice-1',
        message: 'ok',
        narrativeFragments: [],
        appliedEffects: [{ kind: 'reputation', amount: 2, label: 'Hitomi' }],
      },
    } as any);

    await store.selectNpcDialogueChoice('choice-1');

    expect(store.reputationEffects).toHaveLength(1);
    expect(store.reputationEffects[0]).toMatchObject({ amount: 2, npcName: 'Hitomi' });
  });

  it('ignores zero-amount reputation effects', async () => {
    const store = useRunStore();
    store.currentRun = { id: 'run-1', status: 'Active' } as any;

    vi.mocked(eventChoiceApi.chooseCurrentEventOption).mockResolvedValue({
      run: { id: 'run-1', status: 'Active', currentRoom: {} },
      result: {
        choiceId: 'choice-1',
        message: 'ok',
        narrativeFragments: [],
        appliedEffects: [{ kind: 'reputation', amount: 0, label: 'Hitomi' }],
      },
    } as any);

    await store.selectNpcDialogueChoice('choice-1');

    expect(store.reputationEffects).toHaveLength(0);
  });

  it('dismissReputationEffect removes the effect by id', () => {
    const store = useRunStore();
    store.reputationEffects = [{ id: 1, amount: 2, npcName: 'Hitomi' }];

    store.dismissReputationEffect(1);

    expect(store.reputationEffects).toHaveLength(0);
  });
});
