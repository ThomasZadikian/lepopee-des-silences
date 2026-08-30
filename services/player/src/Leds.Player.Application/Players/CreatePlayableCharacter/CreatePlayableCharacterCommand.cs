using MediatR;

namespace Leds.Player.Application.Players.CreatePlayableCharacter;

public sealed record CreatePlayableCharacterCommand(
    Guid PlayerId,
    string DisplayName,
    string ArchetypeKey) : IRequest<PlayerProfileDto>;
