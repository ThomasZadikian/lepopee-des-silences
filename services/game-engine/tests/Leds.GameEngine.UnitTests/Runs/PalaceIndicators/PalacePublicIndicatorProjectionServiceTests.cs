using FluentAssertions;
using Leds.GameEngine.Application.Runs.PalaceIndicators;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs.PalaceIndicators;

public sealed class PalacePublicIndicatorProjectionServiceTests
{
    private readonly PalacePublicIndicatorProjectionService _service = new();

    [Fact]
    public void Project_ShouldReturnEmptyCollection_WhenNoPublicSourcesExist()
    {
        var run = TestGameEngineFactory.CreateRun();

        var indicators = _service.Project(run);

        indicators.Should().BeEmpty();
    }

    [Fact]
    public void Project_ShouldIncludePersistedIndicators()
    {
        var run = TestGameEngineFactory.CreateRun();
        var persistedIndicator = PalaceIndicator.Create(
            run.Id.Value,
            "palace.whispers",
            "Murmures du Palais",
            "Le Palais observe la traversee.",
            "high",
            sourceDecisionId: Guid.NewGuid());

        var indicators = _service.Project(run, [persistedIndicator]);

        indicators.Should().ContainSingle(indicator =>
            indicator.Key == "palace.whispers" &&
            indicator.Source == "run" &&
            indicator.Level == "high");
    }

    [Fact]
    public void Project_ShouldProjectActivePalaceLaws()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.ActivatePalaceLaw(PalaceLaw.Create(
            "law-silence-v1",
            "Loi du Silence",
            "v1",
            [PalaceLawDomain.Narrative]));

        var indicators = _service.Project(run);

        indicators.Should().ContainSingle(indicator =>
            indicator.Key == "law:law-silence-v1" &&
            indicator.Label == "Loi du Silence" &&
            indicator.Category == "law" &&
            indicator.Source == "law");
    }

    [Fact]
    public void Project_ShouldIgnoreConsumedPalaceLaws()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.ActivatePalaceLaw(PalaceLaw.Create(
            "law-silence-v1",
            "Loi du Silence",
            "v1",
            [PalaceLawDomain.Narrative]));
        run.ActivePalaceLaws.Single().Consume(DateTime.UtcNow);

        var indicators = _service.Project(run);

        indicators.Should().NotContain(indicator => indicator.Category == "law");
    }

    [Fact]
    public void Project_ShouldProjectActiveCurse()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.ApplyCurse(ActiveCurse.Create(
            "curse-test-v1",
            "Pacte brulant",
            "Le prochain combat portera cette marque.",
            0.10,
            DateTime.UtcNow,
            severity: 4));

        var indicators = _service.Project(run);

        indicators.Should().ContainSingle(indicator =>
            indicator.Key == "curse:curse-test-v1" &&
            indicator.Category == "curse" &&
            indicator.Level == "high" &&
            indicator.Tone == "danger" &&
            indicator.Source == "curse");
    }

    [Fact]
    public void Project_ShouldIgnoreConsumedCurse()
    {
        var run = TestGameEngineFactory.CreateRun();
        var curse = ActiveCurse.Create(
            "curse-test-v1",
            "Pacte brulant",
            "Le prochain combat portera cette marque.",
            0.10,
            DateTime.UtcNow);
        curse.Consume(DateTime.UtcNow);
        run.ApplyCurse(curse);

        var indicators = _service.Project(run);

        indicators.Should().NotContain(indicator => indicator.Category == "curse");
    }

    [Fact]
    public void Project_ShouldProjectActiveRoomClimate()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.ActivatePalaceLaw(PalaceLaw.Create(
            "law-rain-v1",
            "Loi de Pluie",
            "v1",
            [PalaceLawDomain.Events],
            [PalaceLawEffect.Create(RunModifierType.RoomClimate, 2, RunModifierDuration.UntilRoomEnds)]));

        var indicators = _service.Project(run);

        indicators.Should().ContainSingle(indicator =>
            indicator.Key == "climate:rain" &&
            indicator.Label == "Climat : Pluie" &&
            indicator.Category == "climate" &&
            indicator.Source == "room");
    }

    [Fact]
    public void Project_ShouldNotProjectRoomClimate_WhenModifierDoesNotTargetCurrentRoom()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.AddRunModifier(RunModifier.Create(
            RunModifierType.RoomClimate,
            value: 2,
            RunModifierDuration.UntilRoomEnds,
            sourceType: "PalaceLaw",
            sourceKey: "law-rain-v1",
            expiresAtRoomId: Guid.NewGuid()));

        var indicators = _service.Project(run);

        indicators.Should().NotContain(indicator => indicator.Category == "climate");
    }

    [Fact]
    public void Project_ShouldProjectMultipleRuntimeSourcesWithoutDuplicates()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.ActivatePalaceLaw(PalaceLaw.Create(
            "law-rain-v1",
            "Loi de Pluie",
            "v1",
            [PalaceLawDomain.Events],
            [PalaceLawEffect.Create(RunModifierType.RoomClimate, 2, RunModifierDuration.UntilRoomEnds)]));
        run.ApplyCurse(ActiveCurse.Create(
            "curse-test-v1",
            "Pacte brulant",
            "Le prochain combat portera cette marque.",
            0.10,
            DateTime.UtcNow));

        var firstProjection = _service.Project(run);
        var secondProjection = _service.Project(run, firstProjection
            .Where(indicator => indicator.Source == "run")
            .Select(indicator => PalaceIndicator.Create(
                run.Id.Value,
                indicator.Key,
                indicator.Label,
                indicator.Description ?? string.Empty,
                indicator.Level ?? indicator.Tone ?? "low"))
            .ToArray());

        firstProjection.Should().Contain(indicator => indicator.Category == "law");
        firstProjection.Should().Contain(indicator => indicator.Category == "curse");
        firstProjection.Should().Contain(indicator => indicator.Category == "climate");
        firstProjection.Select(indicator => $"{indicator.Key}:{indicator.Source}")
            .Should()
            .OnlyHaveUniqueItems();
        secondProjection.Select(indicator => $"{indicator.Key}:{indicator.Source}")
            .Should()
            .OnlyHaveUniqueItems();
    }

    [Fact]
    public void Project_ShouldNotExposeNonClimateRunModifiersAsIndicators()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.AddRunModifier(RunModifier.Create(
            RunModifierType.CombatDifficultyMultiplier,
            value: 0.20,
            RunModifierDuration.UntilRunEnds,
            sourceType: "PalaceLaw",
            sourceKey: "law-siege-v1"));

        var indicators = _service.Project(run);

        indicators.Should().BeEmpty();
    }
}
