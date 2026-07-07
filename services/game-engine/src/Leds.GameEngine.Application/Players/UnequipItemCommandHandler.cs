using Leds.GameEngine.Application.Players.Ports;
using MediatR;

namespace Leds.GameEngine.Application.Players;

public sealed class UnequipItemCommandHandler : IRequestHandler<UnequipItemCommand, PlayerProfileView>
{
    private readonly IPlayerProfileGateway _playerProfileGateway;

    public UnequipItemCommandHandler(IPlayerProfileGateway playerProfileGateway)
    {
        _playerProfileGateway = playerProfileGateway;
    }

    public Task<PlayerProfileView> Handle(UnequipItemCommand request, CancellationToken cancellationToken)
    {
        return _playerProfileGateway.UnequipItemAsync(request.PlayerId, request.CharacterId, request.ItemKey, cancellationToken);
    }
}
