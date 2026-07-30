using Leds.GameEngine.Application.Runs.Dtos;

namespace Leds.GameEngine.Application.Runs.MoveParty;

public sealed record MovePartyResponse(
    RunDto Run,
    IReadOnlyCollection<Guid> CollectedItemIds,
    IReadOnlyCollection<Guid> BlockedItemIds);
