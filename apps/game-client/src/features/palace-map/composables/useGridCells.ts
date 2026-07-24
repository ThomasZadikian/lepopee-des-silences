import { computed, type ComputedRef } from 'vue';
import type { NodeDto, RoomDto, RoomGridDto } from '../../runs/types/runTypes';

export type Cell = { x: number; y: number };

/**
 * Pure grid lookups shared by every renderer of a grid room (the CSS 2.5D map and the
 * TresJS 3D scene): which cells are fog-revealed, which node (if any) sits on a given
 * cell, whether the party currently stands there, and the flat list of every cell in
 * the grid.
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

  return { revealedCells, isRevealed, nodesByCell, nodeAt, isParty, cells };
}
