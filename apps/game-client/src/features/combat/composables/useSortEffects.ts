import { ref } from 'vue';

import { playSort, shapeCells, SORTS, SORT_IDS } from '../../palace-map/composables/sorts';
import { isoUnit, projectToScreen } from '../../palace-map/composables/useTerrainDrawPlan';

import type { ProjectionParams } from '../../palace-map/composables/useTerrainDrawPlan';

export type SortEffect = {
  id: number;
  sortId: string;
  cells: Array<{
    cx: number;
    cy: number;
    ux: number;
    uy: number;
    d: number;
    center: boolean;
    dist: number;
  }>;
  startedAt: number;
  duration: number;
};

const SORT_DURATION_MS = 1500;
const SORT_PAUSE_MS = 420;

let sortSeq = 0;

export function useSortEffects() {
  const activeSorts = ref<SortEffect[]>([]);

  function launchSort(
    sortId: string,
    centerX: number,
    centerY: number,
    battlefield: { width: number; height: number; elevation: number[] },
    projection: ProjectionParams,
    casterX?: number,
    casterY?: number,
  ) {
    if (!SORTS[sortId]) return;

    const sort = SORTS[sortId];
    const rawCells = shapeCells(sort.shape, centerX, centerY);

    if (!rawCells) return;

    const { isoUnitX } = isoUnit(projection);
    const destW = isoUnitX * 2.05;
    const ux = destW / 2;
    const uy = destW / 4;

    const cells = rawCells
      .filter((c) => c.x >= 0 && c.y >= 0 && c.x < battlefield.width && c.y < battlefield.height)
      .map((c) => {
        const { screenX, screenY } = projectToScreen(c.x, c.y, projection);
        const elev = battlefield.elevation[(c.y * battlefield.width) + c.x] ?? 0;
        const lift = elev * 20 * (destW / 128);
        const dist = casterX !== undefined && casterY !== undefined
          ? Math.abs(c.x - casterX) + Math.abs(c.y - casterY)
          : c.d;

        return {
          cx: screenX,
          cy: screenY - lift,
          ux,
          uy,
          d: c.d,
          center: c.d === 0,
          dist,
        };
      })
      .sort((a, b) => (a.cx + a.cy) - (b.cx + b.cy));

    activeSorts.value = [
      ...activeSorts.value,
      {
        id: (sortSeq += 1),
        sortId,
        cells,
        startedAt: performance.now(),
        duration: SORT_DURATION_MS + SORT_PAUSE_MS,
      },
    ];
  }

  function pruneSorts(now: number) {
    if (activeSorts.value.length === 0) return;

    activeSorts.value = activeSorts.value.filter((s) => now - s.startedAt < s.duration);
  }

  function renderSorts(ctx: CanvasRenderingContext2D, now: number) {
    pruneSorts(now);

    for (const effect of activeSorts.value) {
      const elapsed = now - effect.startedAt;
      const p = Math.min(1, elapsed / SORT_DURATION_MS);

      playSort(ctx, effect.sortId, effect.cells, p);
    }
  }

  function reset() {
    activeSorts.value = [];
  }

  return {
    activeSorts,
    launchSort,
    renderSorts,
    reset,
  };
}

export function sortIdForSkillKey(skillKey: string): string | null {
  // Correspondances explicites quand le nommage diffère entre catalogue et bestiaire.
  const explicit: Record<string, string> = {
    'canon.skill.fondations-de-thomas': 'fondations',
    'canon.skill.frappe-denclume': 'frappe-enclume',
    'canon.skill.larme-elise': 'larme',
  };
  if (explicit[skillKey]) return explicit[skillKey];

  // Retire les préfixes de namespace (canon.skill., skill.) pour ne garder que le nom.
  const stripped = skillKey
    .replace(/^(canon\.)?skill\./, '');

  if (SORT_IDS.includes(stripped)) return stripped;

  // Fallback : cherche une correspondance partielle en normalisant.
  const normalized = stripped.toLowerCase().replace(/[^a-z0-9-]/g, '');
  if (SORT_IDS.includes(normalized)) return normalized;

  return null;
}
