import { describe, expect, it } from 'vitest';

import { ROSTER } from '../../palace-map/composables/bestiaire';
import { fallbackPropFor, figureIdFor } from './useCombatantSprites';

describe('combatant sprite bridge', () => {
  it.each(
    Object.entries(ROSTER)
      .filter(([, entry]) => entry.catalogKey)
      .map(([figureId, entry]) => [entry.catalogKey!, entry.name, figureId] as const),
  )('maps catalog key %s to painted figure %s', (catalogKey, displayName, expected) => {
    expect(figureIdFor(catalogKey, displayName)).toBe(expected);
  });

  it.each([
    ['character.elise', 'Elise', 'elise'],
    ['character.thomas', 'Thomas', 'thomas'],
    ['character.mane', 'Mané', 'mane'],
    ['character.mina', 'Mina', 'mina'],
    ['character.john', 'John', 'john'],
  ])('maps companion %s through its canonical character key', (key, name, expected) => {
    expect(figureIdFor(key, name)).toBe(expected);
  });

  it('keeps a deterministic fallback for an unpainted combatant', () => {
    expect(figureIdFor('canon.enemy.inconnu', 'Créature inconnue')).toBeNull();
    expect(fallbackPropFor('Elite', false)).toBe('elite');
    expect(fallbackPropFor('Bruiser', false)).toBe('monster');
    expect(fallbackPropFor('Bruiser', true)).toBe('boss');
  });
});
