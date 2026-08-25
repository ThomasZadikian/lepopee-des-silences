import { describe, expect, it } from 'vitest';
import { computed } from 'vue';
import { usePalaceTerrain, hashSeed } from './usePalaceTerrain';
import type { RoomDto, RoomGridDto } from '../../runs/types/runTypes';

function makeGrid(overrides: Partial<RoomGridDto> = {}): RoomGridDto {
  const width = overrides.width ?? 6;
  const height = overrides.height ?? 5;
  return {
    width,
    height,
    partyX: 0,
    partyY: 0,
    doorCells: [],
    revealedCells: [],
    elevation: new Array(width * height).fill(0),
    obstacleCells: [],
    floorCells: [],
    canSearch: false,
    hintCells: [],
    ...overrides,
  };
}

function makeRoom(id: string, grid: RoomGridDto | null): RoomDto {
  return {
    id,
    depth: 0,
    roomType: 'Combat',
    theme: 'Threshold',
    state: 'Active',
    currentNodeDepth: 0,
    maxNodeDepth: 0,
    totalNodeCount: 0,
    bossPreview: { bossId: 'boss-1', name: 'Boss', roomType: 'RoomBoss', dangerHint: 'High' },
    nodes: [],
    availableNodes: [],
    layoutTemplateKey: null,
    layoutTemplateVersion: null,
    grid: grid as RoomGridDto,
  };
}

describe('hashSeed', () => {
  it('is deterministic for the same string', () => {
    expect(hashSeed('room-abc')).toBe(hashSeed('room-abc'));
  });

  it('differs (with high probability) for a different string', () => {
    expect(hashSeed('room-abc')).not.toBe(hashSeed('room-xyz'));
  });
});

describe('usePalaceTerrain', () => {
  it('reads 0 for every cell when there is no grid', () => {
    const room = computed(() => makeRoom('room-1', null));
    const grid = computed(() => null);
    const { terrainHeight } = usePalaceTerrain(room, grid);

    expect(terrainHeight(0, 0)).toBe(0);
  });

  it('reads elevation straight from the server-sent grid, at the right row-major index', () => {
    const width = 4;
    const height = 3;
    const elevation = new Array(width * height).fill(0);
    elevation[(1 * width) + 2] = 3; // cell (x=2, y=1)
    const grid = makeGrid({ width, height, elevation });
    const room = computed(() => makeRoom('room-1', grid));
    const gridRef = computed(() => grid);

    const { terrainHeight } = usePalaceTerrain(room, gridRef);

    expect(terrainHeight(2, 1)).toBe(3);
    expect(terrainHeight(0, 0)).toBe(0);
  });

  it('defaults to 0 for an out-of-range cell rather than throwing', () => {
    const grid = makeGrid({ width: 2, height: 2, elevation: [0, 0, 0, 0] });
    const room = computed(() => makeRoom('room-1', grid));
    const gridRef = computed(() => grid);

    const { terrainHeight } = usePalaceTerrain(room, gridRef);

    expect(terrainHeight(99, 99)).toBe(0);
  });
});
