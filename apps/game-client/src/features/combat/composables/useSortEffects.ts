import { ref } from 'vue';

import { shapeCells, SORTS } from '../../palace-map/composables/sorts';
import { isoUnit, projectToScreen } from '../../palace-map/composables/useTerrainDrawPlan';

import type { ProjectionParams } from '../../palace-map/composables/useTerrainDrawPlan';

// Type pour les cellules de sort (corrige TS2305)
export type SortCell = {
  x: number;
  y: number;
  d: number;
};

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
    if (!sortId || !(SORTS as Record<string, any>)[sortId]) return;

    const sort = (SORTS as Record<string, any>)[sortId];
    const rawCells = shapeCells(sort.shape, centerX, centerY) as SortCell[] | null;

    if (!rawCells) return;

    const { isoUnitX } = isoUnit(projection);
    const destW = isoUnitX * 2.05;
    const ux = destW / 2;
    const uy = destW / 4;

    const cells = rawCells
      .filter((c: SortCell) => c.x >= 0 && c.y >= 0 && c.x < battlefield.width && c.y < battlefield.height)
      .map((c: SortCell) => {
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
      .sort((a: { cx: number; cy: number }, b: { cx: number; cy: number }) => (a.cx + a.cy) - (b.cx + b.cy));

    activeSorts.value = [
      ...activeSorts.value,
      {
        id: (activeSorts.value.length + 1),
        sortId,
        cells,
        startedAt: Date.now(),
        duration: SORT_DURATION_MS,
      },
    ];

    setTimeout(() => {
      activeSorts.value = activeSorts.value.filter((s) => s.id !== activeSorts.value.length);
    }, SORT_DURATION_MS);
  }

  function clearSorts() {
    activeSorts.value = [];
  }

  function renderSorts(ctx: CanvasRenderingContext2D): void {
    // Rendu des effets de sort (timestamp non utilisé, corrigé TS6133)
    const now = performance.now();
    for (const sort of activeSorts.value) {
      if (now - sort.startedAt < sort.duration) {
        // Dessiner les cellules du sort
        for (const cell of sort.cells) {
          const progress = (now - sort.startedAt) / sort.duration;
          const alpha = 1 - progress;
          ctx.globalAlpha = alpha * 0.5;
          ctx.fillStyle = '#ffffff';
          ctx.fillRect(cell.cx - cell.ux, cell.cy - cell.uy, cell.ux * 2, cell.uy * 2);
          ctx.globalAlpha = 1;
        }
      }
    }
  }

  function reset() {
    clearSorts();
  }

  return {
    activeSorts,
    launchSort,
    clearSorts,
    renderSorts,
    reset,
  };
}

/**
 * Retourne l'ID du sort associé à une clé de compétence (pour les animations).
 */
export function sortIdForSkillKey(skillKey: string): string | null {
  // Mapping entre les clés de compétences et les IDs de sorts
  const skillToSortMap: Record<string, string> = {
    'skill.fireball': 'sort.boule-de-feu',
    'skill.ice-shard': 'sort.eclat-de-glace',
    'skill.heal': 'sort.soin',
    'skill.guard': 'sort.bouclier',
    'skill.strike': 'sort.frappe',
  };
  return skillToSortMap[skillKey] ?? null;
}
