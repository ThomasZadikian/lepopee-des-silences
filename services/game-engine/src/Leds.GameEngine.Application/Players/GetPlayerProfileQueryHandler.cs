using Leds.GameEngine.Application.Players.Ports;
using MediatR;

namespace Leds.GameEngine.Application.Players;

public sealed class GetPlayerProfileQueryHandler : IRequestHandler<GetPlayerProfileQuery, PlayerProfileView>
{
    private readonly IPlayerProfileGateway _playerProfileGateway;

    public GetPlayerProfileQueryHandler(IPlayerProfileGateway playerProfileGateway)
    {
        _playerProfileGateway = playerProfileGateway;
    }

    public Task<PlayerProfileView> Handle(GetPlayerProfileQuery request, CancellationToken cancellationToken)
    {
        return _playerProfileGateway.GetProfileAsync(request.PlayerId, cancellationToken);
    }
}
