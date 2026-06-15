using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Events.Contracts;
using Leds.GameEngine.Application.Events.Resolution;
using Leds.GameEngine.Domain.Nodes;
using Leds.SharedBuildingBlocks.Results;

namespace Leds.GameEngine.Infrastructure.Events.Resolution;

public sealed class RareEventContentResolutionStrategy : IEventContentResolutionStrategy
{
    private const string DefaultEventTemplateKey = "event-rare-encounter-v1";
    private const string DefaultEnemyTemplateKey = "enemy-rare-v1";

    private readonly ICatalogContentGateway _catalogContentGateway;

    public RareEventContentResolutionStrategy(ICatalogContentGateway catalogContentGateway)
    {
        _catalogContentGateway = catalogContentGateway;
    }

    public IReadOnlyCollection<NodeEventType> SupportedEventTypes { get; } =
        new[] { NodeEventType.Rare };

    public async Task<Result<ResolvedNodeEventContent>> ResolveAsync(
        EventContentResolutionContext context,
        CancellationToken cancellationToken = default)
    {
        var eventTemplateResult = await _catalogContentGateway.GetEventTemplateByKeyAsync(
            DefaultEventTemplateKey,
            cancellationToken);

        if (eventTemplateResult.IsFailure)
        {
            return Result<ResolvedNodeEventContent>.Failure(eventTemplateResult.Error);
        }

        var enemyTemplateResult = await _catalogContentGateway.GetEnemyTemplateByKeyAsync(
            DefaultEnemyTemplateKey,
            cancellationToken);

        if (enemyTemplateResult.IsFailure)
        {
            return Result<ResolvedNodeEventContent>.Failure(enemyTemplateResult.Error);
        }

        var eventTemplate = eventTemplateResult.Value;
        var enemyTemplate = enemyTemplateResult.Value;

        return Result<ResolvedNodeEventContent>.Success(
            new ResolvedRareCombatEventContent(
                EventTemplateKey: eventTemplate.Key,
                EventTemplateVersion: eventTemplate.Version,
                Tags: eventTemplate.NarrativeTags,
                EnemyTemplateKey: enemyTemplate.Key,
                EnemyTemplateVersion: enemyTemplate.Version,
                RiskLevel: context.RiskLevel));
    }
}