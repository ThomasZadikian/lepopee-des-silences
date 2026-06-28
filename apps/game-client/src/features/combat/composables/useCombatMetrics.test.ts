import { describe, expect, it, beforeEach } from 'vitest';
import { useCombatMetrics } from './useCombatMetrics';
import type { CombatRuntimeDto } from '../types/combatContracts';

function makeCombatant(id: string, side: 'Player' | 'Enemy', vitality = 100, guard = 0): CombatRuntimeDto['allies'][number] {
  return {
    id,
    sourceKey: `source.${id}`,
    displayName: id,
    side,
    archetype: 'Fighter',
    maxVitality: vitality,
    currentVitality: vitality,
    guard,
    mana: 0,
    charge: 0,
    status: 'Active',
    skills: [],
  };
}

function makeCombat(
  allies: CombatRuntimeDto['allies'] = [],
  enemies: CombatRuntimeDto['enemies'] = [],
  activeCombatantId: string | null = null,
): CombatRuntimeDto {
  return {
    id: 'combat-1',
    status: 'Active',
    turnNumber: 1,
    activeCombatantId,
    allies,
    enemies,
    usableBattleItems: [],
  };
}

describe('useCombatMetrics', () => {
  beforeEach(() => {
    // Each test gets a fresh instance
  });

  it('returns initial state with null combatId', () => {
    const { state } = useCombatMetrics();
    expect(state.value.combatId).toBeNull();
  });

  it('returns initial state with zero metrics', () => {
    const { state } = useCombatMetrics();
    expect(state.value.allies.damageDealt).toBe(0);
    expect(state.value.allies.damageTaken).toBe(0);
    expect(state.value.enemies.damageDealt).toBe(0);
    expect(state.value.enemies.damageTaken).toBe(0);
  });

  it('returns empty contributions', () => {
    const { state } = useCombatMetrics();
    expect(Object.keys(state.value.contributions)).toEqual([]);
  });

  it('returns empty floatEvents', () => {
    const { state } = useCombatMetrics();
    expect(state.value.floatEvents).toEqual([]);
  });

  it('snapshotBeforeAction captures combatant state', () => {
    const { snapshotBeforeAction } = useCombatMetrics();
    const combat = makeCombat(
      [makeCombatant('ally-1', 'Player', 100, 10)],
      [makeCombatant('enemy-1', 'Enemy', 50, 0)],
    );
    snapshotBeforeAction(combat);
    // Snapshot is stored internally; verify via processAfterAction
  });

  it('processAfterAction ignores null combat', () => {
    const { snapshotBeforeAction, processAfterAction, state } = useCombatMetrics();
    snapshotBeforeAction(null);
    processAfterAction(null);
    expect(state.value.combatId).toBeNull();
  });

  it('processAfterAction detects damage taken', () => {
    const { snapshotBeforeAction, processAfterAction, state } = useCombatMetrics();
    const before = makeCombat(
      [makeCombatant('ally-1', 'Player', 100)],
      [makeCombatant('enemy-1', 'Enemy', 50)],
    );
    snapshotBeforeAction(before);

    const after = makeCombat(
      [makeCombatant('ally-1', 'Player', 80)],
      [makeCombatant('enemy-1', 'Enemy', 50)],
    );
    processAfterAction(after);

    expect(state.value.allies.damageTaken).toBe(20);
  });

  it('processAfterAction detects healing received', () => {
    const { snapshotBeforeAction, processAfterAction, state } = useCombatMetrics();
    const before = makeCombat(
      [makeCombatant('ally-1', 'Player', 60)],
      [],
    );
    snapshotBeforeAction(before);

    const after = makeCombat(
      [makeCombatant('ally-1', 'Player', 80)],
      [],
    );
    processAfterAction(after);

    expect(state.value.allies.healingReceived).toBe(20);
  });

  it('processAfterAction detects guard absorbed', () => {
    const { snapshotBeforeAction, processAfterAction, state } = useCombatMetrics();
    const before = makeCombat(
      [makeCombatant('ally-1', 'Player', 100, 20)],
      [],
    );
    snapshotBeforeAction(before);

    const after = makeCombat(
      [makeCombatant('ally-1', 'Player', 100, 10)],
      [],
    );
    processAfterAction(after);

    expect(state.value.allies.guardAbsorbed).toBe(10);
  });

  it('processAfterAction detects guard gained', () => {
    const { snapshotBeforeAction, processAfterAction, state } = useCombatMetrics();
    const before = makeCombat(
      [makeCombatant('ally-1', 'Player', 100, 5)],
      [],
    );
    snapshotBeforeAction(before);

    const after = makeCombat(
      [makeCombatant('ally-1', 'Player', 100, 15)],
      [],
    );
    processAfterAction(after);

    expect(state.value.allies.guardGained).toBe(10);
  });

  it('processAfterAction generates float events for damage', () => {
    const { snapshotBeforeAction, processAfterAction, state } = useCombatMetrics();
    const before = makeCombat(
      [makeCombatant('ally-1', 'Player', 100)],
      [],
    );
    snapshotBeforeAction(before);

    const after = makeCombat(
      [makeCombatant('ally-1', 'Player', 80)],
      [],
    );
    processAfterAction(after);

    expect(state.value.floatEvents.length).toBe(1);
    expect(state.value.floatEvents[0].type).toBe('damage');
    expect(state.value.floatEvents[0].amount).toBe(20);
  });

  it('processAfterAction generates float events for healing', () => {
    const { snapshotBeforeAction, processAfterAction, state } = useCombatMetrics();
    const before = makeCombat(
      [makeCombatant('ally-1', 'Player', 60)],
      [],
    );
    snapshotBeforeAction(before);

    const after = makeCombat(
      [makeCombatant('ally-1', 'Player', 90)],
      [],
    );
    processAfterAction(after);

    expect(state.value.floatEvents.length).toBe(1);
    expect(state.value.floatEvents[0].type).toBe('heal');
  });

  it('processAfterAction generates float events for guard', () => {
    const { snapshotBeforeAction, processAfterAction, state } = useCombatMetrics();
    const before = makeCombat(
      [makeCombatant('ally-1', 'Player', 100, 20)],
      [],
    );
    snapshotBeforeAction(before);

    const after = makeCombat(
      [makeCombatant('ally-1', 'Player', 100, 10)],
      [],
    );
    processAfterAction(after);

    expect(state.value.floatEvents.length).toBe(1);
    expect(state.value.floatEvents[0].type).toBe('guard');
  });

  it('processAfterAction resets state on new combat id', () => {
    const { snapshotBeforeAction, processAfterAction, state } = useCombatMetrics();
    const before = makeCombat(
      [makeCombatant('ally-1', 'Player', 100)],
      [],
    );
    snapshotBeforeAction(before);

    const after = makeCombat(
      [makeCombatant('ally-1', 'Player', 80)],
      [],
    );
    after.id = 'combat-2';
    processAfterAction(after);

    expect(state.value.combatId).toBe('combat-2');
    expect(state.value.allies.damageTaken).toBe(20);
  });

  it('processAfterAction tracks contributions', () => {
    const { snapshotBeforeAction, processAfterAction, state } = useCombatMetrics();
    const before = makeCombat(
      [makeCombatant('ally-1', 'Player', 100)],
      [makeCombatant('enemy-1', 'Enemy', 50)],
      'ally-1',
    );
    snapshotBeforeAction(before);

    const after = makeCombat(
      [makeCombatant('ally-1', 'Player', 100)],
      [makeCombatant('enemy-1', 'Enemy', 30)],
      'ally-1',
    );
    processAfterAction(after);

    expect(state.value.contributions['ally-1']).toBeDefined();
    expect(state.value.contributions['ally-1'].damageDealt).toBe(20);
  });

  it('processAfterAction tracks turns taken', () => {
    const { snapshotBeforeAction, processAfterAction, state } = useCombatMetrics();
    const before = makeCombat(
      [makeCombatant('ally-1', 'Player', 100)],
      [makeCombatant('enemy-1', 'Enemy', 50)],
      'ally-1',
    );
    snapshotBeforeAction(before);

    const after = makeCombat(
      [makeCombatant('ally-1', 'Player', 100)],
      [makeCombatant('enemy-1', 'Enemy', 50)],
      'ally-1',
    );
    processAfterAction(after);

    expect(state.value.contributions['ally-1'].turnsTaken).toBe(1);
  });

  it('processAfterAction calculates netVitalityLoss', () => {
    const { snapshotBeforeAction, processAfterAction, state } = useCombatMetrics();
    const before = makeCombat(
      [makeCombatant('ally-1', 'Player', 100)],
      [],
    );
    snapshotBeforeAction(before);

    const after = makeCombat(
      [makeCombatant('ally-1', 'Player', 70)],
      [],
    );
    processAfterAction(after);

    expect(state.value.allies.netVitalityLoss).toBe(30);
  });

  it('processAfterAction does not set negative netVitalityLoss', () => {
    const { snapshotBeforeAction, processAfterAction, state } = useCombatMetrics();
    const before = makeCombat(
      [makeCombatant('ally-1', 'Player', 60)],
      [],
    );
    snapshotBeforeAction(before);

    const after = makeCombat(
      [makeCombatant('ally-1', 'Player', 80)],
      [],
    );
    processAfterAction(after);

    expect(state.value.allies.netVitalityLoss).toBe(0);
  });

  it('processAfterAction accumulates metrics across actions', () => {
    const { snapshotBeforeAction, processAfterAction, state } = useCombatMetrics();

    // First action: 10 damage
    const before1 = makeCombat([makeCombatant('ally-1', 'Player', 100)], []);
    snapshotBeforeAction(before1);
    const after1 = makeCombat([makeCombatant('ally-1', 'Player', 90)], []);
    processAfterAction(after1);

    // Second action: 15 more damage
    const before2 = makeCombat([makeCombatant('ally-1', 'Player', 90)], []);
    snapshotBeforeAction(before2);
    const after2 = makeCombat([makeCombatant('ally-1', 'Player', 75)], []);
    processAfterAction(after2);

    expect(state.value.allies.damageTaken).toBe(25);
  });

  it('reset clears all state', () => {
    const { snapshotBeforeAction, processAfterAction, state, reset } = useCombatMetrics();
    const before = makeCombat([makeCombatant('ally-1', 'Player', 100)], []);
    snapshotBeforeAction(before);
    const after = makeCombat([makeCombatant('ally-1', 'Player', 80)], []);
    after.id = 'combat-99';
    processAfterAction(after);

    reset();

    expect(state.value.combatId).toBeNull();
    expect(state.value.allies.damageTaken).toBe(0);
    expect(state.value.enemies.damageTaken).toBe(0);
    expect(Object.keys(state.value.contributions)).toEqual([]);
    expect(state.value.floatEvents).toEqual([]);
  });

  it('does not process action when combatant not in snapshot', () => {
    const { snapshotBeforeAction, processAfterAction, state } = useCombatMetrics();
    const before = makeCombat([], []);
    snapshotBeforeAction(before);

    const after = makeCombat(
      [makeCombatant('new-ally', 'Player', 80)],
      [],
    );
    processAfterAction(after);

    expect(state.value.allies.damageTaken).toBe(0);
  });
});

describe('metricLabels', () => {
  it('exports labels for all metric types', async () => {
    const { metricLabels } = await import('./useCombatMetrics');
    expect(metricLabels.damageDealt).toBe('Violence rendue');
    expect(metricLabels.damageTaken).toBe('Vitalité perdue');
    expect(metricLabels.healingDone).toBe('Soin prodigué');
    expect(metricLabels.healingReceived).toBe('Soin reçu');
    expect(metricLabels.guardAbsorbed).toBe('Garde encaissée');
    expect(metricLabels.guardGained).toBe('Garde gagnée');
    expect(metricLabels.netVitalityLoss).toBe('Perte nette');
  });
});
