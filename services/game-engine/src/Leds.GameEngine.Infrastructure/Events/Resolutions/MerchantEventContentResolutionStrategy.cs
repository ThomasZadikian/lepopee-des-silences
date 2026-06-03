using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Events.Contracts;
using Leds.GameEngine.Application.Events.Resolution;
using Leds.GameEngine.Domain.Nodes;
using Leds.SharedBuildingBlocks.Results;

namespace Leds.GameEngine.Infrastructure.Events.Resolution;

public sealed class MerchantEventContentResolutionStrategy : IEventContentResolutionStrategy
{
    private const string DefaultEventTemplateKey = "event-combat-shadow-v1";

    private readonly ICatalogContentGateway _catalogContentGateway;

    public MerchantEventContentResolutionStrategy(ICatalogContentGateway catalogContentGateway)
    {
        _catalogContentGateway = catalogContentGateway;
    }

    public IReadOnlyCollection<NodeEventType> SupportedEventTypes { get; } =
        new[] { NodeEventType.Merchant };

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

        var eventTemplate = eventTemplateResult.Value;

        return Result<ResolvedNodeEventContent>.Success(
            new ResolvedMerchantEventContent(
                EventTemplateKey: eventTemplate.Key,
                EventTemplateVersion: eventTemplate.Version,
                Tags: eventTemplate.NarrativeTags,
                MerchantProfileKey: "merchant-placeholder-v1"));
    }
}