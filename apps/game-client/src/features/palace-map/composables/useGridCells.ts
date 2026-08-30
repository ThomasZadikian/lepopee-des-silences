import { computed, type ComputedRef } from 'vue';
import type { NodeDto, RoomDto, RoomGridDto } from '../../runs/types/runTypes';

export type Cell = { x: number; y: number };

/**
 * Pure grid lookups for a grid room: which cells are fog-revealed, which node (if any)
 * sits on a given cell, whether the party or a positioned NPC currently stands there, which
 * cells are impassable obstacles, and the flat list of every cell in the grid.
 */
export function useGridCells(room: ComputedRef<RoomDto>, grid: ComputedRef<RoomGridDto | null>) {
  const revealedCells = computed(() => {
    const set = new Set<string>();
    for (const [x, y] of grid.value?.revealedCells ?? []) {
      set.add(`${x},${y}`);
    }
    return set;
  });

  function isRevealed(x: number, y: number): boolean {
    return revealedCells.value.has(`${x},${y}`);
  }

  const nodesByCell = computed(() => {
    const map = new Map<string, NodeDto>();
    for (const node of room.value.nodes) {
      map.set(`${node.lane},${node.row}`, node);
    }
    return map;
  });

  function nodeAt(x: number, y: number): NodeDto | null {
    return nodesByCell.value.get(`${x},${y}`) ?? null;
  }

  function isParty(x: number, y: number): boolean {
    return grid.value !== null && grid.value.partyX === x && grid.value.partyY === y;
  }

  const obstacleCells = computed(() => {
    const set = new Set<string>();
    for (const [x, y] of grid.value?.obstacleCells ?? []) {
      set.add(`${x},${y}`);
    }
    return set;
  });

  function isObstacle(x: number, y: number): boolean {
    return obstacleCells.value.has(`${x},${y}`);
  }

  const npcCells = computed(() => {
    const set = new Set<string>();
    for (const npc of room.value.roomNpcs ?? []) {
      set.add(`${npc.x},${npc.y}`);
    }
    return set;
  });

  function isNpcOccupied(x: number, y: number): boolean {
    return npcCells.value.has(`${x},${y}`);
  }

  const doorCells = computed(() => {
    const set = new Set<string>();
    for (const [x, y] of grid.value?.doorCells ?? []) {
      set.add(`${x},${y}`);
    }
    return set;
  });

  function isDoor(x: number, y: number): boolean {
    return doorCells.value.has(`${x},${y}`);
  }

  const surfaceOverrides = computed(() => {
    const map = new Map<string, string>();
    for (const cell of grid.value?.surfaceOverrides ?? []) {
      map.set(`${cell.x},${cell.y}`, cell.key);
    }
    return map;
  });

  const decorPlacements = computed(() => {
    const map = new Map<string, string>();
    for (const cell of grid.value?.decorPlacements ?? []) {
      map.set(`${cell.x},${cell.y}`, cell.key);
    }
    return map;
  });

  /**
   * Whether a cell is part of the room at all. Returns false out of bounds too, so callers can
   * use it as a single "is this a real cell" test. A room persisted before rooms had a shape
   * sends no mask at all — treated as "everything is floor", the shape those rooms had.
   */
  function isFloor(x: number, y: number): boolean {
    const g = grid.value;
    if (!g) return false;
    if (x < 0 || x >= g.width || y < 0 || y >= g.height) return false;

    const mask = g.floorCells;
    if (!mask || mask.length !== g.width * g.height) return true;

    return mask[(y * g.width) + x];
  }

  const cells = computed<Cell[]>(() => {
    const g = grid.value;
    if (!g) return [];

    const result: Cell[] = [];
    for (let y = 0; y < g.height; y++) {
      for (let x = 0; x < g.width; x++) {
        result.push({ x, y });
      }
    }
    return result;
  });

  return {
    revealedCells,
    isRevealed,
    nodesByCell,
    nodeAt,
    isParty,
    obstacleCells,
    isObstacle,
    npcCells,
    isNpcOccupied,
    doorCells,
    isDoor,
    isFloor,
    cells,
    surfaceOverrides,
    decorPlacements,
  };
}
