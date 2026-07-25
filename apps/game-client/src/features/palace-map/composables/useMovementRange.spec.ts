import { describe, expect, it } from 'vitest';
import { buildMovementRange, type MovementRangeInput } from './useMovementRange';

/** A flat, fully walkable board with the party at the origin unless overridden. */
function makeInput(overrides: Partial<MovementRangeInput> = {}): MovementRangeInput {
  const gridWidth = overrides.gridWidth ?? 5;
  const gridHeight = overrides.gridHeight ?? 5;

  return {
    gridWidth,
    gridHeight,
    elevation: new Array(gridWidth * gridHeight).fill(0),
    isWalkable: () => true,
    party: { x: 0, y: 0 },
    transitBlockers: new Set(),
    contactTriggers: new Set(),
    ...overrides,
  };
}

describe('buildMovementRange', () => {
  it('prices a flat orthogonal walk at one per step', () => {
    const range = buildMovementRange(makeInput());
    expect(range.costTo(3, 0)).toBe(3);
    expect(range.costTo(2, 2)).toBe(4);
  });

  it('charges for climbing and lets the party descend for free', () => {
    const elevation = new Array(25).fill(0);
    elevation[1] = 2; // (1,0) sits two levels up

    const range = buildMovementRange(makeInput({ elevation }));

    // Up two then back down: 1+2 to climb on, 1 to step off.
    expect(range.costTo(1, 0)).toBe(3);
    expect(range.costTo(2, 0)).toBe(4);
  });

  it('routes around what cannot be walked on, and reports the detour price', () => {
    // A wall across (2,0) and (2,1) forces the walk down to row 2 and back up:
    // (0,1)(0,2)(1,2)(2,2)(3,2)(3,1)(3,0)(4,0) — eight steps for a target four cells away.
    // This gap between what the player eyeballs and what the walk costs is exactly why the
    // route and its price have to be shown before the click.
    const range = buildMovementRange(makeInput({
      isWalkable: (x, y) => !(x === 2 && (y === 0 || y === 1)),
    }));

    expect(range.costTo(4, 0)).toBe(8);
  });

  it('reports no cost at all when a cell is sealed off', () => {
    const range = buildMovementRange(makeInput({
      gridWidth: 3,
      gridHeight: 1,
      isWalkable: (x) => x !== 1,
    }));

    expect(range.costTo(2, 0)).toBeNull();
  });

  it('lets a route end on a blocking cell but never cross it', () => {
    const range = buildMovementRange(makeInput({
      gridWidth: 5,
      gridHeight: 1,
      transitBlockers: new Set(['2,0']),
    }));

    expect(range.costTo(2, 0)).toBe(2);
    expect(range.costTo(4, 0)).toBeNull();
  });

  it('truncates the walk at the first cell that fires on contact', () => {
    const range = buildMovementRange(makeInput({
      gridWidth: 5,
      gridHeight: 1,
      contactTriggers: new Set(['2,0']),
    }));

    const route = range.routeTo(4, 0)!;

    expect(route.path).toEqual([{ x: 1, y: 0 }, { x: 2, y: 0 }]);
    expect(route.cost).toBe(2);
    expect(route.truncated).toBe(true);
  });

  it('does not call a walk truncated when the trigger IS the destination', () => {
    const range = buildMovementRange(makeInput({
      gridWidth: 5,
      gridHeight: 1,
      contactTriggers: new Set(['2,0']),
    }));

    expect(range.routeTo(2, 0)!.truncated).toBe(false);
  });

  it('returns the steps in walking order, excluding the cell the party starts on', () => {
    const range = buildMovementRange(makeInput({ party: { x: 0, y: 0 } }));
    const route = range.routeTo(2, 0)!;

    expect(route.path).toEqual([{ x: 1, y: 0 }, { x: 2, y: 0 }]);
  });

  it('keeps only what the budget can actually pay for', () => {
    const range = buildMovementRange(makeInput({ gridWidth: 5, gridHeight: 1 }));

    const within = range.within(2);

    expect(within).toEqual(new Set(['1,0', '2,0']));
    expect(within.has('3,0')).toBe(false);
  });

  it('never lists the party\'s own cell as somewhere to move', () => {
    const range = buildMovementRange(makeInput({ party: { x: 2, y: 2 } }));
    expect(range.within(99).has('2,2')).toBe(false);
  });

  it('survives a party position outside the board without throwing', () => {
    const range = buildMovementRange(makeInput({ party: { x: -1, y: 0 } }));

    expect(range.costTo(0, 0)).toBeNull();
    expect(range.within(10).size).toBe(0);
  });
});
