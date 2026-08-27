using FluentAssertions;
using Leds.Catalog.Domain.Errors;
using Leds.Catalog.Domain.Skills;

namespace Leds.Catalog.UnitTests.Domain.Skills;

public sealed class SkillEffectSpecValidatorTests
{
    [Fact]
    public void Validate_ShouldAcceptSupportedShapes()
    {
        var effects = new[]
        {
            new SkillEffectSpec("DamageOverTime", "burn", 5, 2),
            new SkillEffectSpec("StatModifier", null, 10, 2, Stat: "AttackPower"),
            new SkillEffectSpec("Stun", "stun", 0, 1, Stat: "Speed"),
            new SkillEffectSpec("AffinityModifier", null, 10, 2,
                AffinityRegister: "silence", AffinityOutcome: "Resistant"),
            new SkillEffectSpec("AffinityModifier", null, 15, 2,
                AffinityRegister: "memoire"),
            new SkillEffectSpec("SkillGrant", null, 0, 0, IsPermanent: true)
        };

        var act = () => SkillEffectSpecValidator.Validate("skill.test", effects);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldRejectMissingKind(string kind)
    {
        var act = () => SkillEffectSpecValidator.Validate("skill.test",
            [new SkillEffectSpec(kind, null, 1, 1)]);

        act.Should().Throw<DomainException>().WithMessage("*effect kind is required*");
    }

    [Fact]
    public void Validate_ShouldRejectUnsupportedKind()
    {
        var act = () => SkillEffectSpecValidator.Validate("skill.test",
            [new SkillEffectSpec("Teleport", null, 1, 1)]);

        act.Should().Throw<DomainException>().WithMessage("*is not supported*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ShouldRejectNonPositiveTemporaryDuration(int duration)
    {
        var act = () => SkillEffectSpecValidator.Validate("skill.test",
            [new SkillEffectSpec("Stun", "stun", 0, duration)]);

        act.Should().Throw<DomainException>().WithMessage("*duration must be positive*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Luck")]
    public void Validate_ShouldRejectUnsupportedStatModifierStat(string? stat)
    {
        var act = () => SkillEffectSpecValidator.Validate("skill.test",
            [new SkillEffectSpec("StatModifier", null, 1, 1, Stat: stat)]);

        act.Should().Throw<DomainException>().WithMessage("*requires a supported Stat*");
    }

    [Fact]
    public void Validate_ShouldRejectUnsupportedOptionalStat()
    {
        var act = () => SkillEffectSpecValidator.Validate("skill.test",
            [new SkillEffectSpec("Stun", "stun", 0, 1, Stat: "Luck")]);

        act.Should().Throw<DomainException>().WithMessage("*effect stat 'Luck' is not supported*");
    }

    [Fact]
    public void Validate_ShouldRejectMissingAffinityRegister()
    {
        var act = () => SkillEffectSpecValidator.Validate("skill.test",
            [new SkillEffectSpec("AffinityModifier", null, 1, 1)]);

        act.Should().Throw<DomainException>().WithMessage("*Emotional register is required*");
    }

    [Fact]
    public void Validate_ShouldRejectAffinityModifierWithoutOutcomeOrMultiplier()
    {
        var effect = new SkillEffectSpec("AffinityModifier", null, 0, 1,
            AffinityRegister: "silence");

        var act = () => SkillEffectSpecValidator.Validate("skill.test", [effect]);

        act.Should().Throw<DomainException>().WithMessage("*requires an outcome or multiplier*");
    }

    [Fact]
    public void Validate_ShouldRejectInvalidAffinityOutcome()
    {
        var effect = new SkillEffectSpec("AffinityModifier", null, 0, 1,
            AffinityRegister: "silence", AffinityOutcome: "Impossible");

        var act = () => SkillEffectSpecValidator.Validate("skill.test", [effect]);

        act.Should().Throw<DomainException>().WithMessage("*affinity outcome is invalid*");
    }
}
