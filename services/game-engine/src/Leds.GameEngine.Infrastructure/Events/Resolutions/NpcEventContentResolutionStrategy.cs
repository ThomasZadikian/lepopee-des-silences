using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Events.Contracts;
using Leds.GameEngine.Application.Events.Npcs;
using Leds.GameEngine.Application.Events.Resolution;
using Leds.GameEngine.Domain.Nodes;
using Leds.SharedBuildingBlocks.Results;

namespace Leds.GameEngine.Infrastructure.Events.Resolution;

public sealed class NpcEventContentResolutionStrategy : IEventContentResolutionStrategy
{
    private const string DefaultEventTemplateKey = "event-combat-shadow-v1";
    private const string DefaultInteractionProfileKey = "npc-interaction-placeholder-v1";

    private readonly ICatalogContentGateway _catalogContentGateway;
    private readonly INpcEncounterSelector _npcEncounterSelector;

    public NpcEventContentResolutionStrategy(
        ICatalogContentGateway catalogContentGateway,
        INpcEncounterSelector npcEncounterSelector)
    {
        _catalogContentGateway = catalogContentGateway;
        _npcEncounterSelector = npcEncounterSelector;
    }

    public IReadOnlyCollection<NodeEventType> SupportedEventTypes { get; } =
        new[] { NodeEventType.Npc };

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

        var allNpcs = await _catalogContentGateway.ListNpcDefinitionsAsync(cancellationToken);

        var eligibilityContext = new NpcEligibilityContext(
            RunId: Guid.Empty,
            RoomId: Guid.Empty,
            NodeId: Guid.Empty,
            Seed: context.Seed,
            PalaceRoomState: context.PalaceRoomState ?? Domain.Rooms.PalaceRoomState.Neutral,
            RoomClimate: context.RoomClimate,
            RoomType: context.RoomType,
            NodeDepth: context.NodeDepth);

        var selectedNpc = _npcEncounterSelector.SelectEligibleNpc(eligibilityContext, allNpcs);

        if (selectedNpc is null)
        {
            return Result<ResolvedNodeEventContent>.Success(
                new ResolvedNpcEventContent(
                    EventTemplateKey: eventTemplate.Key,
                    EventTemplateVersion: eventTemplate.Version,
                    Tags: eventTemplate.NarrativeTags,
                    NpcProfileKey: "npc-placeholder-v1",
                    InteractionProfileKey: DefaultInteractionProfileKey,
                    NpcDisplayName: "Figure du Palais",
                    NpcDescription: "Une présence s'efface avant d'avoir pu parler."));
        }

        return Result<ResolvedNodeEventContent>.Success(
            new ResolvedNpcEventContent(
                EventTemplateKey: eventTemplate.Key,
                EventTemplateVersion: eventTemplate.Version,
                Tags: eventTemplate.NarrativeTags,
                NpcProfileKey: selectedNpc.Key,
                InteractionProfileKey: DefaultInteractionProfileKey,
                NpcDisplayName: selectedNpc.DisplayName,
                NpcDescription: selectedNpc.Description));
    }
}
