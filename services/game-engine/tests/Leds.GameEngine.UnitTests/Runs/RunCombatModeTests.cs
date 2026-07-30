using FluentAssertions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Persistence.Mappers;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

/// <summary>
/// Le système tactique est le seul mode créé pour les nouvelles runs. L'ATB reste relisible
/// pendant la période de transition des sauvegardes historiques.
/// </summary>
public sealed class RunCombatModeTests
{
    private static Run StartRun(RunCombatMode? mode = null)
    {
        var room = TestGameEngineFactory.CreateThresholdRoom(Domain.Nodes.NodeEventType.Combat);

        return mode is null
            ? Run.StartNew(
                playerId: Guid.NewGuid(),
                seed: "seed-combat-mode",
                generatorVersion: "gen-test",
                markovMatrixVersion: "markov-test",
                initialRoom: room,
                startedAt: DateTimeOffset.UtcNow)
            : Run.StartNew(
                playerId: Guid.NewGuid(),
                seed: "seed-combat-mode",
                generatorVersion: "gen-test",
                markovMatrixVersion: "markov-test",
                initialRoom: room,
                startedAt: DateTimeOffset.UtcNow,
                combatMode: mode.Value);
    }

    [Fact]
    public void StartNew_ShouldDefaultToTactical_WhenModeIsNotSpecified()
    {
        StartRun().CombatMode.Should().Be(RunCombatMode.Tactical);
    }

    [Theory]
    [InlineData(RunCombatMode.Atb)]
    [InlineData(RunCombatMode.Tactical)]
    public void StartNew_ShouldKeepTheChosenMode(RunCombatMode chosen)
    {
        StartRun(chosen).CombatMode.Should().Be(chosen);
    }

    [Theory]
    [InlineData(RunCombatMode.Atb)]
    [InlineData(RunCombatMode.Tactical)]
    public void CombatMode_ShouldSurvivePersistenceRoundTrip(RunCombatMode chosen)
    {
        // Le mode est fixe pour toute la durée de la run : reprendre une partie sauvegardée
        // ne doit pas la basculer d'un système à l'autre.
        var reloaded = RunPersistenceMapper.ToDomain(
            RunPersistenceMapper.ToEntity(StartRun(chosen)));

        reloaded.CombatMode.Should().Be(chosen);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Inconnu")]
    public void CombatMode_ShouldFallBackToAtb_ForRunsPredatingTheChoice(string stored)
    {
        // Les runs créées avant l'introduction du choix portent une colonne vide. L'ATB était
        // alors le seul système : les relire ne doit pas les faire basculer en tactique.
        var entity = RunPersistenceMapper.ToEntity(StartRun(RunCombatMode.Tactical));
        entity.CombatMode = stored;

        RunPersistenceMapper.ToDomain(entity).CombatMode.Should().Be(RunCombatMode.Atb);
    }

    [Fact]
    public void RunDto_ShouldExposeTheModeAsText()
    {
        // Le client a besoin du mode pour choisir quel écran de combat monter.
        var dto = RunDto.FromDomain(StartRun(RunCombatMode.Tactical));

        dto.CombatMode.Should().Be("Tactical");
    }

    [Fact]
    public void RunDto_ShouldReportAtb_ForAnAtbRun()
    {
        RunDto.FromDomain(StartRun(RunCombatMode.Atb)).CombatMode.Should().Be("Atb");
    }
}
