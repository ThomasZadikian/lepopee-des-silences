import { describe, expect, it } from 'vitest';

import { SORTS } from '../../palace-map/composables/sorts';
import { fallbackSortId, sortIdForSkillKey } from './useSortEffects';

const MAPPED_SKILLS = [
  'canon.skill.fondations-de-thomas',
  'canon.skill.rempart',
  'canon.skill.dictee',
  'canon.skill.impulsivite',
  'canon.skill.frappe-denclume',
  'canon.skill.larme-elise',
  'canon.skill.berceuse-inversee',
  'canon.skill.silence-partage',
  'canon.skill.se-taire',
  'canon.skill.flamme-froide',
  'canon.skill.regard-infantile',
  'canon.skill.injection-blanche',
  'canon.skill.curee',
  'canon.skill.vol-a-la-tire',
] as const;

describe('tactical spell effects', () => {
  it.each(MAPPED_SKILLS)('maps %s to an existing painted effect', (skillKey) => {
    const sortId = sortIdForSkillKey(skillKey);

    expect(sortId).not.toBeNull();
    expect(SORTS).toHaveProperty(sortId!);
  });

  it('does not invent an effect for an unpainted skill', () => {
    expect(sortIdForSkillKey('canon.skill.inconnue')).toBeNull();
  });
});

describe('fallbackSortId — repli générique pour les sorts non peints', () => {
  const shapes = ['Single', 'Cross', 'Diamond', 'Map'] as const;
  const categories = [
    { category: 'Magic', flavor: 'magique' },
    { category: 'Physical', flavor: 'physique' },
    { category: undefined, flavor: 'physique' },
  ] as const;

  it.each(shapes.flatMap((shape) => categories.map((c) => [shape, c] as const)))(
    'maps shape=%s category=%s to a painted generic entry',
    (shape, { category, flavor }) => {
      const sortId = fallbackSortId(category, shape);

      expect(sortId).toBe(`generique-${flavor}-${shape.toLowerCase()}`);
      expect(SORTS).toHaveProperty(sortId);
    },
  );

  it('defaults to a physical single-target repli when the shape is missing', () => {
    const sortId = fallbackSortId(undefined, undefined);

    expect(sortId).toBe('generique-physique-single');
    expect(SORTS).toHaveProperty(sortId);
  });

  it('is case-insensitive on the tactical area shape', () => {
    expect(fallbackSortId('Magic', 'DIAMOND')).toBe('generique-magique-diamond');
  });
});
