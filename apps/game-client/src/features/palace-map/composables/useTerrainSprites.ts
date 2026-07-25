/**
 * Painted isometric tile sprites — drop-in replacement for `useTerrainSprites.ts`.
 *
 * Same contract as the flat-fill version it replaces: sprites are baked ONCE per variant into
 * an offscreen canvas, cached by a string key, and blitted by the renderer; `GROUND_ANCHOR_RATIO`
 * and `spriteAspectRatio` keep the blit independent of on-screen tile size. Only the painting
 * changes — hand-painted stone/earth/moss with visible grain, readable risers, cliff edges,
 * hidden-node states and danger tells (see ./tilecraft.js).
 *
 * Migration notes
 * ───────────────
 * • `SPRITE_H` grows (130 → 170) because verticals need headroom. Nothing to change in
 *   `TacticalGridMap.vue` / `useTerrainDrawPlan.ts`: both derive their destination rect from
 *   `spriteAspectRatio` + `GROUND_ANCHOR_RATIO`.
 * • `SpriteKey.floor` gains OPTIONAL fields (`theme`, `surfaceSeed`, `cliffLeft`, `cliffRight`,
 *   `hidden`, `danger`). Existing call sites keep working unchanged; pass `theme` to get the
 *   per-room identity, and the cliff flags to get non-rectangular room borders.
 * • Two new kinds: `prop` (tall scenery) and `highlight` (gameplay layer).
 * • ⚠ `obstacle` sprites are now baked on the TALL canvas, like props — a wall silhouette rises
 *   well above its tile and would otherwise be chopped flat. Blit `obstacle` and `prop` with
 *   `propAspectRatio` + `propGroundAnchorRatio`; floors/party/highlights keep `spriteAspectRatio`
 *   + `GROUND_ANCHOR_RATIO`. `usesPropRect(key)` answers that per key.
 */

import {
  createTileForge,
  anchorRatioAt,
  cliffSides,
  drawBackdrop,
  drawAmbient,
  drawRevealFx,
  drawDangerAura,
  drawFireFx,
  drawStarFx,
  drawFogOfWar,
  visionRadius,
  obstacleVariantCount,
  spriteKeyToString as forgeKeyToString,
  THEMES,
  TILE,
  SPRITE_W,
  SPRITE_H,
  GROUND_ANCHOR_RATIO as FORGE_GROUND_ANCHOR_RATIO,
  PROP_SPRITE_H,
  PROP_GROUND_ANCHOR_RATIO,
} from './tilecraft';

export const GROUND_ANCHOR_RATIO = FORGE_GROUND_ANCHOR_RATIO;
export {
  PROP_GROUND_ANCHOR_RATIO, cliffSides, drawBackdrop, drawAmbient, drawRevealFx,
  drawDangerAura, drawFireFx, drawStarFx, drawFogOfWar, visionRadius, obstacleVariantCount,
  anchorRatioAt,
};

export type FloorTint = 'blood' | 'gold' | 'frost' | 'sap' | 'neutral';

/** Room themes, in the order the palace lays them out. */
export type RoomTheme =
  | 'Threshold' | 'Memory' | 'Forest' | 'Rupture' | 'Silence' | 'Antechamber' | 'Final';

/** A tile's hidden-node state: indistinguishable → a slab that rings hollow → open alcove. */
export type HiddenState = 'none' | 'hint' | 'revealed';
/** Pre-contact danger tell. `none` is the ambush: deliberately identical to plain floor. */
export type DangerTell = 'none' | 'tracks' | 'glow' | 'blight';
export type HighlightVariant = 'move' | 'attack' | 'cursor' | 'path';
/** Décor vertical. `npc` / `star` / `campfire` sont les événements de case. */
export type PropKind =
  | 'beam' | 'arch' | 'column' | 'trunk' | 'spire' | 'obeliskProp' | 'cairn'
  | 'npc' | 'star' | 'campfire';

export type SpriteKey =
  | {
      kind: 'floor';
      tint: FloorTint;
      elevation: number;
      resolved?: boolean;
      glow?: boolean;
      theme?: RoomTheme;
      /** 0–4: picks one of five brush variations so a floor never tiles visibly. */
      surfaceSeed?: number;
      /** Front-left / front-right neighbour is outside the room → paint a cliff face. */
      cliffLeft?: boolean;
      cliffRight?: boolean;
      hidden?: HiddenState;
      danger?: DangerTell;
    }
  | {
      kind: 'obstacle';
      theme?: RoomTheme;
      /** 0–2 : chaque thème propose trois obstacles distincts (`obstacleVariantCount`). */
      variant?: number;
    }
  | { kind: 'party'; elevation: number }
  | { kind: 'prop'; theme?: RoomTheme; prop: PropKind }
  | { kind: 'highlight'; variant: HighlightVariant; elevation: number };

/** Fallback when a call site still passes only the legacy `tint`. */
const TINT_THEME: Record<FloorTint, RoomTheme> = {
  frost: 'Threshold',
  gold: 'Memory',
  sap: 'Forest',
  blood: 'Rupture',
  neutral: 'Silence',
};

type ForgeKey = Record<string, unknown> & { kind: string };

function toForgeKey(key: SpriteKey): ForgeKey {
  switch (key.kind) {
    case 'floor':
      return {
        kind: 'floor',
        theme: key.theme ?? TINT_THEME[key.tint],
        elevation: Math.max(0, Math.min(TILE.MAX, key.elevation)),
        surfaceSeed: key.surfaceSeed ?? 0,
        cliffLeft: !!key.cliffLeft,
        cliffRight: !!key.cliffRight,
        hidden: key.hidden ?? 'none',
        danger: key.danger ?? 'none',
        resolved: !!key.resolved,
        glow: !!key.glow,
      };
    case 'obstacle':
      return { kind: 'obstacle', theme: key.theme ?? 'Threshold', variant: key.variant ?? 0 };
    case 'prop':
      return { kind: 'prop', theme: key.theme ?? 'Threshold', prop: key.prop };
    case 'highlight':
      return { kind: 'highlight', variant: key.variant, elevation: key.elevation };
    case 'party':
      return { kind: 'party', elevation: key.elevation };
  }
}

export function spriteKeyToString(key: SpriteKey): string {
  return forgeKeyToString(toForgeKey(key));
}

/** True when the sprite is baked on the tall canvas → blit it with the prop aspect + anchor. */
export function usesPropRect(key: SpriteKey): boolean {
  return key.kind === 'prop' || key.kind === 'obstacle';
}

export interface TerrainSpriteOptions {
  /** Grain density multiplier. Lower it on low-end hardware; visual only. */
  grain?: number;
}

export function useTerrainSprites(options: TerrainSpriteOptions = {}) {
  const forge = createTileForge({ grain: options.grain ?? 1 });

  function getSprite(key: SpriteKey): HTMLCanvasElement {
    return forge.getSprite(toForgeKey(key)) as HTMLCanvasElement;
  }

  return {
    getSprite,
    /** Discard baked sprites — call after a theme-token change, not on resize. */
    clear: () => forge.clear(),
    spriteAspectRatio: SPRITE_W / SPRITE_H,
    usesPropRect,
    /** Tall scenery AND obstacles have their own aspect + anchor; blit them with these. */
    propAspectRatio: SPRITE_W / PROP_SPRITE_H,
    propGroundAnchorRatio: PROP_GROUND_ANCHOR_RATIO,
  };
}

/** The theme's accent/glow hexes, for HUD elements that must match the room. */
export function themePalette(theme: RoomTheme) {
  const t = THEMES[theme] ?? THEMES.Threshold;
  return { accent: t.accent, glow: t.glow, tint: t.tint, label: t.label, sky: t.sky };
}

export const TERRAIN_SPRITE_CONSTANTS = {
  BASE_TILE_W: TILE.W,
  BASE_TILE_H: TILE.H,
  BASE_STEP_PX: TILE.STEP,
  MAX_ELEVATION: TILE.MAX,
  SPRITE_H,
  PROP_SPRITE_H,
};
