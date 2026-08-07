using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Events.Contracts;
using Leds.GameEngine.Application.Events.Resolution;
using Leds.GameEngine.Domain.Nodes;
using Leds.SharedBuildingBlocks.Results;

namespace Leds.GameEngine.Infrastructure.Events.Resolution;

public sealed class ItemEventContentResolutionStrategy : IEventContentResolutionStrategy
{
    // See CombatEventContentResolutionStrategy — same reasoning: this used to fetch a
    // catalog EventTemplate purely to fill EventTemplateKey/Version/Tags below, but that
    // legacy table isn't seeded, so it's synthesized locally instead. ItemTemplateKey used
    // to point at an equally unseeded placeholder ("item-memory-fragment-v1"); it now points
    // at a real canon item so a lookup against the catalog actually resolves.
    private const string DefaultEventTemplateKey = "event-item-v1";
    private const string TemplateVersion = "1.0";
    private const string DefaultItemTemplateKey = "canon.item.lanterne";

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
        var itemDefinitionResult = await _catalogContentGateway.GetItemDefinitionByKeyAsync(
            DefaultItemTemplateKey,
            cancellationToken);

        if (itemDefinitionResult.IsFailure)
        {
            return Result<ResolvedNodeEventContent>.Failure(itemDefinitionResult.Error);
        }

        var itemDefinition = itemDefinitionResult.Value;

        return Result<ResolvedNodeEventContent>.Success(
            new ResolvedItemEventContent(
                EventTemplateKey: DefaultEventTemplateKey,
                EventTemplateVersion: TemplateVersion,
                Tags: [],
                ItemTemplateKey: itemDefinition.Key,
                ItemTemplateVersion: itemDefinition.Version,
                RewardProfile: context.RewardProfile));
    }
}