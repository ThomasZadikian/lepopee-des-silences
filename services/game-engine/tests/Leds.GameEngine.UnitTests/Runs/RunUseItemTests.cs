using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class RunUseItemTests
{
    // ── helper ──────────────────────────────────────────────────────────────

    private static RunItem CreateHealPotion(int quantity = 2) =>
        RunItem.Create(
            definitionKey: "item.heal-potion.v1",
            displayName: "Baume de soin",
            description: "",
            type: RunItemType.Consumable,
            rarity: RunItemRarity.Common,
            quantity: quantity,
            effectType: RunItemEffectType.Heal,
            effectAmount: 10);

    private static RunItem CreatePassiveGuardShard() =>
        RunItem.Create(
            definitionKey: "item.guard-shard.v1",
            displayName: "Éclat de garde",
            description: "",
            type: RunItemType.Passive,
            rarity: RunItemRarity.Common,
            quantity: 1,
            effectType: RunItemEffectType.Guard,
            effectAmount: 8);

    // ── hors combat ─────────────────────────────────────────────────────────

    [Fact]
    public void UseItem_ShouldHealPlayerState_WhenItemIsHealConsumable()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.PlayerState.TakeDamage(15); // blesse le joueur
        var item = CreateHealPotion();
        run.AddRunItem(item);

        var vitalityBefore = run.PlayerState.CurrentVitality;

        var (effectType, amount, depleted) = run.UseItem(item.Id);

        effectType.Should().Be(RunItemEffectType.Heal);
        amount.Should().Be(10);
        depleted.Should().BeFalse(); // quantity passe de 2 à 1
        run.PlayerState.CurrentVitality.Should().Be(vitalityBefore + 10);
        item.Quantity.Should().Be(1);
    }

    [Fact]
    public void UseItem_ShouldMarkItemDepleted_WhenLastChargeConsumed()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.PlayerState.TakeDamage(15);
        var item = CreateHealPotion(quantity: 1);
        run.AddRunItem(item);

        var (_, _, depleted) = run.UseItem(item.Id);

        depleted.Should().BeTrue();
        item.Quantity.Should().Be(0);
    }

    [Fact]
    public void UseItem_ShouldThrow_WhenItemIsNotConsumable()
    {
        var run = TestGameEngineFactory.CreateRun();
        var passiveItem = CreatePassiveGuardShard();
        run.AddRunItem(passiveItem);

        var act = () => run.UseItem(passiveItem.Id);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*not consumable*");
    }

    [Fact]
    public void UseItem_ShouldThrow_WhenItemQuantityIsZero()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.PlayerState.TakeDamage(5);
        var item = CreateHealPotion(quantity: 1);
        run.AddRunItem(item);
        run.UseItem(item.Id); // consomme le dernier

        var act = () => run.UseItem(item.Id);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*no remaining uses*");
    }

    [Fact]
    public void UseItem_ShouldThrow_WhenItemIdNotFound()
    {
        var run = TestGameEngineFactory.CreateRun();

        var act = () => run.UseItem(new RunItemId(Guid.NewGuid()));

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*not found in run inventory*");
    }

    [Fact]
    public void UseItem_ShouldThrow_WhenEffectTypeIsNotManuallyActivatable()
    {
        var run = TestGameEngineFactory.CreateRun();
        var fragmentItem = RunItem.Create(
            "item.fragment.v1", "Fragment", "",
            RunItemType.Consumable,
            RunItemRarity.Common,
            quantity: 1,
            effectType: RunItemEffectType.NarrativeFragment,
            effectAmount: 0);
        run.AddRunItem(fragmentItem);

        var act = () => run.UseItem(fragmentItem.Id);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*cannot be triggered manually*");
    }

    [Fact]
    public void UseItem_ShouldRestoreMana_WhenEffectIsManaRestore()
    {
        var run = TestGameEngineFactory.CreateRun();
        var manaPotion = RunItem.Create(
            "item.mana-potion.v1", "Fiole de mana", "",
            RunItemType.Consumable, RunItemRarity.Common,
            quantity: 1,
            effectType: RunItemEffectType.ManaRestore,
            effectAmount: 20);
        run.AddRunItem(manaPotion);

        var manaBefore = run.PlayerState.Mana;
        run.UseItem(manaPotion.Id);

        run.PlayerState.Mana.Should().Be(manaBefore + 20);
    }
}