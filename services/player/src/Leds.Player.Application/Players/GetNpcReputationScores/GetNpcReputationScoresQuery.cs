using Leds.Player.Application.Players.UpsertNpcReputationScores;
using MediatR;

namespace Leds.Player.Application.Players.GetNpcReputationScores;

public sealed record GetNpcReputationScoresQuery(Guid PlayerId) : IRequest<IReadOnlyCollection<NpcReputationScoreDto>>;
