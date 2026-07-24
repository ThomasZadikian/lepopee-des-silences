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
export type TileLight = 'lit' | 'ghost';

export type SpriteKey =
  | { kind: 'floor'; tint: FloorTint; elevation: number; resolved: boolean; glow: boolean; light: TileLight }
  | { kind: 'obstacle'; light: TileLight }
  | { kind: 'fog'; variant: number; marker: boolean };

export function spriteKeyToString(key: SpriteKey): string {
  switch (key.kind) {
    case 'floor':
      return `floor:${key.tint}:${key.elevation}:${key.resolved ? 'r' : '-'}:${key.glow ? 'g' : '-'}:${key.light}`;
    case 'obstacle':
      return `obstacle:${key.light}`;
    case 'fog':
      return `fog:${key.variant}:${key.marker ? 'marker' : 'blank'}`;
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

function drawIsoDiamond(
  ctx: CanvasRenderingContext2D,
  elevation: number,
  fillColor: string,
  edgeColor: string,
  riserAlpha: number,
) {
  const cx = BASE_TILE_W / 2;
  const lift = (MAX_ELEVATION - elevation) * BASE_STEP_PX;
  const centerY = lift + (BASE_TILE_H / 2);
  const top = { x: cx, y: centerY - (BASE_TILE_H / 2) };
  const right = { x: BASE_TILE_W, y: centerY };
  const bottom = { x: cx, y: centerY + (BASE_TILE_H / 2) };
  const left = { x: 0, y: centerY };
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

  const topFace = new Path2D();
  topFace.moveTo(top.x, top.y);
  topFace.lineTo(right.x, right.y);
  topFace.lineTo(bottom.x, bottom.y);
  topFace.lineTo(left.x, left.y);
  topFace.closePath();
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
    const lift = (MAX_ELEVATION - key.elevation) * BASE_STEP_PX;
    ctx.strokeRect(4, lift + 2, BASE_TILE_W - 8, BASE_TILE_H - 4);
    ctx.restore();
  }

  if (key.resolved) {
    applyOverlay(ctx, canvas, 'rgba(0, 0, 0, 0.45)', 1);
  }

  if (key.light === 'ghost') {
    applyOverlay(ctx, canvas, cssVar('--void'), 0.55);
  }

  return canvas;
}

function bakeObstacleSprite(key: Extract<SpriteKey, { kind: 'obstacle' }>): HTMLCanvasElement {
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

  if (key.light === 'ghost') {
    applyOverlay(ctx, canvas, cssVar('--void'), 0.55);
  }

  return canvas;
}

function bakeFogSprite(key: Extract<SpriteKey, { kind: 'fog' }>): HTMLCanvasElement {
  const canvas = makeCanvas(BASE_TILE_W, SPRITE_H);
  const ctx = canvas.getContext('2d');
  if (!ctx) return canvas;

  // A handful of baked variants (picked per-cell by a seeded hash, see buildDrawPlan) stand
  // in for genuinely-unique-per-cell mist — enough to break the repeating-texture look
  // without baking one sprite per cell.
  const cx = BASE_TILE_W / 2;
  const cy = SPRITE_H / 2;
  const jitterX = ((key.variant * 37) % 23) - 11;
  const jitterY = ((key.variant * 53) % 19) - 9;
  const blobs = [
    { dx: -18 + jitterX, dy: -6 + jitterY, r: 40 },
    { dx: 16 - jitterY, dy: 4 + jitterX, r: 34 },
    { dx: -4 + jitterY, dy: 14 - jitterX, r: 30 },
  ];

  ctx.filter = 'blur(6px)';
  for (const blob of blobs) {
    const gradient = ctx.createRadialGradient(
      cx + blob.dx, cy + blob.dy, 0,
      cx + blob.dx, cy + blob.dy, blob.r,
    );
    gradient.addColorStop(0, 'rgba(20, 18, 32, 0.9)');
    gradient.addColorStop(1, 'rgba(20, 18, 32, 0)');
    ctx.fillStyle = gradient;
    ctx.beginPath();
    ctx.arc(cx + blob.dx, cy + blob.dy, blob.r, 0, Math.PI * 2);
    ctx.fill();
  }
  ctx.filter = 'none';
  applyOverlay(ctx, canvas, 'rgba(10, 9, 18, 0.5)', key.marker ? 0.45 : 0.85);

  return canvas;
}

function bakeSprite(key: SpriteKey): HTMLCanvasElement {
  switch (key.kind) {
    case 'floor':
      return bakeFloorSprite(key);
    case 'obstacle':
      return bakeObstacleSprite(key);
    case 'fog':
      return bakeFogSprite(key);
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
