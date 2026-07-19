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
    public void FromDomain_ShouldOnlyExposeFogOfWarRevealedNodes_ForATacticalRun()
    {
        var run = TestGameEngineFactory.CreateGridRun();

        var dto = RunDto.FromDomain(run);

        // The room has 6 nodes total (1 target + 4 filler + 1 boss), but the boss at
        // (4,4) is far outside the starting vision radius from (0,0) — fog of war must
        // hide it from the client until the party gets close enough.
        run.CurrentRoom.TotalNodeCount.Should().Be(6);
        dto.CurrentRoom.Nodes.Should().NotContain(n => n.IsBoss);
        dto.CurrentRoom.Nodes.Count.Should().BeLessThan(run.CurrentRoom.TotalNodeCount);
    }

    [Fact]
    public void FromDomain_ShouldExposeFullNodeList_ForAClassicRun()
    {
        var run = TestGameEngineFactory.CreateRun();

        var dto = RunDto.FromDomain(run);

        dto.CurrentRoom.Nodes.Count.Should().Be(run.CurrentRoom.TotalNodeCount);
    }
}
