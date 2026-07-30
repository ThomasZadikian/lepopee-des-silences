import { describe, expect, it } from 'vitest';

import { SORTS } from '../../palace-map/composables/sorts';
import { sortIdForSkillKey } from './useSortEffects';

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
