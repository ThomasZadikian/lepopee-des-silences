import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { useRunStore } from './runStore';
import { runApi } from './runApi';
import { rewardApi } from '../../rewards/api/rewardApi';
import { eventChoiceApi } from '../../events/api/eventChoiceApi';

vi.mock('./runApi', () => ({
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
    expect(store.previewedNode).toBeNull();
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
    store.previewedNodeId = 'node-1';
    store.resetPreviewedNode();
    expect(store.previewedNodeId).toBeNull();
  });

  it('resetNpcDialogue clears dialogue state', () => {
    const store = useRunStore();
    store.npcDialogue = { nodeKey: 'npc-1' } as any;
    store.npcDialogueEchoes = [{ text: 'echo' }];
    store.npcDialogueEnded = true;

    store.resetNpcDialogue();

    expect(store.npcDialogue).toBeNull();
    expect(store.npcDialogueEchoes).toEqual([]);
    expect(store.npcDialogueEnded).toBe(false);
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
});
