using FluentAssertions;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class RunForcedWeatherTests
{
    [Fact]
    public void ForceWeather_ShouldReplaceCurrentWeatherAndKeepRemainingRoomPlan()
    {
        var run = CreateRun();

        run.ForceWeather(6, 3, "item.flacon-orage");

        run.GetActiveModifiers(RunModifierType.RoomClimate)
            .Single().Value.Should().Be(6);
        run.GetActiveModifiers(RunModifierType.ForcedWeatherPlan)
            .Single().Value.Should().Be(602);
    }

    [Fact]
    public void RerollCurrentWeather_ShouldCycleCanonicalWeather()
    {
        var run = CreateRun();
        run.ForceWeather(5, 1, "test");

        run.RerollCurrentWeather("item.girouette-os");

        run.GetActiveModifiers(RunModifierType.RoomClimate)
            .Single().Value.Should().Be(6);
    }

    private static Run CreateRun() => Run.StartNew(
        Guid.NewGuid(),
        "weather-seed",
        "test",
        "test",
        TestGameEngineFactory.CreateThresholdRoom(),
        DateTimeOffset.UtcNow,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());
}
