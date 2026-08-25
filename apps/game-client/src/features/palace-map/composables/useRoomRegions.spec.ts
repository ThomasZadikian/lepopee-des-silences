import { describe, expect, it } from 'vitest';
import { computed } from 'vue';
import { useRoomRegions } from './useRoomRegions';
import type { RoomGridDto } from '../../runs/types/runTypes';

function makeGrid(overrides: Partial<RoomGridDto> = {}): RoomGridDto {
  return {
    width: 4,
    height: 4,
    partyX: 0,
    partyY: 0,
    revealedCells: [],
    elevation: new Array(16).fill(0),
    obstacleCells: [],
    floorCells: [],
    doorCells: [],
    canSearch: false,
    hintCells: [],
    groundItems: [],
    ...overrides,
  };
}

describe('useRoomRegions', () => {
  it('treats a whole connected floor with no doors as a single enceinte', () => {
    const grid = makeGrid({ partyX: 0, partyY: 0 });
    const gridRef = computed(() => grid);
    const { regionOf, occupiedRegionId } = useRoomRegions(gridRef);

    // Every floor cell of a 4x4 rectangle, no doors, shares the party's region.
    expect(regionOf(3, 3)).toBe(occupiedRegionId.value);
    expect(regionOf(0, 0)).toBe(occupiedRegionId.value);
  });

  it('splits the floor into two enceintes on either side of a door', () => {
    // 5x1 corridor: (0,0)-(1,0) | door (2,0) | (3,0)-(4,0)
    const grid = makeGrid({
      width: 5,
      height: 1,
      partyX: 0,
      partyY: 0,
      elevation: new Array(5).fill(0),
      doorCells: [[2, 0]],
    });
    const gridRef = computed(() => grid);
    const { regionOf, occupiedRegionId } = useRoomRegions(gridRef);

    expect(occupiedRegionId.value).not.toBeNull();
    expect(regionOf(1, 0)).toBe(occupiedRegionId.value);
    expect(regionOf(3, 0)).not.toBe(occupiedRegionId.value);
    expect(regionOf(3, 0)).not.toBeNull();
  });

  it('assigns no region at all to a door cell', () => {
    const grid = makeGrid({
      width: 5,
      height: 1,
      elevation: new Array(5).fill(0),
      doorCells: [[2, 0]],
    });
    const gridRef = computed(() => grid);
    const { regionOf } = useRoomRegions(gridRef);

    expect(regionOf(2, 0)).toBeNull();
  });

  it('reports a door as part of the occupied enceinte when it opens onto it', () => {
    const grid = makeGrid({
      width: 5,
      height: 1,
      partyX: 0,
      partyY: 0,
      elevation: new Array(5).fill(0),
      doorCells: [[2, 0]],
    });
    const gridRef = computed(() => grid);
    const { isDoor, isInOccupiedRegion } = useRoomRegions(gridRef);

    expect(isDoor(2, 0)).toBe(true);
    expect(isInOccupiedRegion(2, 0)).toBe(true);
    // The far side, across the door, is a different enceinte.
    expect(isInOccupiedRegion(3, 0)).toBe(false);
    expect(isInOccupiedRegion(1, 0)).toBe(true);
  });

  it('treats a void cell as belonging to no region', () => {
    const mask = new Array(16).fill(true);
    mask[(1 * 4) + 2] = false; // (x=2, y=1) is a hole
    const grid = makeGrid({ floorCells: mask });
    const gridRef = computed(() => grid);
    const { regionOf } = useRoomRegions(gridRef);

    expect(regionOf(2, 1)).toBeNull();
  });

  it('returns null occupiedRegionId when there is no grid', () => {
    const gridRef = computed(() => null);
    const { occupiedRegionId, isInOccupiedRegion } = useRoomRegions(gridRef);

    expect(occupiedRegionId.value).toBeNull();
    expect(isInOccupiedRegion(0, 0)).toBe(false);
  });

  it('finds two disconnected floor islands as two separate enceintes', () => {
    // Two 2x1 islands separated by a void column at x=2.
    const mask = [true, true, false, true, true];
    const grid = makeGrid({
      width: 5,
      height: 1,
      partyX: 0,
      partyY: 0,
      elevation: new Array(5).fill(0),
      floorCells: mask,
    });
    const gridRef = computed(() => grid);
    const { regionOf, occupiedRegionId } = useRoomRegions(gridRef);

    expect(regionOf(1, 0)).toBe(occupiedRegionId.value);
    expect(regionOf(3, 0)).not.toBe(occupiedRegionId.value);
    expect(regionOf(3, 0)).not.toBeNull();
  });
});
