using MediatR;

namespace Leds.Player.Application.Players.AddPermanentItems;

public sealed record AddPermanentItemsCommand(
    Guid PlayerId,
    Guid CharacterId,
    IReadOnlyCollection<string> ItemDefinitionKeys,
    Guid? SourceRunId) : IRequest<PlayerProfileDto>;
