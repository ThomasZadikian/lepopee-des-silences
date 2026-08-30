using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Combats;

/// <summary>
/// Le produit réel de <see cref="CombatFactory"/> : deux camps de combattants entièrement
/// constitués — stats mises à l'échelle, compétences résolues, Lois du Palais appliquées — plus
/// les drapeaux de Loi qui ne se posent pas sur un combattant mais sur le combat lui-même.
/// </summary>
/// <remarks>Source unique de constitution des camps avant leur déploiement sur la grille.</remarks>
public sealed record CombatRoster(
    IReadOnlyCollection<Combatant> Allies,
    IReadOnlyCollection<Combatant> Enemies,
    bool HitCounterDoubleDamageEnabled,
    bool FirstHitCriticalEnabled,
    bool LowHpDamageAmplificationEnabled,
    int DotDurationExtensionTicks,
    bool DuelDamageAsymmetryEnabled,
    int DotMagnitudeBonus,
    bool HealingBlocked,
    bool FalaiseWindEnabled,
    bool PostDeathBasicAttackOnlyEnabled,
    bool TapisPropreEnabled,
    bool ThirdCupHealCorruptionEnabled,
    bool PresentationsEnabled,
    bool MiroirEnabled,
    string? ForgottenSkillKey);
