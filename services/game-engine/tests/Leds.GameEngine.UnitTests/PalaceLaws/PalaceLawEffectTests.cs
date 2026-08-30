using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.PalaceLaws;

public sealed class PalaceLawEffectTests
{
    [Fact]
    public void Create_ShouldCreateEffect_WhenInputIsValid()
    {
        var effect = PalaceLawEffect.Create(
            RunModifierType.PermanentCombatDifficultyBonus,
            value: 0.10,
            RunModifierDuration.UntilRunEnds);

        effect.ModifierType.Should().Be(RunModifierType.PermanentCombatDifficultyBonus);
        effect.Value.Should().Be(0.10);
        effect.Duration.Should().Be(RunModifierDuration.UntilRunEnds);
    }

    [Fact]
    public void Create_ShouldThrow_WhenValueIsZero()
    {
        var act = () => PalaceLawEffect.Create(
            RunModifierType.PermanentCombatDifficultyBonus,
            value: 0,
            RunModifierDuration.UntilRunEnds);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Palace law effect value must be non-zero.");
    }
}