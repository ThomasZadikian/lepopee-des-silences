import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { HttpError } from '../../../shared/api/httpClient';
import { combatApi } from '../api/combatApi';
import type { TacticalCombatRuntimeDto } from '../types/combatContracts';
import { useTacticalCombatStore } from './useTacticalCombatStore';

const playback = vi.hoisted(() => ({
  isPlaying: { value: false },
  pinBefore: vi.fn(),
  play: vi.fn(async () => undefined),
  stop: vi.fn(),
  reset: vi.fn(),
}));

vi.mock('../composables/useCombatPlayback', () => ({
  useCombatPlayback: () => playback,
}));

vi.mock('../api/combatApi', () => ({
  combatApi: {
    moveTacticalCombatant: vi.fn(),
    useTacticalSkill: vi.fn(),
    useTacticalItem: vi.fn(),
    endTacticalTurn: vi.fn(),
  },
}));

const skill = {
  key: 'skill.basic.strike',
  displayName: 'Frappe',
  skillType: 'Damage',
  targetingType: 'SingleEnemy',
  effectType: 'Damage',
  manaCost: 0,
  chargeCost: 0,
  basePower: 10,
  tags: [],
  category: 'Physical',
  emotionalRegister: 'Silence',
  tacticalRange: 1,
  tacticalAreaShape: 'Single',
  requiresLineOfSight: true,
  cooldown: 0,
  isUltimate: false,
} as const;

function state(activeCombatantId: string | null = 'ally'): TacticalCombatRuntimeDto {
  const combatant = (
    id: string,
    side: 'Player' | 'Enemy',
    x: number,
    status: 'Active' | 'Defeated' = 'Active',
  ) => ({
    combatant: {
      id,
      sourceKey: id,
      displayName: id,
      side,
      archetype: 'Test',
      maxVitality: 100,
      currentVitality: status === 'Defeated' ? 0 : 100,
      guard: 0,
      mana: 10,
      maxMana: 10,
      charge: 0,
      status,
      naturalEmotionalRegister: 'Silence',
      effectiveAttackRegister: 'Silence',
      incomingAffinities: [],
      skills: [skill],
    },
    x,
    y: 0,
    hasMoved: false,
    hasActed: false,
    movementBudget: 2,
    facing: 'East',
    skillCooldowns: {},
  });

  return {
    id: 'combat',
    status: 'Active',
    roundNumber: 1,
    activeCombatantId,
    initiativeOrder: ['ally', 'enemy', 'missing'],
    battlefield: {
      width: 4,
      height: 1,
      elevation: [0, 0, 0, 0],
      walkable: [true, true, true, true],
      floor: [true, true, true, true],
    },
    allies: [combatant('ally', 'Player', 0)],
    enemies: [
      combatant('enemy', 'Enemy', 3),
      combatant('defeated', 'Enemy', 2, 'Defeated'),
    ],
    usableBattleItems: [{
      itemId: 'potion',
      definitionKey: 'item.potion',
      displayName: 'Potion',
      effectType: 'Heal',
      effectAmount: 10,
      quantity: 1,
      targetingType: 'Self',
      tacticalRange: 0,
      tacticalAreaShape: 'Single',
      requiresLineOfSight: false,
    }],
    usedOnceSkillKeys: [],
    escape: null,
    riskTier: 'Calme',
  };
}

const responseFor = (combat: TacticalCombatRuntimeDto) => ({
  combat,
  logEntries: [{
    occurredAtUtc: '2026-08-26T00:00:00Z',
    type: 'ActionAccepted' as const,
    message: 'ok',
    actorId: 'ally',
    skillKey: null,
    targetIds: [],
  }],
  events: [],
});

describe('useTacticalCombatStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
    playback.isPlaying.value = false;
  });

  it('derives the active turn, initiative, occupancy and reachable cells', () => {
    const store = useTacticalCombatStore();
    store.setCombat(state());

    expect(store.activeCombatant?.combatant.id).toBe('ally');
    expect(store.isPlayerTurn).toBe(true);
    expect(store.activeSkills).toHaveLength(1);
    expect(store.initiativeQueue.map((unit) => unit.combatant.id)).toEqual(['ally', 'enemy']);
    expect(store.occupiedCells).toEqual(new Set(['0,0', '3,0']));
    expect(store.occupantAt(3, 0)?.combatant.id).toBe('enemy');
    expect(store.occupantAt(2, 0)).toBeNull();
    expect(store.reachableCells).toContain('1,0');
    expect(store.reachableCells).toBe(store.reachableCells);

    store.setCombat(state('enemy'));
    expect(store.isPlayerTurn).toBe(false);
  });

  it('keeps skill and item selection mutually exclusive and cancellable', () => {
    const store = useTacticalCombatStore();
    store.setCombat(state());

    store.selectSkill(skill.key);
    expect(store.selectedSkill?.key).toBe(skill.key);
    expect(store.hasPendingAction).toBe(true);
    store.selectSkill(skill.key);
    expect(store.selectedSkillKey).toBeNull();

    store.selectItem('potion');
    expect(store.selectedItem?.itemId).toBe('potion');
    expect(store.selectedSkillKey).toBeNull();
    store.cancelAction();
    expect(store.hasPendingAction).toBe(false);

    store.selectItem('unknown');
    expect(store.selectedItem).toBeNull();
    store.selectItem('unknown');
    expect(store.selectedItemId).toBeNull();
  });

  it('executes every API action and appends logs through the playback queue', async () => {
    const initial = state();
    const next = state('enemy');
    const apiResponse = responseFor(next);
    vi.mocked(combatApi.moveTacticalCombatant).mockResolvedValue(apiResponse);
    vi.mocked(combatApi.useTacticalSkill).mockResolvedValue(apiResponse);
    vi.mocked(combatApi.useTacticalItem).mockResolvedValue(apiResponse);
    vi.mocked(combatApi.endTacticalTurn).mockResolvedValue(apiResponse);

    const store = useTacticalCombatStore();
    store.setCombat(initial);
    await store.moveTo('run', 1, 0);
    await store.useSkillAt('run', skill.key, 3, 0, true);
    await store.useItemAt('run', 'potion', 0, 0, 'ally');
    await store.endTurn('run');

    expect(combatApi.moveTacticalCombatant).toHaveBeenCalledWith('run', 1, 0);
    expect(combatApi.useTacticalSkill).toHaveBeenCalledWith('run', skill.key, 3, 0, true);
    expect(combatApi.useTacticalItem).toHaveBeenCalledWith('run', 'potion', 0, 0, 'ally');
    expect(combatApi.endTacticalTurn).toHaveBeenCalledWith('run');
    expect(playback.pinBefore).toHaveBeenCalledTimes(4);
    expect(playback.play).toHaveBeenCalledTimes(4);
    expect(store.logEntries).toHaveLength(4);
    expect(store.isLoading).toBe(false);
    expect(store.isExecuting).toBe(false);
  });

  it('rejects concurrent commands while one action is pending', async () => {
    let finish!: (value: ReturnType<typeof responseFor>) => void;
    vi.mocked(combatApi.moveTacticalCombatant).mockReturnValue(new Promise((resolve) => {
      finish = resolve;
    }));

    const store = useTacticalCombatStore();
    store.setCombat(state());
    const first = store.moveTo('run', 1, 0);
    const ignored = store.moveTo('run', 2, 0);

    expect(store.isExecuting).toBe(true);
    expect(combatApi.moveTacticalCombatant).toHaveBeenCalledTimes(1);
    finish(responseFor(state()));
    await Promise.all([first, ignored]);
    expect(store.isExecuting).toBe(false);
  });

  it('surfaces typed and generic failures and resets transient state', async () => {
    const store = useTacticalCombatStore();
    store.setCombat(state());

    vi.mocked(combatApi.endTacticalTurn)
      .mockRejectedValueOnce(new HttpError('Conflit métier', 409, null))
      .mockRejectedValueOnce(new Error('Panne locale'))
      .mockRejectedValueOnce('indisponible');

    await store.endTurn('run');
    expect(store.error).toBe('[409] Conflit métier');
    await store.endTurn('run');
    expect(store.error).toBe('Panne locale');
    await store.endTurn('run');
    expect(store.error).toBe('indisponible');
    expect(playback.stop).toHaveBeenCalledTimes(3);

    store.selectSkill(skill.key);
    store.clearCombat();
    expect(store.combat).toBeNull();
    expect(store.logEntries).toEqual([]);
    expect(store.error).toBeNull();
    expect(store.selectedSkillKey).toBeNull();
    expect(store.reachableCells.size).toBe(0);
    expect(playback.reset).toHaveBeenCalledOnce();
  });

  it('plays only non-empty opening timelines and handles a missing active combatant', async () => {
    const store = useTacticalCombatStore();
    store.setCombat(state(null));

    expect(store.activeCombatant).toBeNull();
    expect(store.activeSkills).toEqual([]);
    expect(store.isPlayerTurn).toBe(false);
    expect(store.reachableCells.size).toBe(0);

    await store.playOpening([], state());
    expect(playback.play).not.toHaveBeenCalled();
    await store.playOpening([{ kind: 'TurnStarted', actorId: 'ally', actorName: 'Allié' } as never], state());
    expect(playback.play).toHaveBeenCalledOnce();
  });
});
