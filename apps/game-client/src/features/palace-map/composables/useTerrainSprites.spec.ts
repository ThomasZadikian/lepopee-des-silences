// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import {
  useTerrainSprites,
  spriteKeyToString,
  usesPropRect,
  type RoomTheme,
  type SpriteKey,
} from './useTerrainSprites';

// jsdom has no canvas backend, so `getContext('2d')` returns null and the tile engine's own
// `if (!ctx) return canvas` guards short-circuit every paint. That's exactly what makes these
// tests meaningful anyway: they assert the KEY/CACHE contract (which variants exist, that they
// bake without throwing, that they memoize distinctly) — not pixel output, which no headless
// environment can judge.

const THEMES: RoomTheme[] = [
  'Threshold', 'Memory', 'Forest', 'Rupture', 'Silence', 'Antechamber', 'Final',
];

const ALL_KEYS: SpriteKey[] = [
  // Floors: every theme × every elevation, plus the state/edge flags that change the painting.
  ...THEMES.flatMap((theme) =>
    ([0, 1, 2, 3] as const).map((elevation): SpriteKey =>
      ({ kind: 'floor', tint: 'frost', theme, elevation, surfaceSeed: elevation }))),
  { kind: 'floor', tint: 'gold', theme: 'Memory', elevation: 1, resolved: true },
  { kind: 'floor', tint: 'blood', theme: 'Rupture', elevation: 0, glow: true },
  { kind: 'floor', tint: 'frost', theme: 'Silence', elevation: 2, cliffLeft: true },
  { kind: 'floor', tint: 'frost', theme: 'Silence', elevation: 2, cliffRight: true },
  { kind: 'floor', tint: 'sap', theme: 'Forest', elevation: 0, hidden: 'hint' },
  { kind: 'floor', tint: 'sap', theme: 'Forest', elevation: 0, hidden: 'revealed' },
  { kind: 'floor', tint: 'sap', theme: 'Forest', elevation: 0, danger: 'tracks' },
  { kind: 'floor', tint: 'sap', theme: 'Forest', elevation: 0, danger: 'glow' },
  { kind: 'floor', tint: 'sap', theme: 'Forest', elevation: 0, danger: 'blight' },
  // Walls: three distinct silhouettes per theme, on the tall canvas.
  ...THEMES.flatMap((theme) =>
    ([0, 1, 2] as const).map((variant): SpriteKey => ({ kind: 'obstacle', theme, variant }))),
  // Scenery and gameplay layers.
  { kind: 'prop', theme: 'Antechamber', prop: 'column' },
  { kind: 'prop', theme: 'Threshold', prop: 'arch' },
  ...(['move', 'attack', 'cursor', 'path'] as const).map((variant): SpriteKey =>
    ({ kind: 'highlight', variant, elevation: 0 })),
  ...([0, 1, 2, 3] as const).map((elevation): SpriteKey => ({ kind: 'party', elevation })),
];

describe('useTerrainSprites', () => {
  it('bakes every sprite variant without throwing', () => {
    const { getSprite } = useTerrainSprites();

    for (const key of ALL_KEYS) {
      expect(() => getSprite(key)).not.toThrow();
    }
  });

  it('returns a canvas-like object with the expected dimensions', () => {
    const { getSprite } = useTerrainSprites();
    const sprite = getSprite({ kind: 'floor', tint: 'gold', theme: 'Memory', elevation: 2 });

    expect(sprite.width).toBeGreaterThan(0);
    expect(sprite.height).toBeGreaterThan(0);
  });

  it('memoizes by key: the same key returns the exact same cached instance', () => {
    const { getSprite } = useTerrainSprites();
    const key: SpriteKey = { kind: 'floor', tint: 'blood', theme: 'Rupture', elevation: 1, glow: true };

    expect(getSprite({ ...key })).toBe(getSprite(key));
  });

  it('does not confuse two floor keys that differ only in one field', () => {
    const base = { kind: 'floor', tint: 'gold', theme: 'Memory', elevation: 1 } as const;
    const stringified = spriteKeyToString(base);

    for (const variation of [
      { ...base, elevation: 2 },
      { ...base, theme: 'Forest' as const },
      { ...base, surfaceSeed: 3 },
      { ...base, resolved: true },
      { ...base, glow: true },
      { ...base, cliffLeft: true },
      { ...base, cliffRight: true },
      { ...base, hidden: 'hint' as const },
      { ...base, danger: 'tracks' as const },
    ]) {
      expect(spriteKeyToString(variation)).not.toBe(stringified);
    }
  });

  it('keeps the three wall variants of a theme distinct', () => {
    const keys = ([0, 1, 2] as const).map((variant): SpriteKey =>
      ({ kind: 'obstacle', theme: 'Antechamber', variant }));

    expect(new Set(keys.map(spriteKeyToString)).size).toBe(3);
  });

  it('routes only walls and scenery to the tall canvas', () => {
    expect(usesPropRect({ kind: 'obstacle', theme: 'Forest', variant: 0 })).toBe(true);
    expect(usesPropRect({ kind: 'prop', theme: 'Forest', prop: 'trunk' })).toBe(true);

    expect(usesPropRect({ kind: 'floor', tint: 'sap', theme: 'Forest', elevation: 3 })).toBe(false);
    expect(usesPropRect({ kind: 'party', elevation: 0 })).toBe(false);
    expect(usesPropRect({ kind: 'highlight', variant: 'cursor', elevation: 0 })).toBe(false);
  });

  it('keys an obstacle by its elevation, so two heights never share one baked sprite', () => {
    // The sprite is baked with a plinth AND a silhouette at the cell's own height. If the cache
    // key ignored elevation, the first height baked would be handed back for every other one and
    // the whole per-cell height would silently do nothing.
    const flat = spriteKeyToString({ kind: 'obstacle', theme: 'Forest', variant: 0, elevation: 0 });
    const raised = spriteKeyToString({ kind: 'obstacle', theme: 'Forest', variant: 0, elevation: 3 });

    expect(flat).not.toBe(raised);
  });

  it('exposes stable, positive aspect ratios for both canvases', () => {
    const { spriteAspectRatio, propAspectRatio } = useTerrainSprites();

    expect(spriteAspectRatio).toBeGreaterThan(0);
    expect(propAspectRatio).toBeGreaterThan(0);
    // The tall canvas is taller for the same width, so its width/height ratio is smaller.
    expect(propAspectRatio).toBeLessThan(spriteAspectRatio);
  });
});
