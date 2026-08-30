using MediatR;

namespace Leds.Player.Application.Players;

public sealed record GetPlayerRunSnapshotQuery(Guid PlayerId) : IRequest<PlayerRunSnapshotResponse>;
