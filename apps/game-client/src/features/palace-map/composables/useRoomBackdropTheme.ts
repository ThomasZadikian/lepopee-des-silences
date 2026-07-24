import { computed, type ComputedRef } from 'vue';
import type { RoomDto } from '../../runs/types/runTypes';
import { hashSeed } from './usePalaceTerrain';

// ── Room backdrop: thematic, coherent within a theme, lightly nuanced per room ─────
export const THEME_BACKDROP_CLASS: Record<string, string> = {
  Threshold: 'tgrid__backdrop--threshold',
  Memory: 'tgrid__backdrop--memory',
  Forest: 'tgrid__backdrop--forest',
  Rupture: 'tgrid__backdrop--rupture',
  Silence: 'tgrid__backdrop--silence',
  Antechamber: 'tgrid__backdrop--antechamber',
  Final: 'tgrid__backdrop--final',
};

// Tiles borrow the room's accent color too — an "Antechamber" floor reads gold, a
// "Forest" floor reads green, etc. — instead of every room sharing the same neutral
// grey tile regardless of theme.
export const THEME_ACCENT: Record<string, string> = {
  Threshold: '--frost',
  Memory: '--gold',
  Forest: '--sap',
  Rupture: '--blood',
  Silence: '--frost',
  Antechamber: '--gold',
  Final: '--blood',
};

export type ThemePalette3D = {
  floorColor: number;
  fogColor: number;
  fogDensity: number;
  ambientLightColor: number;
  ambientLightIntensity: number;
  directionalLightColor: number;
  directionalLightIntensity: number;
  accentColor: number;
  /** Only set for the Final theme — drives the slow pulsing glow (see FogCloud/marker loops). */
  pulseSpeed?: number;
};

// Approximate hex equivalents of tokens.css's oklch tone variables — see the same
// note in useNodeGeometry.ts. Not meant to be a pixel-exact conversion.
const GOLD = 0xcbb26a;
const FROST = 0xa7a8e8;
const BLOOD = 0xe08a8a;
const SAP = 0x8fd9b0;
const VOID = 0x1c1c2e;

/**
 * One 3D-scene entry per backdrop theme — the TresJS translation of each CSS
 * .tgrid__backdrop--* variant's own intent (see the CSS comments on those classes):
 * a base floor tone, a fog color/density for the unrevealed distance, and an
 * ambient/directional light pairing that reads as "this room's own light", not a
 * single neutral studio light shared by every theme.
 */
// BALANCE KNOB — light intensities. Three.js's physically-correct lighting model
// needs noticeably higher intensity values than the old 0-1 scale suggests — a
// tinted, non-white ambient at 0.5 alone rendered as near-black on MeshStandardMaterial.
// Ambient sits well above directional here specifically because these are stylized flat
// tiles (no baked shadow/AO to preserve), so a strong ambient + a modest directional for
// just a hint of directionality reads far better than trying to fake realism.
export const THEME_PALETTE_3D: Record<string, ThemePalette3D> = {
  Threshold: {
    // Liminal doorway — cool, pale, a misty vertical light column.
    floorColor: 0x24243a, fogColor: FROST, fogDensity: 0.014,
    ambientLightColor: FROST, ambientLightIntensity: 1.7,
    directionalLightColor: 0xffffff, directionalLightIntensity: 1.4,
    accentColor: FROST,
  },
  Memory: {
    // Warm sepia, a page-turned quality.
    floorColor: 0x332c1e, fogColor: GOLD, fogDensity: 0.008,
    ambientLightColor: GOLD, ambientLightIntensity: 1.8,
    directionalLightColor: 0xffe9c2, directionalLightIntensity: 1.3,
    accentColor: GOLD,
  },
  Forest: {
    // Deep green canopy, dappled light.
    floorColor: 0x1c2e22, fogColor: SAP, fogDensity: 0.015,
    ambientLightColor: SAP, ambientLightIntensity: 1.5,
    directionalLightColor: 0xbfe8c9, directionalLightIntensity: 1.3,
    accentColor: SAP,
  },
  Rupture: {
    // Angular fractures, blood-tinted cracks.
    floorColor: 0x2e1c1e, fogColor: BLOOD, fogDensity: 0.017,
    ambientLightColor: BLOOD, ambientLightIntensity: 1.4,
    directionalLightColor: 0xffb0b0, directionalLightIntensity: 1.3,
    accentColor: BLOOD,
  },
  Silence: {
    // Pale, still, concentric-ripple stillness.
    floorColor: 0x26263a, fogColor: FROST, fogDensity: 0.02,
    ambientLightColor: FROST, ambientLightIntensity: 1.9,
    directionalLightColor: 0xe8e8ff, directionalLightIntensity: 1.0,
    accentColor: FROST,
  },
  Antechamber: {
    // Formal golden colonnade.
    floorColor: 0x362c1c, fogColor: GOLD, fogDensity: 0.006,
    ambientLightColor: GOLD, ambientLightIntensity: 1.7,
    directionalLightColor: 0xffe9c2, directionalLightIntensity: 1.6,
    accentColor: GOLD,
  },
  Final: {
    // Dark, sanguine, slowly pulsing — the confrontation theme. Kept dimmer than the
    // rest on purpose (this is the one room meant to feel oppressive), but still well
    // above the old near-black baseline.
    floorColor: 0x200e10, fogColor: BLOOD, fogDensity: 0.022,
    ambientLightColor: BLOOD, ambientLightIntensity: 0.9,
    directionalLightColor: BLOOD, directionalLightIntensity: 0.75,
    accentColor: BLOOD, pulseSpeed: 1 / 6, // ~6s period, matches tgrid-backdrop-pulse
  },
};

const DEFAULT_PALETTE_3D: ThemePalette3D = {
  floorColor: VOID, fogColor: GOLD, fogDensity: 0.012,
  ambientLightColor: GOLD, ambientLightIntensity: 1.6,
  directionalLightColor: 0xffffff, directionalLightIntensity: 1.3,
  accentColor: GOLD,
};

export function useRoomBackdropTheme(room: ComputedRef<RoomDto>) {
  const backdropClass = computed(() =>
    THEME_BACKDROP_CLASS[room.value.theme] ?? 'tgrid__backdrop--default');

  // Same room + same theme always renders the same backdrop family; this is the only
  // thing that varies it slightly between two rooms sharing a theme (a small hue drift).
  const roomNuance = computed(() => hashSeed(room.value.id ?? '') % 100);

  const themeAccent = computed(() => THEME_ACCENT[room.value.theme] ?? '--gold');

  const palette3D = computed(() => THEME_PALETTE_3D[room.value.theme] ?? DEFAULT_PALETTE_3D);

  return { backdropClass, roomNuance, themeAccent, palette3D };
}
