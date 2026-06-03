using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Events.Contracts;
using Leds.GameEngine.Application.Events.Resolution;
using Leds.GameEngine.Domain.Nodes;
using Leds.SharedBuildingBlocks.Results;

namespace Leds.GameEngine.Infrastructure.Events.Resolution;

public sealed class ItemEventContentResolutionStrategy : IEventContentResolutionStrategy
{
    private const string DefaultEventTemplateKey = "event-combat-shadow-v1";
    private const string DefaultItemTemplateKey = "item-memory-fragment-v1";

    private readonly ICatalogContentGateway _catalogContentGateway;

    public ItemEventContentResolutionStrategy(ICatalogContentGateway catalogContentGateway)
    {
        _catalogContentGateway = catalogContentGateway;
    }

    public IReadOnlyCollection<NodeEventType> SupportedEventTypes { get; } =
        new[] { NodeEventType.Item };

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

        var itemTemplateResult = await _catalogContentGateway.GetItemTemplateByKeyAsync(
            DefaultItemTemplateKey,
            cancellationToken);

        if (itemTemplateResult.IsFailure)
        {
            return Result<ResolvedNodeEventContent>.Failure(itemTemplateResult.Error);
        }

        var eventTemplate = eventTemplateResult.Value;
        var itemTemplate = itemTemplateResult.Value;

        return Result<ResolvedNodeEventContent>.Success(
            new ResolvedItemEventContent(
                EventTemplateKey: eventTemplate.Key,
                EventTemplateVersion: eventTemplate.Version,
                Tags: eventTemplate.NarrativeTags,
                ItemTemplateKey: itemTemplate.Key,
                ItemTemplateVersion: itemTemplate.Version,
                RewardProfile: context.RewardProfile));
    }
}