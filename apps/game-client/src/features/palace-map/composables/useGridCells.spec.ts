import { describe, expect, it } from 'vitest';
import { computed } from 'vue';
import { useGridCells } from './useGridCells';
import type { NodeDto, RoomDto, RoomGridDto } from '../../runs/types/runTypes';

function makeNode(overrides: Partial<NodeDto> = {}): NodeDto {
  return {
    id: 'node-1',
    type: 'Combat',
    row: 1,
    lane: 2,
    riskLevel: 50,
    rewardProfile: 'combat-common',
    parentNodeIds: [],
    state: 'Available',
    isBoss: false,
    isInitial: false,
    hasChosenEventOption: false,
    ...overrides,
  };
}

function makeGrid(overrides: Partial<RoomGridDto> = {}): RoomGridDto {
  return {
    width: 4,
    height: 4,
    partyX: 0,
    partyY: 0,
    doorCells: [],
    revealedCells: [[0, 0], [1, 0]],
    elevation: new Array(16).fill(0),
    obstacleCells: [],
    floorCells: [],
    canSearch: false,
    hintCells: [],
    ...overrides,
  };
}

function makeRoom(grid: RoomGridDto, nodes: NodeDto[] = []): RoomDto {
  return {
    id: 'room-1',
    depth: 0,
    roomType: 'Combat',
    theme: 'Threshold',
    state: 'Active',
    currentNodeDepth: 0,
    maxNodeDepth: 0,
    totalNodeCount: 0,
    bossPreview: { bossId: 'boss-1', name: 'Boss', roomType: 'RoomBoss', dangerHint: 'High' },
    nodes,
    availableNodes: [],
    layoutTemplateKey: null,
    layoutTemplateVersion: null,
    grid,
  };
}

describe('useGridCells', () => {
  it('reports only revealed cells as revealed', () => {
    const grid = makeGrid();
    const room = computed(() => makeRoom(grid));
    const gridRef = computed(() => grid);
    const { isRevealed } = useGridCells(room, gridRef);

    expect(isRevealed(0, 0)).toBe(true);
    expect(isRevealed(1, 0)).toBe(true);
    expect(isRevealed(2, 0)).toBe(false);
  });

  it('finds the node sitting on a given (lane, row) cell', () => {
    const grid = makeGrid();
    const node = makeNode({ lane: 2, row: 1 });
    const room = computed(() => makeRoom(grid, [node]));
    const gridRef = computed(() => grid);
    const { nodeAt } = useGridCells(room, gridRef);

    expect(nodeAt(2, 1)).toEqual(node);
    expect(nodeAt(0, 0)).toBeNull();
  });

  it('identifies the cell the party currently stands on', () => {
    const grid = makeGrid({ partyX: 3, partyY: 2 });
    const room = computed(() => makeRoom(grid));
    const gridRef = computed(() => grid);
    const { isParty } = useGridCells(room, gridRef);

    expect(isParty(3, 2)).toBe(true);
    expect(isParty(0, 0)).toBe(false);
  });

  it('returns false for isParty when there is no grid', () => {
    const room = computed(() => makeRoom(makeGrid()));
    const gridRef = computed(() => null);
    const { isParty } = useGridCells(room, gridRef);

    expect(isParty(0, 0)).toBe(false);
  });

  it('lists every cell in the grid, row-major', () => {
    const grid = makeGrid({ width: 2, height: 2 });
    const room = computed(() => makeRoom(grid));
    const gridRef = computed(() => grid);
    const { cells } = useGridCells(room, gridRef);

    expect(cells.value).toEqual([
      { x: 0, y: 0 }, { x: 1, y: 0 },
      { x: 0, y: 1 }, { x: 1, y: 1 },
    ]);
  });

  it('returns an empty cell list when there is no grid', () => {
    const room = computed(() => makeRoom(makeGrid()));
    const gridRef = computed(() => null);
    const { cells } = useGridCells(room, gridRef);

    expect(cells.value).toEqual([]);
  });

  it('reports only obstacle cells as obstacles', () => {
    const grid = makeGrid({ obstacleCells: [[2, 1], [3, 3]] });
    const room = computed(() => makeRoom(grid));
    const gridRef = computed(() => grid);
    const { isObstacle } = useGridCells(room, gridRef);

    expect(isObstacle(2, 1)).toBe(true);
    expect(isObstacle(3, 3)).toBe(true);
    expect(isObstacle(0, 0)).toBe(false);
  });

  it('returns false for isObstacle when there is no grid', () => {
    const room = computed(() => makeRoom(makeGrid()));
    const gridRef = computed(() => null);
    const { isObstacle } = useGridCells(room, gridRef);

    expect(isObstacle(0, 0)).toBe(false);
  });

  describe('isFloor', () => {
    it('treats every in-bounds cell as floor when no mask is sent', () => {
      // A room persisted before rooms had a shape sends nothing — it was a full rectangle,
      // and must keep reading as one rather than vanishing entirely.
      const grid = makeGrid({ floorCells: [] });
      const room = computed(() => makeRoom(grid));
      const gridRef = computed(() => grid);
      const { isFloor } = useGridCells(room, gridRef);

      expect(isFloor(0, 0)).toBe(true);
      expect(isFloor(3, 2)).toBe(true);
    });

    it('reads holes out of a full-length mask', () => {
      const mask = new Array(4 * 4).fill(true);
      mask[(2 * 4) + 3] = false; // (x=3, y=2)
      const grid = makeGrid({ floorCells: mask });
      const room = computed(() => makeRoom(grid));
      const gridRef = computed(() => grid);
      const { isFloor } = useGridCells(room, gridRef);

      expect(isFloor(3, 2)).toBe(false);
      expect(isFloor(2, 2)).toBe(true);
    });

    it('falls back to all-floor when the mask length disagrees with the grid', () => {
      // Better a plain rectangle than a room that renders as nothing at all.
      const grid = makeGrid({ floorCells: [true, false] });
      const room = computed(() => makeRoom(grid));
      const gridRef = computed(() => grid);
      const { isFloor } = useGridCells(room, gridRef);

      expect(isFloor(1, 0)).toBe(true);
    });

    it('reports out-of-bounds and no-grid as not floor', () => {
      const grid = makeGrid();
      const room = computed(() => makeRoom(grid));
      const { isFloor } = useGridCells(room, computed(() => grid));

      expect(isFloor(-1, 0)).toBe(false);
      expect(isFloor(99, 0)).toBe(false);

      const { isFloor: noGrid } = useGridCells(room, computed(() => null));
      expect(noGrid(0, 0)).toBe(false);
    });
  });
});
