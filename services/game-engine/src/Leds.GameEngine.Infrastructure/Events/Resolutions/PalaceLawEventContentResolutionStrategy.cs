using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Events.Contracts;
using Leds.GameEngine.Application.Events.Resolution;
using Leds.GameEngine.Domain.Nodes;
using Leds.SharedBuildingBlocks.Errors;
using Leds.SharedBuildingBlocks.Results;

namespace Leds.GameEngine.Infrastructure.Events.Resolution;

public sealed class PalaceLawEventContentResolutionStrategy : IEventContentResolutionStrategy
{
    private const string DefaultEventTemplateKey = "event-combat-shadow-v1";

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

        var activeLawKeys = (context.ActivePalaceLawKeys ?? [])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var eligiblePalaceLaws = (await _catalogContentGateway.ListActivePalaceLawDefinitionsAsync(cancellationToken))
            .Where(law => !activeLawKeys.Contains(law.Key))
            .OrderBy(law => law.Priority)
            .ThenBy(law => law.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (eligiblePalaceLaws.Length == 0)
        {
            return Result<ResolvedNodeEventContent>.Failure(Error.Create(
                "event_content.no_eligible_palace_law",
                "No eligible palace law definition is available for this event."));
        }

        var eventTemplate = eventTemplateResult.Value;
        var palaceLaw = SelectDeterministically(context, eligiblePalaceLaws);

        if (context.EventType == NodeEventType.Curse)
        {
            return Result<ResolvedNodeEventContent>.Success(
                new ResolvedCurseEventContent(
                    EventTemplateKey: eventTemplate.Key,
                    EventTemplateVersion: eventTemplate.Version,
                    Tags: eventTemplate.NarrativeTags,
                    PalaceLawDefinitionKey: palaceLaw.Key,
                    PalaceLawName: palaceLaw.Name,
                    PalaceLawDescription: palaceLaw.Description,
                    PalaceLawDefinitionVersion: palaceLaw.Version));
        }

        return Result<ResolvedNodeEventContent>.Success(
            new ResolvedPalaceLawEventContent(
                EventTemplateKey: eventTemplate.Key,
                EventTemplateVersion: eventTemplate.Version,
                Tags: eventTemplate.NarrativeTags,
                PalaceLawDefinitionKey: palaceLaw.Key,
                PalaceLawName: palaceLaw.Name,
                PalaceLawDescription: palaceLaw.Description,
                PalaceLawDefinitionVersion: palaceLaw.Version));
    }

    private static PalaceLawDefinitionSnapshot SelectDeterministically(
        EventContentResolutionContext context,
        IReadOnlyList<PalaceLawDefinitionSnapshot> eligiblePalaceLaws)
    {
        var input = string.Join(
            '|',
            context.Seed.Trim(),
            context.RoomType.ToString(),
            context.RoomDepth.ToString(),
            context.NodeDepth.ToString(),
            context.EventOrder.ToString(),
            context.EventType.ToString(),
            context.RiskLevel.ToString(),
            context.RewardProfile.Trim());

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var value = BinaryPrimitives.ReadUInt64BigEndian(hash.AsSpan(0, sizeof(ulong)));
        var index = (int)(value % (ulong)eligiblePalaceLaws.Count);

        return eligiblePalaceLaws[index];
    }
}
