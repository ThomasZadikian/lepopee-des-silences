using Leds.GameEngine.Domain.Combats.Typing;

namespace Leds.GameEngine.UnitTests.Common;

/// <summary>Test-only fixture. Production affinity data is published exclusively by Catalog.</summary>
public static class TestEmotionalAffinityMatrix
{
    public static EmotionalAffinityMatrixSnapshot Create(string version = "test-affinity-1")
    {
        var registers = Enum.GetValues<EmotionalType>();
        return EmotionalAffinityMatrixSnapshot.Create(
            version,
            from attack in registers
            from defense in registers
            let outcome = Outcome(attack, defense)
            select new EmotionalAffinityRuleSnapshot(
                attack, defense, outcome, Multiplier(outcome)));
    }

    private static DamageEffectiveness Outcome(EmotionalType attack, EmotionalType defense)
    {
        if (attack == EmotionalType.Neutral || defense == EmotionalType.Neutral)
            return DamageEffectiveness.Neutral;

        var (weak, resistant, immune) = defense switch
        {
            EmotionalType.Effroi => (EmotionalType.Memoire, EmotionalType.Rupture, EmotionalType.Silence),
            EmotionalType.Deni => (EmotionalType.Melancolie, EmotionalType.Effroi, EmotionalType.Folie),
            EmotionalType.Melancolie => (EmotionalType.Silence, EmotionalType.Memoire, EmotionalType.Effroi),
            EmotionalType.Rupture => (EmotionalType.Folie, EmotionalType.Melancolie, EmotionalType.Deni),
            EmotionalType.Memoire => (EmotionalType.Deni, EmotionalType.Folie, EmotionalType.Rupture),
            EmotionalType.Silence => (EmotionalType.Rupture, EmotionalType.Deni, EmotionalType.Memoire),
            EmotionalType.Folie => (EmotionalType.Effroi, EmotionalType.Silence, EmotionalType.Melancolie),
            _ => (EmotionalType.Neutral, EmotionalType.Neutral, EmotionalType.Neutral)
        };

        if (attack == weak) return DamageEffectiveness.Weak;
        if (attack == resistant) return DamageEffectiveness.Resistant;
        if (attack == immune) return DamageEffectiveness.Immune;
        return DamageEffectiveness.Neutral;
    }

    private static double Multiplier(DamageEffectiveness outcome) => outcome switch
    {
        DamageEffectiveness.Weak => 1.5,
        DamageEffectiveness.Resistant => 0.75,
        DamageEffectiveness.Immune => 0,
        _ => 1
    };
}
