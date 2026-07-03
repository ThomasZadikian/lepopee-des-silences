using Leds.GameEngine.Application.Players.Ports;
using MediatR;

namespace Leds.GameEngine.Application.Players;

public sealed class EquipSkillCommandHandler : IRequestHandler<EquipSkillCommand, PlayerProfileView>
{
    private readonly IPlayerProfileGateway _playerProfileGateway;

    public EquipSkillCommandHandler(IPlayerProfileGateway playerProfileGateway)
    {
        _playerProfileGateway = playerProfileGateway;
    }

    public Task<PlayerProfileView> Handle(EquipSkillCommand request, CancellationToken cancellationToken)
    {
        return _playerProfileGateway.EquipSkillAsync(request.PlayerId, request.CharacterId, request.SkillKey, cancellationToken);
    }
}
