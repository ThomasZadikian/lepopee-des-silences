import { beforeEach, describe, expect, it } from 'vitest';
import { itemRarityMeta } from './typeColors';
import { useItemVocabulary } from '../../features/item-vocabulary/store';
import { TEST_ITEM_EFFECT_TYPES, TEST_ITEM_RARITIES, TEST_ITEM_TYPES } from '../../features/item-vocabulary/testFixtures';

beforeEach(() => {
  useItemVocabulary().install(TEST_ITEM_TYPES, TEST_ITEM_RARITIES, TEST_ITEM_EFFECT_TYPES);
});

// paintGroundItems() (TacticalGridMap.vue) delegates its ground-loot glow color straight to
// this function — canvas painting itself isn't unit-testable through jsdom (see
// TacticalGridMap.test.ts's header comment), so this is what actually guards that every
// RunItemRarity tier the backend can drop on the ground gets its own distinct color instead
// of collapsing back to the old 2-tier Legendary/everything-else check.
describe('itemRarityMeta', () => {
  it.each(['Common', 'Uncommon', 'Rare', 'Epic', 'Legendary', 'Unique'])(
    'resolves a real color for rarity "%s"',
    (rarity) => {
      const meta = itemRarityMeta(rarity);
      expect(meta.color).toBeTruthy();
      expect(meta.color).not.toBe('oklch(0.62 0.02 272)'); // FALLBACK_ITEM_TYPE_META.color
    },
  );

  it('gives every rarity tier a distinct color', () => {
    const colors = ['Common', 'Uncommon', 'Rare', 'Epic', 'Legendary', 'Unique']
      .map((rarity) => itemRarityMeta(rarity).color);

    expect(new Set(colors).size).toBe(colors.length);
  });

  it('matches case-insensitively, same as the catalog codes', () => {
    expect(itemRarityMeta('legendary').color).toBe(itemRarityMeta('Legendary').color);
  });

  it('falls back to the neutral default for an unknown rarity', () => {
    expect(itemRarityMeta('NotARarity').color).toBe('oklch(0.62 0.02 272)');
  });
});
