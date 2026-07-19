import type { SkillEffectView } from '../../../party/types/skillTypes';

const categoryLabels: Record<string, string> = {
  Damage: 'Offensif',
  Guard: 'Défensif',
  Heal: 'Soins',
  Buff: 'Soutien',
  Debuff: 'Affaiblissement',
  Weaken: 'Affaiblissement',
  Disrupt: 'Perturbation',
  Status: 'Statut',
  CopySkills: 'Copie de sorts',
  ExtendDotDuration: 'Durée de dot',
};

export function categoryLabel(effectType: string): string {
  return categoryLabels[effectType] ?? effectType;
}

const statLabels: Record<string, string> = {
  AttackPower: 'Attaque',
  Defense: 'Défense',
  MagicAttack: 'Attaque magique',
  MagicDefense: 'Défense magique',
  Speed: 'Vitesse',
  Focus: 'Focus',
  CriticalChanceBonus: 'Chances de critique',
  MaxVitality: 'PV max',
};

const effectKindLabels: Record<string, string> = {
  HealOverTime: 'Soin continu',
  GuardOverTime: 'Garde continue',
  DamageOverTime: 'Dégâts continus',
  Silence: 'Silence',
  GuaranteedCritical: 'Critique garanti',
};

/** Formats one skill effect into a short, player-facing French sentence. */
export function formatEffect(effect: SkillEffectView): string {
  const durationSuffix = effect.isPermanent
    ? ' (permanent)'
    : effect.durationTicks > 0
      ? ` (${effect.durationTicks} ticks)`
      : '';

  if (effect.kind === 'StatModifier' && effect.stat) {
    const statLabel = statLabels[effect.stat] ?? effect.stat;
    const sign = effect.magnitude >= 0 ? '+' : '';
    const unit = effect.magnitudeIsPercentOfMax || effect.magnitudeIsPercentOfBaseStat ? '%' : '';
    const target = effect.appliesToActor ? ' (sur soi)' : '';
    return `${statLabel} ${sign}${effect.magnitude}${unit}${target}${durationSuffix}`;
  }

  const label = effectKindLabels[effect.kind] ?? effect.kind;
  if (effect.magnitude === 0) return `${label}${durationSuffix}`;
  const unit = effect.magnitudeIsPercentOfMax || effect.magnitudeIsPercentOfBaseStat ? '%' : '';
  return `${label} ${effect.magnitude}${unit}${durationSuffix}`;
}
