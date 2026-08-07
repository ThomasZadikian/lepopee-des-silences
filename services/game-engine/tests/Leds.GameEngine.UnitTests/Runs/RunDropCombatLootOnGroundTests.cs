using FluentAssertions;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class RunDropCombatLootOnGroundTests
{
    private static RunItem CreateItem(string key = "item.consumable.minor-heal") =>
        RunItem.Create(
            key,
            "Baume de mémoire",
            "Restaure une partie de la vitalité.",
            RunItemType.Consumable,
            RunItemRarity.Common,
            quantity: 1,
            RunItemEffectType.Heal,
            effectAmount: 25);

    [Fact]
    public void DropCombatLootOnGround_ShouldPlaceSingleItem_OnTheCenterCell()
    {
        var run = TestGameEngineFactory.CreateRun();
        var item = CreateItem();

        run.DropCombatLootOnGround([item], centerX: 2, centerY: 2);

        item.IsOnGround.Should().BeTrue();
        item.GroundRoomId.Should().Be(run.CurrentRoom.Id.Value);
        item.GroundX.Should().Be(2);
        item.GroundY.Should().Be(2);
        run.GroundItems.Should().Contain(item);
    }

    [Fact]
    public void DropCombatLootOnGround_ShouldSpreadMultipleItems_AcrossDistinctNeighborCells()
    {
        var run = TestGameEngineFactory.CreateRun();
        var first = CreateItem("item.consumable.minor-heal");
        var second = CreateItem("item.consumable.guard-shard");

        run.DropCombatLootOnGround([first, second], centerX: 2, centerY: 2);

        var placedCells = new[] { (first.GroundX, first.GroundY), (second.GroundX, second.GroundY) };
        placedCells.Should().OnlyHaveUniqueItems();
        placedCells.Should().OnlyContain(cell => cell.Item1 != null && cell.Item2 != null);
    }

    [Fact]
    public void DropCombatLootOnGround_ShouldStackOnTheFallbackCell_WhenMoreItemsThanCandidateCells()
    {
        // Party starts at the (0,0) corner of the 5x5 test room: only (0,0), (1,0), (0,1) are
        // in-bounds candidates (the other two neighbors fall off the grid) — a 4th item has
        // nowhere new to go and must fall back to reusing the first candidate cell.
        var run = TestGameEngineFactory.CreateRun();
        var items = Enumerable.Range(0, 4).Select(i => CreateItem($"item.test.{i}")).ToArray();

        run.DropCombatLootOnGround(items, centerX: 0, centerY: 0);

        items.Should().OnlyContain(item => item.IsOnGround);
        var distinctCells = items.Select(item => (item.GroundX, item.GroundY)).Distinct().Count();
        distinctCells.Should().BeLessThan(items.Length,
            "there are fewer walkable candidate cells around this corner than items to place");
    }

    [Fact]
    public void DropCombatLootOnGround_ShouldDoNothing_WhenThereAreNoItems()
    {
        var run = TestGameEngineFactory.CreateRun();
        var itemCountBefore = run.RunItems.Count;

        run.DropCombatLootOnGround([], centerX: 2, centerY: 2);

        run.RunItems.Count.Should().Be(itemCountBefore);
    }
}
