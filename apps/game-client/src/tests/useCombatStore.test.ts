import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';

import { combatApi } from '../features/combat/api/combatApi';
import type { CombatRuntimeDto } from '../features/combat/types/combatContracts';
import { useCombatStore } from '../features/combat/stores/useCombatStore';

vi.mock('../features/combat/api/combatApi', () => ({
  combatApi: {
    getCurrentCombat: vi.fn(),
    useSkillAction: vi.fn(),
    useItemAction: vi.fn(),
  },
}));

function createCombat(status: CombatRuntimeDto['status'] = 'Active'): CombatRuntimeDto {
  return {
    id: 'combat-1',
    status,
    turnNumber: 1,
    activeCombatantId: status === 'Active' ? 'ally-1' : null,
    allies: [
      {
        id: 'ally-1',
        sourceKey: 'player.self',
        displayName: 'Hero',
        side: 'Player',
        archetype: 'Fighter',
        maxVitality: 100,
        currentVitality: 100,
        guard: 0,
        mana: 0,
        charge: 0,
        status: 'Active',
        skills: [
          {
            key: 'skill.basic.strike',
            displayName: 'Frappe',
            skillType: 'Damage',
            targetingType: 'SingleEnemy',
            effectType: 'Damage',
            manaCost: 0,
            chargeCost: 0,
            basePower: 10,
            tags: [],
          },
        ],
      },
    ],
    enemies: [
      {
        id: 'enemy-1',
        sourceKey: 'enemy.test',
        displayName: 'Enemy',
        side: 'Enemy',
        archetype: 'Guard',
        maxVitality: 30,
        currentVitality: 30,
        guard: 0,
        mana: 0,
        charge: 0,
        status: 'Active',
        skills: [],
      },
    ],
    usableBattleItems: [],
  };
}

describe('useCombatStore visual feedback state', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.clearAllMocks();
    setActivePinia(createPinia());
  });

  it('clears damaged feedback after timeout', () => {
    const store = useCombatStore();

    store.markDamaged(['enemy-1']);

    expect(store.recentlyDamagedIds).toContain('enemy-1');

    vi.advanceTimersByTime(700);

    expect(store.recentlyDamagedIds).not.toContain('enemy-1');
  });

  it('clears guarded feedback after timeout', () => {
    const store = useCombatStore();

    store.markGuarded(['ally-1']);

    expect(store.recentlyGuardedIds).toContain('ally-1');

    vi.advanceTimersByTime(800);

    expect(store.recentlyGuardedIds).not.toContain('ally-1');
  });

  it('clears defeated feedback after timeout', () => {
    const store = useCombatStore();

    store.markDefeated(['enemy-1']);

    expect(store.recentlyDefeatedIds).toContain('enemy-1');

    vi.advanceTimersByTime(900);

    expect(store.recentlyDefeatedIds).not.toContain('enemy-1');
  });

  it('reset clears all visual feedback state and timers', () => {
    const store = useCombatStore();

    store.markDamaged(['enemy-1']);
    store.markGuarded(['ally-1']);
    store.markDefeated(['enemy-2']);
    store.markActing('ally-1');

    store.resetAnimationState();
    vi.advanceTimersByTime(1000);

    expect(store.recentlyDamagedIds).toEqual([]);
    expect(store.recentlyGuardedIds).toEqual([]);
    expect(store.recentlyDefeatedIds).toEqual([]);
    expect(store.recentlyActingId).toBeNull();
  });

  it('clearCombat clears selections, logs, outcome and animation states', () => {
    const store = useCombatStore();

    store.initCombat(createCombat());
    store.selectSkill('skill.basic.strike');
    store.selectTarget('enemy-1');
    store.logEntries.push({
      occurredAtUtc: new Date().toISOString(),
      type: 'CombatCompleted',
      message: 'Combat completed.',
      actorId: null,
      skillKey: null,
      targetIds: [],
    });
    store.terminalEvent = { kind: 'victory' };
    store.markDamaged(['enemy-1']);

    store.clearCombat();

    expect(store.combat).toBeNull();
    expect(store.selectedSkillKey).toBeNull();
    expect(store.selectedTargetIds).toEqual([]);
    expect(store.logEntries).toEqual([]);
    expect(store.terminalEvent).toBeNull();
    expect(store.recentlyDamagedIds).toEqual([]);
  });

  it('completed combat blocks skill submission', () => {
    const store = useCombatStore();

    store.initCombat(createCombat('Completed'));
    store.selectSkill('skill.basic.strike');
    store.selectedTargetIds = ['enemy-1'];

    expect(store.isVictory).toBe(true);
    expect(store.canSubmit).toBe(false);
  });

  it('failed combat blocks skill submission', () => {
    const store = useCombatStore();

    store.initCombat(createCombat('Failed'));
    store.selectSkill('skill.basic.strike');
    store.selectedTargetIds = ['enemy-1'];

    expect(store.isDefeat).toBe(true);
    expect(store.canSubmit).toBe(false);
  });

  it('current-combat null after completion is not a blocking error', async () => {
    vi.mocked(combatApi.getCurrentCombat).mockResolvedValueOnce(null);
    const store = useCombatStore();

    await store.loadCurrentCombat('run-1');

    expect(store.combat).toBeNull();
    expect(store.error).toBeNull();
  });

  it('does not submit a skill when the active combatant is not a player', async () => {
    const combat = createCombat();
    combat.activeCombatantId = 'enemy-1';
    combat.enemies[0].skills = [combat.allies[0].skills[0]];

    const store = useCombatStore();
    store.initCombat(combat);
    store.selectSkill('skill.basic.strike');
    store.selectedTargetIds = ['ally-1'];

    await store.submitAction('run-1');

    expect(combatApi.useSkillAction).not.toHaveBeenCalled();
  });

  it('applies damage log once during playback', async () => {
    const store = useCombatStore();
    store.initCombat(createCombat());

    const playback = store.playCombatLogs([
      {
        occurredAtUtc: new Date().toISOString(),
        type: 'DamageApplied',
        message: 'Enemy deals 15 damage.',
        actorId: 'enemy-1',
        skillKey: null,
        targetIds: ['ally-1'],
      },
    ]);

    await vi.advanceTimersByTimeAsync(700);
    await playback;

    expect(store.combat?.allies[0].currentVitality).toBe(85);
  });

  it('applies guard absorb and vitality damage logs without recomputing total damage', async () => {
    const combat = createCombat();
    combat.enemies[0].guard = 10;

    const store = useCombatStore();
    store.initCombat(combat);

    const playback = store.playCombatLogs([
      {
        occurredAtUtc: new Date().toISOString(),
        type: 'SkillUsed',
        message: 'Hero uses Frappe on Enemy for 14 damage.',
        actorId: 'ally-1',
        skillKey: 'skill.basic.strike',
        targetIds: ['enemy-1'],
      },
      {
        occurredAtUtc: new Date().toISOString(),
        type: 'DamageApplied',
        message: "Enemy's guard absorbs 10 damage.",
        actorId: 'ally-1',
        skillKey: 'skill.basic.strike',
        targetIds: ['enemy-1'],
      },
      {
        occurredAtUtc: new Date().toISOString(),
        type: 'DamageApplied',
        message: 'Enemy takes 4 damage.',
        actorId: 'ally-1',
        skillKey: 'skill.basic.strike',
        targetIds: ['enemy-1'],
      },
    ]);

    expect(store.combat?.enemies[0].guard).toBe(0);
    expect(store.combat?.enemies[0].currentVitality).toBe(30);
    expect(store.feedbackEvents).toEqual([
      expect.objectContaining({ combatantId: 'enemy-1', type: 'guard', amount: 10 }),
    ]);

    await vi.advanceTimersByTimeAsync(700);

    expect(store.combat?.enemies[0].guard).toBe(0);
    expect(store.combat?.enemies[0].currentVitality).toBe(26);
    expect(store.feedbackEvents).toEqual([
      expect.objectContaining({ combatantId: 'enemy-1', type: 'guard', amount: 10 }),
      expect.objectContaining({ combatantId: 'enemy-1', type: 'damage', amount: 4 }),
    ]);
    expect(store.feedbackEvents).not.toEqual(
      expect.arrayContaining([expect.objectContaining({ type: 'damage', amount: 14 })]),
    );

    await vi.advanceTimersByTimeAsync(700);
    await playback;
  });

  it('uses final response vitality as authoritative after playback', async () => {
    const store = useCombatStore();
    store.initCombat(createCombat());

    const playback = store.playCombatLogs([
      {
        occurredAtUtc: new Date().toISOString(),
        type: 'DamageApplied',
        message: 'Enemy deals 30 damage.',
        actorId: 'enemy-1',
        skillKey: null,
        targetIds: ['ally-1'],
      },
    ]);

    await vi.advanceTimersByTimeAsync(700);
    await playback;

    const responseCombat = createCombat();
    responseCombat.turnNumber = 2;
    responseCombat.activeCombatantId = 'ally-1';
    responseCombat.allies[0].currentVitality = 80;

    store.finishCombatResponse(responseCombat);

    expect(store.combat?.turnNumber).toBe(2);
    expect(store.combat?.allies[0].currentVitality).toBe(80);
  });

  it('does not apply duplicate damage logs with different timestamps twice', async () => {
    const store = useCombatStore();
    store.initCombat(createCombat());

    const firstEntry = {
      occurredAtUtc: '2026-01-01T00:00:00.000Z',
      type: 'DamageApplied' as const,
      message: 'Enemy takes 10 damage.',
      actorId: 'ally-1',
      skillKey: 'skill.basic.strike',
      targetIds: ['enemy-1'],
    };
    const secondEntry = {
      ...firstEntry,
      occurredAtUtc: '2026-01-01T00:00:01.000Z',
    };

    const playback = store.playCombatLogs([firstEntry, secondEntry]);

    await vi.advanceTimersByTimeAsync(700);
    await playback;

    expect(store.combat?.enemies[0].currentVitality).toBe(20);
    expect(store.feedbackEvents).toHaveLength(1);
    expect(store.feedbackEvents[0]).toEqual(
      expect.objectContaining({ combatantId: 'enemy-1', type: 'damage', amount: 10 }),
    );
  });

  it('keeps animated enemy vitality when same combat is re-initialized', async () => {
    const store = useCombatStore();
    store.initCombat(createCombat());

    const playback = store.playCombatLogs([
      {
        occurredAtUtc: new Date().toISOString(),
        type: 'DamageApplied',
        message: 'Hero deals 20 damage.',
        actorId: 'ally-1',
        skillKey: 'skill.basic.strike',
        targetIds: ['enemy-1'],
      },
    ]);

    await vi.advanceTimersByTimeAsync(700);
    await playback;

    const staleRuntime = createCombat();
    staleRuntime.activeCombatantId = 'enemy-1';
    staleRuntime.enemies[0].currentVitality = 20;

    store.initCombat(staleRuntime);

    expect(store.combat?.activeCombatantId).toBe('enemy-1');
    expect(store.combat?.enemies[0].currentVitality).toBe(10);
  });
});
