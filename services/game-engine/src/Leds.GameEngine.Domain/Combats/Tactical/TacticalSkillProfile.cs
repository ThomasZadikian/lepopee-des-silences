namespace Leds.GameEngine.Domain.Combats.Tactical;

/// <summary>
/// Contrat spatial d'une compétence en T-RPG.
/// </summary>
public sealed record TacticalSkillProfile(
    int Range,
    TacticalAreaShape AreaShape,
    bool RequiresLineOfSight,
    bool OncePerCombat = false)
{
    private static readonly IReadOnlyDictionary<string, TacticalSkillProfile> AuthoredProfiles =
        new Dictionary<string, TacticalSkillProfile>(StringComparer.Ordinal)
        {
            ["canon.skill.fondations-de-thomas"] = Melee(TacticalAreaShape.Single),
            ["canon.skill.rempart"] = Ranged(2, TacticalAreaShape.Cross),
            ["canon.skill.dictee"] = Ranged(4, TacticalAreaShape.Single),
            ["canon.skill.impulsivite"] = Melee(TacticalAreaShape.Single),
            ["canon.skill.frappe-denclume"] = Melee(TacticalAreaShape.Cross),
            ["canon.skill.larme-elise"] = Ranged(4, TacticalAreaShape.Single),
            ["canon.skill.berceuse-inversee"] = Ranged(3, TacticalAreaShape.Diamond),
            ["canon.skill.silence-partage"] = new(
                Range: int.MaxValue,
                AreaShape: TacticalAreaShape.Map,
                RequiresLineOfSight: false,
                OncePerCombat: true),
            ["canon.skill.se-taire"] = Ranged(3, TacticalAreaShape.Diamond),
            ["canon.skill.flamme-froide"] = Ranged(3, TacticalAreaShape.Cross),
            ["canon.skill.regard-infantile"] = Ranged(4, TacticalAreaShape.Single),
            ["canon.skill.injection-blanche"] = Ranged(3, TacticalAreaShape.Single),
            ["canon.skill.curee"] = Melee(TacticalAreaShape.Single),
            ["canon.skill.vol-a-la-tire"] = Melee(TacticalAreaShape.Single),
        };

    /// <summary>
    /// Retourne le profil authoré par le handoff quand il existe, sinon une dérivation stable
    /// depuis le catalogue historique.
    /// </summary>
    public static TacticalSkillProfile For(CombatantSkill skill)
    {
        ArgumentNullException.ThrowIfNull(skill);

        if (AuthoredProfiles.TryGetValue(skill.Key, out var authored))
            return authored;

        var (range, requiresLineOfSight) = TacticalRange.For(skill);

        return new TacticalSkillProfile(
            Range: range,
            AreaShape: TacticalTargeting.ShapeForCatalogTargeting(skill.TargetingType),
            RequiresLineOfSight: requiresLineOfSight);
    }

    private static TacticalSkillProfile Melee(TacticalAreaShape shape) =>
        new(Range: 1, AreaShape: shape, RequiresLineOfSight: false);

    private static TacticalSkillProfile Ranged(int range, TacticalAreaShape shape) =>
        new(Range: range, AreaShape: shape, RequiresLineOfSight: true);
}
