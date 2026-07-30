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
    /// <summary>Returns the mandatory tactical contract authored in the catalog.</summary>
    public static TacticalSkillProfile For(CombatantSkill skill)
    {
        ArgumentNullException.ThrowIfNull(skill);

        return new TacticalSkillProfile(
            Range: skill.TacticalRange,
            AreaShape: skill.TacticalAreaShape,
            RequiresLineOfSight: skill.RequiresLineOfSight);
    }
}
