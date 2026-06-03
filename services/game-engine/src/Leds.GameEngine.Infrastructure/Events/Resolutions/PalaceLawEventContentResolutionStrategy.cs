using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Events.Contracts;
using Leds.GameEngine.Application.Events.Resolution;
using Leds.GameEngine.Domain.Nodes;
using Leds.SharedBuildingBlocks.Results;

namespace Leds.GameEngine.Infrastructure.Events.Resolution;

public sealed class PalaceLawEventContentResolutionStrategy : IEventContentResolutionStrategy
{
    private const string DefaultEventTemplateKey = "event-combat-shadow-v1";
    private const string DefaultPalaceLawDefinitionKey = "law-silence-v1";

    private readonly ICatalogContentGateway _catalogContentGateway;

    public PalaceLawEventContentResolutionStrategy(ICatalogContentGateway catalogContentGateway)
    {
        _catalogContentGateway = catalogContentGateway;
    }

    public IReadOnlyCollection<NodeEventType> SupportedEventTypes { get; } =
        new[] { NodeEventType.Law, NodeEventType.Curse };

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

        var palaceLawResult = await _catalogContentGateway.GetPalaceLawDefinitionByKeyAsync(
            DefaultPalaceLawDefinitionKey,
            cancellationToken);

        if (palaceLawResult.IsFailure)
        {
            return Result<ResolvedNodeEventContent>.Failure(palaceLawResult.Error);
        }

        var eventTemplate = eventTemplateResult.Value;
        var palaceLaw = palaceLawResult.Value;

        if (context.EventType == NodeEventType.Curse)
        {
            return Result<ResolvedNodeEventContent>.Success(
                new ResolvedCurseEventContent(
                    EventTemplateKey: eventTemplate.Key,
                    EventTemplateVersion: eventTemplate.Version,
                    Tags: eventTemplate.NarrativeTags,
                    PalaceLawDefinitionKey: palaceLaw.Key,
                    PalaceLawDefinitionVersion: palaceLaw.Version));
        }

        return Result<ResolvedNodeEventContent>.Success(
            new ResolvedPalaceLawEventContent(
                EventTemplateKey: eventTemplate.Key,
                EventTemplateVersion: eventTemplate.Version,
                Tags: eventTemplate.NarrativeTags,
                PalaceLawDefinitionKey: palaceLaw.Key,
                PalaceLawDefinitionVersion: palaceLaw.Version));
    }
}