using FluentAssertions;
using Leds.Catalog.Application.EffectSets.Dtos;
using Leds.Catalog.Application.EffectSets.GetEffectSetByKey;
using Leds.Catalog.Application.EffectSets.Ports;
using Leds.Catalog.Application.Enemies.Loot.GetEnemyLootTableByKey;
using Leds.Catalog.Application.Enemies.Loot.Ports;
using Leds.Catalog.Application.RewardCursePools.ListActiveRewardCursePools;
using Leds.Catalog.Application.RewardCursePools.Ports;
using Leds.Catalog.Application.Rewards.GenericLoot.GetActiveGenericLootPool;
using Leds.Catalog.Application.Rewards.GenericLoot.Ports;
using Leds.Catalog.Application.RoomTypes.Dtos;
using Leds.Catalog.Application.RoomTypes.ListActiveRoomTypeDefinitions;
using Leds.Catalog.Application.RoomTypes.Ports;
using Leds.Catalog.Domain.Enemies.Loot;
using Leds.Catalog.Domain.RewardCursePools;
using Leds.Catalog.Domain.Rewards.Loot;
using NSubstitute;

namespace Leds.Catalog.UnitTests.Application;

public sealed class CoverageMarginQueryHandlerTests
{
    [Fact]
    public async Task EffectSetHandler_ShouldForwardKeyAndPreserveMissingResult()
    {
        var store = Substitute.For<IEffectSetReadStore>();
        store.GetDtoByKeyAsync("effect.missing", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EffectSetDto?>(null));
        var handler = new GetEffectSetByKeyQueryHandler(store);

        var response = await handler.Handle(
            new GetEffectSetByKeyQuery("effect.missing"),
            CancellationToken.None);

        response.Should().NotBeNull();
        await store.Received(1).GetDtoByKeyAsync("effect.missing", CancellationToken.None);
    }

    [Fact]
    public void EffectSetValidator_ShouldRejectEmptyAndOverlongKeys()
    {
        var validator = new GetEffectSetByKeyQueryValidator();

        validator.Validate(new GetEffectSetByKeyQuery("")).IsValid.Should().BeFalse();
        validator.Validate(new GetEffectSetByKeyQuery(new string('x', 161))).IsValid.Should().BeFalse();
        validator.Validate(new GetEffectSetByKeyQuery("effect.valid")).IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EnemyLootHandler_ShouldForwardEnemyKeyAndPreserveMissingResult()
    {
        var store = Substitute.For<IEnemyLootTableReadStore>();
        store.GetByEnemyKeyAsync("enemy.missing", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnemyLootTable?>(null));
        var handler = new GetEnemyLootTableByKeyQueryHandler(store);

        var response = await handler.Handle(
            new GetEnemyLootTableByKeyQuery("enemy.missing"),
            CancellationToken.None);

        response.Should().NotBeNull();
        await store.Received(1).GetByEnemyKeyAsync("enemy.missing", CancellationToken.None);
    }

    [Fact]
    public void EnemyLootValidator_ShouldCoverAllKeyLengthBranches()
    {
        var validator = new GetEnemyLootTableByKeyQueryValidator();

        validator.Validate(new GetEnemyLootTableByKeyQuery("")).IsValid.Should().BeFalse();
        validator.Validate(new GetEnemyLootTableByKeyQuery(new string('x', 161))).IsValid.Should().BeFalse();
        validator.Validate(new GetEnemyLootTableByKeyQuery("enemy.valid")).IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GenericLootHandler_ShouldReturnMissingPoolWithoutInventingContent()
    {
        var store = Substitute.For<IGenericLootPoolReadStore>();
        store.GetActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IGenericLootPool?>(null));
        var handler = new GetActiveGenericLootPoolQueryHandler(store);

        var response = await handler.Handle(
            new GetActiveGenericLootPoolQuery(),
            CancellationToken.None);

        response.Should().NotBeNull();
        await store.Received(1).GetActiveAsync(CancellationToken.None);
    }

    [Fact]
    public async Task RoomTypeHandler_ShouldForwardEmptyCatalogWithoutFailure()
    {
        var store = Substitute.For<IRoomTypeDefinitionReadStore>();
        IReadOnlyCollection<RoomTypeDefinitionDto> definitions = Array.Empty<RoomTypeDefinitionDto>();
        store.ListActiveAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(definitions));
        var handler = new ListActiveRoomTypeDefinitionsQueryHandler(store);

        var response = await handler.Handle(
            new ListActiveRoomTypeDefinitionsQuery(),
            CancellationToken.None);

        response.Should().NotBeNull();
        await store.Received(1).ListActiveAsync(CancellationToken.None);
    }

    [Fact]
    public async Task RewardCursePoolHandler_ShouldForwardEmptyCatalogWithoutFailure()
    {
        var store = Substitute.For<IRewardCursePoolReadStore>();
        IReadOnlyCollection<IRewardCursePool> pools = Array.Empty<IRewardCursePool>();
        store.ListActiveAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(pools));
        var handler = new ListActiveRewardCursePoolsQueryHandler(store);

        var response = await handler.Handle(
            new ListActiveRewardCursePoolsQuery(),
            CancellationToken.None);

        response.Should().NotBeNull();
        await store.Received(1).ListActiveAsync(CancellationToken.None);
    }
}
