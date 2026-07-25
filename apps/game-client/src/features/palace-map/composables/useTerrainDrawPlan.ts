import type { DangerTell, NodeDto } from '../../runs/types/runTypes';
import type { Cell } from './useGridCells';
import { hashSeed } from './usePalaceTerrain';
import {
  TERRAIN_SPRITE_CONSTANTS,
  cliffSides,
  obstacleVariantCount,
  type FloorTint,
  type RoomTheme,
  type SpriteKey,
} from './useTerrainSprites';

/** How many hand-painted brush variations a floor cycles through, so a large flat area never
 * reads as one tile stamped repeatedly. Formula from the tile-engine handoff. */
const SURFACE_SEED_COUNT = 5;

function surfaceSeedFor(x: number, y: number): number {
  return ((x * 7) + (y * 13)) % SURFACE_SEED_COUNT;
}

/** Backend DangerTell → the tile engine's own spelling. */
const DANGER_SPRITE: Record<DangerTell, 'none' | 'tracks' | 'glow' | 'blight'> = {
  None: 'none',
  Tracks: 'tracks',
  Glow: 'glow',
  Blight: 'blight',
};

function dangerFor(node: NodeDto | null): 'none' | 'tracks' | 'glow' | 'blight' {
  if (!node || node.state === 'Resolved') return 'none';
  return DANGER_SPRITE[node.dangerTell ?? 'None'] ?? 'none';
}

// BALANCE KNOB — shrinks the whole diamond inward so the outermost tile's own half-width/
// half-height never reaches the canvas edge (mirrors the old CSS ISO_FIT).
const ISO_FIT = 0.82;
// The board's vertical center sits a bit below the canvas's true center, clearing the
// top-tabs chrome that overlays the top-left corner (mirrors the old CSS ISO_V_CENTER).
const ISO_V_CENTER = 0.56;

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

export type ScreenToCellInput = {
  screenX: number;
  screenY: number;
  gridWidth: number;
  gridHeight: number;
  canvasWidth: number;
  canvasHeight: number;
  /** Flat, row-major, one 0..3 value per cell — same shape as BuildDrawPlanInput.elevation. */
  elevation: number[];
  obstacleCells: Set<string>;
  /** Same predicate the draw plan uses — a hole paints no tile, so it must not be clickable
   * either. Defaults to "everything inside the grid is floor". */
  isFloor?: (x: number, y: number) => boolean;
};

/**
 * Inverse of what's actually PAINTED, not of the flat projection: a tile at elevation > 0 is
 * drawn shifted up on screen by its own lift (see useTerrainSprites' diamondCenterY /
 * TacticalGridMap's elevationLiftPx) while `projectToScreen`/`unprojectFromScreen` only know
 * about the flat, elevation-0 grid. Using the flat inverse for click hit-testing meant an
 * elevated tile could only be clicked by aiming at its unlifted footprint — visibly offset
 * below where the tile is actually drawn. This tests each cell's real lifted top-face diamond
 * instead, and — since a taller tile can visually overlap a shorter one behind it — breaks any
 * overlap in favor of the higher sortKey, matching the painter's-algorithm draw order (the tile
 * actually visible at that pixel wins the hit test, same as it wins the paint).
 */
export function screenToCell(input: ScreenToCellInput): Cell | null {
  const projection: ProjectionParams = {
    canvasWidth: input.canvasWidth,
    canvasHeight: input.canvasHeight,
    gridWidth: input.gridWidth,
    gridHeight: input.gridHeight,
  };
  const { isoUnitX } = isoUnit(projection);
  if (isoUnitX === 0) return null;

  // Mirrors TacticalGridMap's spriteDest/elevationLiftPx exactly (destW/destH/lift scale) —
  // kept in lockstep by formula here rather than threaded through as extra parameters, since
  // every one of these is fully derived from isoUnitX and the fixed sprite constants.
  const { BASE_TILE_W, BASE_TILE_H, BASE_STEP_PX, MAX_ELEVATION, SPRITE_H } = TERRAIN_SPRITE_CONSTANTS;
  const destW = isoUnitX * 2.05;
  const destH = (destW * SPRITE_H) / BASE_TILE_W;
  const liftPerLevel = (destH / SPRITE_H) * BASE_STEP_PX;
  const halfW = destW / 2;
  const halfH = (BASE_TILE_H * (destW / BASE_TILE_W)) / 2;

  let best: { x: number; y: number; sortKey: number } | null = null;
  const isFloor = input.isFloor
    ?? ((x: number, y: number) => x >= 0 && x < input.gridWidth && y >= 0 && y < input.gridHeight);

  for (let y = 0; y < input.gridHeight; y++) {
    for (let x = 0; x < input.gridWidth; x++) {
      if (!isFloor(x, y)) continue;

      const isObstacle = input.obstacleCells.has(`${x},${y}`);
      const elevationLevel = isObstacle ? MAX_ELEVATION : (input.elevation[(y * input.gridWidth) + x] ?? 0);

      const { screenX: cx, screenY: flatCy } = projectToScreen(x, y, projection);
      const cy = flatCy - (elevationLevel * liftPerLevel);

      const dx = Math.abs(input.screenX - cx) / halfW;
      const dy = Math.abs(input.screenY - cy) / halfH;
      if (dx + dy > 1) continue;

      const sortKey = ((x + y) * 4) + elevationLevel;
      if (!best || sortKey > best.sortKey) best = { x, y, sortKey };
    }
  }

  return best ? { x: best.x, y: best.y } : null;
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
  /** The room theme's own accent tone (see THEME_ACCENT), used for plain floor tiles. */
  ambientTint: FloorTint;
  /** The room's theme — what gives its tiles their painted material (marble, moss, planks…).
   * Backend `RoomThemeResolver` emits exactly the 7 names the tile engine paints. */
  theme: RoomTheme;
  /** Flat, row-major, one 0..3 value per cell. */
  elevation: number[];
  obstacleCells: Set<string>;
  /** Whether a cell is part of the room at all. Cliff faces are painted on tiles whose
   * front-left/front-right neighbour is NOT floor, which is what makes a non-rectangular room
   * read as having real edges. Defaults to "everything inside the grid is floor" — the grid is
   * still a full rectangle server-side, so today only the outer border grows cliffs. */
  isFloor?: (x: number, y: number) => boolean;
  /** Keyed "x,y" (matches useGridCells' nodesByCell, which keys by lane,row = x,y). */
  nodesByCell: Map<string, NodeDto>;
  /** Resolves a node's own tint (see NODE_TILE_TONE), 'neutral' for unmapped types. */
  nodeTintFor: (node: NodeDto) => FloorTint;
  /** The party's current (possibly mid-step-animation) cell — always integer grid
   * coordinates, see usePartyTokenPath. Null before a grid exists. */
  party: { x: number; y: number } | null;
  /** The cell under the pointer, if any — painted as a highlight sprite hugging that tile's
   * own diamond at its own elevation. Null when the pointer is off the board. */
  hoveredCell?: { x: number; y: number } | null;
};

/**
 * Pure function: given the current grid/terrain state, decides which sprite each cell should
 * paint and where, sorted back-to-front for the canvas painter's algorithm. No canvas or DOM
 * access here — this is what makes it directly unit-testable without a browser.
 *
 * Terrain always paints at full visibility regardless of fog-of-war reveal state — there is no
 * more fog rendering layer. Reveal state still gates movement/click-ability elsewhere
 * (useGridCells.isRevealed), this function just no longer reads it.
 */
export function buildDrawPlan(input: BuildDrawPlanInput): DrawPlanEntry[] {
  const projection: ProjectionParams = {
    canvasWidth: input.canvasWidth,
    canvasHeight: input.canvasHeight,
    gridWidth: input.gridWidth,
    gridHeight: input.gridHeight,
  };

  const entries: DrawPlanEntry[] = [];

  const isFloor = input.isFloor
    ?? ((x: number, y: number) => x >= 0 && x < input.gridWidth && y >= 0 && y < input.gridHeight);
  const wallVariants = obstacleVariantCount(input.theme);

  for (const cell of input.cells) {
    // A hole in the room's shape paints nothing at all — that void is what makes the
    // surrounding tiles read as an edge, via the cliff faces they grow towards it.
    if (!isFloor(cell.x, cell.y)) continue;

    const cellKey = `${cell.x},${cell.y}`;
    const node = input.nodesByCell.get(cellKey) ?? null;
    const obstacle = input.obstacleCells.has(cellKey);
    const cellElevation = input.elevation[(cell.y * input.gridWidth) + cell.x] ?? 0;
    // A wall is painted as a full-height silhouette regardless of the ground under it, so it
    // must also SORT at full height — otherwise a tall floor tile beside it would paint over
    // the wall it is supposed to stand behind.
    const sortElevation = obstacle ? TERRAIN_SPRITE_CONSTANTS.MAX_ELEVATION : cellElevation;
    const { screenX, screenY } = projectToScreen(cell.x, cell.y, projection);

    let spriteKey: SpriteKey;

    if (obstacle) {
      spriteKey = {
        kind: 'obstacle',
        theme: input.theme,
        // Deterministic per cell: the same wall keeps the same silhouette across repaints,
        // but three distinct ones alternate across the room instead of one stamped everywhere.
        variant: wallVariants > 0 ? hashSeed(cellKey) % wallVariants : 0,
      };
    } else {
      const tint: FloorTint = node
        ? (node.isBoss ? 'blood' : input.nodeTintFor(node))
        : input.ambientTint;
      spriteKey = {
        kind: 'floor',
        tint,
        theme: input.theme,
        elevation: cellElevation,
        surfaceSeed: surfaceSeedFor(cell.x, cell.y),
        ...cliffSides(cell.x, cell.y, isFloor),
        // The painted tell for a node that fires on contact. A contact node with no tell paints
        // as plain floor — that IS the ambush, and must stay indistinguishable.
        danger: dangerFor(node),
        resolved: node?.state === 'Resolved',
        glow: node?.isBoss ?? false,
      };
    }

    entries.push({
      cellKey,
      x: cell.x,
      y: cell.y,
      spriteKey,
      screenX,
      screenY,
      sortKey: ((cell.x + cell.y) * 4) + sortElevation,
    });
  }

  if (input.hoveredCell) {
    const { x, y } = input.hoveredCell;
    const elevationLevel = input.elevation[(y * input.gridWidth) + x] ?? 0;
    const { screenX, screenY } = projectToScreen(x, y, projection);

    entries.push({
      cellKey: 'hover',
      x,
      y,
      spriteKey: { kind: 'highlight', variant: 'cursor', elevation: elevationLevel },
      screenX,
      screenY,
      // Above the tile it marks, below the party token standing on it (+0.5).
      sortKey: ((x + y) * 4) + elevationLevel + 0.25,
    });
  }

  if (input.party) {
    const { x, y } = input.party;
    const elevationLevel = input.elevation[(y * input.gridWidth) + x] ?? 0;
    const { screenX, screenY } = projectToScreen(x, y, projection);

    entries.push({
      cellKey: 'party',
      x,
      y,
      spriteKey: { kind: 'party', elevation: elevationLevel },
      screenX,
      screenY,
      // +0.5 sorts the party strictly after its own floor/obstacle tile (same base sortKey)
      // without relying on stable-sort insertion order to break the tie — it should always
      // paint on top of the ground it's standing on, but still get occluded by any tile with
      // a genuinely higher sortKey (a taller cell further along the diagonal).
      sortKey: ((x + y) * 4) + elevationLevel + 0.5,
    });
  }

  return entries.sort((a, b) => a.sortKey - b.sortKey);
}
