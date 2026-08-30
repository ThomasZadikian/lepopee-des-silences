import { describe, expect, it } from 'vitest';

import {
  battleCellKey,
  buildBattlePlan,
  hasDirectLos,
  hasLos,
  manhattan,
  reachableCellsFrom,
  reachableCellsWithPathsFrom,
} from './useTacticalBattlePlan';

const battlefield = {
  gridWidth: 3,
  gridHeight: 3,
  elevation: [0, 0, 0, 0, 1, 0, 0, 0, 0],
  walkable: [true, true, true, true, true, true, true, true, true],
};

describe('useTacticalBattlePlan', () => {
  it('builds ordered floor, obstacle and gameplay highlight entries', () => {
    const plan = buildBattlePlan({
      canvasWidth: 800,
      canvasHeight: 600,
      gridWidth: 2,
      gridHeight: 2,
      elevation: [0, 1, 0, 0],
      walkable: [true, false, true, true],
      floor: [true, true, true, false],
      theme: 'Threshold',
      ambientTint: 'neutral',
      reachableCells: new Set(['0,0']),
      threatCells: new Set(['0,0']),
      targetableCells: new Set(['0,0']),
      blockedCells: new Set(['0,0']),
      pathCells: new Set(['0,0']),
      aoeCells: new Set(['0,0']),
      heightCells: new Set(['0,0']),
      occupiedCells: new Set(['0,0', '0,1']),
      hoveredCell: { x: 0, y: 0 },
    });

    expect(plan).toEqual([...plan].sort((left, right) => left.sortKey - right.sortKey));
    expect(plan.find((entry) => entry.cellKey === '1,0')?.spriteKey.kind).toBe('obstacle');
    expect(plan.find((entry) => entry.cellKey === '0,1')?.spriteKey.kind).toBe('floor');
    expect(plan.some((entry) => entry.cellKey === '1,1')).toBe(false);
    expect(plan.filter((entry) => entry.cellKey.startsWith('0,0:')).map((entry) => entry.cellKey))
      .toEqual([
        '0,0:move',
        '0,0:threat',
        '0,0:attack',
        '0,0:blocked',
        '0,0:path',
        '0,0:aoe',
        '0,0:height',
        '0,0:cursor',
      ]);
  });

  it('uses camera projection and clamps optional visible bounds', () => {
    const plan = buildBattlePlan({
      canvasWidth: 640,
      canvasHeight: 360,
      gridWidth: 4,
      gridHeight: 4,
      elevation: new Array(16).fill(0),
      walkable: new Array(16).fill(true),
      theme: 'Memory',
      ambientTint: 'gold',
      camera: { camX: 1, camY: 1, zoom: 1 },
      visibleBounds: { minX: -5, maxX: 1, minY: 1, maxY: 99 },
    });

    expect(plan).toHaveLength(6);
    expect(plan.every((entry) => entry.x <= 1 && entry.y >= 1)).toBe(true);
    expect(plan.every((entry) => Number.isFinite(entry.screenX) && Number.isFinite(entry.screenY)))
      .toBe(true);
  });

  it('finds reachable cells while respecting budget, elevation and occupied cells', () => {
    const result = reachableCellsWithPathsFrom(
      battlefield,
      { x: 0, y: 0 },
      3,
      new Set(['1,0']),
    );

    expect(result.cells).not.toContain('0,0');
    expect(result.cells).not.toContain('1,0');
    expect(result.cells).toContain('0,1');
    expect(result.cells).toContain('1,1');
    expect(result.previous.get('1,1')).toEqual({ x: 0, y: 1 });
    expect(reachableCellsFrom(battlefield, { x: 0, y: 0 }, 0, new Set())).toEqual(new Set());
  });

  it('does not cross non-walkable cells or leave the battlefield', () => {
    const result = reachableCellsWithPathsFrom(
      { ...battlefield, walkable: [true, false, true, true, false, true, true, true, true] },
      { x: 0, y: 0 },
      5,
      new Set(),
    );

    expect(result.cells).not.toContain('1,0');
    expect(result.cells).not.toContain('1,1');
    expect([...result.cells].every((key) => !key.startsWith('-'))).toBe(true);
  });

  it('evaluates line of sight through floor holes and steep intermediate cells', () => {
    expect(hasLos(battlefield, { x: 0, y: 0 }, { x: 1, y: 0 })).toBe(true);
    expect(hasLos(battlefield, { x: 0, y: 0 }, { x: 2, y: 0 })).toBe(true);

    const hole = { ...battlefield, floor: [true, false, true, true, true, true, true, true, true] };
    expect(hasLos(hole, { x: 0, y: 0 }, { x: 2, y: 0 })).toBe(false);

    const cliff = { ...battlefield, elevation: [0, 3, 0, 0, 0, 0, 0, 0, 0] };
    expect(hasLos(cliff, { x: 0, y: 0 }, { x: 2, y: 0 })).toBe(false);
    expect(hasLos(cliff, { x: 0, y: 0 }, { x: 2, y: 0 }, 3)).toBe(true);
    expect(hasDirectLos(battlefield, { x: 0, y: 0 }, { x: 2, y: 0 })).toBe(true);
  });

  it('exposes stable grid geometry helpers', () => {
    expect(battleCellKey(4, 7)).toBe('4,7');
    expect(manhattan({ x: 1, y: 2 }, { x: 4, y: -2 })).toBe(7);
  });
});
