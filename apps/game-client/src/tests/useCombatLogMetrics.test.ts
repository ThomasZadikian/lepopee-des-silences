import { describe, expect, it } from 'vitest';

import type { CombatLogEntryDto, CombatRuntimeDto } from '../features/combat/types/combatContracts';
import { buildMetricsFromLogs } from '../features/combat/composables/useCombatLogMetrics';

const baseCombat: CombatRuntimeDto = {
  id: 'combat-1',
  status: 'Active',
  turnNumber: 1,
  activeCombatantId: 'ally-1',
  allies: [
    {
      id: 'ally-1', sourceKey: 'player.self', displayName: 'Hero',
      side: 'Player', archetype: 'Fighter',
      maxVitality: 100, currentVitality: 80, guard: 0, mana: 0, charge: 0,
      status: 'Active', skills: [],
    },
  ],
  enemies: [
    {
      id: 'enemy-1', sourceKey: 'enemy.test', displayName: 'Foe',
      side: 'Enemy', archetype: 'Guard',
      maxVitality: 50, currentVitality: 30, guard: 0, mana: 0, charge: 0,
      status: 'Active', skills: [],
    },
  ],
  usableBattleItems: [],
};

function entry(
  partial: Partial<CombatLogEntryDto> & Pick<CombatLogEntryDto, 'type' | 'message'>,
): CombatLogEntryDto {
  return {
    occurredAtUtc: '2026-01-01T00:00:00Z',
    actorId: null,
    skillKey: null,
    targetIds: [],
    ...partial,
  };
}

describe('buildMetricsFromLogs', () => {
  it('returns zero contributions when no entries', () => {
    const result = buildMetricsFromLogs([], baseCombat);
    expect(result.contributions['ally-1'].damageDealt).toBe(0);
    expect(result.contributions['enemy-1'].damageTaken).toBe(0);
  });

  it('returns empty contributions map when combat is null', () => {
    const result = buildMetricsFromLogs(
      [entry({ type: 'DamageApplied', message: 'Foe takes 10 damage.', actorId: 'ally-1', targetIds: ['enemy-1'] })],
      null,
    );
    expect(result.contributions).toEqual({});
  });

  it('increments turnsTaken from SkillUsed', () => {
    const result = buildMetricsFromLogs(
      [entry({ type: 'SkillUsed', message: 'Hero uses Frappe.', actorId: 'ally-1' })],
      baseCombat,
    );
    expect(result.contributions['ally-1'].turnsTaken).toBe(1);
    expect(result.contributions['enemy-1'].turnsTaken).toBe(0);
  });

  it('increments turnsTaken from ItemUsed', () => {
    const result = buildMetricsFromLogs(
      [entry({ type: 'ItemUsed', message: 'Hero uses Potion.', actorId: 'ally-1' })],
      baseCombat,
    );
    expect(result.contributions['ally-1'].turnsTaken).toBe(1);
  });

  it('assigns damageDealt to actor and damageTaken to target', () => {
    const result = buildMetricsFromLogs(
      [entry({ type: 'DamageApplied', message: 'Foe takes 15 damage.', actorId: 'ally-1', targetIds: ['enemy-1'] })],
      baseCombat,
    );
    expect(result.contributions['ally-1'].damageDealt).toBe(15);
    expect(result.contributions['enemy-1'].damageTaken).toBe(15);
    expect(result.contributions['enemy-1'].guardAbsorbed).toBe(0);
  });

  it('assigns guardAbsorbed to target (not damageTaken) on guard absorb', () => {
    const result = buildMetricsFromLogs(
      [entry({ type: 'DamageApplied', message: "Foe's guard absorbs 8 damage.", actorId: 'ally-1', targetIds: ['enemy-1'] })],
      baseCombat,
    );
    expect(result.contributions['ally-1'].damageDealt).toBe(8);
    expect(result.contributions['enemy-1'].guardAbsorbed).toBe(8);
    expect(result.contributions['enemy-1'].damageTaken).toBe(0);
  });

  it('assigns healingDone to actor and healingReceived to target', () => {
    const result = buildMetricsFromLogs(
      [entry({ type: 'HealApplied', message: 'Hero restores 20 HP.', actorId: 'ally-1', targetIds: ['ally-1'] })],
      baseCombat,
    );
    expect(result.contributions['ally-1'].healingDone).toBe(20);
    expect(result.contributions['ally-1'].healingReceived).toBe(20);
  });

  it('assigns guardGained to target', () => {
    const result = buildMetricsFromLogs(
      [entry({ type: 'GuardGained', message: 'Hero gains 5 guard.', actorId: 'ally-1', targetIds: ['ally-1'] })],
      baseCombat,
    );
    expect(result.contributions['ally-1'].guardGained).toBe(5);
  });

  it('accumulates across multiple entries of the same type', () => {
    const result = buildMetricsFromLogs(
      [
        entry({ type: 'DamageApplied', message: 'Foe takes 10 damage.', actorId: 'ally-1', targetIds: ['enemy-1'] }),
        entry({ type: 'DamageApplied', message: 'Foe takes 7 damage.', actorId: 'ally-1', targetIds: ['enemy-1'] }),
      ],
      baseCombat,
    );
    expect(result.contributions['ally-1'].damageDealt).toBe(17);
    expect(result.contributions['enemy-1'].damageTaken).toBe(17);
  });

  it('combatId reflects the current combat', () => {
    const result = buildMetricsFromLogs([], baseCombat);
    expect(result.combatId).toBe('combat-1');
  });

  it('combatId is null when combat is null', () => {
    const result = buildMetricsFromLogs([], null);
    expect(result.combatId).toBeNull();
  });

  it('tracks damageToGuard on actor when guard absorbs', () => {
    const result = buildMetricsFromLogs(
      [
        entry({ type: 'DamageApplied', message: "Foe's guard absorbs 8 damage.", actorId: 'ally-1', targetIds: ['enemy-1'] }),
        entry({ type: 'DamageApplied', message: 'Foe takes 5 damage.', actorId: 'ally-1', targetIds: ['enemy-1'] }),
      ],
      baseCombat,
    );
    expect(result.contributions['ally-1'].damageDealt).toBe(13);
    expect(result.contributions['ally-1'].damageToGuard).toBe(8);
    expect(result.contributions['enemy-1'].guardAbsorbed).toBe(8);
    expect(result.contributions['enemy-1'].damageTaken).toBe(5);
  });
});
