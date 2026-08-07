import type { DangerTell, NodeDto } from '../../runs/types/runTypes';
import type { Cell } from './useGridCells';
import { hashSeed } from './usePalaceTerrain';
import { propKindFor } from './useNodePresentation';
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

/**
 * A cache reads in two stages: a hollow-sounding slab before it is found, an open alcove after.
 * The unfound stage comes from the grid's hint cells (position only — the node itself is
 * withheld); the found stage comes from the node, which the server starts sending once it has
 * been searched out.
 */
function hiddenFor(
  cellKey: string,
  hintCells: Set<string> | undefined,
  node: NodeDto | null,
): 'none' | 'hint' | 'revealed' {
  if (hintCells?.has(cellKey)) return 'hint';
  if (node?.hiddenState === 'Revealed') return 'revealed';
  return 'none';
}

// BALANCE KNOB — shrinks the whole diamond inward so the outermost tile's own half-width/
// half-height never reaches the canvas edge (mirrors the old CSS ISO_FIT).
const ISO_FIT = 0.92;
// BALANCE KNOB — the same, for height. The tile size used to be derived from the canvas WIDTH
// alone, so on a wide-but-short viewport the board's vertical span (plus a tile's own height
// and the lift of a level-3 tile) ran off the bottom and the frontmost cell was clipped. The
// unit is now whichever of the two constraints is tighter. Lower than ISO_FIT because a 2:1
// diamond spends its height budget on the tile body and the elevation lift, not just spacing.
const ISO_FIT_V = 0.74;
// The board's vertical center sits a bit below the canvas's true center, clearing the
// top-tabs chrome that overlays the top-left corner (mirrors the old CSS ISO_V_CENTER).
const ISO_V_CENTER = 0.56;

export type ProjectionParams = {
  canvasWidth: number;
  canvasHeight: number;
  gridWidth: number;
  gridHeight: number;
};

// ── Camera layer ─────────────────────────────────────────────────────────────────
// Additive to everything above: isoUnit/projectToScreen/unprojectFromScreen/screenToCell restent
// disponibles pour les consommateurs qui veulent explicitement un fit-to-grid. L'exploration
// ET le combat tactique utilisent désormais cette couche caméra : centrage sur un point monde
// arbitraire (camX/camY), échelle pixel-par-case fixe et indépendante de la taille de la grille.
// Une grande salle/arène est donc réellement plus grande que le viewport au lieu d'être réduite
// jusqu'à tenir entièrement à l'écran.

/** Pixel width of one grid cell at zoom = 1 — the camera-mode equivalent of isoUnit's
 * fit-to-canvas unit, but a fixed balance knob instead of a function of grid/canvas size. */
export const BASE_TILE_PX = 96;

/** No manual zoom yet (see plan) — one named knob so promoting it to a user-adjustable ref
 * later touches no projection math. */
export const DEFAULT_ZOOM = 1;

export type CameraParams = {
  /** Grid-space focus point the camera centers on — fractional so it can sit mid-step during
   * party movement, in lockstep with usePartyTokenPath's own animated coordinates. */
  camX: number;
  camY: number;
  /** Multiplier on BASE_TILE_PX. 1 = default scale. */
  zoom: number;
};

export function cameraTileUnit(camera: CameraParams): { isoUnitX: number; isoUnitY: number } {
  const isoUnitX = BASE_TILE_PX * camera.zoom;
  return { isoUnitX, isoUnitY: isoUnitX / 2 };
}

/** Camera-space equivalent of projectToScreen: centers on (camX, camY) instead of the grid's
 * own center, and scales by the camera's fixed zoom instead of isoUnit's fit-to-canvas unit. */
export function projectToScreenCamera(
  x: number,
  y: number,
  params: { canvasWidth: number; canvasHeight: number } & CameraParams,
): { screenX: number; screenY: number } {
  const { isoUnitX, isoUnitY } = cameraTileUnit(params);
  const dx = x - params.camX;
  const dy = y - params.camY;
  return {
    screenX: (params.canvasWidth / 2) - ((dx - dy) * isoUnitX),
    screenY: (params.canvasHeight * ISO_V_CENTER) + ((dx + dy) * isoUnitY),
  };
}

/** Inverse of projectToScreenCamera. */
export function unprojectFromScreenCamera(
  screenX: number,
  screenY: number,
  params: { canvasWidth: number; canvasHeight: number } & CameraParams,
): { x: number; y: number } {
  const { isoUnitX, isoUnitY } = cameraTileUnit(params);
  const diff = isoUnitX === 0 ? 0 : ((params.canvasWidth / 2) - screenX) / isoUnitX; // dx - dy
  const sum = isoUnitY === 0 ? 0 : (screenY - (params.canvasHeight * ISO_V_CENTER)) / isoUnitY; // dx + dy
  // See unprojectFromScreen's own comment: `+ 0` folds a possible -0 into +0.
  return {
    x: Math.round(params.camX + ((sum + diff) / 2)) + 0,
    y: Math.round(params.camY + ((sum - diff) / 2)) + 0,
  };
}

/** Grid-space bounding box (inclusive, clamped to the grid, with a margin so partially-visible
 * edge tiles keep painting) of what the camera's viewport can actually show — computed from the
 * four screen corners' unprojected grid coordinates, since an isometric camera's visible region
 * is a rotated shape in grid space, not an axis-aligned rectangle. Used to cull both the draw
 * plan and screenToCellCamera's hit-test scan once the grid is bigger than the viewport. */
export function visibleCellRange(
  camera: CameraParams,
  canvasWidth: number,
  canvasHeight: number,
  gridWidth: number,
  gridHeight: number,
  margin = 2,
): { minX: number; maxX: number; minY: number; maxY: number } {
  const params = { canvasWidth, canvasHeight, ...camera };
  const corners = [
    unprojectFromScreenCamera(0, 0, params),
    unprojectFromScreenCamera(canvasWidth, 0, params),
    unprojectFromScreenCamera(0, canvasHeight, params),
    unprojectFromScreenCamera(canvasWidth, canvasHeight, params),
  ];
  const xs = corners.map((c) => c.x);
  const ys = corners.map((c) => c.y);
  return {
    minX: Math.max(0, Math.min(...xs) - margin),
    maxX: Math.min(gridWidth - 1, Math.max(...xs) + margin),
    minY: Math.max(0, Math.min(...ys) - margin),
    maxY: Math.min(gridHeight - 1, Math.max(...ys) + margin),
  };
}

export function isoUnit(params: ProjectionParams): { isoUnitX: number; isoUnitY: number } {
  const span = params.gridWidth + params.gridHeight;
  if (span === 0) return { isoUnitX: 0, isoUnitY: 0 };

  const widthConstrained = (params.canvasWidth * ISO_FIT) / span;
  // Doubled because the vertical unit is half the horizontal one on a 2:1 diamond, so the
  // board's vertical span only eats half as much per cell.
  const heightConstrained = (params.canvasHeight * 2 * ISO_FIT_V) / span;

  const isoUnitX = Math.min(widthConstrained, heightConstrained);
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
/** Shape shared by screenToCell and screenToCellCamera's hit-test — everything except how a
 * cell's own (x, y) turns into a screen position, which the caller supplies as `project` so
 * this stays agnostic to fit-to-canvas vs. fixed-zoom-camera projection. */
function hitTestCells(
  screenX: number,
  screenY: number,
  gridWidth: number,
  gridHeight: number,
  elevation: number[],
  obstacleCells: Set<string>,
  isFloor: (x: number, y: number) => boolean,
  isoUnitX: number,
  project: (x: number, y: number) => { screenX: number; screenY: number },
  bounds: { minX: number; maxX: number; minY: number; maxY: number },
): Cell | null {
  if (isoUnitX === 0) return null;

  // Mirrors TacticalGridMap's spriteDest/elevationLiftPx exactly (destW/destH/lift scale) —
  // kept in lockstep by formula here rather than threaded through as extra parameters, since
  // every one of these is fully derived from isoUnitX and the fixed sprite constants.
  const { BASE_TILE_W, BASE_TILE_H, BASE_STEP_PX, SPRITE_H } = TERRAIN_SPRITE_CONSTANTS;
  const destW = isoUnitX * 2.05;
  const destH = (destW * SPRITE_H) / BASE_TILE_W;
  const liftPerLevel = (destH / SPRITE_H) * BASE_STEP_PX;
  const halfW = destW / 2;
  const halfH = (BASE_TILE_H * (destW / BASE_TILE_W)) / 2;

  // Walkable candidates win outright over obstacles. A wall is never somewhere the party can be
  // sent, so when the pointer covers both a wall and open ground, the click belongs to the
  // ground — otherwise a cell tucked behind a wall becomes genuinely hard to pick.
  let best: { x: number; y: number; sortKey: number } | null = null;
  let bestObstacle: { x: number; y: number; sortKey: number } | null = null;

  const minX = Math.max(0, bounds.minX);
  const maxX = Math.min(gridWidth - 1, bounds.maxX);
  const minY = Math.max(0, bounds.minY);
  const maxY = Math.min(gridHeight - 1, bounds.maxY);

  for (let y = minY; y <= maxY; y++) {
    for (let x = minX; x <= maxX; x++) {
      if (!isFloor(x, y)) continue;

      const isObstacle = obstacleCells.has(`${x},${y}`);
      // An obstacle's own cell height, like every other cell. This used to be MAX_ELEVATION,
      // which lifted the clickable diamond three steps above where the tile is actually
      // painted — so it drifted over the cells BEHIND it and, ranking highest, stole their
      // clicks. Depth order still pins obstacles at MAX so a wall paints in front; that is a
      // painting concern and has no business deciding what the pointer is over.
      const elevationLevel = elevation[(y * gridWidth) + x] ?? 0;

      const { screenX: cx, screenY: flatCy } = project(x, y);
      const cy = flatCy - (elevationLevel * liftPerLevel);

      const dx = Math.abs(screenX - cx) / halfW;
      const dy = Math.abs(screenY - cy) / halfH;
      if (dx + dy > 1) continue;

      const sortKey = ((x + y) * 4) + elevationLevel;

      if (isObstacle) {
        if (!bestObstacle || sortKey > bestObstacle.sortKey) bestObstacle = { x, y, sortKey };
      } else if (!best || sortKey > best.sortKey) {
        best = { x, y, sortKey };
      }
    }
  }

  const hit = best ?? bestObstacle;
  return hit ? { x: hit.x, y: hit.y } : null;
}

export function screenToCell(input: ScreenToCellInput): Cell | null {
  const projection: ProjectionParams = {
    canvasWidth: input.canvasWidth,
    canvasHeight: input.canvasHeight,
    gridWidth: input.gridWidth,
    gridHeight: input.gridHeight,
  };
  const { isoUnitX } = isoUnit(projection);
  const isFloor = input.isFloor
    ?? ((x: number, y: number) => x >= 0 && x < input.gridWidth && y >= 0 && y < input.gridHeight);

  return hitTestCells(
    input.screenX, input.screenY, input.gridWidth, input.gridHeight,
    input.elevation, input.obstacleCells, isFloor, isoUnitX,
    (x, y) => projectToScreen(x, y, projection),
    { minX: 0, maxX: input.gridWidth - 1, minY: 0, maxY: input.gridHeight - 1 },
  );
}

export type ScreenToCellCameraInput = {
  screenX: number;
  screenY: number;
  canvasWidth: number;
  canvasHeight: number;
  gridWidth: number;
  gridHeight: number;
  camera: CameraParams;
  elevation: number[];
  obstacleCells: Set<string>;
  isFloor?: (x: number, y: number) => boolean;
  /** Restricts the scan to this range instead of the full 0..gridWidth/0..gridHeight — the perf
   * fix for a grid bigger than the viewport (see visibleCellRange). Defaults to the full grid. */
  visibleBounds?: { minX: number; maxX: number; minY: number; maxY: number };
};

/** Camera-space equivalent of screenToCell — same hit-test, projected through the camera
 * instead of the fit-to-canvas grid projection, and scan-bounded to the camera's viewport. */
export function screenToCellCamera(input: ScreenToCellCameraInput): Cell | null {
  const { isoUnitX } = cameraTileUnit(input.camera);
  const isFloor = input.isFloor
    ?? ((x: number, y: number) => x >= 0 && x < input.gridWidth && y >= 0 && y < input.gridHeight);
  const params = { canvasWidth: input.canvasWidth, canvasHeight: input.canvasHeight, ...input.camera };

  return hitTestCells(
    input.screenX, input.screenY, input.gridWidth, input.gridHeight,
    input.elevation, input.obstacleCells, isFloor, isoUnitX,
    (x, y) => projectToScreenCamera(x, y, params),
    input.visibleBounds ?? { minX: 0, maxX: input.gridWidth - 1, minY: 0, maxY: input.gridHeight - 1 },
  );
}

export type DrawPlanEntry = {
  cellKey: string;
  x: number;
  y: number;
  spriteKey: SpriteKey;
  screenX: number;
  screenY: number;
  /** The cell's own elevation. A rendering hint, deliberately NOT part of the sprite key: a
   * prop looks the same at any height, so baking elevation into its key would fragment the
   * cache for nothing — but the renderer still has to lift it onto the tile it stands on. */
  elevation: number;
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
  /** Cells holding an unfound cache — painted as a slab that rings hollow, which is the whole
   * tell that makes searching worth a player's budget. Position only; what is under it stays
   * unknown until searched. */
  hintCells?: Set<string>;
  /** Cells the party may legally click to move to (revealed, walkable, affordable). Painted
   * with the 'move' highlight so the reachable area is readable at a glance instead of having
   * to be guessed from the fog. */
  reachableCells?: Set<string>;
  /** The exact steps the party would walk to reach the hovered cell, in order — the route the
   * pathfinder chose, which is rarely the straight line the player pictures once a wall or a
   * climb is in the way. Empty or absent when nothing is hovered. */
  pathCells?: readonly { x: number; y: number }[];
  /** The cell under the pointer, if any — painted as a highlight sprite hugging that tile's
   * own diamond at its own elevation. Null when the pointer is off the board. */
  hoveredCell?: { x: number; y: number } | null;
  /** When present, every position in this plan is projected through the camera (see
   * useCamera.ts) instead of the fit-to-canvas grid projection. Absent means today's behavior:
   * the whole grid scaled to fit the canvas, centered on its own middle. */
  camera?: CameraParams;
  /** Restricts which cells are even considered, before any per-cell work (sprite key
   * resolution, projection, sort key) — the perf fix for a grid bigger than the viewport.
   * Ignored when `camera` is absent (today's rooms fit the canvas in one screen, nothing to
   * cull). Defaults to the full grid when `camera` is present but this is omitted. */
  visibleBounds?: { minX: number; maxX: number; minY: number; maxY: number };
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
  const cameraParams = input.camera
    ? { canvasWidth: input.canvasWidth, canvasHeight: input.canvasHeight, ...input.camera }
    : null;
  const project = cameraParams
    ? (x: number, y: number) => projectToScreenCamera(x, y, cameraParams)
    : (x: number, y: number) => projectToScreen(x, y, projection);
  const bounds = input.camera
    ? (input.visibleBounds ?? { minX: 0, maxX: input.gridWidth - 1, minY: 0, maxY: input.gridHeight - 1 })
    : null;
  const inBounds = (x: number, y: number) =>
    !bounds || (x >= bounds.minX && x <= bounds.maxX && y >= bounds.minY && y <= bounds.maxY);

  const entries: DrawPlanEntry[] = [];

  const isFloor = input.isFloor
    ?? ((x: number, y: number) => x >= 0 && x < input.gridWidth && y >= 0 && y < input.gridHeight);
  const wallVariants = obstacleVariantCount(input.theme);

  for (const cell of input.cells) {
    // A hole in the room's shape paints nothing at all — that void is what makes the
    // surrounding tiles read as an edge, via the cliff faces they grow towards it.
    if (!isFloor(cell.x, cell.y)) continue;
    // Outside the camera's viewport (plus margin) — skip before any per-cell work at all.
    // Absent camera (today's fit-to-canvas rooms) never culls anything.
    if (!inBounds(cell.x, cell.y)) continue;

    const cellKey = `${cell.x},${cell.y}`;
    const node = input.nodesByCell.get(cellKey) ?? null;
    const obstacle = input.obstacleCells.has(cellKey);
    const cellElevation = input.elevation[(cell.y * input.gridWidth) + cell.x] ?? 0;
    // A wall is painted as a full-height silhouette regardless of the ground under it, so it
    // must also SORT at full height — otherwise a tall floor tile beside it would paint over
    // the wall it is supposed to stand behind.
    const sortElevation = obstacle ? TERRAIN_SPRITE_CONSTANTS.MAX_ELEVATION : cellElevation;
    const { screenX, screenY } = project(cell.x, cell.y);

    let spriteKey: SpriteKey;

    if (obstacle) {
      spriteKey = {
        kind: 'obstacle',
        theme: input.theme,
        // Deterministic per cell: the same wall keeps the same silhouette across repaints,
        // but three distinct ones alternate across the room instead of one stamped everywhere.
        variant: wallVariants > 0 ? hashSeed(cellKey) % wallVariants : 0,
        // Its own cell's height, NOT the board maximum. A low silhouette (rubble, boulder,
        // deadfall) on flat ground has to read as something you cannot cross rather than as
        // one more tower — see the obstacle key's own note. Depth ORDER still pins obstacles
        // at MAX_ELEVATION, just below.
        elevation: cellElevation,
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
        // A slab that rings hollow, or the open alcove it becomes once searched.
        hidden: hiddenFor(cellKey, input.hintCells, node),
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
      elevation: cellElevation,
      sortKey: ((cell.x + cell.y) * 4) + sortElevation,
    });
  }

  // Scenery standing on a node's tile — painted after every floor tile so it can overlap the
  // ones behind it, and sorted just under the party so the party never disappears behind a
  // prop on its own cell.
  for (const cell of input.cells) {
    if (!isFloor(cell.x, cell.y)) continue;
    if (!inBounds(cell.x, cell.y)) continue;

    const node = input.nodesByCell.get(`${cell.x},${cell.y}`);
    if (!node || node.state === 'Resolved') continue;

    const prop = propKindFor(node);
    if (!prop) continue;

    const elevationLevel = input.elevation[(cell.y * input.gridWidth) + cell.x] ?? 0;
    const { screenX, screenY } = project(cell.x, cell.y);

    entries.push({
      cellKey: `prop:${cell.x},${cell.y}`,
      x: cell.x,
      y: cell.y,
      spriteKey: { kind: 'prop', theme: input.theme, prop },
      screenX,
      screenY,
      elevation: elevationLevel,
      sortKey: ((cell.x + cell.y) * 4) + elevationLevel + 0.45,
    });
  }

  if (input.reachableCells) {
    for (const cellKey of input.reachableCells) {
      const [x, y] = cellKey.split(',').map(Number);
      const elevationLevel = input.elevation[(y * input.gridWidth) + x] ?? 0;
      const { screenX, screenY } = project(x, y);

      entries.push({
        cellKey: `move:${cellKey}`,
        x,
        y,
        spriteKey: { kind: 'highlight', variant: 'move', elevation: elevationLevel },
        screenX,
        screenY,
        elevation: elevationLevel,
        // Below the cursor highlight (+0.25) so hovering a reachable cell still reads as
        // hovered rather than merging into the reachable wash.
        sortKey: ((x + y) * 4) + elevationLevel + 0.2,
      });
    }
  }

  // The route the party would actually walk, laid over the reachable wash so the player can see
  // the detour a wall or a climb forces before paying for it.
  for (const step of input.pathCells ?? []) {
    const elevationLevel = input.elevation[(step.y * input.gridWidth) + step.x] ?? 0;
    const { screenX, screenY } = project(step.x, step.y);

    entries.push({
      cellKey: `path:${step.x},${step.y}`,
      x: step.x,
      y: step.y,
      spriteKey: { kind: 'highlight', variant: 'path', elevation: elevationLevel },
      screenX,
      screenY,
      elevation: elevationLevel,
      // Over the reachable wash (+0.2), under the cursor (+0.25): the route reads as a stronger
      // mark on top of the range, and the cell under the pointer still wins.
      sortKey: ((step.x + step.y) * 4) + elevationLevel + 0.22,
    });
  }

  if (input.hoveredCell) {
    const { x, y } = input.hoveredCell;
    const elevationLevel = input.elevation[(y * input.gridWidth) + x] ?? 0;
    const { screenX, screenY } = project(x, y);

    entries.push({
      cellKey: 'hover',
      x,
      y,
      spriteKey: { kind: 'highlight', variant: 'cursor', elevation: elevationLevel },
      screenX,
      screenY,
      elevation: elevationLevel,
      // Above the tile it marks, below the party token standing on it (+0.5).
      sortKey: ((x + y) * 4) + elevationLevel + 0.25,
    });
  }

  if (input.party) {
    const { x, y } = input.party;
    const elevationLevel = input.elevation[(y * input.gridWidth) + x] ?? 0;
    const { screenX, screenY } = project(x, y);

    entries.push({
      cellKey: 'party',
      x,
      y,
      spriteKey: { kind: 'party', elevation: elevationLevel },
      screenX,
      screenY,
      elevation: elevationLevel,
      // +0.5 sorts the party strictly after its own floor/obstacle tile (same base sortKey)
      // without relying on stable-sort insertion order to break the tie — it should always
      // paint on top of the ground it's standing on, but still get occluded by any tile with
      // a genuinely higher sortKey (a taller cell further along the diagonal).
      sortKey: ((x + y) * 4) + elevationLevel + 0.5,
    });
  }

  return entries.sort((a, b) => a.sortKey - b.sortKey);
}
