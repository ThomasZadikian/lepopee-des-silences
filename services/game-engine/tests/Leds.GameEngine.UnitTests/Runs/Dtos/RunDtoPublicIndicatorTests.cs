using FluentAssertions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs.Dtos;

public sealed class RunDtoPublicIndicatorTests
{
    [Fact]
    public void FromDomain_ShouldExposeEmptyPalaceIndicators_WhenNoIndicatorsAreProvided()
    {
        var run = TestGameEngineFactory.CreateRun();

        var dto = RunDto.FromDomain(run);

        dto.PalaceIndicators.Should().NotBeNull();
        dto.PalaceIndicators.Should().BeEmpty();
    }

    [Fact]
    public void FromDomain_ShouldMapPalaceIndicatorToPublicProjection()
    {
        var run = TestGameEngineFactory.CreateRun();
        var indicator = PalaceIndicator.Create(
            run.Id.Value,
            "palace.whispers",
            "Murmures du Palais",
            "Le Palais observe la traversée.",
            "high",
            sourceDecisionId: Guid.NewGuid());

        var dto = RunDto.FromDomain(run, [indicator]);

        dto.PalaceIndicators.Should().ContainSingle();
        var publicIndicator = dto.PalaceIndicators!.Single();
        publicIndicator.Key.Should().Be("palace.whispers");
        publicIndicator.Label.Should().Be("Murmures du Palais");
        publicIndicator.Description.Should().Be("Le Palais observe la traversée.");
        publicIndicator.Level.Should().Be("high");
        publicIndicator.Tone.Should().BeNull();
        publicIndicator.Source.Should().Be("run");
    }

    [Fact]
    public void FromDomain_ShouldNotUseActiveModifiersAsPublicIndicators()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.AddRunModifier(RunModifier.Create(
            RunModifierType.CombatDifficultyMultiplier,
            value: 0.20,
            RunModifierDuration.UntilRunEnds,
            sourceType: "PalaceLaw",
            sourceKey: "law-siege-v1"));

        var dto = RunDto.FromDomain(run);

        dto.ActiveModifiers.Should().NotBeNull();
        dto.PalaceIndicators.Should().BeEmpty();
    }

    [Fact]
    public void FromDomain_ShouldIgnoreExpiredIndicators()
    {
        var run = TestGameEngineFactory.CreateRun();
        var expiredIndicator = PalaceIndicator.Create(
            run.Id.Value,
            "palace.expired",
            "Ancien signe",
            "Ce signe ne devrait plus être public.",
            "low",
            expiresAtUtc: DateTime.UtcNow.AddMinutes(-1));

        var dto = RunDto.FromDomain(run, [expiredIndicator]);

        dto.PalaceIndicators.Should().BeEmpty();
    }

    [Fact]
    public void PalacePublicIndicatorDto_ShouldNotExposeInternalFields()
    {
        var propertyNames = typeof(PalacePublicIndicatorDto)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        propertyNames.Should().NotContain([
            "SourceDecisionId",
            "CreatedAtUtc",
            "ExpiresAtUtc",
            "Weight",
            "Probability",
            "Coefficient",
            "Matrix",
            "Markov",
            "RawScore",
            "AdaptiveScore",
            "Value"
        ]);
    }
}
