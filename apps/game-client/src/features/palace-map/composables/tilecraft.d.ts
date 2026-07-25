/**
 * Types for the dependency-free Canvas2D tile-painting engine (`./tilecraft.js`).
 *
 * Written as a plain declaration module — NOT wrapped in `declare module './tilecraft'`.
 * An ambient module declaration with a relative specifier is not reliably resolved by
 * TypeScript; a sibling `.d.ts` is, because `./tilecraft` resolves to `tilecraft.d.ts`
 * before `tilecraft.js`. That matters here because `tsconfig.app.json` has no `allowJs`
 * and only includes `src/**\/*.{ts,tsx,vue}`, so this file is the only type contract for
 * the engine — Vite still resolves the real `.js` at runtime.
 */

export const TILE: { W: number; H: number; STEP: number; MAX: number; PAD: number };
export const SPRITE_W: number;
export const SPRITE_H: number;
export const GROUND_ANCHOR_Y: number;
export const GROUND_ANCHOR_RATIO: number;
export const PROP_EXTRA_H: number;
export const PROP_SPRITE_H: number;
export const PROP_GROUND_ANCHOR_RATIO: number;

export const THEME_NAMES: string[];
export const THEMES: Record<string, {
  label: string; tint: string; surface: string; wall: string; particle: string;
  top: string; topDeep: string; seam: string; riser: string; riserDeep: string;
  accent: string; glow: string; sky: [string, string, string]; props: string[]; walls: string[];
}>;

export function hashSeed(input: string): number;

export function createTileForge(options?: { grain?: number }): {
  getSprite(key: Record<string, unknown>): HTMLCanvasElement;
  clear(): void;
  spriteAspectRatio: number;
  groundAnchorRatio: number;
  propAspectRatio: number;
  propGroundAnchorRatio: number;
};

export function spriteKeyToString(key: Record<string, unknown>): string;
export function obstacleVariantCount(theme: string): number;

/** Ground-anchor ratio of a tile's own top face at `elev` — runtime effects that must sit
 * ON the tile (not at its base) need this, otherwise they stick to elevation 0. */
export function anchorRatioAt(elev?: number): number;

/** A tile whose front-left `(x+1, y)` / front-right `(x, y+1)` neighbour is outside the
 * room gets a broken rock face plunging into the dark. */
export function cliffSides(
  x: number, y: number, isFloor: (x: number, y: number) => boolean,
): { cliffLeft: boolean; cliffRight: boolean };

export function drawBackdrop(
  ctx: CanvasRenderingContext2D, w: number, h: number, theme: string, t?: number, seed?: string,
  options?: { scenery?: boolean },
): void;
export function drawAmbient(
  ctx: CanvasRenderingContext2D, w: number, h: number, theme: string, t: number,
): void;
export function drawDangerAura(
  ctx: CanvasRenderingContext2D,
  dx: number, dy: number, dw: number, dh: number, t: number, elevation?: number,
): void;
export function drawRevealFx(
  ctx: CanvasRenderingContext2D,
  dx: number, dy: number, dw: number, dh: number, t: number, theme: string, elevation?: number,
): void;
export function drawFireFx(
  ctx: CanvasRenderingContext2D,
  dx: number, dy: number, dw: number, dh: number, t: number, anchorRatio?: number,
): void;
export function drawStarFx(
  ctx: CanvasRenderingContext2D,
  dx: number, dy: number, dw: number, dh: number, t: number, anchorRatio?: number,
): void;
export function drawFogOfWar(
  ctx: CanvasRenderingContext2D, w: number, h: number,
  centers: Array<{ x: number; y: number; radius?: number }>,
  radius: number, theme: string, t?: number,
): void;
export function visionRadius(cells: number, isoUnitX: number): number;

/** The engine ships its own copy of the isometric projection. This project keeps
 * `useTerrainDrawPlan.ts` as the single source of truth for projection/hit-testing (the two
 * are identical: ISO_FIT 0.82, ISO_V_CENTER 0.56, same formulas) — declared here only for
 * completeness, deliberately not imported. */
export const ISO_FIT: number;
export const ISO_V_CENTER: number;
export function isoUnit(p: {
  canvasWidth: number; gridWidth: number; gridHeight: number;
}): { isoUnitX: number; isoUnitY: number };
export function projectToScreen(x: number, y: number, p: {
  canvasWidth: number; canvasHeight: number; gridWidth: number; gridHeight: number;
}): { screenX: number; screenY: number };
