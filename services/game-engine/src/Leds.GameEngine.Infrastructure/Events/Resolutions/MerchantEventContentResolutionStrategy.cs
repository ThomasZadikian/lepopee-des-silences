using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Events.Contracts;
using Leds.GameEngine.Application.Events.Resolution;
using Leds.GameEngine.Domain.Nodes;
using Leds.SharedBuildingBlocks.Results;

namespace Leds.GameEngine.Infrastructure.Events.Resolution;

public sealed class MerchantEventContentResolutionStrategy : IEventContentResolutionStrategy
{
    // See CombatEventContentResolutionStrategy — same reasoning: this used to fetch a
    // catalog EventTemplate purely to fill EventTemplateKey/Version/Tags below, but that
    // legacy table isn't seeded, so it's synthesized locally instead.
    private const string DefaultEventTemplateKey = "event-merchant-v1";
    private const string TemplateVersion = "1.0";

    private readonly ICatalogContentGateway _catalogContentGateway;

    public MerchantEventContentResolutionStrategy(ICatalogContentGateway catalogContentGateway)
    {
        _catalogContentGateway = catalogContentGateway;
    }

    public IReadOnlyCollection<NodeEventType> SupportedEventTypes { get; } =
        new[] { NodeEventType.Merchant };

    public Task<Result<ResolvedNodeEventContent>> ResolveAsync(
        EventContentResolutionContext context,
        CancellationToken cancellationToken = default)
    {
        ResolvedNodeEventContent content = new ResolvedMerchantEventContent(
            EventTemplateKey: DefaultEventTemplateKey,
            EventTemplateVersion: TemplateVersion,
            Tags: [],
            MerchantProfileKey: "merchant-placeholder-v1");

        return Task.FromResult(Result<ResolvedNodeEventContent>.Success(content));
    }
}