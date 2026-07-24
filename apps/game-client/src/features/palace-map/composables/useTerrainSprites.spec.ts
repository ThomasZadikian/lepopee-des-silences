// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { useTerrainSprites, spriteKeyToString, type SpriteKey } from './useTerrainSprites';

const ALL_KEYS: SpriteKey[] = [
  ...(['blood', 'gold', 'frost', 'sap', 'neutral'] as const).flatMap((tint) =>
    ([0, 1, 2, 3] as const).flatMap((elevation) =>
      ([false, true] as const).flatMap((resolved) =>
        ([false, true] as const).flatMap((glow) =>
          (['lit', 'ghost'] as const).map((light) =>
            ({ kind: 'floor', tint, elevation, resolved, glow, light } as const)))))),
  { kind: 'obstacle', light: 'lit' },
  { kind: 'obstacle', light: 'ghost' },
  { kind: 'fog', variant: 0, marker: false },
  { kind: 'fog', variant: 1, marker: false },
  { kind: 'fog', variant: 2, marker: true },
  { kind: 'fog', variant: 3, marker: true },
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
    const sprite = getSprite({ kind: 'floor', tint: 'gold', elevation: 2, resolved: false, glow: false, light: 'lit' });

    expect(sprite.width).toBeGreaterThan(0);
    expect(sprite.height).toBeGreaterThan(0);
  });

  it('memoizes by key: the same key returns the exact same cached instance', () => {
    const { getSprite } = useTerrainSprites();
    const key: SpriteKey = { kind: 'floor', tint: 'blood', elevation: 1, resolved: false, glow: true, light: 'lit' };

    const first = getSprite(key);
    const second = getSprite({ ...key });

    expect(second).toBe(first);
  });

  it('does not confuse two floor keys that differ only in one field', () => {
    const { getSprite } = useTerrainSprites();
    const base = { kind: 'floor', tint: 'gold', elevation: 1, resolved: false, glow: false, light: 'lit' } as const;

    expect(getSprite(base)).toBe(getSprite({ ...base }));
    expect(spriteKeyToString(base)).not.toBe(spriteKeyToString({ ...base, elevation: 2 }));
    expect(spriteKeyToString(base)).not.toBe(spriteKeyToString({ ...base, resolved: true }));
    expect(spriteKeyToString(base)).not.toBe(spriteKeyToString({ ...base, light: 'ghost' }));
  });

  it('exposes a stable, positive sprite aspect ratio', () => {
    const { spriteAspectRatio } = useTerrainSprites();
    expect(spriteAspectRatio).toBeGreaterThan(0);
  });
});
