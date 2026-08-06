using Leds.GameEngine.Domain.Combats.Typing;

namespace Leds.GameEngine.IntegrationTests.Common;

public static class TestEmotionalAffinityMatrix
{
    public static EmotionalAffinityMatrixSnapshot Create() =>
        EmotionalAffinityMatrixSnapshot.Create(
            "integration-test-affinity-1",
            from attack in Enum.GetValues<EmotionalType>()
            from defense in Enum.GetValues<EmotionalType>()
            select new EmotionalAffinityRuleSnapshot(
                attack, defense, DamageEffectiveness.Neutral, 1.0));
}
