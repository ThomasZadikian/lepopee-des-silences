using FluentAssertions;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.UnitTests.Combats.Tactical;

public sealed class TacticalTargetingCoverageTests
{
    [Theory]
    [InlineData(TacticalAreaShape.Single, 1)]
    [InlineData(TacticalAreaShape.Cross, 5)]
    [InlineData(TacticalAreaShape.Diamond, 13)]
    public void CellsInArea_ShouldReturnExpectedCellsAtCenter(TacticalAreaShape shape, int expectedCount)
    {
        var battlefield = FlatBattlefield(5, 5);
        var cells = TacticalTargeting.CellsInArea(battlefield, new GridPosition(2, 2), shape);
        cells.Should().HaveCount(expectedCount);
        cells.Should().OnlyContain(cell => battlefield.Contains(cell));
    }

    [Fact]
    public void CellsInArea_ShouldClipAtBattlefieldEdges()
    {
        var battlefield = FlatBattlefield(3, 3);
        TacticalTargeting.CellsInArea(battlefield, new GridPosition(0, 0), TacticalAreaShape.Diamond)
            .Should().HaveCount(6);
    }

    [Fact]
    public void CellsInArea_ShouldRejectMapAndUnknownShapes()
    {
        var battlefield = FlatBattlefield(3, 3);
        FluentActions.Invoking(() => TacticalTargeting.CellsInArea(
                battlefield, new GridPosition(1, 1), TacticalAreaShape.Map))
            .Should().Throw<DomainException>();
        FluentActions.Invoking(() => TacticalTargeting.CellsInArea(
                battlefield, new GridPosition(1, 1), (TacticalAreaShape)999))
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void CellsInArea_ShouldRejectNullBattlefield()
    {
        FluentActions.Invoking(() => TacticalTargeting.CellsInArea(
                null!, new GridPosition(0, 0), TacticalAreaShape.Single))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsInRange_ShouldRejectTargetBeyondRange()
    {
        var battlefield = FlatBattlefield(4, 4);
        TacticalTargeting.IsInRange(
            battlefield, new GridPosition(0, 0), new GridPosition(3, 0), 2, false)
            .Should().BeFalse();
    }

    [Fact]
    public void IsInRange_ShouldChargeOnlyElevationBeyondFirstStep()
    {
        var battlefield = TacticalBattlefield.Rehydrate(
            3, 1, elevation: [0, 1, 3], walkable: [true, true, true]);
        TacticalTargeting.IsInRange(
            battlefield, new GridPosition(0, 0), new GridPosition(1, 0), 1, false).Should().BeTrue();
        TacticalTargeting.IsInRange(
            battlefield, new GridPosition(0, 0), new GridPosition(2, 0), 3, false).Should().BeTrue();
        TacticalTargeting.IsInRange(
            battlefield, new GridPosition(0, 0), new GridPosition(2, 0), 2, false).Should().BeFalse();
    }

    [Fact]
    public void IsInRange_ShouldGiveHigherAttackerPlungingSight()
    {
        var battlefield = TacticalBattlefield.Rehydrate(
            2, 1, elevation: [2, 0], walkable: [true, true]);
        TacticalTargeting.IsInRange(
            battlefield, new GridPosition(0, 0), new GridPosition(1, 0), 2, true).Should().BeTrue();
    }

    [Fact]
    public void IsInRange_ShouldSkipLineOfSightWhenNotRequired()
    {
        var battlefield = TacticalBattlefield.Rehydrate(
            3, 1,
            elevation: [0, 0, 0],
            walkable: [true, false, true],
            isFloor: [true, true, true]);
        TacticalTargeting.IsInRange(
            battlefield, new GridPosition(0, 0), new GridPosition(2, 0), 2, false).Should().BeTrue();
        TacticalTargeting.IsInRange(
            battlefield, new GridPosition(0, 0), new GridPosition(2, 0), 2, true).Should().BeFalse();
    }

    [Theory]
    [InlineData("Self", TacticalAreaShape.Single)]
    [InlineData("SingleAlly", TacticalAreaShape.Single)]
    [InlineData("SingleEnemy", TacticalAreaShape.Single)]
    [InlineData("AllEnemies", TacticalAreaShape.Diamond)]
    [InlineData("AllAllies", TacticalAreaShape.Diamond)]
    public void ShapeForCatalogTargeting_ShouldMapSupportedModes(string targetingType, TacticalAreaShape expected)
    {
        TacticalTargeting.ShapeForCatalogTargeting(targetingType).Should().Be(expected);
    }

    [Fact]
    public void ShapeForCatalogTargeting_ShouldRejectUnknownMode()
    {
        FluentActions.Invoking(() => TacticalTargeting.ShapeForCatalogTargeting("Unknown"))
            .Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("SingleEnemy", true)]
    [InlineData("AllEnemies", true)]
    [InlineData("Self", false)]
    [InlineData("SingleAlly", false)]
    [InlineData("AllAllies", false)]
    public void IsHostile_ShouldMapSupportedModes(string targetingType, bool expected)
    {
        TacticalTargeting.IsHostile(targetingType).Should().Be(expected);
    }

    [Fact]
    public void IsHostile_ShouldRejectUnknownMode()
    {
        FluentActions.Invoking(() => TacticalTargeting.IsHostile("Unknown"))
            .Should().Throw<DomainException>();
    }

    private static TacticalBattlefield FlatBattlefield(int width, int height)
    {
        var size = width * height;
        return TacticalBattlefield.Rehydrate(
            width,
            height,
            Enumerable.Repeat(0, size).ToArray(),
            Enumerable.Repeat(true, size).ToArray());
    }
}
