import { describe, expect, it } from 'vitest';
import type { NodeDto } from '../../runs/types/runTypes';
import { buildDrawPlan, projectToScreen, unprojectFromScreen, type ProjectionParams } from './useTerrainDrawPlan';
import type { Cell } from './useGridCells';

const PARAMS: ProjectionParams = { canvasWidth: 200, canvasHeight: 200, gridWidth: 5, gridHeight: 5 };

function makeNode(overrides: Partial<NodeDto> = {}): NodeDto {
  return {
    id: 'node-1',
    type: 'Combat',
    row: 0,
    lane: 0,
    riskLevel: 40,
    rewardProfile: 'combat-common',
    parentNodeIds: [],
    state: 'Available',
    isBoss: false,
    isInitial: false,
    hasChosenEventOption: false,
    ...overrides,
  };
}

function cellsFor(width: number, height: number): Cell[] {
  const cells: Cell[] = [];
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      cells.push({ x, y });
    }
  }
  return cells;
}

describe('projectToScreen / unprojectFromScreen', () => {
  it('round-trips a handful of interior cells back to themselves', () => {
    for (const [x, y] of [[0, 0], [2, 2], [4, 4], [1, 3], [3, 0]]) {
      const { screenX, screenY } = projectToScreen(x, y, PARAMS);
      const back = unprojectFromScreen(screenX, screenY, PARAMS);
      expect(back).toEqual({ x, y });
    }
  });

  it('projects distinct cells to distinct screen positions', () => {
    const a = projectToScreen(0, 0, PARAMS);
    const b = projectToScreen(1, 0, PARAMS);
    const c = projectToScreen(0, 1, PARAMS);
    expect(a).not.toEqual(b);
    expect(a).not.toEqual(c);
    expect(b).not.toEqual(c);
  });

  it('does not throw or divide-by-zero-crash on a zero-size canvas', () => {
    const degenerate: ProjectionParams = { canvasWidth: 0, canvasHeight: 0, gridWidth: 5, gridHeight: 5 };
    expect(() => projectToScreen(2, 2, degenerate)).not.toThrow();
    expect(() => unprojectFromScreen(0, 0, degenerate)).not.toThrow();
  });
});

describe('buildDrawPlan', () => {
  const baseInput = {
    cells: cellsFor(5, 5),
    gridWidth: 5,
    gridHeight: 5,
    canvasWidth: PARAMS.canvasWidth,
    canvasHeight: PARAMS.canvasHeight,
    roomId: 'room-1',
    ambientTint: 'frost' as const,
    elevation: new Array(25).fill(0),
    revealedCells: new Set<string>(),
    obstacleCells: new Set<string>(),
    nodesByCell: new Map<string, NodeDto>(),
    nodeTintFor: () => 'blood' as const,
  };

  it('paints an unrevealed cell with no node as fog', () => {
    const plan = buildDrawPlan(baseInput);
    const entry = plan.find((e) => e.x === 3 && e.y === 3)!;
    expect(entry.spriteKey.kind).toBe('fog');
  });

  it('paints a revealed cell with no node as an ambient-tinted floor', () => {
    const plan = buildDrawPlan({ ...baseInput, revealedCells: new Set(['2,2']) });
    const entry = plan.find((e) => e.x === 2 && e.y === 2)!;
    expect(entry.spriteKey).toMatchObject({ kind: 'floor', tint: 'frost', resolved: false, glow: false, light: 'lit' });
  });

  it('paints a revealed obstacle cell as an obstacle, not floor or fog', () => {
    const plan = buildDrawPlan({
      ...baseInput,
      revealedCells: new Set(['1,1']),
      obstacleCells: new Set(['1,1']),
    });
    const entry = plan.find((e) => e.x === 1 && e.y === 1)!;
    expect(entry.spriteKey).toEqual({ kind: 'obstacle', light: 'lit' });
  });

  it('paints an unrevealed obstacle cell (no node) as fog — terrain shape stays hidden until revealed', () => {
    const plan = buildDrawPlan({
      ...baseInput,
      obstacleCells: new Set(['1,1']),
    });
    const entry = plan.find((e) => e.x === 1 && e.y === 1)!;
    expect(entry.spriteKey.kind).toBe('fog');
  });

  it('paints a revealed node cell using the node tint, not the ambient theme tint', () => {
    const node = makeNode({ lane: 0, row: 0 });
    const plan = buildDrawPlan({
      ...baseInput,
      revealedCells: new Set(['0,0']),
      nodesByCell: new Map([['0,0', node]]),
      nodeTintFor: () => 'gold',
    });
    const entry = plan.find((e) => e.x === 0 && e.y === 0)!;
    expect(entry.spriteKey).toMatchObject({ kind: 'floor', tint: 'gold', light: 'lit' });
  });

  it('paints an unrevealed node cell as a ghost floor tile, not fog — the objective marker rule', () => {
    const node = makeNode({ lane: 4, row: 4, state: 'Available' });
    const plan = buildDrawPlan({
      ...baseInput,
      nodesByCell: new Map([['4,4', node]]),
    });
    const entry = plan.find((e) => e.x === 4 && e.y === 4)!;
    expect(entry.spriteKey).toMatchObject({ kind: 'floor', light: 'ghost' });
  });

  it('gives a boss node a blood tint and a glow, regardless of nodeTintFor', () => {
    const boss = makeNode({ lane: 0, row: 0, isBoss: true, type: 'RoomBoss' });
    const plan = buildDrawPlan({
      ...baseInput,
      revealedCells: new Set(['0,0']),
      nodesByCell: new Map([['0,0', boss]]),
      nodeTintFor: () => 'sap',
    });
    const entry = plan.find((e) => e.x === 0 && e.y === 0)!;
    expect(entry.spriteKey).toMatchObject({ kind: 'floor', tint: 'blood', glow: true });
  });

  it('marks a resolved node tile as resolved', () => {
    const resolved = makeNode({ lane: 0, row: 0, state: 'Resolved' });
    const plan = buildDrawPlan({
      ...baseInput,
      revealedCells: new Set(['0,0']),
      nodesByCell: new Map([['0,0', resolved]]),
    });
    const entry = plan.find((e) => e.x === 0 && e.y === 0)!;
    expect(entry.spriteKey).toMatchObject({ kind: 'floor', resolved: true });
  });

  it('reads elevation from the flat row-major array at the right index', () => {
    const elevation = new Array(25).fill(0);
    elevation[(2 * 5) + 3] = 2; // cell (x=3, y=2)
    const plan = buildDrawPlan({
      ...baseInput,
      elevation,
      revealedCells: new Set(['3,2']),
    });
    const entry = plan.find((e) => e.x === 3 && e.y === 2)!;
    expect(entry.spriteKey).toMatchObject({ kind: 'floor', elevation: 2 });
  });

  it('sorts back-to-front: lower (x+y) sums come before higher ones', () => {
    const plan = buildDrawPlan(baseInput);
    for (let i = 1; i < plan.length; i++) {
      expect(plan[i].sortKey).toBeGreaterThanOrEqual(plan[i - 1].sortKey);
    }
  });

  it('breaks a same-(x+y) tie by elevation: a taller tile sorts after a shorter one', () => {
    const elevation = new Array(25).fill(0);
    elevation[(0 * 5) + 2] = 3; // (2,0): x+y=2
    // (1,1) also has x+y=2, elevation 0 by default.
    const plan = buildDrawPlan({ ...baseInput, elevation });
    const shorter = plan.find((e) => e.x === 1 && e.y === 1)!;
    const taller = plan.find((e) => e.x === 2 && e.y === 0)!;
    expect(taller.sortKey).toBeGreaterThan(shorter.sortKey);
  });

  it('produces exactly one entry per grid cell', () => {
    const plan = buildDrawPlan(baseInput);
    expect(plan).toHaveLength(25);
  });
});
