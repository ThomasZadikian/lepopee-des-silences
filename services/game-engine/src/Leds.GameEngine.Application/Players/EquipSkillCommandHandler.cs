using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Players.Ports;
using MediatR;

namespace Leds.GameEngine.Application.Players;

public sealed class EquipSkillCommandHandler : IRequestHandler<EquipSkillCommand, PlayerProfileView>
{
    private readonly IPlayerProfileGateway _playerProfileGateway;
    private readonly SkillArchetypeGate _skillArchetypeGate;

    public EquipSkillCommandHandler(IPlayerProfileGateway playerProfileGateway, SkillArchetypeGate skillArchetypeGate)
    {
        _playerProfileGateway = playerProfileGateway;
        _skillArchetypeGate = skillArchetypeGate;
    }

    public async Task<PlayerProfileView> Handle(EquipSkillCommand request, CancellationToken cancellationToken)
    {
        var profile = await _playerProfileGateway.GetProfileAsync(request.PlayerId, cancellationToken);
        var character = profile.Characters.FirstOrDefault(c => c.Id == request.CharacterId);
        if (character is null)
        {
            throw new ConflictException("Character not found on this player's profile.");
        }

        await _skillArchetypeGate.EnsureCanEquipAsync(character.DefinitionKey, request.SkillKey, cancellationToken);

        return await _playerProfileGateway.EquipSkillAsync(
            request.PlayerId, request.CharacterId, request.SkillKey, cancellationToken);
    }
}
