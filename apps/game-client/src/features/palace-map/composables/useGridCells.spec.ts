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
    movementBudget: 10,
    movementBudgetRemaining: 10,
    partyX: 0,
    partyY: 0,
    canChallengeBossRemotely: false,
    revealedCells: [[0, 0], [1, 0]],
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
});
