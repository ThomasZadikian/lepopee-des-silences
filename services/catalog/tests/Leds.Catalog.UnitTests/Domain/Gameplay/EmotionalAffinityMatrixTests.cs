using FluentAssertions;
using Leds.Catalog.Domain.Errors;
using Leds.Catalog.Domain.Gameplay;
using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.UnitTests.Domain.Gameplay;

public sealed class EmotionalAffinityMatrixTests
{
    [Fact]
    public void Canonical_matrix_should_match_the_validated_design()
    {
        var matrix = EmotionalAffinityMatrix.Canonical;

        matrix.Version.Should().Be(EmotionalAffinityMatrix.CanonicalVersion);
        matrix.Rules.Should().HaveCount(64);
        matrix.Resolve(EmotionalRegister.Memoire, EmotionalRegister.Effroi).Should().Be(AffinityOutcome.Weak);
        matrix.Resolve(EmotionalRegister.Rupture, EmotionalRegister.Effroi).Should().Be(AffinityOutcome.Resistant);
        matrix.Resolve(EmotionalRegister.Silence, EmotionalRegister.Effroi).Should().Be(AffinityOutcome.Immune);
        matrix.Resolve(EmotionalRegister.Neutral, EmotionalRegister.Effroi).Should().Be(AffinityOutcome.Neutral);
        matrix.Resolve(EmotionalRegister.Memoire, EmotionalRegister.Neutral).Should().Be(AffinityOutcome.Neutral);
    }

    [Fact]
    public void Create_ShouldAcceptCompleteUniqueMatrix()
    {
        var rules = CompleteNeutralRules();

        var matrix = EmotionalAffinityMatrix.Create("affinity-1.0.0", rules);

        matrix.Rules.Should().HaveCount(
            EmotionalRegisterCatalog.Active.Count * EmotionalRegisterCatalog.Active.Count);
    }

    [Fact]
    public void Create_ShouldRejectMissingPair()
    {
        var rules = CompleteNeutralRules().Skip(1);

        var act = () => EmotionalAffinityMatrix.Create("affinity-1.0.0", rules);

        act.Should().Throw<DomainException>()
            .WithMessage("*must contain exactly*");
    }

    [Fact]
    public void Create_ShouldRejectDuplicatePair()
    {
        var rules = CompleteNeutralRules().ToList();
        rules.Add(rules[0]);

        var act = () => EmotionalAffinityMatrix.Create("affinity-1.0.0", rules);

        act.Should().Throw<DomainException>()
            .WithMessage("*duplicate pairs*");
    }

    private static IReadOnlyCollection<EmotionalAffinityRule> CompleteNeutralRules() =>
        EmotionalRegisterCatalog.Active
            .SelectMany(attack => EmotionalRegisterCatalog.Active.Select(defense =>
                new EmotionalAffinityRule(attack.Value, defense.Value, AffinityOutcome.Neutral)))
            .ToArray();
}
