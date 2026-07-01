import { describe, expect, it } from 'vitest';
import { buildTurnForecast } from './useTurnForecast';
import type { CombatantRuntimeDto } from '../types/combatContracts';

function makeCombatant(overrides: Partial<CombatantRuntimeDto> = {}): CombatantRuntimeDto {
  return {
    id: 'c-1',
    sourceKey: 'x',
    displayName: 'Combatant',
    side: 'Player',
    archetype: 'Fighter',
    maxVitality: 100,
    currentVitality: 100,
    guard: 0,
    mana: 0,
    charge: 0,
    status: 'Active',
    atbGauge: 0,
    atbFillPerTick: 10,
    skills: [],
    ...overrides,
  };
}

describe('buildTurnForecast', () => {
  it('returns an empty forecast when there are no combatants', () => {
    expect(buildTurnForecast([])).toEqual([]);
  });

  it('excludes defeated combatants', () => {
    const alive = makeCombatant({ id: 'alive', status: 'Active' });
    const dead = makeCombatant({ id: 'dead', status: 'Defeated' });
    const forecast = buildTurnForecast([alive, dead], 3);
    expect(forecast.every((entry) => entry.combatantId === 'alive')).toBe(true);
  });

  it('puts whoever is already ready first', () => {
    const ready = makeCombatant({ id: 'ready', atbGauge: 50_000, atbFillPerTick: 5 });
    const filling = makeCombatant({ id: 'filling', atbGauge: 0, atbFillPerTick: 5 });
    const forecast = buildTurnForecast([ready, filling], 2);
    expect(forecast[0].combatantId).toBe('ready');
  });

  it('orders by who reaches the ready threshold soonest given their fill rate', () => {
    // slow needs (50000-0)/5 = 10000 ticks to first act; fast needs
    // (50000-40000)/20 = 500 ticks, and — being 4x faster once reset to 0 —
    // keeps winning every slot until slow finally catches up.
    const slow = makeCombatant({ id: 'slow', atbGauge: 0, atbFillPerTick: 5 });
    const fast = makeCombatant({ id: 'fast', atbGauge: 40_000, atbFillPerTick: 20 });
    const forecast = buildTurnForecast([slow, fast], 5);
    expect(forecast[0].combatantId).toBe('fast');
    expect(forecast.slice(0, 4).every((entry) => entry.combatantId === 'fast')).toBe(true);
    expect(forecast.some((entry) => entry.combatantId === 'slow')).toBe(true);
  });

  it('re-adapts the order when a fill rate (speed) changes', () => {
    const a = makeCombatant({ id: 'a', atbGauge: 0, atbFillPerTick: 5 });
    const b = makeCombatant({ id: 'b', atbGauge: 0, atbFillPerTick: 4 });
    const before = buildTurnForecast([a, b], 1);
    expect(before[0].combatantId).toBe('a');

    // "b" gets a speed buff and now fills faster than "a"
    const bBuffed = makeCombatant({ id: 'b', atbGauge: 0, atbFillPerTick: 12 });
    const after = buildTurnForecast([a, bBuffed], 1);
    expect(after[0].combatantId).toBe('b');
  });

  it('simulates multiple turns ahead, resetting the gauge of whoever just acted', () => {
    // Two combatants with identical rates but "first" is already ready.
    // After it acts (gauge resets to 0), it should not immediately act again
    // ahead of "second" who was already close to ready.
    const first = makeCombatant({ id: 'first', atbGauge: 50_000, atbFillPerTick: 10 });
    const second = makeCombatant({ id: 'second', atbGauge: 45_000, atbFillPerTick: 10 });
    const forecast = buildTurnForecast([first, second], 3);
    expect(forecast[0].combatantId).toBe('first');
    expect(forecast[1].combatantId).toBe('second');
    expect(forecast[2].combatantId).toBe('first');
  });

  it('assigns sequential slot indices starting at 0', () => {
    const forecast = buildTurnForecast([makeCombatant()], 4);
    expect(forecast.map((entry) => entry.slot)).toEqual([0, 1, 2, 3]);
  });

  it('carries the combatant side through to the forecast entry', () => {
    const enemy = makeCombatant({ id: 'e', side: 'Enemy' });
    const forecast = buildTurnForecast([enemy], 1);
    expect(forecast[0].side).toBe('Enemy');
  });

  it('requests the default number of slots when none is given', () => {
    const forecast = buildTurnForecast([makeCombatant()]);
    expect(forecast.length).toBe(8);
  });
});
