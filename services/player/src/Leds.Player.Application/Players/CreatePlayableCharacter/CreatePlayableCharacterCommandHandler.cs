using Leds.Player.Application.Abstractions;
using Leds.Player.Application.Common.Exceptions;
using Leds.Player.Domain.Players;
using MediatR;

namespace Leds.Player.Application.Players.CreatePlayableCharacter;

public sealed class CreatePlayableCharacterCommandHandler
    : IRequestHandler<CreatePlayableCharacterCommand, PlayerProfileDto>
{
    private readonly IPlayerProfileRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreatePlayableCharacterCommandHandler(
        IPlayerProfileRepository repository,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<PlayerProfileDto> Handle(
        CreatePlayableCharacterCommand request,
        CancellationToken cancellationToken)
    {
        var playerId = new PlayerId(request.PlayerId);
        var profile = await _repository.GetByIdAsync(playerId, cancellationToken)
            ?? throw new NotFoundException($"Player '{request.PlayerId}' was not found.");

        profile.CreatePlayableCharacter(request.DisplayName, request.ArchetypeKey, _timeProvider.GetUtcNow());
        await _repository.SaveAsync(profile, cancellationToken);

        return PlayerProfileDto.FromDomain(profile);
    }
}
