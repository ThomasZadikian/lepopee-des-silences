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
    // See CombatEventContentResolutionStrategy — same reasoning: this used to fetch a
    // catalog EventTemplate purely to fill EventTemplateKey/Version/Tags below, but that
    // legacy table isn't seeded, so it's synthesized locally instead.
    private const string DefaultEventTemplateKey = "event-palace-law-v1";
    private const string TemplateVersion = "1.0";

    private readonly ICatalogContentGateway _catalogContentGateway;

    public PalaceLawEventContentResolutionStrategy(ICatalogContentGateway catalogContentGateway)
    {
        _catalogContentGateway = catalogContentGateway;
    }

    // NodeEventType.Law is no longer a selectable map node (ambient promulgation replaced
    // it — see Run.PromulgateLaw), so this strategy now only serves Curse events. It still
    // shares the catalog law pool and eligibility filtering Curse relies on.
    public IReadOnlyCollection<NodeEventType> SupportedEventTypes { get; } =
        new[] { NodeEventType.Curse };

    public async Task<Result<ResolvedNodeEventContent>> ResolveAsync(
        EventContentResolutionContext context,
        CancellationToken cancellationToken = default)
    {
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

        var palaceLaw = SelectDeterministically(context, eligiblePalaceLaws);

        return Result<ResolvedNodeEventContent>.Success(
            new ResolvedCurseEventContent(
                EventTemplateKey: DefaultEventTemplateKey,
                EventTemplateVersion: TemplateVersion,
                Tags: [],
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
