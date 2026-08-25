using Leds.Player.Application.Abstractions;
using Leds.Player.Application.Common.Exceptions;
using Leds.Player.Domain.Players;
using MediatR;

namespace Leds.Player.Application.Players.UnlockDifficultyLevel;

public sealed class UnlockDifficultyLevelCommandHandler
    : IRequestHandler<UnlockDifficultyLevelCommand, PlayerProfileDto>
{
    private readonly IPlayerProfileRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UnlockDifficultyLevelCommandHandler(IPlayerProfileRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<PlayerProfileDto> Handle(UnlockDifficultyLevelCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(new PlayerId(request.PlayerId), cancellationToken)
            ?? throw new NotFoundException("Player", request.PlayerId);
        profile.UnlockDifficultyLevel(request.Level, _timeProvider.GetUtcNow());
        await _repository.SaveAsync(profile, cancellationToken);
        return PlayerProfileDto.FromDomain(profile);
    }
}
