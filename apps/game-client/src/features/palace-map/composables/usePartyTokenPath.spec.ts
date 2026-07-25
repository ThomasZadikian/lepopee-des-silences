import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { computed, nextTick, ref } from 'vue';
import { usePartyTokenPath, PARTY_STEP_MS } from './usePartyTokenPath';
import type { RoomDto, RoomGridDto } from '../../runs/types/runTypes';

function makeGrid(partyX: number, partyY: number): RoomGridDto {
  return {
    width: 6,
    height: 5,
    movementBudget: 10,
    movementBudgetRemaining: 10,
    partyX,
    partyY,
    canChallengeBossRemotely: false,
    revealedCells: [],
    elevation: new Array(6 * 5).fill(0),
    obstacleCells: [],
  };
}

function makeRoom(id: string, grid: RoomGridDto): RoomDto {
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
    grid,
  };
}

describe('usePartyTokenPath', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('snaps immediately (no animation) the first time a room is seen', () => {
    const grid = ref(makeGrid(2, 3));
    const room = computed(() => makeRoom('room-1', grid.value));
    const gridRef = computed(() => grid.value);

    const { displayPartyX, displayPartyY } = usePartyTokenPath(room, gridRef);

    expect(displayPartyX.value).toBe(2);
    expect(displayPartyY.value).toBe(3);
  });

  it('walks the X axis fully before moving on the Y axis, one cell per step', async () => {
    const grid = ref(makeGrid(0, 0));
    const room = computed(() => makeRoom('room-1', grid.value));
    const gridRef = computed(() => grid.value);

    const { displayPartyX, displayPartyY } = usePartyTokenPath(room, gridRef);

    // Move to (2, 1) within the same room — should walk x:0->1->2, then y:0->1.
    grid.value = { ...grid.value, partyX: 2, partyY: 1 };
    await nextTick();

    const path: Array<[number, number]> = [];
    for (let i = 0; i < 3; i++) {
      vi.advanceTimersByTime(PARTY_STEP_MS);
      path.push([displayPartyX.value, displayPartyY.value]);
    }

    expect(path).toEqual([
      [1, 0],
      [2, 0],
      [2, 1],
    ]);
  });

  it('snaps instantly to a new room instead of animating across unrelated coordinates', async () => {
    const roomId = ref('room-1');
    const grid = ref(makeGrid(0, 0));
    const room = computed(() => makeRoom(roomId.value, grid.value));
    const gridRef = computed(() => grid.value);

    const { displayPartyX, displayPartyY } = usePartyTokenPath(room, gridRef);

    // A brand-new room with the party already deep inside it must snap there
    // immediately, not animate a walk from the previous room's last position.
    roomId.value = 'room-2';
    grid.value = makeGrid(4, 4);
    await nextTick();

    expect(displayPartyX.value).toBe(4);
    expect(displayPartyY.value).toBe(4);

    // No animation timer should be pending — advancing time changes nothing further.
    vi.advanceTimersByTime(PARTY_STEP_MS * 5);
    expect(displayPartyX.value).toBe(4);
    expect(displayPartyY.value).toBe(4);
  });
});
