namespace Leds.GameEngine.Domain.Runs;

/// <summary>
/// Canonical display metadata for every <see cref="RunItemEffectType"/> — same pattern
/// as Catalog's EmotionalRegisterCatalog/ItemTypeCatalog, but owned directly by
/// game-engine since RunItemEffectType is a game-engine domain enum with no separate
/// Catalog-side type: Catalog only ever carries the matching string in its
/// EffectRunType field. One definition per member, always in sync with the enum by
/// construction — a new member left out of `Definitions` fails
/// <see cref="RunItemEffectTypeCatalogTests"/> immediately instead of silently
/// rendering with no label.
/// </summary>
public static class RunItemEffectTypeCatalog
{
    public const string Version = "item-effect-types-1.0.0";

    private static readonly IReadOnlyList<RunItemEffectTypeDefinition> Definitions =
    [
        new("none", "Aucun effet", "·", "oklch(0.65 0.02 270)", RunItemEffectType.None),
        new("heal", "Soin", "✚", "oklch(0.84 0.145 150)", RunItemEffectType.Heal),
        new("guard", "Garde", "◇", "oklch(0.83 0.131 230)", RunItemEffectType.Guard),
        new("manarestore", "Restauration de mana", "✦", "oklch(0.83 0.145 275)", RunItemEffectType.ManaRestore),
        new("chargerestore", "Restauration de charge", "⚡", "oklch(0.86 0.16 70)", RunItemEffectType.ChargeRestore),
        new("nextcombatguard", "Garde (prochain combat)", "◈", "oklch(0.81 0.131 210)", RunItemEffectType.NextCombatGuard),
        new("narrativefragment", "Fragment narratif", "❍", "oklch(0.87 0.131 90)", RunItemEffectType.NarrativeFragment),
        new("attacktypeoverride", "Changement de type d'attaque", "⚔", "oklch(0.81 0.174 30)", RunItemEffectType.AttackTypeOverride),
        new("teamspeedbonus", "Bonus de vitesse d'équipe", "➤", "oklch(0.85 0.131 190)", RunItemEffectType.TeamSpeedBonus),
        new("healandmanarestorepercent", "Soin et mana (%)", "✚", "oklch(0.83 0.131 190)", RunItemEffectType.HealAndManaRestorePercent),
        new("healpercent", "Soin (%)", "✚", "oklch(0.84 0.145 150)", RunItemEffectType.HealPercent),
        new("conditionalhealorpoison", "Soin ou poison (conditionnel)", "☠", "oklch(0.81 0.174 340)", RunItemEffectType.ConditionalHealOrPoison),
        new("healpercentandcleansedot", "Soin (%) et purge des effets périodiques", "✧", "oklch(0.84 0.145 160)", RunItemEffectType.HealPercentAndCleanseDot),
        new("healpercentandsilence", "Soin (%) et silence", "✧", "oklch(0.82 0.131 200)", RunItemEffectType.HealPercentAndSilence),
        new("revivepercent", "Réanimation (%)", "✶", "oklch(0.86 0.174 85)", RunItemEffectType.RevivePercent),
        new("healpercentandevasion", "Soin (%) et esquive", "✧", "oklch(0.84 0.145 150)", RunItemEffectType.HealPercentAndEvasion),
        new("forceweatherorage", "Invoque l'Orage", "⚡", "oklch(0.78 0.174 260)", RunItemEffectType.ForceWeatherOrage),
        new("forceweatheraccalmie", "Invoque l'Accalmie", "❋", "oklch(0.87 0.087 200)", RunItemEffectType.ForceWeatherAccalmie),
        new("rerollweather", "Relance la météo", "↻", "oklch(0.85 0.131 190)", RunItemEffectType.RerollWeather),
        new("grantteamskillpoints", "Points de compétence d'équipe", "✶", "oklch(0.86 0.16 85)", RunItemEffectType.GrantTeamSkillPoints),
        new("granttemporaryskill", "Sort temporaire", "◈", "oklch(0.83 0.145 305)", RunItemEffectType.GrantTemporarySkill)
    ];

    public static IReadOnlyList<RunItemEffectTypeDefinition> All => Definitions;
}

public sealed record RunItemEffectTypeDefinition(
    string Code,
    string DisplayName,
    string Glyph,
    string Color,
    RunItemEffectType Value);
