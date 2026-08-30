import type { ComputedRef } from 'vue';
import type { RoomDto, RoomGridDto } from '../../runs/types/runTypes';

/** Small deterministic string hash — reused for the room-level backdrop nuance and fog jitter. */
export function hashSeed(seed: string): number {
  let hash = 0;
  for (let i = 0; i < seed.length; i++) {
    hash = (hash * 31 + seed.charCodeAt(i)) >>> 0;
  }
  return hash;
}

/**
 * Terrain elevation reader. Elevation is now real, server-authoritative data (a room-generation
 * concern — see `RoomGridDto.elevation`, flat row-major, one 0..3 value per cell) that actually
 * affects movement cost and line-of-sight server-side; this composable only reads it back out
 * for the renderer, it no longer generates anything client-side.
 */
export function usePalaceTerrain(
  _room: ComputedRef<RoomDto>,
  grid: ComputedRef<RoomGridDto | null>,
) {
  function terrainHeight(x: number, y: number): number {
    const g = grid.value;
    if (!g) return 0;
    return g.elevation[(y * g.width) + x] ?? 0;
  }

  return { terrainHeight };
}
