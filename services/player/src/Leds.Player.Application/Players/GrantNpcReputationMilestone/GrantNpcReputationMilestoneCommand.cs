using MediatR;

namespace Leds.Player.Application.Players.GrantNpcReputationMilestone;

public sealed record GrantNpcReputationMilestoneCommand(Guid PlayerId, string NpcKey, string MilestoneKey, Guid? SourceRunId) : IRequest<PlayerProfileDto>;
