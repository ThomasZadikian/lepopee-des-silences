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
  ROOMS,
  ROOM_KEYS,
  NODE_PROP,
  PROP_KINDS,
  themeLabel,
  themeProps,
  fogColor,
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
  anchorRatioAt, ROOMS, ROOM_KEYS, NODE_PROP, PROP_KINDS, themeLabel, themeProps, fogColor,
};

export type FloorTint = 'blood' | 'gold' | 'frost' | 'sap' | 'neutral';

/** Thèmes de rendu génériques — le repli, quand la salle n'est pas identifiée. */
export type RenderTheme =
  | 'Threshold' | 'Memory' | 'Forest' | 'Rupture' | 'Silence' | 'Antechamber' | 'Final';

/** Clé de salle du catalogue : `catalogRoomKey` de `RoomDto` ("room.halldentree"…). */
export type CatalogRoomKey = string;

/**
 * Identité visuelle d'une tuile. Passer de préférence le `catalogRoomKey` : les 27 salles
 * du Palais ont chacune leur palette. Un `RenderTheme` reste accepté (repli à 7 palettes).
 */
export type RoomTheme = RenderTheme | CatalogRoomKey;

/** true si la clé correspond à une des 27 salles canon. */
export function isCatalogRoom(key: string | undefined): boolean {
  return !!key && Object.prototype.hasOwnProperty.call(ROOMS, key);
}

/**
 * Résout l'identité visuelle à utiliser pour une salle : la salle canon si le catalogue l'a
 * fournie, sinon le thème génerique dérivé du `RoomType`. Point d'entrée unique côté client.
 */
export function resolveRoomVisual(
  catalogRoomKey: string | undefined,
  fallbackTheme: RenderTheme = 'Threshold',
): RoomTheme {
  return isCatalogRoom(catalogRoomKey) ? (catalogRoomKey as CatalogRoomKey) : fallbackTheme;
}

/** A tile's hidden-node state: indistinguishable → a slab that rings hollow → open alcove. */
export type HiddenState = 'none' | 'hint' | 'revealed';
/** Pre-contact danger tell. `none` is the ambush: deliberately identical to plain floor. */
export type DangerTell = 'none' | 'tracks' | 'glow' | 'blight';
export type HighlightVariant = 'move' | 'attack' | 'cursor' | 'path';
/**
 * Décor vertical. Les sept premiers sont les décors de salle ; les suivants sont les décors
 * d'événement, un par type de nœud (voir `NODE_PROP`).
 * `monster` est la menace génerique posée sur un nœud ennemi, `elite` la même bête cornue
 * (élite / rare), `boss` l'objectif de la salle — silhouette deux fois plus haute, sceau au
 * sol, visible de l'autre bout du plateau.
 */
export type PropKind =
  | 'beam' | 'arch' | 'column' | 'trunk' | 'spire' | 'obeliskProp' | 'cairn'
  | 'npc' | 'merchant' | 'star' | 'campfire' | 'curse' | 'monster' | 'elite' | 'boss';

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
      /**
       * DIVERGENCE ASSUMÉE vis-à-vis du handoff (voir bakeObstacle dans tilecraft.js).
       * Le socle de l'obstacle est cuit à CETTE élévation plutôt qu'à MAX_ELEVATION : sans ça
       * un éboulis posé au niveau 0 reçoit un piédestal de trois paliers absent du terrain, et
       * tous les obstacles lisent comme des tours. L'ordre de peinture, lui, garde bien les
       * obstacles à MAX_ELEVATION — c'est là que la préoccupation d'origine est traitée.
       */
      elevation?: number;
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
      return {
        kind: 'obstacle', theme: key.theme ?? 'Threshold',
        variant: key.variant ?? 0, elevation: key.elevation ?? 0,
      };
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

/** Accent / lueur / libellé d'un thème OU d'une salle — pour le HUD, qui doit suivre la salle. */
export function themePalette(theme: RoomTheme) {
  const room = ROOMS[theme];
  const t = room ? { ...(THEMES[room.base] ?? THEMES.Threshold), ...room } : (THEMES[theme] ?? THEMES.Threshold);
  return {
    accent: t.accent, glow: t.glow, tint: t.tint, label: t.label, sky: t.sky,
    fog: fogColor(theme),
  };
}

export const TERRAIN_SPRITE_CONSTANTS = {
  BASE_TILE_W: TILE.W,
  BASE_TILE_H: TILE.H,
  BASE_STEP_PX: TILE.STEP,
  MAX_ELEVATION: TILE.MAX,
  SPRITE_H,
  PROP_SPRITE_H,
};
