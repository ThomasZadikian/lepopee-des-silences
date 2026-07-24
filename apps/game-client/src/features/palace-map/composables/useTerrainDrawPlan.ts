import type { NodeDto } from '../../runs/types/runTypes';
import { hashSeed } from './usePalaceTerrain';
import type { Cell } from './useGridCells';
import type { FloorTint, SpriteKey } from './useTerrainSprites';

// BALANCE KNOB — shrinks the whole diamond inward so the outermost tile's own half-width/
// half-height never reaches the canvas edge (mirrors the old CSS ISO_FIT).
const ISO_FIT = 0.82;
// The board's vertical center sits a bit below the canvas's true center, clearing the
// top-tabs chrome that overlays the top-left corner (mirrors the old CSS ISO_V_CENTER).
const ISO_V_CENTER = 0.56;
const FOG_VARIANT_COUNT = 4;

export type ProjectionParams = {
  canvasWidth: number;
  canvasHeight: number;
  gridWidth: number;
  gridHeight: number;
};

export function isoUnit(params: ProjectionParams): { isoUnitX: number; isoUnitY: number } {
  const isoUnitX = (params.canvasWidth / (params.gridWidth + params.gridHeight)) * ISO_FIT;
  return { isoUnitX, isoUnitY: isoUnitX / 2 };
}

/** World grid cell → canvas pixel position (the center of the tile's diamond). */
export function projectToScreen(
  x: number,
  y: number,
  params: ProjectionParams,
): { screenX: number; screenY: number } {
  const { isoUnitX, isoUnitY } = isoUnit(params);
  const maxSpan = params.gridWidth - 1 + (params.gridHeight - 1);
  return {
    screenX: (params.canvasWidth / 2) - ((x - y) * isoUnitX),
    screenY: (params.canvasHeight * ISO_V_CENTER) + (((x + y) - (maxSpan / 2)) * isoUnitY),
  };
}

/** Inverse of projectToScreen — canvas pixel position → nearest world grid cell. Used for
 * click-to-move hit testing against the single `<canvas>` element (no per-cell DOM nodes). */
export function unprojectFromScreen(
  screenX: number,
  screenY: number,
  params: ProjectionParams,
): { x: number; y: number } {
  const { isoUnitX, isoUnitY } = isoUnit(params);
  const maxSpan = params.gridWidth - 1 + (params.gridHeight - 1);
  const diff = isoUnitX === 0 ? 0 : ((params.canvasWidth / 2) - screenX) / isoUnitX; // x - y
  const sum = isoUnitY === 0
    ? maxSpan / 2
    : (((screenY - (params.canvasHeight * ISO_V_CENTER)) / isoUnitY) + (maxSpan / 2)); // x + y
  // `+ 0` folds a possible -0 (e.g. Math.round(-0.0001)) into +0 — a grid coordinate has no
  // meaningful sign, and leaving it would make an exact-cell equality check (x === 0) or a
  // deep-equal assertion fail against a plain `{ x: 0, y: 0 }` fixture.
  return {
    x: Math.round((sum + diff) / 2) + 0,
    y: Math.round((sum - diff) / 2) + 0,
  };
}

export type DrawPlanEntry = {
  cellKey: string;
  x: number;
  y: number;
  spriteKey: SpriteKey;
  screenX: number;
  screenY: number;
  /** Painter's-algorithm depth order: further-back and shorter tiles paint first. */
  sortKey: number;
};

export type BuildDrawPlanInput = {
  cells: Cell[];
  gridWidth: number;
  gridHeight: number;
  canvasWidth: number;
  canvasHeight: number;
  roomId: string;
  /** The room theme's own accent tone (see THEME_ACCENT), used for plain floor tiles. */
  ambientTint: FloorTint;
  /** Flat, row-major, one 0..3 value per cell. */
  elevation: number[];
  revealedCells: Set<string>;
  obstacleCells: Set<string>;
  /** Keyed "x,y" (matches useGridCells' nodesByCell, which keys by lane,row = x,y). */
  nodesByCell: Map<string, NodeDto>;
  /** Resolves a node's own tint (see NODE_TILE_TONE), 'neutral' for unmapped types. */
  nodeTintFor: (node: NodeDto) => FloorTint;
};

/**
 * Pure function: given the current grid/fog/terrain state, decides which sprite each cell
 * should paint and where, sorted back-to-front for the canvas painter's algorithm. No canvas
 * or DOM access here — this is what makes it directly unit-testable without a browser.
 */
export function buildDrawPlan(input: BuildDrawPlanInput): DrawPlanEntry[] {
  const projection: ProjectionParams = {
    canvasWidth: input.canvasWidth,
    canvasHeight: input.canvasHeight,
    gridWidth: input.gridWidth,
    gridHeight: input.gridHeight,
  };

  const entries: DrawPlanEntry[] = [];

  for (const cell of input.cells) {
    const cellKey = `${cell.x},${cell.y}`;
    const revealed = input.revealedCells.has(cellKey);
    const node = input.nodesByCell.get(cellKey) ?? null;
    const obstacle = input.obstacleCells.has(cellKey);
    const elevationLevel = input.elevation[(cell.y * input.gridWidth) + cell.x] ?? 0;
    const { screenX, screenY } = projectToScreen(cell.x, cell.y, projection);

    let spriteKey: SpriteKey;

    if (!revealed && !node) {
      // Fog always wins for a cell with no known objective on it — terrain shape (even
      // "this is a wall") stays hidden until revealed, matching the existing backend
      // invariant that only a node's position/type leaks through fog, never its terrain.
      const variant = hashSeed(`${input.roomId}:fog:${cell.x}:${cell.y}`) % FOG_VARIANT_COUNT;
      spriteKey = { kind: 'fog', variant, marker: false };
    } else if (obstacle) {
      spriteKey = { kind: 'obstacle', light: revealed ? 'lit' : 'ghost' };
    } else {
      const tint: FloorTint = node
        ? (node.isBoss ? 'blood' : input.nodeTintFor(node))
        : input.ambientTint;
      spriteKey = {
        kind: 'floor',
        tint,
        elevation: elevationLevel,
        resolved: node?.state === 'Resolved',
        glow: node?.isBoss ?? false,
        light: revealed ? 'lit' : 'ghost',
      };
    }

    entries.push({
      cellKey,
      x: cell.x,
      y: cell.y,
      spriteKey,
      screenX,
      screenY,
      sortKey: ((cell.x + cell.y) * 4) + elevationLevel,
    });
  }

  return entries.sort((a, b) => a.sortKey - b.sortKey);
}
