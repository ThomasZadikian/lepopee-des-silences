import {
  isoUnit,
  projectToScreen,
  projectToScreenCamera,
  type CameraParams,
  type ProjectionParams,
} from '../../palace-map/composables/useTerrainDrawPlan';
import { hashSeed } from '../../palace-map/composables/usePalaceTerrain';
import {
  TERRAIN_SPRITE_CONSTANTS,
  obstacleVariantCount,
  type FloorTint,
  type HighlightVariant,
  type RoomTheme,
  type SpriteKey,
} from '../../palace-map/composables/useTerrainSprites';

/**
 * Le plan de dessin d'un champ de bataille tactique.
 *
 * Fonction pure, sans canvas ni DOM. Elle accepte désormais directement une caméra : le plan
 * n'a plus besoin d'être projeté une première fois en fit-to-grid puis reprojeté dans la scène.
 * Sans caméra, le comportement historique reste disponible pour les tests/consommateurs qui
 * n'ont pas encore migré.
 */

export type BattleCell = { x: number; y: number };

export type BattleVisibleBounds = {
  minX: number;
  maxX: number;
  minY: number;
  maxY: number;
};

export type BattleDrawPlanEntry = {
  cellKey: string;
  x: number;
  y: number;
  spriteKey: SpriteKey;
  screenX: number;
  screenY: number;
  elevation: number;
  /** Ordre du peintre : le fond et le bas d'abord. */
  sortKey: number;
};

export type BuildBattlePlanInput = {
  canvasWidth: number;
  canvasHeight: number;
  gridWidth: number;
  gridHeight: number;
  elevation: number[];
  walkable: boolean[];
  floor?: boolean[];
  theme: RoomTheme;
  ambientTint: FloorTint;
  /** Caméra fixe/animée du combat. Absente = projection historique fit-to-grid. */
  camera?: CameraParams;
  /** Limites déjà calculées par visibleCellRange : évite de parcourir toute une immense carte. */
  visibleBounds?: BattleVisibleBounds;
  reachableCells?: Set<string>;
  targetableCells?: Set<string>;
  blockedCells?: Set<string>;
  aoeCells?: Set<string>;
  threatCells?: Set<string>;
  heightCells?: Set<string>;
  occupiedCells?: Set<string>;
  hoveredCell?: BattleCell | null;
  pathCells?: Set<string>;
};

export const battleCellKey = (x: number, y: number): string => `${x},${y}`;

const surfaceSeedFor = (x: number, y: number): number => ((x * 7) + (y * 13)) % 5;

const elevationAt = (input: BuildBattlePlanInput, x: number, y: number): number =>
  input.elevation[(y * input.gridWidth) + x] ?? 0;

const isWalkable = (input: BuildBattlePlanInput, x: number, y: number): boolean => {
  if (x < 0 || y < 0 || x >= input.gridWidth || y >= input.gridHeight) return false;
  return input.walkable[(y * input.gridWidth) + x] ?? false;
};

const isFloor = (input: BuildBattlePlanInput, x: number, y: number): boolean => {
  if (x < 0 || y < 0 || x >= input.gridWidth || y >= input.gridHeight) return false;
  return input.floor
    ? input.floor[(y * input.gridWidth) + x] ?? false
    : isWalkable(input, x, y);
};

const sortKeyFor = (x: number, y: number, elevation: number): number =>
  ((x + y) * 4) + elevation;

export function buildBattlePlan(input: BuildBattlePlanInput): BattleDrawPlanEntry[] {
  const projection: ProjectionParams = {
    canvasWidth: input.canvasWidth,
    canvasHeight: input.canvasHeight,
    gridWidth: input.gridWidth,
    gridHeight: input.gridHeight,
  };

  const project = input.camera
    ? (x: number, y: number) => projectToScreenCamera(x, y, {
        canvasWidth: input.canvasWidth,
        canvasHeight: input.canvasHeight,
        ...input.camera!,
      })
    : (x: number, y: number) => projectToScreen(x, y, projection);

  const bounds = input.visibleBounds ?? {
    minX: 0,
    maxX: input.gridWidth - 1,
    minY: 0,
    maxY: input.gridHeight - 1,
  };

  const minX = Math.max(0, bounds.minX);
  const maxX = Math.min(input.gridWidth - 1, bounds.maxX);
  const minY = Math.max(0, bounds.minY);
  const maxY = Math.min(input.gridHeight - 1, bounds.maxY);

  const entries: BattleDrawPlanEntry[] = [];

  for (let y = minY; y <= maxY; y += 1) {
    for (let x = minX; x <= maxX; x += 1) {
      if (!isFloor(input, x, y)) continue;

      const cellKey = battleCellKey(x, y);
      const elevation = elevationAt(input, x, y);
      const { screenX, screenY } = project(x, y);
      const walkable = isWalkable(input, x, y);
      const sortKey = sortKeyFor(x, y, elevation);

      if (!walkable) {
        const wallVariants = obstacleVariantCount(input.theme);
        entries.push({
          cellKey,
          x,
          y,
          spriteKey: {
            kind: 'obstacle',
            theme: input.theme,
            variant: wallVariants > 0 ? hashSeed(cellKey) % wallVariants : 0,
            elevation,
          },
          screenX,
          screenY,
          elevation,
          sortKey: sortKeyFor(x, y, TERRAIN_SPRITE_CONSTANTS.MAX_ELEVATION),
        });
        continue;
      }

      entries.push({
        cellKey,
        x,
        y,
        spriteKey: {
          kind: 'floor',
          tint: input.ambientTint,
          theme: input.theme,
          elevation,
          surfaceSeed: surfaceSeedFor(x, y),
          cliffLeft: !isFloor(input, x, y + 1),
          cliffRight: !isFloor(input, x + 1, y),
        },
        screenX,
        screenY,
        elevation,
        sortKey,
      });

      const pushHighlight = (suffix: string, variant: HighlightVariant, order: number) => {
        entries.push({
          cellKey: `${cellKey}:${suffix}`,
          x,
          y,
          spriteKey: { kind: 'highlight', variant, elevation },
          screenX,
          screenY,
          elevation,
          sortKey: sortKey + order,
        });
      };

      if (input.reachableCells?.has(cellKey)) pushHighlight('move', 'move', 0.1);
      if (input.threatCells?.has(cellKey)) pushHighlight('threat', 'threat', 0.12);
      if (input.targetableCells?.has(cellKey)) pushHighlight('attack', 'attack', 0.2);
      if (input.blockedCells?.has(cellKey)) pushHighlight('blocked', 'blocked', 0.21);
      if (input.pathCells?.has(cellKey)) pushHighlight('path', 'path', 0.24);
      if (input.aoeCells?.has(cellKey)) pushHighlight('aoe', 'aoe', 0.26);
      if (input.heightCells?.has(cellKey)) pushHighlight('height', 'height', 0.28);
      if (input.occupiedCells?.has(cellKey) && !input.aoeCells?.has(cellKey)) {
        pushHighlight('occupied', 'occupied', 0.29);
      }
      if (input.hoveredCell && input.hoveredCell.x === x && input.hoveredCell.y === y) {
        pushHighlight('cursor', 'cursor', 0.3);
      }
    }
  }

  return entries.sort((a, b) => a.sortKey - b.sortKey);
}

/**
 * Les cases atteignables par un combattant, à budget donné.
 * A* sur le sous-graphe praticable, coût d'un pas `1 + |Δélévation|`.
 */
export function reachableCellsFrom(
  input: Pick<BuildBattlePlanInput, 'gridWidth' | 'gridHeight' | 'elevation' | 'walkable'>,
  origin: BattleCell,
  budget: number,
  occupied: ReadonlySet<string>,
): Set<string> {
  return reachableCellsWithPathsFrom(input, origin, budget, occupied).cells;
}

type AStarFrontierNode = {
  cell: BattleCell;
  g: number;
  h: number;
  f: number;
  previous: BattleCell;
};

export function reachableCellsWithPathsFrom(
  input: Pick<BuildBattlePlanInput, 'gridWidth' | 'gridHeight' | 'elevation' | 'walkable'>,
  origin: BattleCell,
  budget: number,
  occupied: ReadonlySet<string>,
): { cells: Set<string>; previous: Map<string, BattleCell> } {
  const reached = new Map<string, number>([[battleCellKey(origin.x, origin.y), 0]]);
  const previous = new Map<string, BattleCell>();
  const frontier: AStarFrontierNode[] = [{
    cell: origin,
    g: 0,
    h: 0,
    f: 0,
    previous: origin,
  }];

  const cellElevation = (x: number, y: number): number =>
    input.elevation[(y * input.gridWidth) + x] ?? 0;

  const walkable = (x: number, y: number): boolean => {
    if (x < 0 || y < 0 || x >= input.gridWidth || y >= input.gridHeight) return false;
    return input.walkable[(y * input.gridWidth) + x] ?? false;
  };

  while (frontier.length > 0) {
    frontier.sort((a, b) => a.f - b.f);
    const current = frontier.shift()!;
    if (current.g > budget) continue;

    const neighbours: BattleCell[] = [
      { x: current.cell.x + 1, y: current.cell.y },
      { x: current.cell.x - 1, y: current.cell.y },
      { x: current.cell.x, y: current.cell.y + 1 },
      { x: current.cell.x, y: current.cell.y - 1 },
    ];

    for (const next of neighbours) {
      if (!walkable(next.x, next.y)) continue;

      const key = battleCellKey(next.x, next.y);
      if (occupied.has(key)) continue;

      const climb = Math.abs(
        cellElevation(next.x, next.y) - cellElevation(current.cell.x, current.cell.y),
      );
      const g = current.g + 1 + climb;
      if (g > budget) continue;

      const best = reached.get(key);
      if (best !== undefined && best <= g) continue;

      reached.set(key, g);
      previous.set(key, current.previous);
      frontier.push({
        cell: next,
        g,
        h: 0,
        f: g,
        previous: current.cell,
      });
    }
  }

  reached.delete(battleCellKey(origin.x, origin.y));
  return { cells: new Set(reached.keys()), previous };
}

export function hasLos(
  input: Pick<BuildBattlePlanInput, 'gridWidth' | 'gridHeight' | 'elevation' | 'walkable' | 'floor'>,
  from: BattleCell,
  to: BattleCell,
  maxClimb: number = 1,
): boolean {
  if (manhattan(from, to) <= 1) return true;

  const floorAt = (x: number, y: number): boolean => {
    if (x < 0 || y < 0 || x >= input.gridWidth || y >= input.gridHeight) return false;
    return input.floor
      ? input.floor[(y * input.gridWidth) + x] ?? false
      : input.walkable[(y * input.gridWidth) + x] ?? false;
  };

  const cellElevation = (x: number, y: number): number =>
    input.elevation[(y * input.gridWidth) + x] ?? 0;

  const dx = Math.abs(to.x - from.x);
  const dy = Math.abs(to.y - from.y);
  const sx = from.x < to.x ? 1 : -1;
  const sy = from.y < to.y ? 1 : -1;
  let err = dx - dy;
  let x = from.x;
  let y = from.y;
  let prevElev = cellElevation(from.x, from.y);

  while (x !== to.x || y !== to.y) {
    const e2 = 2 * err;
    if (e2 > -dy) { err -= dy; x += sx; }
    if (e2 < dx) { err += dx; y += sy; }

    if (x === to.x && y === to.y) break;
    if (!floorAt(x, y)) return false;

    const currentElev = cellElevation(x, y);
    if (Math.abs(currentElev - prevElev) > maxClimb) return false;
    prevElev = currentElev;
  }

  return true;
}

export const manhattan = (a: BattleCell, b: BattleCell): number =>
  Math.abs(a.x - b.x) + Math.abs(a.y - b.y);

/** @deprecated Utiliser `hasLos` à la place. */
export function hasDirectLos(
  input: Pick<BuildBattlePlanInput, 'gridWidth' | 'gridHeight' | 'elevation' | 'walkable' | 'floor'>,
  from: BattleCell,
  to: BattleCell,
): boolean {
  return hasLos(input, from, to);
}

// Compatibilité avec les tests/anciens consommateurs qui importent encore ces helpers ici.
export { isoUnit, projectToScreen };
