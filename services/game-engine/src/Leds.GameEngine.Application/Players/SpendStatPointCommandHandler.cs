using Leds.GameEngine.Application.Players.Ports;
using MediatR;

namespace Leds.GameEngine.Application.Players;

public sealed class SpendStatPointCommandHandler : IRequestHandler<SpendStatPointCommand, PlayerProfileView>
{
    private readonly IPlayerProfileGateway _playerProfileGateway;

    public SpendStatPointCommandHandler(IPlayerProfileGateway playerProfileGateway)
    {
        _playerProfileGateway = playerProfileGateway;
    }

    public Task<PlayerProfileView> Handle(SpendStatPointCommand request, CancellationToken cancellationToken)
    {
        return _playerProfileGateway.SpendStatPointAsync(request.PlayerId, request.CharacterId, request.Stat, cancellationToken);
    }
}
