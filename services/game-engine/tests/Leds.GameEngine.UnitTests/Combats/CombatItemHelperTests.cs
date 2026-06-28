using FluentAssertions;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Combats;

public sealed class CombatItemHelperTests
{
    [Fact]
    public void GetUsableBattleItems_ShouldReturnBattleItems_WhenRunHasUsableBattleItems()
    {
        var run = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Combat).Run;
        var battleItem = RunItem.Create(
            "item.consumable.minor-heal",
            "Baume de mémoire",
            "Restores vitality",
            RunItemType.Consumable,
            RunItemRarity.Common,
            quantity: 2,
            RunItemEffectType.Heal,
            effectAmount: 15);
        run.AddRunItem(battleItem);

        var result = CombatItemHelper.GetUsableBattleItems(run);

        result.Should().HaveCount(1);
        result.Single().DefinitionKey.Should().Be("item.consumable.minor-heal");
        result.Single().EffectType.Should().Be("Heal");
        result.Single().EffectAmount.Should().Be(15);
        result.Single().Quantity.Should().Be(2);
        result.Single().TargetingType.Should().Be("SingleAlly");
    }

    [Fact]
    public void GetUsableBattleItems_ShouldNotReturnNonBattleItems()
    {
        var run = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Item).Run;
        var nonBattleItem = RunItem.Create(
            "item.consumable.guard-shard",
            "Éclat de garde",
            "Grants guard",
            RunItemType.Consumable,
            RunItemRarity.Common,
            quantity: 1,
            RunItemEffectType.Guard,
            effectAmount: 8);
        run.AddRunItem(nonBattleItem);

        var result = CombatItemHelper.GetUsableBattleItems(run);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetUsableBattleItems_ShouldNotReturnConsumedItems()
    {
        var run = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Combat).Run;
        var item = RunItem.Create(
            "item.consumable.minor-heal",
            "Baume de mémoire",
            "Restores vitality",
            RunItemType.Consumable,
            RunItemRarity.Common,
            quantity: 1,
            RunItemEffectType.Heal,
            effectAmount: 15);
        run.AddRunItem(item);
        item.ConsumeOne();

        var result = CombatItemHelper.GetUsableBattleItems(run);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetUsableBattleItems_ShouldReturnEmpty_WhenRunHasNoItems()
    {
        var run = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Combat).Run;

        var result = CombatItemHelper.GetUsableBattleItems(run);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetUsableBattleItems_ShouldReturnMultipleItems_WhenRunHasMultipleBattleItems()
    {
        var run = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Combat).Run;
        var item1 = RunItem.Create(
            "item.consumable.minor-heal",
            "Baume de mémoire",
            "Restores vitality",
            RunItemType.Consumable,
            RunItemRarity.Common,
            quantity: 2,
            RunItemEffectType.Heal,
            effectAmount: 15);
        var item2 = RunItem.Create(
            "item.consumable.mana-potion",
            "Potion de mana",
            "Restores mana",
            RunItemType.Consumable,
            RunItemRarity.Uncommon,
            quantity: 1,
            RunItemEffectType.ManaRestore,
            effectAmount: 5);
        run.AddRunItem(item1);
        run.AddRunItem(item2);

        var result = CombatItemHelper.GetUsableBattleItems(run);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void GetUsableBattleItems_ShouldFilterMixedItemsCorrectly()
    {
        var run = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Combat).Run;
        var battleItem = RunItem.Create(
            "item.battle",
            "Battle Item",
            "Heals in battle",
            RunItemType.Consumable,
            RunItemRarity.Common,
            quantity: 1,
            RunItemEffectType.Heal,
            effectAmount: 10);
        var nonBattleItem = RunItem.Create(
            "item.non-battle",
            "Non-Battle Item",
            "Grants guard",
            RunItemType.Consumable,
            RunItemRarity.Common,
            quantity: 1,
            RunItemEffectType.Guard,
            effectAmount: 10);
        run.AddRunItem(battleItem);
        run.AddRunItem(nonBattleItem);

        var result = CombatItemHelper.GetUsableBattleItems(run);

        result.Should().HaveCount(1);
        result.Single().DefinitionKey.Should().Be("item.battle");
    }
}
