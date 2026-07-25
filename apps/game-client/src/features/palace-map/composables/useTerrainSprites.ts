/**
 * Procedural isometric tile sprites — the canvas analog of SigilIcon.vue's "draw a shape
 * per kind" convention: every visual variant is drawn ONCE with plain Canvas2D primitives
 * (no images, no external assets) into a small offscreen canvas, cached by a string key, and
 * blitted (`drawImage`) many times by the renderer. Sprites are baked at a fixed reference
 * resolution decoupled from the on-screen tile size, so a window resize never invalidates the
 * cache — only the destination size passed to `drawImage` changes.
 */

// BALANCE/ART KNOBS — sprite canvas geometry, not gameplay.
const BASE_TILE_W = 128;
const BASE_TILE_H = 64; // 2:1 diamond, the classic isometric ratio
const BASE_STEP_PX = 20; // per-elevation-level lift, in sprite-space pixels
const MAX_ELEVATION = 3; // mirrors the backend's RoomGrid.MaxElevation
const SPRITE_PAD = 6;
// BALANCE/ART KNOB — party token diameter, as a fraction of a tile's width.
const PARTY_DIAMETER_RATIO = 0.45;
const SPRITE_H = BASE_TILE_H + (MAX_ELEVATION * BASE_STEP_PX) + SPRITE_PAD;
// The sprite-space Y of the elevation-0 diamond's center — the one fixed point every sprite
// (regardless of its own elevation) shares, since a taller tile is drawn higher within the
// same fixed-size canvas rather than the canvas itself growing. The renderer anchors this
// point to a cell's flat ground-projected screen position, so lifting a tile up never moves
// its footprint on the board.
const GROUND_ANCHOR_Y = (MAX_ELEVATION * BASE_STEP_PX) + (BASE_TILE_H / 2);
/** Fraction of the full sprite height where GROUND_ANCHOR_Y sits — used by the renderer to
 * position a blitted sprite regardless of destination scale. */
export const GROUND_ANCHOR_RATIO = GROUND_ANCHOR_Y / SPRITE_H;

/** One of the four accent tones already used app-wide (theme accents and node tones both
 * resolve to one of these — see THEME_ACCENT / NODE_TILE_TONE). 'neutral' is the fallback
 * for a node type with no specific tone entry. */
export type FloorTint = 'blood' | 'gold' | 'frost' | 'sap' | 'neutral';

export type SpriteKey =
  | { kind: 'floor'; tint: FloorTint; elevation: number; resolved: boolean; glow: boolean }
  | { kind: 'obstacle' }
  | { kind: 'party'; elevation: number };

export function spriteKeyToString(key: SpriteKey): string {
  switch (key.kind) {
    case 'floor':
      return `floor:${key.tint}:${key.elevation}:${key.resolved ? 'r' : '-'}:${key.glow ? 'g' : '-'}`;
    case 'obstacle':
      return 'obstacle';
    case 'party':
      return `party:${key.elevation}`;
  }
}

function resolveCssColor(varName: string, fallback: string): string {
  if (typeof window === 'undefined' || typeof document === 'undefined') return fallback;
  const value = getComputedStyle(document.documentElement).getPropertyValue(varName).trim();
  return value || fallback;
}

// Fallback hex approximations of the dark-palette tokens (shared/styles/tokens.css) — only
// used when getComputedStyle can't resolve the real oklch() value (e.g. no stylesheet loaded,
// as in a plain jsdom test environment).
const CSS_VAR_FALLBACK: Record<string, string> = {
  '--gold': '#dcb45c',
  '--frost': '#a6b4e8',
  '--blood': '#e0a394',
  '--sap': '#8fd6b0',
  '--void': '#242038',
  '--line-strong': '#9a93c4',
};

function cssVar(varName: string): string {
  return resolveCssColor(varName, CSS_VAR_FALLBACK[varName] ?? '#8a84ad');
}

const FLOOR_TINT_VAR: Record<FloorTint, string> = {
  blood: '--blood',
  gold: '--gold',
  frost: '--frost',
  sap: '--sap',
  neutral: '--frost',
};

function makeCanvas(width: number, height: number): HTMLCanvasElement {
  const canvas = document.createElement('canvas');
  canvas.width = width;
  canvas.height = height;
  return canvas;
}

/** Sprite-space center Y of a tile's top face at a given elevation (0 = ground level). */
function diamondCenterY(elevation: number): number {
  const lift = (MAX_ELEVATION - elevation) * BASE_STEP_PX;
  return lift + (BASE_TILE_H / 2);
}

/** The top face's four corners, optionally inset (negative = outset) from the tile's own edge —
 * used both to fill/stroke the tile itself and to trace a glow outline that hugs its diamond
 * shape instead of an unrelated bounding rectangle. */
function diamondCorners(elevation: number, inset = 0) {
  const cx = BASE_TILE_W / 2;
  const centerY = diamondCenterY(elevation);
  const halfW = (BASE_TILE_W / 2) - inset;
  const halfH = (BASE_TILE_H / 2) - inset;
  return {
    top: { x: cx, y: centerY - halfH },
    right: { x: cx + halfW, y: centerY },
    bottom: { x: cx, y: centerY + halfH },
    left: { x: cx - halfW, y: centerY },
  };
}

function diamondPath(elevation: number, inset = 0): Path2D {
  const { top, right, bottom, left } = diamondCorners(elevation, inset);
  const path = new Path2D();
  path.moveTo(top.x, top.y);
  path.lineTo(right.x, right.y);
  path.lineTo(bottom.x, bottom.y);
  path.lineTo(left.x, left.y);
  path.closePath();
  return path;
}

function drawIsoDiamond(
  ctx: CanvasRenderingContext2D,
  elevation: number,
  fillColor: string,
  edgeColor: string,
  riserAlpha: number,
) {
  const { right, bottom, left } = diamondCorners(elevation);
  const groundY = (MAX_ELEVATION * BASE_STEP_PX) + (BASE_TILE_H / 2);

  if (elevation > 0) {
    const riser = new Path2D();
    riser.moveTo(left.x, left.y);
    riser.lineTo(bottom.x, bottom.y);
    riser.lineTo(right.x, right.y);
    riser.lineTo(right.x, groundY);
    riser.lineTo(bottom.x, groundY + (BASE_TILE_H / 2));
    riser.lineTo(left.x, groundY);
    riser.closePath();
    ctx.fillStyle = fillColor;
    ctx.fill(riser);
    ctx.fillStyle = `rgba(0, 0, 0, ${riserAlpha})`;
    ctx.fill(riser);
  }

  const topFace = diamondPath(elevation);
  ctx.fillStyle = fillColor;
  ctx.fill(topFace);
  ctx.lineWidth = 2;
  ctx.strokeStyle = edgeColor;
  ctx.stroke(topFace);
  ctx.fillStyle = 'rgba(255, 255, 255, 0.14)';
  ctx.fill(topFace);
}

function applyOverlay(ctx: CanvasRenderingContext2D, canvas: HTMLCanvasElement, color: string, alpha: number) {
  ctx.globalAlpha = alpha;
  ctx.fillStyle = color;
  ctx.fillRect(0, 0, canvas.width, canvas.height);
  ctx.globalAlpha = 1;
}

function bakeFloorSprite(key: Extract<SpriteKey, { kind: 'floor' }>): HTMLCanvasElement {
  const canvas = makeCanvas(BASE_TILE_W, SPRITE_H);
  const ctx = canvas.getContext('2d');
  if (!ctx) return canvas;

  const tint = cssVar(FLOOR_TINT_VAR[key.tint]);
  const edge = cssVar('--line-strong');
  drawIsoDiamond(ctx, key.elevation, tint, edge, 0.35);

  if (key.glow) {
    ctx.save();
    ctx.shadowColor = cssVar('--blood');
    ctx.shadowBlur = 18;
    ctx.strokeStyle = cssVar('--blood');
    ctx.lineWidth = 3;
    // Traces the tile's own diamond silhouette (slightly outset) instead of its bounding
    // rectangle — a rectangular glow around a diamond tile reads as an unrelated square
    // slapped on top rather than a halo around the tile itself.
    ctx.stroke(diamondPath(key.elevation, -3));
    ctx.restore();
  }

  if (key.resolved) {
    applyOverlay(ctx, canvas, 'rgba(0, 0, 0, 0.45)', 1);
  }

  return canvas;
}

function bakeObstacleSprite(): HTMLCanvasElement {
  const canvas = makeCanvas(BASE_TILE_W, SPRITE_H);
  const ctx = canvas.getContext('2d');
  if (!ctx) return canvas;

  // Obstacles always read as a solid, imposing block regardless of the cell's own
  // elevation value — a wall should never look shorter than the floor beside it.
  const tint = cssVar('--void');
  const edge = cssVar('--line-strong');
  drawIsoDiamond(ctx, MAX_ELEVATION, tint, edge, 0.45);

  // Diagonal hatch across the top face — the one purely-decorative cue that reads as
  // "impassable", distinct from any floor tint at any elevation.
  ctx.save();
  ctx.beginPath();
  ctx.rect(0, 0, BASE_TILE_W, BASE_TILE_H);
  ctx.clip();
  ctx.strokeStyle = 'rgba(255, 255, 255, 0.12)';
  ctx.lineWidth = 3;
  for (let offset = -BASE_TILE_H; offset < BASE_TILE_W; offset += 14) {
    ctx.beginPath();
    ctx.moveTo(offset, 0);
    ctx.lineTo(offset + BASE_TILE_H, BASE_TILE_H);
    ctx.stroke();
  }
  ctx.restore();

  return canvas;
}

function bakePartySprite(key: Extract<SpriteKey, { kind: 'party' }>): HTMLCanvasElement {
  const canvas = makeCanvas(BASE_TILE_W, SPRITE_H);
  const ctx = canvas.getContext('2d');
  if (!ctx) return canvas;

  // Anchored at the same ground-face center a floor tile's own diamond would sit at for this
  // elevation (see diamondCenterY/GROUND_ANCHOR_RATIO) — baking the party at its cell's real
  // elevation, rather than positioning it via a separate DOM overlay, is what keeps it
  // pixel-aligned with the terrain under it (no separate lift formula to keep in sync) and
  // lets the normal depth-sorted blit loop occlude it behind a taller tile in front of it.
  const cx = BASE_TILE_W / 2;
  const groundY = diamondCenterY(key.elevation);
  const radius = (BASE_TILE_W * PARTY_DIAMETER_RATIO) / 2;

  ctx.save();
  ctx.strokeStyle = cssVar('--gold');
  ctx.globalAlpha = 0.7;
  ctx.lineWidth = 1.5;
  ctx.beginPath();
  ctx.ellipse(cx, groundY + (radius * 0.55), radius * 0.85, radius * 0.32, 0, 0, Math.PI * 2);
  ctx.stroke();
  ctx.restore();

  ctx.save();
  ctx.shadowColor = cssVar('--gold');
  ctx.shadowBlur = 14;
  const gradient = ctx.createRadialGradient(
    cx - (radius * 0.3), groundY - (radius * 0.35), radius * 0.1,
    cx, groundY, radius,
  );
  gradient.addColorStop(0, '#ffe9b8');
  gradient.addColorStop(1, cssVar('--gold'));
  ctx.fillStyle = gradient;
  ctx.beginPath();
  ctx.arc(cx, groundY, radius, 0, Math.PI * 2);
  ctx.fill();
  ctx.strokeStyle = 'rgba(255, 255, 255, 0.7)';
  ctx.lineWidth = 2;
  ctx.stroke();
  ctx.restore();

  return canvas;
}

function bakeSprite(key: SpriteKey): HTMLCanvasElement {
  switch (key.kind) {
    case 'floor':
      return bakeFloorSprite(key);
    case 'obstacle':
      return bakeObstacleSprite();
    case 'party':
      return bakePartySprite(key);
  }
}

export function useTerrainSprites() {
  const cache = new Map<string, HTMLCanvasElement>();

  function getSprite(key: SpriteKey): HTMLCanvasElement {
    const cacheKey = spriteKeyToString(key);
    const cached = cache.get(cacheKey);
    if (cached) return cached;

    const sprite = bakeSprite(key);
    cache.set(cacheKey, sprite);
    return sprite;
  }

  return { getSprite, spriteAspectRatio: BASE_TILE_W / SPRITE_H };
}

export const TERRAIN_SPRITE_CONSTANTS = {
  BASE_TILE_W,
  BASE_TILE_H,
  BASE_STEP_PX,
  MAX_ELEVATION,
  SPRITE_H,
};
