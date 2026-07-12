using FluentAssertions;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Rewards.Loot;
using Leds.GameEngine.Application.Rewards.RewardOfferFactory;
using Leds.GameEngine.Domain.Rewards;
using Leds.SharedBuildingBlocks.Errors;
using Leds.SharedBuildingBlocks.Results;
using Moq;

namespace Leds.GameEngine.UnitTests.Catalog;

public sealed class CatalogRewardTemplateFactoryTests
{
    private readonly Mock<ICatalogContentGateway> _gatewayMock = new();

    private RewardOfferFactory CreateFactory() =>
        new(new CombatRiskProfileResolver(), _gatewayMock.Object, new EnemyLootRewardBuilder(_gatewayMock.Object));

    [Fact]
    public async Task CreateFromTemplateKeyAsync_ShouldReturnNull_WhenTemplateNotFound()
    {
        _gatewayMock
            .Setup(p => p.GetRewardTemplateByKeyAsync("reward.nonexistent", default))
            .ReturnsAsync(Result<CatalogRewardTemplateSnapshot>.Failure(Error.Create("catalog.reward_template_not_found", "not found")));

        var result = await CreateFactory().CreateFromTemplateKeyAsync("reward.nonexistent", 25);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateFromTemplateKeyAsync_ShouldReturnOfferWithChoicesFromTemplate()
    {
        var template = new CatalogRewardTemplateSnapshot(
            "reward.combat.test", "1.0", "Test", "Test reward",
            "Combat", 2, 3,
            [
                new CatalogRewardTemplateOptionSnapshot("Heal", "Soin test", "Soin test", null, null, null, 10, "Flat", 1),
                new CatalogRewardTemplateOptionSnapshot("TemporaryItem", "Objet test", "Objet test", "item.test", "Item", null, 5, "Flat", 1),
            ]);

        _gatewayMock
            .Setup(p => p.GetRewardTemplateByKeyAsync("reward.combat.test", default))
            .ReturnsAsync(Result<CatalogRewardTemplateSnapshot>.Success(template));

        var offer = await CreateFactory().CreateFromTemplateKeyAsync("reward.combat.test", 25);

        offer.Should().NotBeNull();
        offer!.Choices.Should().HaveCount(2);
        offer.Choices.Should().Contain(c => c.RewardType == RewardType.Heal);
        offer.Choices.Should().Contain(c => c.RewardType == RewardType.TemporaryItem);
    }

    [Fact]
    public async Task CreateFromTemplateKeyAsync_ShouldParseSourceFromTemplate()
    {
        var template = new CatalogRewardTemplateSnapshot(
            "reward.item.test", "1.0", "Item test", "Item reward",
            "NodeEvent", 1, 3,
            [
                new CatalogRewardTemplateOptionSnapshot("Heal", "Soin", "Soin", null, null, null, 10, "Flat", 1),
            ]);

        _gatewayMock
            .Setup(p => p.GetRewardTemplateByKeyAsync("reward.item.test", default))
            .ReturnsAsync(Result<CatalogRewardTemplateSnapshot>.Success(template));

        var offer = await CreateFactory().CreateFromTemplateKeyAsync("reward.item.test", 15);

        offer.Should().NotBeNull();
        offer!.Source.Should().Be(RewardSource.NodeEvent);
    }

    [Fact]
    public async Task CreateFromTemplateKeyAsync_ShouldBuildItemPayloadFromTemplateOption()
    {
        var template = new CatalogRewardTemplateSnapshot(
            "reward.combat.test", "1.0", "Test", "",
            "Combat", 2, 3,
            [
                new CatalogRewardTemplateOptionSnapshot("TemporaryItem", "Baume", "Soin", "item.consumable.minor-heal", "Item", null, 15, "Flat", 1),
            ]);

        _gatewayMock
            .Setup(p => p.GetRewardTemplateByKeyAsync("reward.combat.test", default))
            .ReturnsAsync(Result<CatalogRewardTemplateSnapshot>.Success(template));

        var offer = await CreateFactory().CreateFromTemplateKeyAsync("reward.combat.test", 25);

        offer.Should().NotBeNull();
        var choice = offer!.Choices.Single();
        choice.RewardType.Should().Be(RewardType.TemporaryItem);
        choice.PayloadKey.Should().StartWith("item:item.consumable.minor-heal:");
    }

    [Fact]
    public async Task CreateFromTemplateKeyAsync_ShouldBuildHealPayload_WhenNoPayloadType()
    {
        var template = new CatalogRewardTemplateSnapshot(
            "reward.combat.test", "1.0", "Test", "",
            "Combat", 2, 3,
            [
                new CatalogRewardTemplateOptionSnapshot("Heal", "Soin", "Soin", null, null, null, 10, "Flat", 1),
            ]);

        _gatewayMock
            .Setup(p => p.GetRewardTemplateByKeyAsync("reward.combat.test", default))
            .ReturnsAsync(Result<CatalogRewardTemplateSnapshot>.Success(template));

        var offer = await CreateFactory().CreateFromTemplateKeyAsync("reward.combat.test", 25);

        offer.Should().NotBeNull();
        var choice = offer!.Choices.Single();
        choice.PayloadKey.Should().Be("heal:10");
    }

    [Fact]
    public async Task CreateFromTemplateKeyAsync_ShouldReturnPendingOffer()
    {
        var template = new CatalogRewardTemplateSnapshot(
            "reward.combat.test", "1.0", "Test", "",
            "Combat", 2, 3,
            [
                new CatalogRewardTemplateOptionSnapshot("Heal", "Soin", "Soin", null, null, null, 10, "Flat", 1),
            ]);

        _gatewayMock
            .Setup(p => p.GetRewardTemplateByKeyAsync("reward.combat.test", default))
            .ReturnsAsync(Result<CatalogRewardTemplateSnapshot>.Success(template));

        var offer = await CreateFactory().CreateFromTemplateKeyAsync("reward.combat.test", 25);

        offer!.State.Should().Be(RewardOfferState.Pending);
        offer.IsPending.Should().BeTrue();
    }

    [Fact]
    public async Task CreateFromTemplateKeyAsync_ShouldNotIncludeCombatScaling_WhenTemplateSourceIsNodeEvent()
    {
        var template = new CatalogRewardTemplateSnapshot(
            "reward.merchant.test", "1.0", "Marchand", "",
            "NodeEvent", 1, 3,
            [
                new CatalogRewardTemplateOptionSnapshot("Heal", "Soin", "Soin", null, null, null, 10, "Flat", 1),
            ]);

        _gatewayMock
            .Setup(p => p.GetRewardTemplateByKeyAsync("reward.merchant.test", default))
            .ReturnsAsync(Result<CatalogRewardTemplateSnapshot>.Success(template));

        var offer = await CreateFactory().CreateFromTemplateKeyAsync("reward.merchant.test", 20);

        offer!.CombatScaling.Should().BeNull();
    }
}
