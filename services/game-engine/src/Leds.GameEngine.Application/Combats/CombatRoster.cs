using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Combats;

/// <summary>
/// Le produit réel de <see cref="CombatFactory"/> : deux camps de combattants entièrement
/// constitués — stats mises à l'échelle, compétences résolues, Lois du Palais appliquées — plus
/// les drapeaux de Loi qui ne se posent pas sur un combattant mais sur le combat lui-même.
/// </summary>
/// <remarks>
/// <para>
/// C'est la couture entre les deux systèmes de combat (SFD v2, §2). L'ATB et le tactique sont
/// indépendants dans leur <b>déroulé</b> — ordonnancement, modèle spatial, économie d'action —
/// mais ils affrontent le même bestiaire, avec les mêmes stats et sous les mêmes Lois. Un
/// roster est ce fonds commun ; chaque système l'emballe ensuite à sa manière.
/// </para>
/// <para>
/// Sans cette séparation, brancher le combat tactique aurait exigé de dupliquer les ~400 lignes
/// de constitution de camp de la fabrique. Toute divergence ultérieure entre les deux copies
/// aurait produit un ennemi qui frappe différemment selon le mode de combat choisi — exactement
/// ce que l'indépendance des systèmes ne doit <b>pas</b> vouloir dire.
/// </para>
/// </remarks>
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
