import type {
  CombatantSkillRuntimeDto,
  TacticalAreaShape,
} from '../types/combatContracts';

export type TacticalShape = Lowercase<TacticalAreaShape>;

export type TacticalSkillProfile = {
  range: number;
  shape: TacticalShape;
  requiresLineOfSight: boolean;
};

const SHAPE_BY_CONTRACT: Record<TacticalAreaShape, TacticalShape> = {
  Single: 'single',
  Cross: 'cross',
  Diamond: 'diamond',
  Map: 'map',
};

/**
 * Returns the server-authored tactical profile used by targeting, previews and HUD labels.
 * Keeping this conversion pure prevents the client from inventing a second set of combat rules.
 */
export function tacticalSkillProfile(
  skill: CombatantSkillRuntimeDto,
): TacticalSkillProfile {
  return {
    range: skill.tacticalRange,
    shape: SHAPE_BY_CONTRACT[skill.tacticalAreaShape],
    requiresLineOfSight: skill.requiresLineOfSight,
  };
}
