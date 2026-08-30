// Types for the dependency-free tilecraft painter (tilecraft.js).
//
// NOTE — diverges from the handoff on purpose: the shipped file wraps everything in
// `declare module './tilecraft' { … }`. An ambient module declaration with a RELATIVE
// specifier is not reliably resolved by TypeScript, and tsconfig.app.json has no `allowJs`,
// so the .js is never type-checked and this file carries the whole contract. Kept as a plain
// declaration file next to tilecraft.js, which TS resolves natively. Re-apply this unwrap on
// every handoff refresh.
export const TILE: { W: number; H: number; STEP: number; MAX: number; PAD: number };
export const SPRITE_W: number;
export const SPRITE_H: number;
export const GROUND_ANCHOR_RATIO: number;
export const PROP_SPRITE_H: number;
export const PROP_GROUND_ANCHOR_RATIO: number;
export const PROP_EXTRA_H: number;
export const THEME_NAMES: string[];
export type TilecraftTheme = {
  label: string; tint: string; surface: string; wall: string; particle: string;
  top: string; topDeep: string; seam: string; riser: string; riserDeep: string;
  accent: string; glow: string; sky: [string, string, string]; props: string[]; walls: string[];
  fog?: string; rug?: string;
};
export const THEMES: Record<string, TilecraftTheme>;
/** Les 27 salles canon, indexées par `catalogRoomKey` ("room.halldentree"…). */
export const ROOMS: Record<string, Partial<TilecraftTheme> & {
  label: string; base: string; chain: string | null; step: number;
}>;
export const ROOM_KEYS: string[];
export const PROP_KINDS: string[];
export const NODE_PROP: Record<string, string>;
export function themeLabel(themeOrRoom: string): string;
// Exported by tilecraft.js and imported by useTerrainSprites, but missing from the shipped
// .d.ts — declared here so the composable type-checks. Report upstream on the next handoff.
export function themeProps(themeOrRoom: string): string[];
export function fogColor(themeOrRoom: string): string;
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
export function drawDangerAura(
  ctx: CanvasRenderingContext2D,
  dx: number, dy: number, dw: number, dh: number, t: number, elevation?: number,
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
export function anchorRatioAt(elev?: number): number;
export function cliffSides(
  x: number, y: number, isFloor: (x: number, y: number) => boolean,
): { cliffLeft: boolean; cliffRight: boolean; cliffFarLeft: boolean; cliffFarRight: boolean };
export function drawBackdrop(
  ctx: CanvasRenderingContext2D, w: number, h: number, theme: string, t?: number, seed?: string,
  options?: { scenery?: boolean },
): void;
export function drawAmbient(
  ctx: CanvasRenderingContext2D, w: number, h: number, theme: string, t: number,
): void;
export function drawRevealFx(
  ctx: CanvasRenderingContext2D,
  dx: number, dy: number, dw: number, dh: number, t: number, theme: string, elevation?: number,
): void;
export const ISO_FIT: number;
export const ISO_V_CENTER: number;
export function isoUnit(p: {
  canvasWidth: number; gridWidth: number; gridHeight: number;
}): { isoUnitX: number; isoUnitY: number };
export function projectToScreen(x: number, y: number, p: {
  canvasWidth: number; canvasHeight: number; gridWidth: number; gridHeight: number;
}): { screenX: number; screenY: number };

// ── Couche combat tactique (lot design_handoff_combat) ────────────────────────────────
// Le .d.ts livré enveloppait tout dans `declare module './tilecraft' { … }`, ce qui prive
// le fichier de tout export de premier niveau : TypeScript ne le voit alors plus comme le
// module de déclaration de tilecraft.js ("File is not a module", TS2306). Les déclarations
// sont donc reportées ici au format du dépôt — export direct, sans wrapper.

export type RiskTier = {
  label: string; accent: string; enemies: number; fog: number; ambient: number; grade: number;
};
export const RISK_TIERS: Record<string, RiskTier>;
export const RISK_KEYS: string[];
export function riskTier(key: string): RiskTier;

export const HIGHLIGHT_VARIANTS: string[];
export const ALLY_RIM: string;
export const ENEMY_RIM: string;
export const ALLY_ROLES: string[];

/** Désaturation du décor : à appeler APRÈS les tuiles, AVANT les surbrillances et les unités. */
export function drawCombatGrade(
  ctx: CanvasRenderingContext2D, w: number, h: number, amount?: number, tint?: string,
): void;
export function drawUnitRing(
  ctx: CanvasRenderingContext2D, dx: number, dy: number, dw: number, dh: number,
  options?: { side?: 'ally' | 'enemy'; hp?: number; active?: boolean; downed?: boolean; elevation?: number },
  t?: number,
): void;
export function drawActionPips(
  ctx: CanvasRenderingContext2D, cx: number, cy: number, moved: boolean, acted: boolean, t?: number,
): void;
export function drawDissolveFx(
  ctx: CanvasRenderingContext2D, dx: number, dy: number, dw: number, dh: number,
  p: number, color?: string, elevation?: number,
): void;
export function drawDeployFx(
  ctx: CanvasRenderingContext2D, dx: number, dy: number, dw: number, dh: number,
  p: number, color?: string, elevation?: number,
): void;
export function drawImpactFx(
  ctx: CanvasRenderingContext2D, dx: number, dy: number, dw: number, dh: number,
  p: number, color?: string, elevation?: number,
): void;
