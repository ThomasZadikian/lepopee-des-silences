/**
 * Centralized "coloration selon le type" palette — a pastel accent color assigned
 * per spell "registre émotionnel" or per item type, so the same principle used in
 * the combat spell hotbar reads consistently across the Grimoire, Besace and
 * Équipement menus (see docs/design/direction-visuelle-palais-respire.md: cold
 * dark chrome, pastel accents carrying information, not decoration).
 */
export type TypeColorMeta = { label: string; glyph: string; color: string };

export const EMOTIONAL_TYPE_META: Record<string, TypeColorMeta> = {
  Effroi: { label: 'Effroi', glyph: '✶', color: 'oklch(0.80 0.11 18)' },
  Deni: { label: 'Déni', glyph: '◇', color: 'oklch(0.85 0.09 78)' },
  Melancolie: { label: 'Mélancolie', glyph: '❍', color: 'oklch(0.82 0.08 248)' },
  Rupture: { label: 'Rupture', glyph: '⟡', color: 'oklch(0.80 0.11 38)' },
  Memoire: { label: 'Mémoire', glyph: '◈', color: 'oklch(0.88 0.08 86)' },
  Silence: { label: 'Silence', glyph: '○', color: 'oklch(0.83 0.02 272)' },
  Folie: { label: 'Folie', glyph: '✳', color: 'oklch(0.80 0.13 340)' },
  Neutral: { label: 'Neutre', glyph: '·', color: 'oklch(0.62 0.02 272)' },
};

export function emotionalTypeMeta(type?: string | null): TypeColorMeta | null {
  if (!type || type === 'Neutral') return null;
  return EMOTIONAL_TYPE_META[type] ?? null;
}

export type ItemTypeKey = 'Weapon' | 'Accessory' | 'Relic' | 'Grimoire' | 'Consumable' | 'Other';

const ITEM_TYPE_META: Record<ItemTypeKey, TypeColorMeta> = {
  Weapon: { label: 'Arme', glyph: '⚔', color: 'oklch(0.80 0.12 30)' },
  Accessory: { label: 'Accessoire', glyph: '◆', color: 'oklch(0.82 0.09 255)' },
  Relic: { label: 'Relique', glyph: '◈', color: 'oklch(0.82 0.10 305)' },
  Grimoire: { label: 'Grimoire', glyph: '❍', color: 'oklch(0.86 0.09 90)' },
  Consumable: { label: 'Consommable', glyph: '✳', color: 'oklch(0.83 0.10 150)' },
  Other: { label: 'Objet', glyph: '·', color: 'oklch(0.62 0.02 272)' },
};

/** Normalizes the several catalog/run-item type vocabularies into one axis. */
export function normalizeItemType(rawType: string | undefined | null): ItemTypeKey {
  switch (rawType) {
    case 'Weapon':
      return 'Weapon';
    case 'Accessory':
    case 'Equipment':
      return 'Accessory';
    case 'Relic':
    case 'Heritage':
      return 'Relic';
    case 'Grimoire':
      return 'Grimoire';
    case 'Consumable':
      return 'Consumable';
    default:
      return 'Other';
  }
}

export function itemTypeMeta(rawType: string | undefined | null): TypeColorMeta {
  return ITEM_TYPE_META[normalizeItemType(rawType)];
}
