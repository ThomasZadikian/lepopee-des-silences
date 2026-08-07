/**
 * Centralized "coloration selon le type" palette — a pastel accent color assigned
 * per spell "registre émotionnel" or per item category, so the same principle used
 * in the combat spell hotbar reads consistently across the Grimoire, Besace and
 * Équipement menus (see docs/design/direction-visuelle-palais-respire.md: cold
 * dark chrome, pastel accents carrying information, not decoration).
 *
 * Item label/glyph/color are Catalog-owned (see useItemVocabulary) — this module is
 * now only a thin, template-friendly wrapper around that store, with a neutral
 * fallback for a category the vocabulary hasn't loaded yet or doesn't recognize.
 * It never re-derives a classification of its own.
 */
import { useItemVocabulary } from '../../features/item-vocabulary/store';

export type TypeColorMeta = { label: string; glyph: string; color: string };

const FALLBACK_ITEM_TYPE_META: TypeColorMeta = {
  label: 'Objet',
  glyph: '·',
  color: 'oklch(0.62 0.02 272)',
};

export function itemTypeMeta(category: string | undefined | null): TypeColorMeta {
  const definition = useItemVocabulary().itemTypeOf(category);
  if (!definition) return FALLBACK_ITEM_TYPE_META;
  return { label: definition.displayName, glyph: definition.glyph, color: definition.color };
}

export function itemRarityMeta(rarity: string | undefined | null): TypeColorMeta {
  const definition = useItemVocabulary().itemRarityOf(rarity);
  if (!definition) return FALLBACK_ITEM_TYPE_META;
  return { label: definition.displayName, glyph: definition.glyph, color: definition.color };
}

export function itemEffectTypeMeta(effectType: string | undefined | null): TypeColorMeta {
  const definition = useItemVocabulary().itemEffectTypeOf(effectType);
  if (!definition) return FALLBACK_ITEM_TYPE_META;
  return { label: definition.displayName, glyph: definition.glyph, color: definition.color };
}
