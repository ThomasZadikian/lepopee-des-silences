using FluentAssertions;
using Leds.GameEngine.Application.RoomMaps;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Typing;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Coverage;

public sealed class FinalPureDomainBranchMarginTests
{
    [Fact]
    public void CombatantSkill_Create_ShouldCoverAllValidationAndOptionalFallbackBranches()
    {
        Assert.Throws<DomainException>(() => Skill(key: " "));
        Assert.Throws<DomainException>(() => Skill(displayName: " "));
        Assert.Throws<DomainException>(() => Skill(basePower: -1));
        Assert.Throws<DomainException>(() => Skill(manaCost: -1));
        Assert.Throws<DomainException>(() => Skill(chargeCost: -1));
        Assert.Throws<DomainException>(() => Skill(tacticalRange: -1));
        Assert.Throws<DomainException>(() => Skill(cooldown: -1));
        Assert.Throws<DomainException>(() => Skill(emotionalRegister: " "));

        var fallback = Skill(category: " ");
        fallback.Category.Should().Be("Physical");
        fallback.Tags.Should().BeEmpty();
        fallback.StatusEffects.Should().BeEmpty();

        var explicitCollections = CombatantSkill.Create(
            "skill.full", "Full", "Damage", "SingleEnemy", "Damage",
            1, 2, 3,
            tags: ["tag"],
            statusEffects: [],
            category: "Magic",
            emotionalRegister: "Memoire");
        explicitCollections.Tags.Should().ContainSingle("tag");
        explicitCollections.Category.Should().Be("Magic");

        Assert.Throws<DomainException>(() => fallback.WithPowerMultiplier(0));
        fallback.WithPowerMultiplier(1.5).BasePower.Should().BeGreaterThan(fallback.BasePower);
        fallback.WithoutResourceCosts().ManaCost.Should().Be(0);
    }

    [Fact]
    public void RunItem_ShouldCoverValidationStackabilityBattleEffectsGroundAndContainerBranches()
    {
        Assert.Throws<DomainException>(() => Item(key: " "));
        Assert.Throws<DomainException>(() => Item(name: " "));
        Assert.Throws<DomainException>(() => Item(quantity: 0));

        foreach (var effect in Enum.GetValues<RunItemEffectType>())
        {
            var item = Item(effect: effect);
            _ = item.IsBattleItem;
            _ = item.BattleTargetingType;
            _ = item.IsUsable;
            _ = item.IsUsableInCombat;
        }

        foreach (var type in Enum.GetValues<RunItemType>())
        {
            var item = Item(type: type);
            _ = item.EffectiveMaxStack;
            _ = item.IsUsable;
        }

        var stack = Item(quantity: 1);
        stack.CanAddQuantity(0).Should().BeFalse();
        stack.CanAddQuantity(1).Should().BeTrue();
        Assert.Throws<DomainException>(() => stack.AddQuantity(0));
        stack.AddQuantity(1);
        stack.Quantity.Should().Be(2);

        var capped = RunItem.Rehydrate(
            RunItemId.New(), "item.cap", "Cap", "Test", RunItemType.Consumable,
            default, 20, RunItemEffectType.Heal, 1, DateTime.UtcNow, maxStack: 20);
        capped.CanAddQuantity(1).Should().BeFalse();
        Assert.Throws<DomainException>(() => capped.AddQuantity(1));

        var nonConsumable = Item(type: RunItemType.Equipment);
        Assert.Throws<DomainException>(() => nonConsumable.ConsumeOne());
        var depleted = RunItem.Rehydrate(
            RunItemId.New(), "item.zero", "Zero", "Test", RunItemType.Consumable,
            default, 0, RunItemEffectType.Heal, 1, DateTime.UtcNow);
        Assert.Throws<DomainException>(() => depleted.ConsumeOne());
        stack.ConsumeOne();

        Assert.Throws<DomainException>(() => stack.PlaceOnGround(Guid.Empty, 0, 0));
        Assert.Throws<DomainException>(() => stack.PlaceOnGround(Guid.NewGuid(), -1, 0));
        stack.PlaceOnGround(Guid.NewGuid(), 2, 3);
        stack.IsOnGround.Should().BeTrue();
        stack.CollectFromGround();
        stack.IsOnGround.Should().BeFalse();

        Assert.Throws<DomainException>(() => stack.PourLiquidInto("liquid.water"));
        Assert.Throws<DomainException>(() => stack.EmptyContents());
        var container = RunItem.Create(
            "item.container", "Container", "Test", RunItemType.Consumable,
            default, 1, RunItemEffectType.None, 0, isContainer: true, containerCapacity: 1);
        Assert.Throws<DomainException>(() => container.PourLiquidInto(" "));
        container.PourLiquidInto(" liquid.water ");
        container.ContainedLiquidDefinitionKey.Should().Be("liquid.water");
        Assert.Throws<DomainException>(() => container.PourLiquidInto("liquid.other"));
        container.EmptyContents();
        Assert.Throws<DomainException>(() => container.EmptyContents());
    }

    [Fact]
    public void RewardOffer_ShouldCoverNullEmptyDuplicateStateAndSelectionBranches()
    {
        var choice = RewardChoice.Create(default, "Choice", "Description", "payload");

        Assert.Throws<DomainException>(() => RewardOffer.Create(default, null!));
        Assert.Throws<DomainException>(() => RewardOffer.Create(default, []));
        Assert.Throws<DomainException>(() => RewardOffer.Create(default, [choice, choice]));

        var offer = RewardOffer.Create(default, [choice]);
        offer.IsPending.Should().BeTrue();
        Assert.Throws<DomainException>(() => offer.ReplaceChoices(null!));
        Assert.Throws<DomainException>(() => offer.ReplaceChoices([]));
        Assert.Throws<DomainException>(() => offer.ReplaceChoices([choice, choice]));
        Assert.Throws<DomainException>(() => offer.SelectChoice(RewardChoiceId.New()));

        var replacement = RewardChoice.Create(default, "Replacement", "Description", "replacement");
        offer.ReplaceChoices([replacement]);
        offer.SelectChoice(replacement.Id);
        offer.IsPending.Should().BeFalse();
        Assert.Throws<DomainException>(() => offer.SelectChoice(replacement.Id));
        Assert.Throws<DomainException>(() => offer.ReplaceChoices([replacement]));
    }

    [Fact]
    public void RoomTypeGenerationProfile_ShouldCoverEveryRangeAndFallbackBranch()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new RoomTypeGenerationProfile(default, null!, 0, 10));
        Assert.Throws<DomainException>(() =>
            new RoomTypeGenerationProfile(default, [], 0, 10));

        var weights = new[] { new NodeTypeWeight(NodeEventType.Combat, 2) };
        Assert.Throws<ArgumentException>(() => new NodeTypeWeight(NodeEventType.Combat, 0));
        Assert.Throws<DomainException>(() =>
            new RoomTypeGenerationProfile(default, weights, -1, 10));
        Assert.Throws<DomainException>(() =>
            new RoomTypeGenerationProfile(default, weights, 101, 101));
        Assert.Throws<DomainException>(() =>
            new RoomTypeGenerationProfile(default, weights, 50, 49));
        Assert.Throws<DomainException>(() =>
            new RoomTypeGenerationProfile(default, weights, 50, 101));

        var fallback = new RoomTypeGenerationProfile(default, weights, 0, 100);
        fallback.RewardProfilesByNodeType.Should().BeEmpty();
        fallback.TotalWeight.Should().Be(2);

        var rewards = new Dictionary<NodeEventType, IReadOnlyList<string>>
        {
            [NodeEventType.Combat] = ["combat"]
        };
        new RoomTypeGenerationProfile(default, weights, 0, 100, rewards)
            .RewardProfilesByNodeType.Should().BeSameAs(rewards);
    }

    [Fact]
    public void EmotionalAffinityModifier_ShouldCoverAllValidationDurationAndConsumptionBranches()
    {
        var outcome = Enum.GetValues<DamageEffectiveness>().First();
        Assert.Throws<DomainException>(() =>
            EmotionalAffinityModifier.Create(" ", EmotionalType.Neutral, outcome));
        Assert.Throws<DomainException>(() =>
            EmotionalAffinityModifier.Create("source", EmotionalType.Neutral));
        Assert.Throws<DomainException>(() =>
            EmotionalAffinityModifier.Create("source", EmotionalType.Neutral, outcome, durationActivations: 0));

        var permanent = EmotionalAffinityModifier.Create(
            " source ", EmotionalType.Neutral, outcomeOverride: outcome);
        permanent.IsExpired.Should().BeFalse();
        permanent.ConsumeActivation();
        permanent.RemainingActivations.Should().BeNull();

        var timed = EmotionalAffinityModifier.Create(
            "source", EmotionalType.Neutral, multiplierPercent: 10, durationActivations: 2);
        timed.ConsumeActivation();
        timed.RemainingActivations.Should().Be(1);
        timed.ConsumeActivation();
        timed.IsExpired.Should().BeTrue();
        timed.ConsumeActivation();
        timed.RemainingActivations.Should().Be(0);
    }

    private static CombatantSkill Skill(
        string key = "skill.test",
        string displayName = "Test",
        int manaCost = 0,
        int chargeCost = 0,
        int basePower = 10,
        string category = "Physical",
        int tacticalRange = 1,
        int cooldown = 0,
        string? emotionalRegister = "Memoire") =>
        CombatantSkill.Create(
            key, displayName, "Damage", "SingleEnemy", "Damage",
            manaCost, chargeCost, basePower,
            category: category,
            tacticalRange: tacticalRange,
            cooldown: cooldown,
            emotionalRegister: emotionalRegister);

    private static RunItem Item(
        string key = "item.test",
        string name = "Test",
        int quantity = 1,
        RunItemType type = RunItemType.Consumable,
        RunItemEffectType effect = RunItemEffectType.Heal) =>
        RunItem.Create(key, name, "Test", type, default, quantity, effect, 10);
}
