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

export type SortCell = {
  x: number;
  y: number;
  d: number;
};

type SortWorldCell = {
  x: number;
  y: number;
  d: number;
  center: boolean;
  dist: number;
};

type PaintedSortCell = {
  cx: number;
  cy: number;
  ux: number;
  uy: number;
  d: number;
  center: boolean;
  dist: number;
};

/**
 * Contrat compatible avec l'ancien fit-to-grid et la caméra fixe/animée du combat.
 */
export type SortProjectionParams = ProjectionParams & Partial<CameraParams>;

export type SortEffect = {
  id: number;
  sortId: string;
  /** Projection initiale conservée pour compatibilité/debug. */
  cells: PaintedSortCell[];
  /** Coordonnées monde : source réelle du rendu à chaque frame. */
  worldCells: SortWorldCell[];
  startedAt: number;
  duration: number;
  /** Position écran initiale, conservée pour compatibilité/debug. */
  from: { x: number; y: number } | null;
  fromWorld: { x: number; y: number } | null;
  battlefieldWidth: number;
  elevation: number[];
  projection: SortProjectionParams;
  color: string;
  timer: ReturnType<typeof globalThis.setTimeout>;
  finish: () => void;
};

const SORT_DURATION_MS = Math.round(1500 * PACE);
const renderPaintedSort = playSort as unknown as (
  ctx: CanvasRenderingContext2D,
  id: string,
  cells: PaintedSortCell[],
  progress: number,
  from: { x: number; y: number } | null,
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

function paintCellsFor(
  worldCells: SortWorldCell[],
  battlefieldWidth: number,
  elevation: number[],
  projection: SortProjectionParams,
): PaintedSortCell[] {
  const { isoUnitX } = sortTileUnit(projection);
  const destW = isoUnitX * 2.05;
  const ux = destW / 2;
  const uy = destW / 4;

  return worldCells
    .map((cell) => {
      const { screenX, screenY } = projectSortPoint(cell.x, cell.y, projection);
      const elev = elevation[(cell.y * battlefieldWidth) + cell.x] ?? 0;
      const lift = elev * 20 * (destW / 128);

      return {
        cx: screenX,
        cy: screenY - lift,
        ux,
        uy,
        d: cell.d,
        center: cell.center,
        dist: cell.dist,
      };
    })
    .sort((a, b) => (a.cx + a.cy) - (b.cx + b.cy));
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

    const worldCells: SortWorldCell[] = rawCells
      .filter((cell: SortCell) =>
        cell.x >= 0
          && cell.y >= 0
          && cell.x < battlefield.width
          && cell.y < battlefield.height)
      .map((cell: SortCell) => ({
        x: cell.x,
        y: cell.y,
        d: cell.d,
        center: cell.d === 0,
        dist: casterX !== undefined && casterY !== undefined
          ? Math.abs(cell.x - casterX) + Math.abs(cell.y - casterY)
          : cell.d,
      }));

    const cells = paintCellsFor(
      worldCells,
      battlefield.width,
      battlefield.elevation,
      projection,
    );

    const fromWorld = casterX !== undefined && casterY !== undefined
      ? { x: casterX, y: casterY }
      : null;
    const projectedFrom = fromWorld
      ? projectSortPoint(fromWorld.x, fromWorld.y, projection)
      : null;

    const id = (effectSequence += 1);

    return new Promise<void>((resolve) => {
      const effect: SortEffect = {
        id,
        sortId,
        cells,
        worldCells,
        startedAt: performance.now(),
        duration: SORT_DURATION_MS,
        from: projectedFrom ? { x: projectedFrom.screenX, y: projectedFrom.screenY } : null,
        fromWorld,
        battlefieldWidth: battlefield.width,
        elevation: battlefield.elevation,
        projection: { ...projection },
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

  /**
   * `projection` est optionnelle pour ne pas casser les tests/anciens consommateurs. Dans la
   * scène tactique, on fournit la caméra courante à chaque frame : l'effet reste donc attaché
   * aux cases monde même si le viewport/caméra a changé depuis son lancement.
   */
  function renderSorts(
    ctx: CanvasRenderingContext2D,
    projection?: SortProjectionParams,
  ): void {
    const now = performance.now();

    for (const sort of activeSorts.value) {
      const activeProjection = projection ?? sort.projection;
      const cells = paintCellsFor(
        sort.worldCells,
        sort.battlefieldWidth,
        sort.elevation,
        activeProjection,
      );
      const projectedFrom = sort.fromWorld
        ? projectSortPoint(sort.fromWorld.x, sort.fromWorld.y, activeProjection)
        : null;
      const from = projectedFrom
        ? { x: projectedFrom.screenX, y: projectedFrom.screenY }
        : null;
      const progress = (now - sort.startedAt) / sort.duration;

      renderPaintedSort(ctx, sort.sortId, cells, progress, from, sort.color);
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

/** Retourne l'ID du sort associé à une clé de compétence. */
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

export function fallbackSortId(
  category: string | undefined,
  tacticalAreaShape: string | undefined,
): string {
  const flavor = category === 'Magic' ? 'magique' : 'physique';
  const shape = (tacticalAreaShape ?? 'Single').toLowerCase();
  return `generique-${flavor}-${shape}`;
}
