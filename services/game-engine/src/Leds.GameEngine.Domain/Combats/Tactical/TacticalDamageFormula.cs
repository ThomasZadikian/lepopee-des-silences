namespace Leds.GameEngine.Domain.Combats.Tactical;

/// <summary>Canonical T-RPG base damage before affinity, critical and situational modifiers.</summary>
public static class TacticalDamageFormula
{
    public const double MinimumVariation = 0.85;
    public const double MaximumVariation = 1.15;

    public static int CalculateBaseDamage(
        int skillPower,
        int attack,
        int defense,
        double deterministicRoll)
    {
        if (skillPower <= 0 || attack < 0)
            return 0;

        // Zero defense is the authored maximum: ignore the attack/defense ratio and
        // force the upper 115% variation.
        if (defense <= 0)
        {
            return Round(skillPower * MaximumVariation);
        }

        var roll = Math.Clamp(deterministicRoll, 0.0, 1.0);
        var variation = MinimumVariation
            + ((MaximumVariation - MinimumVariation) * roll);
        var scaled = skillPower * (attack / (double)defense) * variation;
        return Math.Max(0, Round(scaled));
    }

    private static int Round(double value) =>
        (int)Math.Round(value, MidpointRounding.AwayFromZero);
}
