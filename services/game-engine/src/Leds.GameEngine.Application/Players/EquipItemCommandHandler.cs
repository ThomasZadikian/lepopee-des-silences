using Leds.GameEngine.Application.Players.Ports;
using MediatR;

namespace Leds.GameEngine.Application.Players;

public sealed class EquipItemCommandHandler : IRequestHandler<EquipItemCommand, PlayerProfileView>
{
    private readonly IPlayerProfileGateway _playerProfileGateway;

    public EquipItemCommandHandler(IPlayerProfileGateway playerProfileGateway)
    {
        _playerProfileGateway = playerProfileGateway;
    }

    public Task<PlayerProfileView> Handle(EquipItemCommand request, CancellationToken cancellationToken)
    {
        return _playerProfileGateway.EquipItemAsync(request.PlayerId, request.CharacterId, request.ItemKey, cancellationToken);
    }
}
