import type { ItemEffectTypeCatalog, ItemRarityCatalog, ItemTypeCatalog } from './types';

/**
 * Mirrors the real backend catalogs (ItemTypeCatalog/ItemRarityCatalog/
 * RunItemEffectTypeCatalog, services/catalog + services/game-engine) closely enough
 * for component tests to assert real labels — not a second, drifting vocabulary.
 * Seed with `useItemVocabulary().install(TEST_ITEM_TYPES, TEST_ITEM_RARITIES,
 * TEST_ITEM_EFFECT_TYPES)` in a test's setup.
 */
export const TEST_ITEM_TYPES: ItemTypeCatalog = {
  version: 'item-types-test-1.0.0',
  definitions: [
    { code: 'consumable', displayName: 'Consommable', glyph: '✳', color: 'oklch(0.83 0.10 150)' },
    { code: 'equipment', displayName: 'Équipement', glyph: '◆', color: 'oklch(0.82 0.09 255)' },
    { code: 'relic', displayName: 'Relique', glyph: '◈', color: 'oklch(0.82 0.10 305)' },
    { code: 'key', displayName: 'Clé', glyph: '⚷', color: 'oklch(0.83 0.08 220)' },
    { code: 'currency', displayName: 'Monnaie', glyph: '○', color: 'oklch(0.85 0.11 60)' },
    { code: 'material', displayName: 'Matériau', glyph: '▲', color: 'oklch(0.75 0.06 130)' },
    { code: 'weapon', displayName: 'Arme', glyph: '⚔', color: 'oklch(0.80 0.12 30)' },
    { code: 'grimoire', displayName: 'Grimoire', glyph: '❍', color: 'oklch(0.86 0.09 90)' },
    { code: 'weatherinstrument', displayName: 'Instrument météorologique', glyph: '❋', color: 'oklch(0.84 0.09 190)' },
    { code: 'skillessence', displayName: 'Essence de sort', glyph: '✶', color: 'oklch(0.82 0.12 335)' },
  ],
};

export const TEST_ITEM_RARITIES: ItemRarityCatalog = {
  version: 'item-rarities-test-1.0.0',
  definitions: [
    { code: 'common', displayName: 'Commune', glyph: '○', color: 'oklch(0.65 0.02 270)', palaceShardCost: 150, himLitShardCost: 0 },
    { code: 'uncommon', displayName: 'Peu commune', glyph: '◇', color: 'oklch(0.80 0.10 150)', palaceShardCost: 250, himLitShardCost: 0 },
    { code: 'rare', displayName: 'Rare', glyph: '◈', color: 'oklch(0.80 0.10 230)', palaceShardCost: 350, himLitShardCost: 0 },
    { code: 'epic', displayName: 'Épique', glyph: '❖', color: 'oklch(0.80 0.12 300)', palaceShardCost: 500, himLitShardCost: 25 },
    { code: 'legendary', displayName: 'Légendaire', glyph: '✶', color: 'oklch(0.85 0.12 85)', palaceShardCost: 750, himLitShardCost: 50 },
    { code: 'unique', displayName: 'Unique', glyph: '✺', color: 'oklch(0.78 0.15 20)', palaceShardCost: 1000, himLitShardCost: 75 },
  ],
};

export const TEST_ITEM_EFFECT_TYPES: ItemEffectTypeCatalog = {
  version: 'item-effect-types-test-1.0.0',
  definitions: [
    { code: 'none', displayName: 'Aucun effet', glyph: '·', color: 'oklch(0.65 0.02 270)' },
    { code: 'heal', displayName: 'Soin', glyph: '✚', color: 'oklch(0.83 0.10 150)' },
    { code: 'guard', displayName: 'Garde', glyph: '◇', color: 'oklch(0.82 0.09 230)' },
    { code: 'manarestore', displayName: 'Restauration de mana', glyph: '✦', color: 'oklch(0.82 0.10 275)' },
    { code: 'chargerestore', displayName: 'Restauration de charge', glyph: '⚡', color: 'oklch(0.85 0.11 70)' },
    { code: 'nextcombatguard', displayName: 'Garde (prochain combat)', glyph: '◈', color: 'oklch(0.80 0.09 210)' },
    { code: 'narrativefragment', displayName: 'Fragment narratif', glyph: '❍', color: 'oklch(0.86 0.09 90)' },
    { code: 'attacktypeoverride', displayName: "Changement de type d'attaque", glyph: '⚔', color: 'oklch(0.80 0.12 30)' },
    { code: 'teamspeedbonus', displayName: "Bonus de vitesse d'équipe", glyph: '➤', color: 'oklch(0.84 0.09 190)' },
    { code: 'healandmanarestorepercent', displayName: 'Soin et mana (%)', glyph: '✚', color: 'oklch(0.82 0.09 190)' },
    { code: 'healpercent', displayName: 'Soin (%)', glyph: '✚', color: 'oklch(0.83 0.10 150)' },
    { code: 'conditionalhealorpoison', displayName: 'Soin ou poison (conditionnel)', glyph: '☠', color: 'oklch(0.78 0.12 340)' },
    { code: 'healpercentandcleansedot', displayName: 'Soin (%) et purge des effets périodiques', glyph: '✧', color: 'oklch(0.83 0.10 160)' },
    { code: 'healpercentandsilence', displayName: 'Soin (%) et silence', glyph: '✧', color: 'oklch(0.81 0.09 200)' },
    { code: 'revivepercent', displayName: 'Réanimation (%)', glyph: '✶', color: 'oklch(0.85 0.12 85)' },
    { code: 'healpercentandevasion', displayName: 'Soin (%) et esquive', glyph: '✧', color: 'oklch(0.83 0.10 150)' },
    { code: 'forceweatherorage', displayName: "Invoque l'Orage", glyph: '⚡', color: 'oklch(0.75 0.12 260)' },
    { code: 'forceweatheraccalmie', displayName: "Invoque l'Accalmie", glyph: '❋', color: 'oklch(0.86 0.06 200)' },
    { code: 'rerollweather', displayName: 'Relance la météo', glyph: '↻', color: 'oklch(0.84 0.09 190)' },
    { code: 'grantteamskillpoints', displayName: "Points de compétence d'équipe", glyph: '✶', color: 'oklch(0.85 0.11 85)' },
    { code: 'granttemporaryskill', displayName: 'Sort temporaire', glyph: '◈', color: 'oklch(0.82 0.10 305)' },
  ],
};
