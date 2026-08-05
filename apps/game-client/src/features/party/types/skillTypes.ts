export type SkillEffectView = {
  kind: string;
  statusKey: string | null;
  magnitude: number;
  durationTicks: number;
  tickInterval: number;
  stat: string | null;
  magnitudeIsPercentOfMax: boolean;
  magnitudeIsPercentOfBaseStat: boolean;
  appliesToActor: boolean;
  isPermanent: boolean;
};

export type SkillDefinitionView = {
  key: string;
  displayName: string;
  description: string;
  skillType: string;
  targetingType: string;
  effectType: string;
  manaCost: number;
  chargeCost: number;
  basePower: number;
  category: string;
  basePowerIsPercentOfMaxVitality: boolean;
  effects: SkillEffectView[];
  acquisitionHints: string[];
  tacticalRange?: number;
  tacticalAreaShape?: 'Single' | 'Cross' | 'Diamond' | 'Map';
  requiresLineOfSight?: boolean;
  cooldown?: number;
  isUltimate?: boolean;
  emotionalRegister?: string;
  /** The spell's own intrinsic "élément" — see EmotionalTypeProfileProvider. Null for
   * basic attacks and untyped spells; distinct from emotionalRegister above. */
  emotionalType?: string | null;
  /** Player archetypes allowed to equip this skill — empty/absent means unrestricted. */
  allowedArchetypes?: string[] | null;
  /** "Player"/"Enemy"/"Any" — the Grimoire filters out "Enemy" defensively even though the
   * server is also supposed to exclude them. */
  audience?: string;
};
