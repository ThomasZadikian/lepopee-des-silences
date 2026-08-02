using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
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

    // ── Tasse de thé (Majordome) : soin + mana en % du max ────────────────────

    [Fact]
    public void UseItem_ShouldHealAndRestoreManaByPercent_WhenEffectIsHealAndManaRestorePercent()
    {
        var room = TestGameEngineFactory.CreateThresholdRoom();
        var run = Run.StartNew(
            Guid.NewGuid(), "seed-tea", "gen-test", "markov-test", room, DateTimeOffset.UtcNow,
            maxHp: 40, currentHp: 20, mana: 10, maxMana: 20);
        var tea = RunItem.Create(
            "item.tasse-de-the.v1", "Tasse de thé", "",
            RunItemType.Consumable, RunItemRarity.Rare,
            quantity: 1,
            effectType: RunItemEffectType.HealAndManaRestorePercent,
            effectAmount: 35);
        run.AddRunItem(tea);

        run.UseItem(tea.Id);

        run.PlayerState.CurrentVitality.Should().Be(20 + 14); // 35% of 40 max HP
        run.PlayerState.Mana.Should().Be(10 + 7); // 35% of 20 max mana
    }

    [Fact]
    public void UseItem_ShouldScaleHealPortion_ByHealingBonusPercent()
    {
        var room = TestGameEngineFactory.CreateThresholdRoom();
        var run = Run.StartNew(
            Guid.NewGuid(), "seed-tea-bonus", "gen-test", "markov-test", room, DateTimeOffset.UtcNow,
            maxHp: 40, currentHp: 20, mana: 10, maxMana: 20, healingBonusPercent: 15);
        var tea = RunItem.Create(
            "item.tasse-de-the.v1", "Tasse de thé", "",
            RunItemType.Consumable, RunItemRarity.Rare,
            quantity: 1,
            effectType: RunItemEffectType.HealAndManaRestorePercent,
            effectAmount: 35);
        run.AddRunItem(tea);

        run.UseItem(tea.Id);

        // 35% of 40 = 14, +15% healing bonus = 16 (rounded).
        run.PlayerState.CurrentVitality.Should().Be(20 + 16);
        // Mana restore is unaffected by the healing bonus.
        run.PlayerState.Mana.Should().Be(10 + 7);
    }

    // ── Objets HealPercent hors combat (régression : "Unsupported item effect type") ──
    // ApplyEffect (UseTacticalItemCommandHandler) supporte déjà ces types en combat ;
    // ApplyItemEffectToPlayerState (hors combat) ne les gérait pas, alors que
    // RunItem.IsUsable/isUsable ne distingue jamais "utilisable en combat seulement".

    [Theory]
    [InlineData(RunItemEffectType.HealPercent)]
    [InlineData(RunItemEffectType.ConditionalHealOrPoison)]
    [InlineData(RunItemEffectType.HealPercentAndCleanseDot)]
    [InlineData(RunItemEffectType.HealPercentAndSilence)]
    [InlineData(RunItemEffectType.HealPercentAndEvasion)]
    public void UseItem_ShouldHealByPercentOfMaxVitality_ForEveryHealPercentVariant(
        RunItemEffectType effectType)
    {
        var room = TestGameEngineFactory.CreateThresholdRoom();
        var run = Run.StartNew(
            Guid.NewGuid(), "seed-heal-percent", "gen-test", "markov-test", room, DateTimeOffset.UtcNow,
            maxHp: 40, currentHp: 20);
        var item = RunItem.Create(
            "item.heal-percent-test.v1", "Onguent", "",
            RunItemType.Consumable, RunItemRarity.Common,
            quantity: 1,
            effectType: effectType,
            effectAmount: 25);
        run.AddRunItem(item);

        run.UseItem(item.Id);

        run.PlayerState.CurrentVitality.Should().Be(20 + 10); // 25% of 40 max HP
    }

    [Fact]
    public void UseItem_ShouldThrowAClearMessage_WhenEffectIsRevivePercent()
    {
        var run = TestGameEngineFactory.CreateRun();
        var item = RunItem.Create(
            "item.revive-test.v1", "Réserve de dernier recours", "",
            RunItemType.Consumable, RunItemRarity.Rare,
            quantity: 1,
            effectType: RunItemEffectType.RevivePercent,
            effectAmount: 50);
        run.AddRunItem(item);

        var act = () => run.UseItem(item.Id);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*only be used in combat*defeated ally*");
    }

    // ---------------------------------------------------------------------------
    // "Loi des Poches Cousues" (RunModifierType.ConsumablesRestrictedInCombat):
    // blocks consumables in combat, boosts them +25% out of combat.
    // ---------------------------------------------------------------------------

    private static RunModifier CreatePochesCousuesModifier() => RunModifier.Create(
        RunModifierType.ConsumablesRestrictedInCombat,
        value: 1,
        RunModifierDuration.UntilRoomEnds,
        sourceType: "PalaceLaw",
        sourceKey: "law-poches-cousues");

    private static (Run Run, TacticalCombat Combat) CreateRunWithActiveCombat()
    {
        var run = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Combat).Run;
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
        var combat = TestTacticalCombatHelper.Create(
            run.Id, RoomId.New(), NodeId.New(), [ally], [enemy]);
        run.StartTacticalCombat(combat);

        return (run, combat);
    }

    [Fact]
    public void UseItem_ShouldThrow_WhenConsumablesRestrictedInCombat_AndRunHasActiveCombat()
    {
        var (run, _) = CreateRunWithActiveCombat();
        run.AddRunModifier(CreatePochesCousuesModifier());
        var item = CreateHealPotion();
        run.AddRunItem(item);

        var act = () => run.UseItem(item.Id);

        // Now that tactical combat is wired in, ANY active combat blocks UseItem
        // outright (items route through the tactical targeting action instead) — this
        // check fires before the Poches Cousues modifier is even read, so its specific
        // message no longer surfaces here. The modifier's in-combat block is redundant
        // with this blanket rule; its only remaining effect is the out-of-combat +25%
        // boost, covered by the tests below.
        act.Should()
            .Throw<DomainException>()
            .WithMessage("*tactical targeting action*");
        item.Quantity.Should().Be(2, because: "a rejected use must not consume a charge.");
    }

    [Fact]
    public void UseItem_ShouldBoostEffectByTwentyFivePercent_WhenConsumablesRestrictedActive_AndOutOfCombat()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.PlayerState.TakeDamage(15);
        run.AddRunModifier(CreatePochesCousuesModifier());
        var item = CreateHealPotion();
        run.AddRunItem(item);
        var vitalityBefore = run.PlayerState.CurrentVitality;

        var (_, amount, _) = run.UseItem(item.Id);

        // round(10 * 1.25) = 13.
        amount.Should().Be(13);
        run.PlayerState.CurrentVitality.Should().Be(vitalityBefore + 13);
    }

    [Fact]
    public void UseItem_ShouldNotBoostEffect_WhenConsumablesRestrictedModifierIsConsumed()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.PlayerState.TakeDamage(15);
        var modifier = CreatePochesCousuesModifier();
        run.AddRunModifier(modifier);
        modifier.Consume(DateTime.UtcNow);
        var item = CreateHealPotion();
        run.AddRunItem(item);

        var (_, amount, _) = run.UseItem(item.Id);

        amount.Should().Be(10);
    }

    [Fact]
    public void UseItem_ShouldThrow_WhenRunHasActiveCombat_EvenWithoutTheConsumablesRestrictedModifier()
    {
        var (run, _) = CreateRunWithActiveCombat();
        var item = CreateHealPotion();
        run.AddRunItem(item);

        // Tactical combat blocks UseItem unconditionally — the Poches Cousues modifier
        // was never the actual gate; it happened to always be present in the old
        // scenario this test used to cover.
        var act = () => run.UseItem(item.Id);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*tactical targeting action*");
    }
}