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
};
