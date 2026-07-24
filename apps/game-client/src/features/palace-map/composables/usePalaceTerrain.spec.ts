import { describe, expect, it } from 'vitest';
import { computed } from 'vue';
import { usePalaceTerrain, hashSeed, mulberry32 } from './usePalaceTerrain';
import type { RoomDto, RoomGridDto } from '../../runs/types/runTypes';

function makeGrid(overrides: Partial<RoomGridDto> = {}): RoomGridDto {
  return {
    width: 6,
    height: 5,
    movementBudget: 10,
    movementBudgetRemaining: 10,
    partyX: 0,
    partyY: 0,
    canChallengeBossRemotely: false,
    revealedCells: [],
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

describe('hashSeed / mulberry32', () => {
  it('hashSeed is deterministic for the same string', () => {
    expect(hashSeed('room-abc')).toBe(hashSeed('room-abc'));
  });

  it('mulberry32 produces a deterministic sequence for the same seed', () => {
    const a = mulberry32(hashSeed('room-abc'));
    const b = mulberry32(hashSeed('room-abc'));
    expect([a(), a(), a()]).toEqual([b(), b(), b()]);
  });
});

describe('usePalaceTerrain', () => {
  it('returns an empty heightmap when there is no grid', () => {
    const room = computed(() => makeRoom('room-1', null));
    const grid = computed(() => null);
    const { heightMap, terrainHeight } = usePalaceTerrain(room, grid);

    expect(heightMap.value).toEqual([]);
    expect(terrainHeight(0, 0)).toBe(0);
  });

  it('is deterministic: the same roomId always generates the same terrain', () => {
    const grid = makeGrid();
    const roomA = computed(() => makeRoom('room-fixed', grid));
    const roomB = computed(() => makeRoom('room-fixed', grid));
    const gridRef = computed(() => grid);

    const a = usePalaceTerrain(roomA, gridRef);
    const b = usePalaceTerrain(roomB, gridRef);

    expect(a.heightMap.value).toEqual(b.heightMap.value);
  });

  it('produces a different terrain for a different roomId (with high probability)', () => {
    const grid = makeGrid();
    const gridRef = computed(() => grid);
    const a = usePalaceTerrain(computed(() => makeRoom('room-one', grid)), gridRef);
    const b = usePalaceTerrain(computed(() => makeRoom('room-two', grid)), gridRef);

    expect(a.heightMap.value).not.toEqual(b.heightMap.value);
  });

  it('is 1-Lipschitz: any two orthogonally adjacent cells differ in height by at most 1', () => {
    const grid = makeGrid({ width: 10, height: 8 });
    const room = computed(() => makeRoom('room-lipschitz', grid));
    const gridRef = computed(() => grid);
    const { heightMap } = usePalaceTerrain(room, gridRef);

    const map = heightMap.value;
    for (let y = 0; y < grid.height; y++) {
      for (let x = 0; x < grid.width; x++) {
        const here = map[y][x];
        if (x + 1 < grid.width) {
          expect(Math.abs(here - map[y][x + 1])).toBeLessThanOrEqual(1);
        }
        if (y + 1 < grid.height) {
          expect(Math.abs(here - map[y + 1][x])).toBeLessThanOrEqual(1);
        }
      }
    }
  });

  it('never produces a negative height', () => {
    const grid = makeGrid({ width: 12, height: 12 });
    const room = computed(() => makeRoom('room-nonneg', grid));
    const gridRef = computed(() => grid);
    const { heightMap } = usePalaceTerrain(room, gridRef);

    for (const row of heightMap.value) {
      for (const h of row) {
        expect(h).toBeGreaterThanOrEqual(0);
      }
    }
  });
});
