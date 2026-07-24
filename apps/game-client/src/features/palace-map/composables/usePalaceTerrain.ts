import { computed, type ComputedRef } from 'vue';
import type { RoomDto, RoomGridDto } from '../../runs/types/runTypes';

/** Small deterministic string hash — reused for terrain height and the room-level backdrop nuance. */
export function hashSeed(seed: string): number {
  let hash = 0;
  for (let i = 0; i < seed.length; i++) {
    hash = (hash * 31 + seed.charCodeAt(i)) >>> 0;
  }
  return hash;
}

/** Small deterministic PRNG (mulberry32) — same room always generates the same terrain. */
export function mulberry32(seed: number): () => number {
  let a = seed;
  return () => {
    a |= 0;
    a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

/**
 * Purely cosmetic terrain heightmap, deterministic from roomId — never sent to or
 * read from the backend. Only shades/lifts cells; no mechanical effect during
 * exploration (height only matters once the future tactical combat chantier lands).
 *
 * Scattering a handful of "peaks" and taking, per cell, the max over
 * max(0, peakHeight - manhattanDistance) gives actual little mountains (a smooth
 * falloff from each peak) instead of independently-rolled per-cell noise — and since
 * the max of 1-Lipschitz cone functions is itself 1-Lipschitz, two orthogonally
 * adjacent cells can never differ by more than one level: a height-2 tile always has
 * a height-1 (or higher) tile next to it, never a sheer drop straight to 0.
 */
export function usePalaceTerrain(
  room: ComputedRef<RoomDto>,
  grid: ComputedRef<RoomGridDto | null>,
) {
  const heightMap = computed<number[][]>(() => {
    const g = grid.value;
    if (!g) return [];

    const rng = mulberry32(hashSeed(room.value.id ?? ''));
    const peakCount = Math.max(1, Math.round((g.width * g.height) / 18));
    const peaks = Array.from({ length: peakCount }, () => ({
      x: Math.floor(rng() * g.width),
      y: Math.floor(rng() * g.height),
      height: 1 + Math.floor(rng() * 3), // 1..3
    }));

    const map: number[][] = [];
    for (let y = 0; y < g.height; y++) {
      const row: number[] = [];
      for (let x = 0; x < g.width; x++) {
        let cellHeight = 0;
        for (const peak of peaks) {
          const distance = Math.abs(x - peak.x) + Math.abs(y - peak.y);
          cellHeight = Math.max(cellHeight, peak.height - distance);
        }
        row.push(Math.max(0, cellHeight));
      }
      map.push(row);
    }
    return map;
  });

  function terrainHeight(x: number, y: number): number {
    return heightMap.value[y]?.[x] ?? 0;
  }

  return { heightMap, terrainHeight };
}
