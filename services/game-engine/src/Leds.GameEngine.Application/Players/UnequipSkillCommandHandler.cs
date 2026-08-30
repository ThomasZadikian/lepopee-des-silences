using Leds.GameEngine.Application.Players.Ports;
using MediatR;

namespace Leds.GameEngine.Application.Players;

public sealed class UnequipSkillCommandHandler : IRequestHandler<UnequipSkillCommand, PlayerProfileView>
{
    private readonly IPlayerProfileGateway _playerProfileGateway;

    public UnequipSkillCommandHandler(IPlayerProfileGateway playerProfileGateway)
    {
        _playerProfileGateway = playerProfileGateway;
    }

    public Task<PlayerProfileView> Handle(UnequipSkillCommand request, CancellationToken cancellationToken)
    {
        return _playerProfileGateway.UnequipSkillAsync(request.PlayerId, request.CharacterId, request.SkillKey, cancellationToken);
    }
}
