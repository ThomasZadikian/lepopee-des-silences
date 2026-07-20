using FluentAssertions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs.Dtos;

public sealed class RunDtoTacticalExplorationTests
{
    [Fact]
    public void FromDomain_ShouldExposeClassicExplorationMode_AndNullGrid_ForAClassicRun()
    {
        var run = TestGameEngineFactory.CreateRun();

        var dto = RunDto.FromDomain(run);

        dto.ExplorationMode.Should().Be("Classic");
        dto.CurrentRoom.Grid.Should().BeNull();
    }

    [Fact]
    public void FromDomain_ShouldExposeTacticalExplorationMode_AndGrid_ForATacticalRun()
    {
        var run = TestGameEngineFactory.CreateGridRun();

        var dto = RunDto.FromDomain(run);

        dto.ExplorationMode.Should().Be("Tactical");
        dto.CurrentRoom.Grid.Should().NotBeNull();
        dto.CurrentRoom.Grid!.Width.Should().Be(run.CurrentRoom.Grid!.Width);
        dto.CurrentRoom.Grid.PartyX.Should().Be(run.CurrentRoom.Grid.PartyX);
        dto.CurrentRoom.Grid.PartyY.Should().Be(run.CurrentRoom.Grid.PartyY);
    }

    [Fact]
    public void FromDomain_ShouldExposeAllAvailableNodes_EvenOutsideFogOfWar_ForATacticalRun()
    {
        var run = TestGameEngineFactory.CreateGridRun();

        var dto = RunDto.FromDomain(run);

        // Every node starts Available in grid mode (no DAG-style locked layering), so the
        // full objective list — including the boss at (4,4), far outside the starting
        // vision radius from (0,0) — is exposed as a marker so the player knows where to
        // head, even before physically getting there. Only the *terrain* (RevealedCells)
        // stays fog-gated, not which objectives exist and where (see the test below).
        run.CurrentRoom.TotalNodeCount.Should().Be(6);
        dto.CurrentRoom.Nodes.Should().Contain(n => n.IsBoss);
        dto.CurrentRoom.Nodes.Count.Should().Be(run.CurrentRoom.TotalNodeCount);
    }

    [Fact]
    public void FromDomain_ShouldStillGateRevealedCells_ByFogOfWar_ForATacticalRun()
    {
        var run = TestGameEngineFactory.CreateGridRun();

        var dto = RunDto.FromDomain(run);

        // Node markers are no longer fog-gated (see test above), but the terrain itself
        // still is — the party has only just spawned, so most of the grid is unexplored.
        var totalCells = run.CurrentRoom.Grid!.Width * run.CurrentRoom.Grid.Height;
        dto.CurrentRoom.Grid!.RevealedCells.Count.Should().BeLessThan(totalCells);
    }

    [Fact]
    public void FromDomain_ShouldExposeFullNodeList_ForAClassicRun()
    {
        var run = TestGameEngineFactory.CreateRun();

        var dto = RunDto.FromDomain(run);

        dto.CurrentRoom.Nodes.Count.Should().Be(run.CurrentRoom.TotalNodeCount);
    }
}
