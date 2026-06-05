using FluentAssertions;
using Leds.GameEngine.Application.Events.Contracts;
using Leds.GameEngine.Application.Events.Ports;
using Leds.GameEngine.Application.Events.Resolution;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Catalog;
using Leds.GameEngine.Infrastructure.Events.Resolution;

namespace Leds.GameEngine.UnitTests.Events;

public sealed class EventContentResolverTests
{
    private readonly IEventContentResolver _resolver;

    public EventContentResolverTests()
    {
        var catalogGateway = new InMemoryCatalogContentGateway();

        _resolver = new EventContentResolver(
            new IEventContentResolutionStrategy[]
            {
                new CombatEventContentResolutionStrategy(catalogGateway),
                new RoomBossEventContentResolutionStrategy(catalogGateway),
                new ItemEventContentResolutionStrategy(catalogGateway),
                new PalaceLawEventContentResolutionStrategy(catalogGateway),
                new NpcEventContentResolutionStrategy(catalogGateway),
                new RestEventContentResolutionStrategy(catalogGateway),
                new MerchantEventContentResolutionStrategy(catalogGateway),
                new RareEventContentResolutionStrategy(catalogGateway)
            });
    }

    [Fact]
    public async Task ResolveAsync_ShouldResolveCombatContent_WhenEventTypeIsCombat()
    {
        var result = await _resolver.ResolveAsync(CreateContext(NodeEventType.Combat));

        result.IsSuccess.Should().BeTrue();

        var content = result.Value.Should()
            .BeOfType<ResolvedCombatEventContent>()
            .Subject;

        content.Kind.Should().Be(ResolvedEventContentKind.Combat);
        content.EnemyTemplateKey.Should().Be("enemy-shadow-v1");
        content.RiskLevel.Should().Be(25);
    }

    [Fact]
    public async Task ResolveAsync_ShouldResolveEliteContent_WhenEventTypeIsElite()
    {
        var result = await _resolver.ResolveAsync(CreateContext(NodeEventType.Elite));

        result.IsSuccess.Should().BeTrue();

        var content = result.Value.Should()
            .BeOfType<ResolvedEliteEventContent>()
            .Subject;

        content.Kind.Should().Be(ResolvedEventContentKind.Elite);
        content.EnemyTemplateKey.Should().Be("enemy-shadow-v1");
    }

    [Fact]
    public async Task ResolveAsync_ShouldResolveItemContent_WhenEventTypeIsItem()
    {
        var result = await _resolver.ResolveAsync(CreateContext(NodeEventType.Item));

        result.IsSuccess.Should().BeTrue();

        var content = result.Value.Should()
            .BeOfType<ResolvedItemEventContent>()
            .Subject;

        content.ItemTemplateKey.Should().Be("item-memory-fragment-v1");
        content.RewardProfile.Should().Be("standard");
    }

    [Fact]
    public async Task ResolveAsync_ShouldResolvePalaceLawContent_WhenEventTypeIsLaw()
    {
        var result = await _resolver.ResolveAsync(CreateContext(NodeEventType.Law));

        result.IsSuccess.Should().BeTrue();

        var content = result.Value.Should()
            .BeOfType<ResolvedPalaceLawEventContent>()
            .Subject;

        content.PalaceLawDefinitionKey.Should().Be("law-silence-v1");
    }

    [Fact]
    public async Task ResolveAsync_ShouldResolveCurseContent_WhenEventTypeIsCurse()
    {
        var result = await _resolver.ResolveAsync(CreateContext(NodeEventType.Curse));

        result.IsSuccess.Should().BeTrue();

        var content = result.Value.Should()
            .BeOfType<ResolvedCurseEventContent>()
            .Subject;

        content.Kind.Should().Be(ResolvedEventContentKind.Curse);
        content.PalaceLawDefinitionKey.Should().Be("law-silence-v1");
    }

    [Fact]
    public async Task ResolveAsync_ShouldResolveNpcContent_WhenEventTypeIsNpc()
    {
        var result = await _resolver.ResolveAsync(CreateContext(NodeEventType.Npc));

        result.IsSuccess.Should().BeTrue();

        var content = result.Value.Should()
            .BeOfType<ResolvedNpcEventContent>()
            .Subject;

        content.NpcProfileKey.Should().Be("npc-placeholder-v1");
        content.InteractionProfileKey.Should().Be("npc-interaction-placeholder-v1");
    }

    [Fact]
    public async Task ResolveAsync_ShouldResolveRestContent_WhenEventTypeIsRest()
    {
        var result = await _resolver.ResolveAsync(CreateContext(NodeEventType.Rest));

        result.IsSuccess.Should().BeTrue();

        var content = result.Value.Should()
            .BeOfType<ResolvedRestEventContent>()
            .Subject;

        content.RecoveryRatio.Should().Be(30);
    }

    [Fact]
    public async Task ResolveAsync_ShouldResolveMerchantContent_WhenEventTypeIsMerchant()
    {
        var result = await _resolver.ResolveAsync(CreateContext(NodeEventType.Merchant));

        result.IsSuccess.Should().BeTrue();

        var content = result.Value.Should()
            .BeOfType<ResolvedMerchantEventContent>()
            .Subject;

        content.MerchantProfileKey.Should().Be("merchant-placeholder-v1");
    }

    [Fact]
    public async Task ResolveAsync_ShouldResolveRareContent_WhenEventTypeIsRare()
    {
        var result = await _resolver.ResolveAsync(CreateContext(NodeEventType.Rare));

        result.IsSuccess.Should().BeTrue();

        var content = result.Value.Should()
            .BeOfType<ResolvedRareEventContent>()
            .Subject;

        content.RareEventProfileKey.Should().Be("rare-event-placeholder-v1");
    }

    [Fact]
    public async Task ResolveAsync_ShouldRejectMemoryContent()
    {
        var result = await _resolver.ResolveAsync(CreateContext(NodeEventType.Memory));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("event_content.unsupported_standard_pipeline_event_type");
    }

    [Fact]
    public async Task ResolveAsync_ShouldResolveRoomBossContent()
    {
        var result = await _resolver.ResolveAsync(CreateContext(NodeEventType.RoomBoss));

        result.IsSuccess.Should().BeTrue();

        var content = result.Value.Should()
            .BeOfType<ResolvedRoomBossEventContent>()
            .Subject;

        content.Kind.Should().Be(ResolvedEventContentKind.Boss);
        content.EnemyTemplateKey.Should().Be("boss-threshold-guardian-v1");
        content.RiskLevel.Should().Be(25);
    }

    [Fact]
    public async Task ResolveAsync_ShouldRejectFinalBossContent()
    {
        var result = await _resolver.ResolveAsync(CreateContext(NodeEventType.FinalBoss));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("event_content.unsupported_standard_pipeline_event_type");
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnFailure_WhenSeedIsBlank()
    {
        var result = await _resolver.ResolveAsync(
            CreateContext(NodeEventType.Combat) with
            {
                Seed = " "
            });

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("event_content.seed_required");
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnFailure_WhenRewardProfileIsBlank()
    {
        var result = await _resolver.ResolveAsync(
            CreateContext(NodeEventType.Item) with
            {
                RewardProfile = " "
            });

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("event_content.reward_profile_required");
    }

    private static EventContentResolutionContext CreateContext(NodeEventType eventType)
    {
        return new EventContentResolutionContext(
            Seed: "seed-event-content-tests",
            RoomType: RoomType.Forest,
            RoomDepth: 1,
            NodeDepth: 2,
            EventOrder: 1,
            EventType: eventType,
            RiskLevel: 25,
            RewardProfile: "standard");
    }
}