import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { demoPlayerId, useRunStore } from './runStore';
import { runApi } from '../api/runApi';
import { rewardApi } from '../../rewards/api/rewardApi';
import { eventChoiceApi } from '../../events/api/eventChoiceApi';
import { combatApi } from '../../combat/api/combatApi';

vi.mock('../api/runApi', () => ({
  runApi: {
    startRun: vi.fn(),
    getRun: vi.fn(),
    getOpenRun: vi.fn(),
    resolveCurrentEvent: vi.fn(),
    progressRun: vi.fn(),
    generateNextNodes: vi.fn(),
    confirmRoomExit: vi.fn(),
    saveAndExitRun: vi.fn(),
    resumeRun: vi.fn(),
    exitMidRoom: vi.fn(),
    abandonRun: vi.fn(),
    getPermanentItemCandidates: vi.fn(),
    confirmPermanentItemSelection: vi.fn(),
    removePalaceLaw: vi.fn(),
    wagerNode: vi.fn(),
    moveParty: vi.fn(),
    advanceRoomActors: vi.fn(),
    interactWithRoomNpc: vi.fn(),
    chooseRoomNpcDialogueChoice: vi.fn(),
    enterGridNode: vi.fn(),
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

vi.mock('../../combat/api/combatApi', () => ({
  combatApi: {
    getCurrentTacticalCombat: vi.fn(),
  },
}));

describe('useRunStore computed properties', () => {
  beforeEach(() => {
    vi.useRealTimers();
    setActivePinia(createPinia());
    vi.clearAllMocks();
    try { localStorage.clear(); } catch {}
    vi.mocked(combatApi.getCurrentTacticalCombat).mockResolvedValue(null as any);
  });

  it('discovers an active backend run when localStorage no longer contains its id', async () => {
    const store = useRunStore();
    vi.mocked(runApi.getOpenRun).mockResolvedValue({
      run: {
        id: 'run-orphaned-locally',
        playerId: demoPlayerId,
        seed: 'seed-42',
        status: 'Active',
        currentRoomNumber: 4,
        currentRoom: {},
      },
    } as any);

    await store.loadResumableRun();

    expect(runApi.getOpenRun).toHaveBeenCalledWith(demoPlayerId);
    expect(store.resumableRun).toMatchObject({
      id: 'run-orphaned-locally',
      status: 'Active',
      currentRoomNumber: 4,
    });
  });

  it('abandons a discovered run without requiring it to be loaded first', async () => {
    const store = useRunStore();
    store.resumableRun = {
      id: 'run-orphaned-locally',
      seed: 'seed-42',
      savedAt: '',
      currentRoomNumber: 4,
      status: 'Active',
    };
    vi.mocked(runApi.abandonRun).mockResolvedValue({
      run: { id: 'run-orphaned-locally', status: 'Resolved', currentRoom: {} },
    } as any);

    const abandoned = await store.abandonResumableRun();

    expect(abandoned).toBe(true);
    expect(runApi.abandonRun).toHaveBeenCalledWith('run-orphaned-locally');
    expect(store.resumableRun).toBeNull();
  });

  it('currentRoom returns null when no run', () => {
    const store = useRunStore();
    expect(store.currentRoom).toBeNull();
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
    vi.useRealTimers();
    setActivePinia(createPinia());
    vi.clearAllMocks();
    vi.mocked(combatApi.getCurrentTacticalCombat).mockResolvedValue(null as any);
  });

  it('startRun calls the API with the demo player id', async () => {
    const store = useRunStore();

    vi.mocked(runApi.startRun).mockResolvedValue({
      run: { id: 'run-1', status: 'Active', currentRoom: {} },
    } as any);

    await store.startRun();

    expect(runApi.startRun).toHaveBeenCalledWith(expect.any(String));
    expect(store.currentRun?.id).toBe('run-1');
  });

  it('loadRun restores the active tactical combat', async () => {
    const store = useRunStore();
    const run = {
      id: 'run-1',
      status: 'Active',
      activeCombatId: 'combat-1',
      pendingRewardOfferId: null,
      currentRoom: {},
    };

    vi.mocked(runApi.getRun).mockResolvedValue({ run } as any);
    vi.mocked(combatApi.getCurrentTacticalCombat).mockResolvedValue({
      id: 'combat-1',
      allies: [],
      enemies: [],
    } as any);

    await store.loadRun('run-1');

    expect(combatApi.getCurrentTacticalCombat).toHaveBeenCalledWith('run-1');
  });

  it('clearCurrentRun resets all run state', () => {
    const store = useRunStore();
    store.currentRun = { id: 'run-1' } as any;
    store.pendingRewardOffer = { id: 'offer-1' } as any;
    store.lastOutcome = { id: 'outcome-1' } as any;
    store.npcDialogue = { nodeKey: 'npc-1' } as any;
    store.lastChoiceResult = { id: 'result-1' } as any;
    store.error = 'error';

    store.clearCurrentRun();

    expect(store.currentRun).toBeNull();
    expect(store.pendingRewardOffer).toBeNull();
    expect(store.lastOutcome).toBeNull();
    expect(store.npcDialogue).toBeNull();
    expect(store.lastChoiceResult).toBeNull();
    expect(store.error).toBeNull();
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

  it('grantPermanentItem sends a single-item request without resolving the end-of-run selection', async () => {
    // Equipping a run-found weapon mid-run grants it right away (see BesacePage's
    // toggleEquip) — this must never mark the end-of-run ceremony as resolved, or every
    // OTHER eligible item found later this run would silently lose its keepsake screen.
    const store = useRunStore();
    store.currentRun = { id: 'run-1', status: 'Active' } as any;

    vi.mocked(runApi.confirmPermanentItemSelection).mockResolvedValue({
      runId: 'run-1',
      confirmedItemDefinitionKeys: ['weapon.lame-seuil'],
    });

    await store.grantPermanentItem('weapon.lame-seuil');

    expect(runApi.confirmPermanentItemSelection).toHaveBeenCalledWith('run-1', ['weapon.lame-seuil']);
    expect(store.isPermanentItemSelectionResolved).toBe(false);
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
    vi.useFakeTimers();
    const store = useRunStore();
    store.currentRun = {
      id: 'run-1',
      status: 'Active',
      currentRoom: { grid: { partyX: 0, partyY: 0 } },
    } as any;

    vi.mocked(runApi.moveParty).mockResolvedValue({
      run: {
        id: 'run-1',
        status: 'Active',
        currentRoom: { grid: { partyX: 1, partyY: 0 } },
      },
    } as any);
    vi.mocked(runApi.advanceRoomActors).mockResolvedValue({
      run: {
        id: 'run-1',
        status: 'Active',
        currentRoom: { state: 'Active', grid: { partyX: 1, partyY: 0 } },
      },
      movements: [],
      triggeredNodeId: null,
    } as any);

    const moving = store.movePartyTo(1, 0);
    await vi.runAllTimersAsync();
    await moving;

    expect(runApi.moveParty).toHaveBeenCalledWith('run-1', 1, 0);
    expect(runApi.advanceRoomActors).toHaveBeenCalledWith('run-1', 'HostilesOnly');
    expect(store.currentRun?.currentRoom.grid.partyX).toBe(1);
  });

  it('interactWithRoomNpc opens the Catalog dialogue returned for the adjacent actor', async () => {
    const store = useRunStore();
    store.currentRun = {
      id: 'run-1',
      status: 'Active',
      currentRoom: { state: 'Active' },
    } as any;
    vi.mocked(runApi.interactWithRoomNpc).mockResolvedValue({
      run: { id: 'run-1', status: 'Active', currentRoom: { state: 'Active' } },
      actor: { id: 'npc-1' },
      npcDialogue: {
        npcKey: 'npc.majordome',
        speaker: 'Le Majordome',
        nodeKey: 'seuil',
        lines: ['Vos pieds, je vous prie.'],
        choices: [{ choiceId: 'saluer', label: 'Saluer', consequencePreview: '' }],
        aggregateState: 'Latent',
        encounterActive: true,
      },
      localRuleNotices: [{
        ruleKey: 'hall-rule',
        ruleName: 'Protocole du Hall',
        outcome: 'Informed',
        message: 'Le majordome vous rappelle le protocole.',
      }],
    } as any);

    await store.interactWithRoomNpc('npc-1');

    expect(runApi.interactWithRoomNpc).toHaveBeenCalledWith('run-1', 'npc-1');
    expect(store.actorInteractionNotice).toBe('Le majordome vous rappelle le protocole.');
    expect(store.npcDialogue?.speaker).toBe('Le Majordome');
    expect(store.gameplayPhase).toBe('NpcDialogue');
  });

  it('selectNpcDialogueChoice uses the room actor dialogue endpoint after a physical interaction', async () => {
    const store = useRunStore();
    store.currentRun = {
      id: 'run-1',
      status: 'Active',
      currentRoom: { state: 'Active' },
    } as any;
    vi.mocked(runApi.interactWithRoomNpc).mockResolvedValue({
      run: { id: 'run-1', status: 'Active', currentRoom: { state: 'Active' } },
      actor: { id: 'npc-1' },
      npcDialogue: {
        npcKey: 'npc.majordome', speaker: 'Le Majordome', nodeKey: 'seuil',
        lines: ['Vos pieds.'],
        choices: [{ choiceId: 'saluer', label: 'Saluer', consequencePreview: '' }],
        aggregateState: 'Latent', encounterActive: true,
      },
      localRuleNotices: [],
    } as any);
    vi.mocked(runApi.chooseRoomNpcDialogueChoice).mockResolvedValue({
      run: { id: 'run-1', status: 'Active', currentRoom: { state: 'Active' } },
      result: {
        choiceId: 'saluer',
        accepted: true,
        message: 'La rencontre se referme.',
        narrativeFragments: [],
      },
      npcDialogue: null,
    } as any);

    await store.interactWithRoomNpc('npc-1');
    await store.selectNpcDialogueChoice('saluer');

    expect(runApi.chooseRoomNpcDialogueChoice).toHaveBeenCalledWith('run-1', 'npc-1', 'saluer');
    expect(eventChoiceApi.chooseCurrentEventOption).not.toHaveBeenCalled();
    expect(store.npcDialogueEnded).toBe(true);

    await store.continueAfterNpcDialogue();
    expect(runApi.progressRun).not.toHaveBeenCalled();
    expect(store.gameplayPhase).toBe('Map');
  });

  it('enterGridNode selects the node then resolves it immediately, so the room returns to a movable state', async () => {
    const store = useRunStore();
    store.currentRun = { id: 'run-1', status: 'Active' } as any;

    vi.mocked(runApi.enterGridNode).mockResolvedValue({
      run: { id: 'run-1', status: 'Active', currentRoom: { state: 'NodeSelected' } },
    } as any);
    vi.mocked(runApi.resolveCurrentEvent).mockResolvedValue({
      run: { id: 'run-1', status: 'Active', currentRoom: { state: 'NodeResolved' } },
      outcome: { title: 'Objet trouvé' },
    } as any);

    await store.enterGridNode('node-1');

    expect(runApi.enterGridNode).toHaveBeenCalledWith('run-1', 'node-1');
    expect(runApi.resolveCurrentEvent).toHaveBeenCalledWith('run-1');
    expect(store.currentRun?.currentRoom.state).toBe('NodeResolved');
    expect(store.lastOutcome?.title).toBe('Objet trouvé');
    expect(store.error).toBeNull();
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
