using Leds.GameEngine.Domain.Combats;
using MediatR;

namespace Leds.GameEngine.Application.Runs.SetRoomRiskTier;

public sealed record SetRoomRiskTierCommand(Guid RunId, RiskTier Tier)
    : IRequest<SetRoomRiskTierResponse>;
