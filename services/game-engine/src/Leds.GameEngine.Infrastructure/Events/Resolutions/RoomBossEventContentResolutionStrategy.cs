using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Events.Contracts;
using Leds.GameEngine.Application.Events.Resolution;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Bosses;
using Leds.SharedBuildingBlocks.Results;

namespace Leds.GameEngine.Infrastructure.Events.Resolution;

public sealed class RoomBossEventContentResolutionStrategy : IEventContentResolutionStrategy
{
    private readonly ICatalogContentGateway _catalogContentGateway;
    private readonly IRoomBossProfileResolver _bossProfileResolver;

    public RoomBossEventContentResolutionStrategy(
        ICatalogContentGateway catalogContentGateway,
        IRoomBossProfileResolver bossProfileResolver)
    {
        _catalogContentGateway = catalogContentGateway;
        _bossProfileResolver = bossProfileResolver;
    }

    public IReadOnlyCollection<NodeEventType> SupportedEventTypes { get; } =
        new[] { NodeEventType.RoomBoss };

    public async Task<Result<ResolvedNodeEventContent>> ResolveAsync(
        EventContentResolutionContext context,
        CancellationToken cancellationToken = default)
    {
        var bossProfile = _bossProfileResolver.Resolve(context.RoomType);

        // Convention : event template key = "event-" + enemyTemplateKey
        // e.g. "boss-threshold-guardian-v1" → "event-boss-threshold-guardian-v1"
        var eventTemplateKey = $"event-{bossProfile.EnemyTemplateKey}";

        var eventTemplateResult = await _catalogContentGateway.GetEventTemplateByKeyAsync(
            eventTemplateKey,
            cancellationToken);

        if (eventTemplateResult.IsFailure)
        {
            return Result<ResolvedNodeEventContent>.Failure(eventTemplateResult.Error);
        }

        var enemyTemplateResult = await _catalogContentGateway.GetEnemyTemplateByKeyAsync(
            bossProfile.EnemyTemplateKey,
            cancellationToken);

        if (enemyTemplateResult.IsFailure)
        {
            return Result<ResolvedNodeEventContent>.Failure(enemyTemplateResult.Error);
        }

        var eventTemplate = eventTemplateResult.Value;
        var enemyTemplate = enemyTemplateResult.Value;

        return Result<ResolvedNodeEventContent>.Success(
            new ResolvedRoomBossEventContent(
                EventTemplateKey: eventTemplate.Key,
                EventTemplateVersion: eventTemplate.Version,
                Tags: eventTemplate.NarrativeTags,
                EnemyTemplateKey: enemyTemplate.Key,
                EnemyTemplateVersion: enemyTemplate.Version,
                RiskLevel: context.RiskLevel));
    }
}
