using MediatR;

namespace Leds.GameEngine.Application.Runs.SyncPartyStats;

public sealed record SyncPartyStatsCommand(Guid RunId) : IRequest<SyncPartyStatsResponse>;
