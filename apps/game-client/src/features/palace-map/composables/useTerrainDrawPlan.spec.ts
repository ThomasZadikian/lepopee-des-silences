import { describe, expect, it } from 'vitest';
import type { NodeDto } from '../../runs/types/runTypes';
import {
  buildDrawPlan,
  cameraTileUnit,
  isoUnit,
  projectToScreen,
  projectToScreenCamera,
  screenToCell,
  screenToCellCamera,
  unprojectFromScreen,
  unprojectFromScreenCamera,
  visibleCellRange,
  type CameraParams,
  type ProjectionParams,
} from './useTerrainDrawPlan';
import { TERRAIN_SPRITE_CONSTANTS } from './useTerrainSprites';
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

describe('isoUnit', () => {
  it('shrinks the tile so a short, wide canvas still fits the whole board', () => {
    // Deriving the unit from the width alone let the frontmost row run off the bottom of a
    // letterboxed viewport — the "one tile sticking out of the scene" case.
    const wideAndShort = { canvasWidth: 1900, canvasHeight: 500, gridWidth: 10, gridHeight: 8 };
    const roomy = { canvasWidth: 1900, canvasHeight: 1400, gridWidth: 10, gridHeight: 8 };

    expect(isoUnit(wideAndShort).isoUnitX).toBeLessThan(isoUnit(roomy).isoUnitX);
  });

  it('keeps the whole board inside the canvas on a short viewport', () => {
    const params = { canvasWidth: 1900, canvasHeight: 500, gridWidth: 10, gridHeight: 8 };
    const { isoUnitX } = isoUnit(params);
    const destW = isoUnitX * 2.05;
    const destH = (destW * TERRAIN_SPRITE_CONSTANTS.SPRITE_H) / TERRAIN_SPRITE_CONSTANTS.BASE_TILE_W;

    let lowest = -Infinity;
    for (let y = 0; y < 8; y++) {
      for (let x = 0; x < 10; x++) {
        const { screenY } = projectToScreen(x, y, params);
        lowest = Math.max(lowest, screenY + (destH * (1 - 0.5412)));
      }
    }

    expect(lowest).toBeLessThanOrEqual(params.canvasHeight);
  });

  it('never divides by zero on a degenerate grid', () => {
    expect(isoUnit({ canvasWidth: 100, canvasHeight: 100, gridWidth: 0, gridHeight: 0 }).isoUnitX).toBe(0);
  });
});

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
    ambientTint: 'frost' as const,
    theme: 'Threshold' as const,
    elevation: new Array(25).fill(0),
    obstacleCells: new Set<string>(),
    nodesByCell: new Map<string, NodeDto>(),
    nodeTintFor: () => 'blood' as const,
    party: null,
  };

  it('paints a cell with no node as an ambient-tinted floor, regardless of reveal state', () => {
    // There is no fog-of-war rendering anymore — terrain always paints at full visibility;
    // useGridCells.isRevealed still gates movement/clicks elsewhere, buildDrawPlan just no
    // longer reads it.
    const plan = buildDrawPlan(baseInput);
    const entry = plan.find((e) => e.x === 3 && e.y === 3)!;
    expect(entry.spriteKey).toMatchObject({ kind: 'floor', tint: 'frost', resolved: false, glow: false });
  });

  it('paints an obstacle cell as an obstacle, not floor', () => {
    const plan = buildDrawPlan({
      ...baseInput,
      obstacleCells: new Set(['1,1']),
    });
    const entry = plan.find((e) => e.x === 1 && e.y === 1)!;
    expect(entry.spriteKey).toMatchObject({ kind: 'obstacle', theme: 'Threshold' });
  });

  it('paints a node cell using the node tint, not the ambient theme tint', () => {
    const node = makeNode({ lane: 0, row: 0 });
    const plan = buildDrawPlan({
      ...baseInput,
      nodesByCell: new Map([['0,0', node]]),
      nodeTintFor: () => 'gold',
    });
    const entry = plan.find((e) => e.x === 0 && e.y === 0)!;
    expect(entry.spriteKey).toMatchObject({ kind: 'floor', tint: 'gold' });
  });

  it('gives a boss node a blood tint and a glow, regardless of nodeTintFor', () => {
    const boss = makeNode({ lane: 0, row: 0, isBoss: true, type: 'RoomBoss' });
    const plan = buildDrawPlan({
      ...baseInput,
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

  it('produces one entry per grid cell, plus ambient decor, plus one for the party when present', () => {
    // 25 floor cells + party: null (no exclusion) + Threshold's own themeProps pool (non-empty,
    // see SURFACE_EXTRA_PROP in tilecraft.js) → DECOR_MIN_COUNT (2) ambient decor props, since
    // 25 eligible cells / DECOR_CELL_SPAN (14) rounds down to 1, below the floor.
    const plan = buildDrawPlan(baseInput);
    expect(plan).toHaveLength(25 + 2);
    expect(plan.filter((e) => e.cellKey.startsWith('decor:'))).toHaveLength(2);

    const withParty = buildDrawPlan({ ...baseInput, party: { x: 0, y: 0 } });
    expect(withParty).toHaveLength(26 + 2);
  });

  describe('painted-tile fields', () => {
    it('stamps the room theme onto both floor and obstacle keys', () => {
      const plan = buildDrawPlan({
        ...baseInput,
        theme: 'Forest',
        obstacleCells: new Set(['1,1']),
      });

      expect(plan.find((e) => e.x === 0 && e.y === 0)!.spriteKey).toMatchObject({ theme: 'Forest' });
      expect(plan.find((e) => e.x === 1 && e.y === 1)!.spriteKey).toMatchObject({ theme: 'Forest' });
    });

    it('varies the brush seed across neighbours but keeps it stable for a given cell', () => {
      const first = buildDrawPlan(baseInput);
      const second = buildDrawPlan(baseInput);

      const seedAt = (plan: typeof first, x: number, y: number) => {
        const key = plan.find((e) => e.x === x && e.y === y)!.spriteKey;
        return key.kind === 'floor' ? key.surfaceSeed : undefined;
      };

      expect(seedAt(first, 2, 2)).toBe(seedAt(second, 2, 2));
      expect(seedAt(first, 0, 0)).not.toBe(seedAt(first, 1, 0));
      // Five brush variations, so every seed must land in 0..4.
      for (const entry of first) {
        if (entry.spriteKey.kind !== 'floor') continue;
        expect(entry.spriteKey.surfaceSeed).toBeGreaterThanOrEqual(0);
        expect(entry.spriteKey.surfaceSeed).toBeLessThan(5);
      }
    });

    it('grows cliff faces only on the grid edges its front neighbours fall off', () => {
      const plan = buildDrawPlan(baseInput);

      // (4, 2): front-left neighbour (5,2) is off-grid, front-right (4,3) is not.
      expect(plan.find((e) => e.x === 4 && e.y === 2)!.spriteKey)
        .toMatchObject({ cliffLeft: true, cliffRight: false });
      // (2, 4): the mirror case.
      expect(plan.find((e) => e.x === 2 && e.y === 4)!.spriteKey)
        .toMatchObject({ cliffLeft: false, cliffRight: true });
      // (4, 4): the outer corner — both.
      expect(plan.find((e) => e.x === 4 && e.y === 4)!.spriteKey)
        .toMatchObject({ cliffLeft: true, cliffRight: true });
      // An interior cell has no cliff at all.
      expect(plan.find((e) => e.x === 1 && e.y === 1)!.spriteKey)
        .toMatchObject({ cliffLeft: false, cliffRight: false });
    });

    it('lets a caller declare cells outside the room, carving cliffs inside the grid', () => {
      // Forward-looking: rooms are still full rectangles server-side, so this proves the hook
      // works before the backend can express a non-rectangular room.
      const plan = buildDrawPlan({
        ...baseInput,
        isFloor: (x, y) => !(x === 3 && y === 2) && x >= 0 && x < 5 && y >= 0 && y < 5,
      });

      // (2,2)'s front-left neighbour (3,2) is now outside the room.
      expect(plan.find((e) => e.x === 2 && e.y === 2)!.spriteKey).toMatchObject({ cliffLeft: true });
    });

    it('gives walls a stable per-cell silhouette variant', () => {
      const input = { ...baseInput, obstacleCells: new Set(['1,1', '2,3', '4,0']) };
      const first = buildDrawPlan(input);
      const second = buildDrawPlan(input);

      const variantAt = (plan: typeof first, x: number, y: number) => {
        const key = plan.find((e) => e.x === x && e.y === y)!.spriteKey;
        return key.kind === 'obstacle' ? key.variant : undefined;
      };

      expect(variantAt(first, 1, 1)).toBe(variantAt(second, 1, 1));
      for (const [x, y] of [[1, 1], [2, 3], [4, 0]]) {
        expect(variantAt(first, x, y)).toBeGreaterThanOrEqual(0);
        expect(variantAt(first, x, y)).toBeLessThan(3);
      }
    });

    it('sorts a wall at full height so a tall floor beside it never paints over it', () => {
      const elevation = new Array(25).fill(0);
      elevation[(1 * 5) + 2] = 3; // (2,1) is a tall floor, x+y=3
      const plan = buildDrawPlan({
        ...baseInput,
        elevation,
        obstacleCells: new Set(['1,2']), // (1,2) is a wall on flat ground, also x+y=3
      });

      const wall = plan.find((e) => e.x === 1 && e.y === 2)!;
      const tallFloor = plan.find((e) => e.x === 2 && e.y === 1)!;
      // Same diagonal, and the wall's ground elevation is 0 — without the full-height rule the
      // tall floor would sort after it and paint over the wall.
      expect(wall.sortKey).toBe(tallFloor.sortKey);
    });
  });

  describe('room shape', () => {
    it('paints nothing at all on a hole, and grows cliffs on the tiles around it', () => {
      const plan = buildDrawPlan({
        ...baseInput,
        isFloor: (x, y) => !(x === 3 && y === 2) && x >= 0 && x < 5 && y >= 0 && y < 5,
      });

      expect(plan.some((e) => e.x === 3 && e.y === 2)).toBe(false);
      // (2,2)'s front-left neighbour is now outside the room.
      expect(plan.find((e) => e.x === 2 && e.y === 2)!.spriteKey).toMatchObject({ cliffLeft: true });
    });
  });

  describe('danger tells', () => {
    it('paints the tell a contact node carries', () => {
      const node = makeNode({ lane: 1, row: 1, dangerTell: 'Tracks' });
      const plan = buildDrawPlan({ ...baseInput, nodesByCell: new Map([['1,1', node]]) });

      expect(plan.find((e) => e.x === 1 && e.y === 1)!.spriteKey).toMatchObject({ danger: 'tracks' });
    });

    it('leaves an ambush indistinguishable from plain floor', () => {
      // DangerTell 'None' on a contact node IS the ambush — it must paint exactly like the
      // empty tile beside it, tell included.
      const ambush = makeNode({ lane: 1, row: 1, dangerTell: 'None', contactBehavior: 'TriggerOnEnter' });
      const plan = buildDrawPlan({ ...baseInput, nodesByCell: new Map([['1,1', ambush]]) });

      const ambushTile = plan.find((e) => e.x === 1 && e.y === 1)!;
      const plainTile = plan.find((e) => e.x === 2 && e.y === 2)!;

      expect(ambushTile.spriteKey).toMatchObject({ danger: 'none' });
      expect((ambushTile.spriteKey as { danger?: string }).danger)
        .toBe((plainTile.spriteKey as { danger?: string }).danger);
    });

    it('drops the tell once the node is resolved', () => {
      const spent = makeNode({ lane: 1, row: 1, dangerTell: 'Glow', state: 'Resolved' });
      const plan = buildDrawPlan({ ...baseInput, nodesByCell: new Map([['1,1', spent]]) });

      expect(plan.find((e) => e.x === 1 && e.y === 1)!.spriteKey).toMatchObject({ danger: 'none' });
    });
  });

  describe('event props', () => {
    it('stands a campfire on a Rest node and a figure on an NPC', () => {
      const plan = buildDrawPlan({
        ...baseInput,
        nodesByCell: new Map([
          ['1,1', makeNode({ lane: 1, row: 1, type: 'Rest' })],
          ['2,2', makeNode({ lane: 2, row: 2, type: 'Npc' })],
        ]),
      });

      expect(plan.find((e) => e.cellKey === 'prop:1,1')!.spriteKey)
        .toMatchObject({ kind: 'prop', prop: 'campfire' });
      expect(plan.find((e) => e.cellKey === 'prop:2,2')!.spriteKey)
        .toMatchObject({ kind: 'prop', prop: 'npc' });
    });

    it('stands the beast on a combat node, the horned one on an elite', () => {
      const plan = buildDrawPlan({
        ...baseInput,
        nodesByCell: new Map([
          ['1,1', makeNode({ lane: 1, row: 1, type: 'Combat' })],
          ['2,2', makeNode({ lane: 2, row: 2, type: 'Elite' })],
        ]),
      });

      expect(plan.find((e) => e.cellKey === 'prop:1,1')!.spriteKey)
        .toMatchObject({ kind: 'prop', prop: 'monster' });
      expect(plan.find((e) => e.cellKey === 'prop:2,2')!.spriteKey)
        .toMatchObject({ kind: 'prop', prop: 'elite' });
    });

    it('stands the boss silhouette on the room objective', () => {
      const plan = buildDrawPlan({
        ...baseInput,
        nodesByCell: new Map([
          ['1,1', makeNode({ lane: 1, row: 1, type: 'RoomBoss', isBoss: true })],
        ]),
      });

      expect(plan.find((e) => e.cellKey === 'prop:1,1')!.spriteKey)
        .toMatchObject({ kind: 'prop', prop: 'boss' });
    });

    it('clears the prop once the node is spent', () => {
      const plan = buildDrawPlan({
        ...baseInput,
        nodesByCell: new Map([['1,1', makeNode({ lane: 1, row: 1, type: 'Rest', state: 'Resolved' })]]),
      });

      expect(plan.some((e) => e.cellKey === 'prop:1,1')).toBe(false);
    });

    it('sorts a prop above its own tile but below the party standing there', () => {
      const plan = buildDrawPlan({
        ...baseInput,
        nodesByCell: new Map([['2,2', makeNode({ lane: 2, row: 2, type: 'Rest' })]]),
        party: { x: 2, y: 2 },
      });

      const tile = plan.find((e) => e.x === 2 && e.y === 2 && e.spriteKey.kind === 'floor')!;
      const prop = plan.find((e) => e.cellKey === 'prop:2,2')!;
      const party = plan.find((e) => e.spriteKey.kind === 'party')!;

      expect(prop.sortKey).toBeGreaterThan(tile.sortKey);
      expect(party.sortKey).toBeGreaterThan(prop.sortKey);
    });
  });

  describe('doors and ambient decor', () => {
    it('stands a threshold prop on every door cell', () => {
      const plan = buildDrawPlan({
        ...baseInput,
        doorCells: new Set(['2,2']),
      });

      expect(plan.find((e) => e.cellKey === 'door:2,2')!.spriteKey)
        .toMatchObject({ kind: 'prop', prop: 'threshold' });
    });

    it('never places ambient decor on a node, obstacle, door or the party cell', () => {
      const plan = buildDrawPlan({
        ...baseInput,
        nodesByCell: new Map([['1,1', makeNode({ lane: 1, row: 1, type: 'Rest' })]]),
        obstacleCells: new Set(['2,2']),
        doorCells: new Set(['3,3']),
        party: { x: 0, y: 0 },
      });

      const decorCells = plan
        .filter((e) => e.cellKey.startsWith('decor:'))
        .map((e) => `${e.x},${e.y}`);

      expect(decorCells).not.toContain('1,1');
      expect(decorCells).not.toContain('2,2');
      expect(decorCells).not.toContain('3,3');
      expect(decorCells).not.toContain('0,0');
    });

    it('produces the same decor for the same grid across repaints', () => {
      const first = buildDrawPlan(baseInput).filter((e) => e.cellKey.startsWith('decor:'));
      const second = buildDrawPlan(baseInput).filter((e) => e.cellKey.startsWith('decor:'));

      expect(first.map((e) => e.cellKey)).toEqual(second.map((e) => e.cellKey));
      expect(first.map((e) => e.spriteKey)).toEqual(second.map((e) => e.spriteKey));
    });

    it('never exceeds DECOR_MAX_COUNT even on a very large room', () => {
      const size = 35;
      const plan = buildDrawPlan({
        ...baseInput,
        cells: cellsFor(size, size),
        gridWidth: size,
        gridHeight: size,
        elevation: new Array(size * size).fill(0),
      });

      const decorCount = plan.filter((e) => e.cellKey.startsWith('decor:')).length;
      expect(decorCount).toBeGreaterThan(0);
      expect(decorCount).toBeLessThanOrEqual(12);
    });
  });

  describe('authored surface/decor placement (RoomGridDto.surfaceOverrides/decorPlacements)', () => {
    it('overrides a floor tile surface only on the cell it names', () => {
      const plan = buildDrawPlan({
        ...baseInput,
        surfaceOverrides: new Map([['2,2', 'carpet']]),
      });

      const overridden = plan.find((e) => e.x === 2 && e.y === 2)!;
      const plain = plan.find((e) => e.x === 0 && e.y === 0)!;
      expect(overridden.spriteKey).toMatchObject({ kind: 'floor', surface: 'carpet' });
      expect(plain.spriteKey).toMatchObject({ kind: 'floor', surface: undefined });
    });

    it('paints an authored decor placement as its own prop, independent of ambient decor', () => {
      const plan = buildDrawPlan({
        ...baseInput,
        decorPlacements: new Map([['2,2', 'column']]),
      });

      expect(plan.find((e) => e.cellKey === 'authored-decor:2,2')!.spriteKey)
        .toMatchObject({ kind: 'prop', prop: 'column' });
    });

    it('never scatters ambient decor onto an authored decor cell', () => {
      const plan = buildDrawPlan({
        ...baseInput,
        decorPlacements: new Map([['1,1', 'column']]),
      });

      const decorCells = plan
        .filter((e) => e.cellKey.startsWith('decor:'))
        .map((e) => `${e.x},${e.y}`);

      expect(decorCells).not.toContain('1,1');
    });
  });

  describe('hidden caches', () => {
    it('paints a hollow-sounding slab on a cell holding an unfound cache', () => {
      // The position is all the client ever gets — no node, no type, no reward. Without this
      // tell the player would have nothing to notice and no reason to spend budget searching.
      const plan = buildDrawPlan({ ...baseInput, hintCells: new Set(['3,1']) });

      expect(plan.find((e) => e.x === 3 && e.y === 1)!.spriteKey).toMatchObject({ hidden: 'hint' });
      expect(plan.find((e) => e.x === 0 && e.y === 0)!.spriteKey).toMatchObject({ hidden: 'none' });
    });

    it('opens the alcove once the cache has been searched out', () => {
      const found = makeNode({ lane: 3, row: 1, type: 'Item', hiddenState: 'Revealed' });
      const plan = buildDrawPlan({
        ...baseInput,
        nodesByCell: new Map([['3,1', found]]),
      });

      expect(plan.find((e) => e.x === 3 && e.y === 1)!.spriteKey).toMatchObject({ hidden: 'revealed' });
    });
  });

  describe('reachable cells', () => {
    it('marks each reachable cell with a move highlight at its own elevation', () => {
      const elevation = new Array(25).fill(0);
      elevation[(1 * 5) + 2] = 2; // (2,1)
      const plan = buildDrawPlan({
        ...baseInput,
        elevation,
        reachableCells: new Set(['2,1', '3,3']),
      });

      expect(plan.find((e) => e.cellKey === 'move:2,1')!.spriteKey)
        .toMatchObject({ kind: 'highlight', variant: 'move', elevation: 2 });
      expect(plan.find((e) => e.cellKey === 'move:3,3')).toBeDefined();
    });

    it('keeps the hovered cell readable on top of its own reachable wash', () => {
      const plan = buildDrawPlan({
        ...baseInput,
        reachableCells: new Set(['2,2']),
        hoveredCell: { x: 2, y: 2 },
      });

      const move = plan.find((e) => e.cellKey === 'move:2,2')!;
      const cursor = plan.find((e) => e.cellKey === 'hover')!;

      expect(cursor.sortKey).toBeGreaterThan(move.sortKey);
    });

    it('adds nothing when no cell is reachable', () => {
      const plan = buildDrawPlan({ ...baseInput, reachableCells: new Set<string>() });
      expect(plan.some((e) => e.cellKey.startsWith('move:'))).toBe(false);
    });
  });

  describe('hover highlight', () => {
    it('adds no entry when nothing is hovered', () => {
      const plan = buildDrawPlan(baseInput);
      expect(plan.some((e) => e.spriteKey.kind === 'highlight')).toBe(false);
    });

    it('paints over the hovered tile but under a party token standing on it', () => {
      const plan = buildDrawPlan({
        ...baseInput,
        hoveredCell: { x: 2, y: 2 },
        party: { x: 2, y: 2 },
      });

      const tile = plan.find((e) => e.x === 2 && e.y === 2 && e.spriteKey.kind === 'floor')!;
      const highlight = plan.find((e) => e.spriteKey.kind === 'highlight')!;
      const party = plan.find((e) => e.spriteKey.kind === 'party')!;

      expect(highlight.sortKey).toBeGreaterThan(tile.sortKey);
      expect(party.sortKey).toBeGreaterThan(highlight.sortKey);
    });

    it("takes the hovered cell's own elevation so it hugs a raised tile", () => {
      const elevation = new Array(25).fill(0);
      elevation[(3 * 5) + 1] = 2; // (1,3)
      const plan = buildDrawPlan({ ...baseInput, elevation, hoveredCell: { x: 1, y: 3 } });

      expect(plan.find((e) => e.spriteKey.kind === 'highlight')!.spriteKey)
        .toMatchObject({ kind: 'highlight', variant: 'cursor', elevation: 2 });
    });
  });

  describe('party token', () => {
    it('is omitted entirely when there is no party (e.g. before a grid exists)', () => {
      const plan = buildDrawPlan(baseInput);
      expect(plan.some((e) => e.spriteKey.kind === 'party')).toBe(false);
    });

    it('projects to the same screen position as the cell it stands on', () => {
      const plan = buildDrawPlan({ ...baseInput, party: { x: 2, y: 3 } });
      const partyEntry = plan.find((e) => e.spriteKey.kind === 'party')!;
      const cellEntry = plan.find((e) => e.x === 2 && e.y === 3 && e.spriteKey.kind !== 'party')!;
      expect(partyEntry.screenX).toBe(cellEntry.screenX);
      expect(partyEntry.screenY).toBe(cellEntry.screenY);
    });

    it("reads its own elevation from the cell it's standing on", () => {
      const elevation = new Array(25).fill(0);
      elevation[(3 * 5) + 2] = 2; // cell (x=2, y=3)
      const plan = buildDrawPlan({ ...baseInput, elevation, party: { x: 2, y: 3 } });
      const partyEntry = plan.find((e) => e.spriteKey.kind === 'party')!;
      expect(partyEntry.spriteKey).toMatchObject({ kind: 'party', elevation: 2 });
    });

    it('sorts strictly after its own floor tile, so it paints on top of the ground it stands on', () => {
      const plan = buildDrawPlan({ ...baseInput, party: { x: 2, y: 3 } });
      const partyEntry = plan.find((e) => e.spriteKey.kind === 'party')!;
      const cellEntry = plan.find((e) => e.x === 2 && e.y === 3 && e.spriteKey.kind !== 'party')!;
      expect(partyEntry.sortKey).toBeGreaterThan(cellEntry.sortKey);
    });

    it('still sorts behind a taller tile further along the diagonal (gets occluded by it)', () => {
      const elevation = new Array(25).fill(0);
      elevation[(0 * 5) + 4] = 3; // (4,0): x+y=4, elevation 3
      // Party at (2,2): x+y=4, elevation 0 — same diagonal sum, but shorter.
      const plan = buildDrawPlan({ ...baseInput, elevation, party: { x: 2, y: 2 } });
      const partyEntry = plan.find((e) => e.spriteKey.kind === 'party')!;
      const tallTile = plan.find((e) => e.x === 4 && e.y === 0)!;
      expect(tallTile.sortKey).toBeGreaterThan(partyEntry.sortKey);
    });
  });
});

describe('screenToCell', () => {
  const screenToCellBase = {
    gridWidth: 5,
    gridHeight: 5,
    canvasWidth: PARAMS.canvasWidth,
    canvasHeight: PARAMS.canvasHeight,
    elevation: new Array(25).fill(0),
    obstacleCells: new Set<string>(),
  };

  function liftPx(elevationLevel: number): number {
    const { isoUnitX } = isoUnit(PARAMS);
    const destW = isoUnitX * 2.05;
    return (destW / TERRAIN_SPRITE_CONSTANTS.BASE_TILE_W) * TERRAIN_SPRITE_CONSTANTS.BASE_STEP_PX * elevationLevel;
  }

  it('matches the flat projection when every cell is at elevation 0 (parity with the old inverse)', () => {
    for (const [x, y] of [[0, 0], [2, 2], [4, 4], [1, 3]]) {
      const { screenX, screenY } = projectToScreen(x, y, PARAMS);
      expect(screenToCell({ ...screenToCellBase, screenX, screenY })).toEqual({ x, y });
    }
  });

  it('hits an elevated cell where it is actually drawn, not at its flat footprint', () => {
    const elevation = new Array(25).fill(0);
    elevation[(2 * 5) + 2] = 3; // cell (2,2)
    const { screenX, screenY } = projectToScreen(2, 2, PARAMS);
    const liftedY = screenY - liftPx(3);

    expect(screenToCell({ ...screenToCellBase, elevation, screenX, screenY: liftedY })).toEqual({ x: 2, y: 2 });
  });

  it('no longer resolves a click at an elevated cell\'s old flat position to that cell', () => {
    const elevation = new Array(25).fill(0);
    elevation[(2 * 5) + 2] = 3;
    const { screenX, screenY } = projectToScreen(2, 2, PARAMS);

    expect(screenToCell({ ...screenToCellBase, elevation, screenX, screenY })).not.toEqual({ x: 2, y: 2 });
  });

  it('hit-tests an obstacle at its own elevation, not at max', () => {
    // The reverse of what this file used to assert. Testing walls at MAX_ELEVATION lifted their
    // clickable diamond three steps above where the tile is painted, so it drifted over the
    // cells BEHIND the wall and — ranking highest — stole their clicks. Depth ORDER still pins
    // obstacles at MAX so a wall paints in front; that is a painting concern only.
    const obstacleCells = new Set(['1,1']);
    const flat = projectToScreen(1, 1, PARAMS);

    expect(screenToCell({ ...screenToCellBase, obstacleCells, screenX: flat.screenX, screenY: flat.screenY }))
      .toEqual({ x: 1, y: 1 });

    const liftedY = flat.screenY - liftPx(TERRAIN_SPRITE_CONSTANTS.MAX_ELEVATION);
    expect(screenToCell({ ...screenToCellBase, obstacleCells, screenX: flat.screenX, screenY: liftedY }))
      .not.toEqual({ x: 1, y: 1 });
  });

  it('gives the click to open ground when a wall and a walkable cell both cover the pointer', () => {
    // Directly the reported complaint: a cell tucked behind a wall was hard to pick, because the
    // wall won every overlap. A wall is never a destination, so it only wins when nothing else
    // is under the pointer.
    const obstacleCells = new Set(['2,2']);
    const { screenX, screenY } = projectToScreen(2, 2, PARAMS);

    // Nudge toward the neighbour that overlaps this diamond's edge.
    const nudged = { screenX: screenX + 1, screenY: screenY + 1 };
    const hit = screenToCell({ ...screenToCellBase, obstacleCells, ...nudged });

    // Whatever it resolves to, it is allowed to be the wall ONLY if no walkable cell covers the
    // same point — which the plain (no-obstacle) run tells us.
    const withoutObstacle = screenToCell({ ...screenToCellBase, ...nudged });
    if (withoutObstacle && !(withoutObstacle.x === 2 && withoutObstacle.y === 2)) {
      expect(hit).toEqual(withoutObstacle);
    }
  });

  it('resolves overlapping diamonds toward the higher sortKey (the visually front-most tile)', () => {
    const elevation = new Array(25).fill(0);
    elevation[(0 * 5) + 1] = 3; // cell (1,0): sortKey (1+0)*4+3=7, vs cell (0,0)'s sortKey 0

    const a = projectToScreen(0, 0, PARAMS);
    const bFlat = projectToScreen(1, 0, PARAMS);
    const b = { screenX: bFlat.screenX, screenY: bFlat.screenY - liftPx(3) };
    const midpoint = { screenX: (a.screenX + b.screenX) / 2, screenY: (a.screenY + b.screenY) / 2 };

    expect(screenToCell({ ...screenToCellBase, elevation, ...midpoint })).toEqual({ x: 1, y: 0 });
  });

  it('returns null well outside any tile', () => {
    expect(screenToCell({ ...screenToCellBase, screenX: -500, screenY: -500 })).toBeNull();
  });
});

// Combat (TacticalCombatScene.vue, useTacticalBattlePlan.ts, useSortEffects.ts) imports and
// calls isoUnit/projectToScreen/screenToCell directly — never anything camera-aware. Adding the
// camera layer must not change what these produce for a "combat-shaped" call (small fixed grid,
// no camera params), or combat's rendering/hit-testing would silently drift. This is the safety
// net the exploration-camera plan calls for.
describe('combat-shape regression — camera layer does not change existing exports', () => {
  const COMBAT_PARAMS: ProjectionParams = { canvasWidth: 900, canvasHeight: 600, gridWidth: 8, gridHeight: 6 };

  it('projectToScreen is unaffected by the camera additions', () => {
    for (const [x, y] of [[0, 0], [3, 2], [7, 5]]) {
      expect(projectToScreen(x, y, COMBAT_PARAMS)).toEqual({
        screenX: (COMBAT_PARAMS.canvasWidth / 2) - ((x - y) * isoUnit(COMBAT_PARAMS).isoUnitX),
        screenY: expect.any(Number),
      });
    }
  });

  it('screenToCell round-trips through projectToScreen exactly as before', () => {
    const base = {
      gridWidth: COMBAT_PARAMS.gridWidth,
      gridHeight: COMBAT_PARAMS.gridHeight,
      canvasWidth: COMBAT_PARAMS.canvasWidth,
      canvasHeight: COMBAT_PARAMS.canvasHeight,
      elevation: new Array(COMBAT_PARAMS.gridWidth * COMBAT_PARAMS.gridHeight).fill(0),
      obstacleCells: new Set<string>(),
    };
    for (const [x, y] of [[0, 0], [3, 2], [7, 5]]) {
      const { screenX, screenY } = projectToScreen(x, y, COMBAT_PARAMS);
      expect(screenToCell({ ...base, screenX, screenY })).toEqual({ x, y });
    }
  });
});

describe('projectToScreenCamera / unprojectFromScreenCamera', () => {
  const CANVAS = { canvasWidth: 800, canvasHeight: 600 };
  const CENTERED: CameraParams = { camX: 5, camY: 5, zoom: 1 };

  it('places the camera focus point at the canvas center', () => {
    const { screenX, screenY } = projectToScreenCamera(CENTERED.camX, CENTERED.camY, { ...CANVAS, ...CENTERED });
    expect(screenX).toBeCloseTo(CANVAS.canvasWidth / 2, 5);
  });

  it('is independent of grid size, unlike the fit-to-canvas projection', () => {
    const { isoUnitX: smallGrid } = cameraTileUnit(CENTERED);
    const { isoUnitX: sameZoomDifferentFocus } = cameraTileUnit({ ...CENTERED, camX: 40, camY: 40 });
    expect(smallGrid).toBe(sameZoomDifferentFocus);
  });

  it('scales tile size with zoom', () => {
    const base = cameraTileUnit({ ...CENTERED, zoom: 1 });
    const doubled = cameraTileUnit({ ...CENTERED, zoom: 2 });
    expect(doubled.isoUnitX).toBeCloseTo(base.isoUnitX * 2, 5);
  });

  it('round-trips screen -> grid -> screen for an off-center cell', () => {
    const params = { ...CANVAS, ...CENTERED };
    for (const [x, y] of [[3, 3], [8, 2], [5, 9]]) {
      const { screenX, screenY } = projectToScreenCamera(x, y, params);
      expect(unprojectFromScreenCamera(screenX, screenY, params)).toEqual({ x, y });
    }
  });
});

describe('visibleCellRange', () => {
  it('centers the visible range on the camera focus point', () => {
    const camera: CameraParams = { camX: 20, camY: 15, zoom: 1 };
    const bounds = visibleCellRange(camera, 800, 600, 40, 30, 2);
    const midX = (bounds.minX + bounds.maxX) / 2;
    const midY = (bounds.minY + bounds.maxY) / 2;
    // Rounded to whole grid cells at each of the four corners, so the midpoint can land up to
    // half a cell off the exact focus point rather than exactly on it.
    expect(Math.abs(midX - camera.camX)).toBeLessThanOrEqual(1);
    expect(Math.abs(midY - camera.camY)).toBeLessThanOrEqual(1);
  });

  it('clamps to the grid when the camera sits near a corner', () => {
    const camera: CameraParams = { camX: 0, camY: 0, zoom: 1 };
    const bounds = visibleCellRange(camera, 800, 600, 40, 30, 2);
    expect(bounds.minX).toBeGreaterThanOrEqual(0);
    expect(bounds.minY).toBeGreaterThanOrEqual(0);
    expect(bounds.maxX).toBeLessThanOrEqual(39);
    expect(bounds.maxY).toBeLessThanOrEqual(29);
  });
});

describe('screenToCellCamera', () => {
  const camera: CameraParams = { camX: 4, camY: 4, zoom: 1 };
  const base = {
    canvasWidth: 800,
    canvasHeight: 600,
    gridWidth: 9,
    gridHeight: 9,
    camera,
    elevation: new Array(81).fill(0),
    obstacleCells: new Set<string>(),
  };

  it('resolves a click at a projected cell back to that same cell', () => {
    for (const [x, y] of [[4, 4], [2, 6], [7, 1]]) {
      const { screenX, screenY } = projectToScreenCamera(x, y, { canvasWidth: base.canvasWidth, canvasHeight: base.canvasHeight, ...camera });
      expect(screenToCellCamera({ ...base, screenX, screenY })).toEqual({ x, y });
    }
  });

  it('finds nothing outside a restricted visibleBounds even if the projected point matches', () => {
    const { screenX, screenY } = projectToScreenCamera(8, 8, { canvasWidth: base.canvasWidth, canvasHeight: base.canvasHeight, ...camera });
    const restricted = screenToCellCamera({
      ...base, screenX, screenY,
      visibleBounds: { minX: 0, maxX: 3, minY: 0, maxY: 3 },
    });
    expect(restricted).toBeNull();
  });
});
