import { ref } from 'vue';

import { playSort, shapeCells, SORTS } from '../../palace-map/composables/sorts';
import {
  cameraTileUnit,
  isoUnit,
  projectToScreen,
  projectToScreenCamera,
  type CameraParams,
  type ProjectionParams,
} from '../../palace-map/composables/useTerrainDrawPlan';
import { PACE } from './useCombatPlayback';

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
  from: { x: number; y: number } | null;
  color: string;
  timer: ReturnType<typeof globalThis.setTimeout>;
  finish: () => void;
};

/**
 * Contrat transitoire compatible avec les deux modes de projection :
 * - sans camX/camY/zoom : ancien fit-to-grid ;
 * - avec camX/camY/zoom : caméra fixe du combat.
 *
 * Cela évite de casser les autres consommateurs/tests pendant la migration de la scène.
 */
export type SortProjectionParams = ProjectionParams & Partial<CameraParams>;

const SORT_DURATION_MS = Math.round(1500 * PACE);
const renderPaintedSort = playSort as unknown as (
  ctx: CanvasRenderingContext2D,
  id: string,
  cells: SortEffect['cells'],
  progress: number,
  from: SortEffect['from'],
  color: string,
) => void;

function isCameraProjection(
  projection: SortProjectionParams,
): projection is ProjectionParams & CameraParams {
  return typeof projection.camX === 'number'
    && Number.isFinite(projection.camX)
    && typeof projection.camY === 'number'
    && Number.isFinite(projection.camY)
    && typeof projection.zoom === 'number'
    && Number.isFinite(projection.zoom);
}

function sortTileUnit(
  projection: SortProjectionParams,
): { isoUnitX: number; isoUnitY: number } {
  return isCameraProjection(projection)
    ? cameraTileUnit(projection)
    : isoUnit(projection);
}

function projectSortPoint(
  x: number,
  y: number,
  projection: SortProjectionParams,
): { screenX: number; screenY: number } {
  return isCameraProjection(projection)
    ? projectToScreenCamera(x, y, projection)
    : projectToScreen(x, y, projection);
}

export function useSortEffects() {
  const activeSorts = ref<SortEffect[]>([]);
  let effectSequence = 0;

  function completeSort(id: number): void {
    const effect = activeSorts.value.find((candidate) => candidate.id === id);
    if (!effect) return;

    globalThis.clearTimeout(effect.timer);
    activeSorts.value = activeSorts.value.filter((candidate) => candidate.id !== id);
    effect.finish();
  }

  function launchSort(
    sortId: string,
    centerX: number,
    centerY: number,
    battlefield: {
      width: number;
      height: number;
      elevation: number[];
      floor?: boolean[];
    },
    projection: SortProjectionParams,
    casterX?: number,
    casterY?: number,
    catalogShape?: string,
    catalogColor = '#8b9dcf',
  ): Promise<void> {
    if (!sortId || !(SORTS as Record<string, any>)[sortId]) return Promise.resolve();

    const sort = (SORTS as Record<string, any>)[sortId];
    const shape = (catalogShape ?? sort.shape).toLowerCase();
    const shapedCells = shapeCells(shape, centerX, centerY) as SortCell[] | null;
    const rawCells = shapedCells ?? Array.from(
      { length: battlefield.width * battlefield.height },
      (_, index) => ({
        x: index % battlefield.width,
        y: Math.floor(index / battlefield.width),
        d: 0,
      }),
    ).filter((_, index) => battlefield.floor?.[index] ?? true);

    const { isoUnitX } = sortTileUnit(projection);
    const destW = isoUnitX * 2.05;
    const ux = destW / 2;
    const uy = destW / 4;

    const cells = rawCells
      .filter((c: SortCell) =>
        c.x >= 0
          && c.y >= 0
          && c.x < battlefield.width
          && c.y < battlefield.height)
      .map((c: SortCell) => {
        const { screenX, screenY } = projectSortPoint(c.x, c.y, projection);
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
      .sort(
        (a: { cx: number; cy: number }, b: { cx: number; cy: number }) =>
          (a.cx + a.cy) - (b.cx + b.cy),
      );

    const from = casterX !== undefined && casterY !== undefined
      ? projectSortPoint(casterX, casterY, projection)
      : null;

    const id = (effectSequence += 1);

    return new Promise<void>((resolve) => {
      const effect: SortEffect = {
        id,
        sortId,
        cells,
        startedAt: performance.now(),
        duration: SORT_DURATION_MS,
        from: from ? { x: from.screenX, y: from.screenY } : null,
        color: catalogColor,
        timer: globalThis.setTimeout(() => completeSort(id), SORT_DURATION_MS),
        finish: resolve,
      };

      activeSorts.value = [...activeSorts.value, effect];
    });
  }

  function clearSorts() {
    const running = activeSorts.value;
    activeSorts.value = [];
    for (const effect of running) {
      globalThis.clearTimeout(effect.timer);
      effect.finish();
    }
  }

  function renderSorts(ctx: CanvasRenderingContext2D): void {
    const now = performance.now();
    for (const sort of activeSorts.value) {
      const progress = (now - sort.startedAt) / sort.duration;
      renderPaintedSort(ctx, sort.sortId, sort.cells, progress, sort.from, sort.color);
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
  const skillToSortMap: Record<string, string> = {
    'canon.skill.fondations-de-thomas': 'fondations',
    'canon.skill.rempart': 'rempart',
    'canon.skill.dictee': 'dictee',
    'canon.skill.impulsivite': 'impulsivite',
    'canon.skill.frappe-denclume': 'frappe-enclume',
    'canon.skill.larme-elise': 'larme',
    'canon.skill.berceuse-inversee': 'berceuse-inversee',
    'canon.skill.silence-partage': 'silence-partage',
    'canon.skill.se-taire': 'se-taire',
    'canon.skill.flamme-froide': 'flamme-froide',
    'canon.skill.regard-infantile': 'regard-infantile',
    'canon.skill.injection-blanche': 'injection-blanche',
    'canon.skill.curee': 'curee',
    'canon.skill.vol-a-la-tire': 'vol-a-la-tire',
  };
  return skillToSortMap[skillKey] ?? null;
}

/**
 * Repli générique quand aucune peinture dédiée n'existe pour ce sort (voir sorts.ts : le
 * catalogue en compte 138, 19 sont peints à la main) — correct sur sa forme et sa couleur
 * (physique/magique) plutôt que silencieux. `category`/`tacticalAreaShape` viennent de la
 * définition catalogue du sort, pas du combattant qui l'a lancé : la forme est une propriété
 * du sort, pas de qui le joue. `TacticalAreaShape` côté backend n'a que 4 valeurs (Single,
 * Cross, Diamond, Map), donc ce repli ne peut jamais désigner un `generique-*` absent de
 * sorts.ts.
 */
export function fallbackSortId(
  category: string | undefined,
  tacticalAreaShape: string | undefined,
): string {
  const flavor = category === 'Magic' ? 'magique' : 'physique';
  const shape = (tacticalAreaShape ?? 'Single').toLowerCase();
  return `generique-${flavor}-${shape}`;
}