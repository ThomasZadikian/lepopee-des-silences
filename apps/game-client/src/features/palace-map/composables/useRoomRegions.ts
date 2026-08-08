import { computed, type ComputedRef } from 'vue';
import type { RoomGridDto } from '../../runs/types/runTypes';

/**
 * Splits a room's floor into enceintes ("regions") by 4-connected flood-fill, with door cells
 * (a sub-room's threshold — `RoomGridDto.doorCells`) acting as hard boundaries rather than being
 * assigned to either side. Mirrors the Claude Design "salle" handoff's `computeRegions` (§1 —
 * éclairage par enceinte) at the level today's data model actually supports: a room without
 * sub-rooms has exactly one region (its whole connected floor, per the generator's own
 * `AllFloorConnected` invariant), and a room with sub-rooms gets one region per chamber plus one
 * for the surrounding space, separated at each door.
 *
 * Purely derived — no backend change beyond `doorCells` itself (Chantier 1-3's
 * `PartitionSubRooms` already knew which cell it left open; this only exposes it). Composes with
 * fog of war, does not replace it: fog alone still decides known vs unknown (`useGridCells.
 * isRevealed`), regions only nuance the ALREADY-known — full light in the party's own enceinte,
 * darkened memory everywhere else already explored.
 */
export function useRoomRegions(grid: ComputedRef<RoomGridDto | null>) {
  const doorSet = computed(() => {
    const set = new Set<string>();
    for (const [x, y] of grid.value?.doorCells ?? []) {
      set.add(`${x},${y}`);
    }
    return set;
  });

  function isDoor(x: number, y: number): boolean {
    return doorSet.value.has(`${x},${y}`);
  }

  function isFloorCell(x: number, y: number): boolean {
    const g = grid.value;
    if (!g) return false;
    if (x < 0 || x >= g.width || y < 0 || y >= g.height) return false;

    const mask = g.floorCells;
    if (!mask || mask.length !== g.width * g.height) return true;

    return mask[(y * g.width) + x];
  }

  const ORTHOGONAL_STEPS: readonly [number, number][] = [[1, 0], [-1, 0], [0, 1], [0, -1]];

  /** `"x,y"` -> region id. Cells that are void or a door hold no entry at all. */
  const regionMap = computed(() => {
    const g = grid.value;
    const map = new Map<string, number>();
    if (!g) return map;

    let nextRegionId = 0;
    const visited = new Set<string>();

    for (let y = 0; y < g.height; y++) {
      for (let x = 0; x < g.width; x++) {
        const startKey = `${x},${y}`;
        if (visited.has(startKey) || !isFloorCell(x, y) || isDoor(x, y)) continue;

        const regionId = nextRegionId++;
        const stack: [number, number][] = [[x, y]];
        visited.add(startKey);

        while (stack.length > 0) {
          const [cx, cy] = stack.pop()!;
          map.set(`${cx},${cy}`, regionId);

          for (const [dx, dy] of ORTHOGONAL_STEPS) {
            const nx = cx + dx;
            const ny = cy + dy;
            const key = `${nx},${ny}`;
            if (visited.has(key) || !isFloorCell(nx, ny) || isDoor(nx, ny)) continue;
            visited.add(key);
            stack.push([nx, ny]);
          }
        }
      }
    }

    return map;
  });

  function regionOf(x: number, y: number): number | null {
    return regionMap.value.get(`${x},${y}`) ?? null;
  }

  /** The enceinte the party currently stands in. Null while the party is on a door cell (a
   * threshold belongs to no region of its own) or before a grid exists. */
  const occupiedRegionId = computed(() => {
    const g = grid.value;
    if (!g) return null;
    return regionOf(g.partyX, g.partyY);
  });

  /**
   * Whether (x, y) reads as the party's current enceinte — full light. A door cell counts as
   * occupied the moment it opens onto the party's own region: a threshold reads as an extension
   * of whichever room it serves, never as a dark pocket of its own (README §1, "un seuil suit
   * l'enceinte la plus éclairée qu'il touche").
   */
  function isInOccupiedRegion(x: number, y: number): boolean {
    const occupied = occupiedRegionId.value;
    if (occupied === null) return false;

    if (isDoor(x, y)) {
      return ORTHOGONAL_STEPS.some(([dx, dy]) => regionOf(x + dx, y + dy) === occupied);
    }

    return regionOf(x, y) === occupied;
  }

  return { regionOf, occupiedRegionId, isDoor, isInOccupiedRegion };
}
