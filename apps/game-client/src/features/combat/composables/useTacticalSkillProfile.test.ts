import { describe, expect, it } from 'vitest';

import type { CombatantSkillRuntimeDto } from '../types/combatContracts';
import { tacticalSkillProfile } from './useTacticalSkillProfile';

function skill(
  overrides: Partial<CombatantSkillRuntimeDto> = {},
): CombatantSkillRuntimeDto {
  return {
    key: 'skill.test',
    displayName: 'Geste test',
    skillType: 'Damage',
    targetingType: 'SingleEnemy',
    effectType: 'Damage',
    manaCost: 0,
    chargeCost: 0,
    basePower: 10,
    tags: [],
    category: 'Physical',
    tacticalRange: 1,
    tacticalAreaShape: 'Single',
    requiresLineOfSight: false,
    ...overrides,
  };
}

describe('tacticalSkillProfile', () => {
  it('uses the tactical contract without deriving it from legacy targeting', () => {
    expect(tacticalSkillProfile(skill({
      targetingType: 'AllEnemies',
      tacticalRange: 4,
      tacticalAreaShape: 'Cross',
      requiresLineOfSight: true,
    }))).toEqual({
      range: 4,
      shape: 'cross',
      requiresLineOfSight: true,
    });
  });

  it.each([
    ['Single', 'single'],
    ['Cross', 'cross'],
    ['Diamond', 'diamond'],
    ['Map', 'map'],
  ] as const)('normalizes the %s area shape', (contractShape, expected) => {
    expect(tacticalSkillProfile(skill({ tacticalAreaShape: contractShape })).shape)
      .toBe(expected);
  });
});
