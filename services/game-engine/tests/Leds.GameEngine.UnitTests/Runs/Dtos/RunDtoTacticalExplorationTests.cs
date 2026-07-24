using FluentAssertions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs.Dtos;

public sealed class RunDtoTacticalExplorationTests
{
    [Fact]
    public void FromDomain_ShouldExposeGrid_ForARun()
    {
        var run = TestGameEngineFactory.CreateRun();

        var dto = RunDto.FromDomain(run);

        dto.CurrentRoom.Grid.Should().NotBeNull();
        dto.CurrentRoom.Grid!.Width.Should().Be(run.CurrentRoom.Grid.Width);
        dto.CurrentRoom.Grid.PartyX.Should().Be(run.CurrentRoom.Grid.PartyX);
        dto.CurrentRoom.Grid.PartyY.Should().Be(run.CurrentRoom.Grid.PartyY);
    }

    [Fact]
    public void FromDomain_ShouldExposeAllAvailableNodes_EvenOutsideFogOfWar()
    {
        var run = TestGameEngineFactory.CreateRun();

        var dto = RunDto.FromDomain(run);

        // Every node starts Available in grid mode (no DAG-style locked layering), so the
        // full objective list — including the boss far outside the starting vision radius —
        // is exposed as a marker so the player knows where to head, even before physically
        // getting there. Only the *terrain* (RevealedCells) stays fog-gated, not which
        // objectives exist and where (see the test below).
        run.CurrentRoom.TotalNodeCount.Should().Be(6);
        dto.CurrentRoom.Nodes.Should().Contain(n => n.IsBoss);
        dto.CurrentRoom.Nodes.Count.Should().Be(run.CurrentRoom.TotalNodeCount);
    }

    [Fact]
    public void FromDomain_ShouldStillGateRevealedCells_ByFogOfWar()
    {
        var run = TestGameEngineFactory.CreateRun();

        var dto = RunDto.FromDomain(run);

        // Node markers are no longer fog-gated (see test above), but the terrain itself
        // still is — the party has only just spawned, so most of the grid is unexplored.
        var totalCells = run.CurrentRoom.Grid.Width * run.CurrentRoom.Grid.Height;
        dto.CurrentRoom.Grid!.RevealedCells.Count.Should().BeLessThan(totalCells);
    }
}
