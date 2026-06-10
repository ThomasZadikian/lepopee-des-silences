using MediatR;

namespace Leds.Player.Application.Players.CreatePlayerProfile;

public sealed record CreatePlayerProfileCommand(string DisplayName) : IRequest<CreatePlayerProfileResponse>;
