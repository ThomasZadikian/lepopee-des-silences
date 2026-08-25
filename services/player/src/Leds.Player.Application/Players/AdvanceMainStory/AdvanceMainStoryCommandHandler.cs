using Leds.Player.Application.Abstractions;
using Leds.Player.Application.Common.Exceptions;
using Leds.Player.Domain.Players;
using MediatR;

namespace Leds.Player.Application.Players.AdvanceMainStory;

public sealed class AdvanceMainStoryCommandHandler
    : IRequestHandler<AdvanceMainStoryCommand, PlayerProfileDto>
{
    private readonly IPlayerProfileRepository _repository;
    private readonly TimeProvider _timeProvider;

    public AdvanceMainStoryCommandHandler(IPlayerProfileRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<PlayerProfileDto> Handle(AdvanceMainStoryCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(new PlayerId(request.PlayerId), cancellationToken)
            ?? throw new NotFoundException("Player", request.PlayerId);

        profile.AdvanceMainStory(
            request.SequenceKey,
            request.SequenceVersion,
            request.StepKey,
            request.CheckpointKey,
            request.UnlockedRoomKeys,
            request.VisibleRoomKeys,
            request.Complete,
            _timeProvider.GetUtcNow());
        await _repository.SaveAsync(profile, cancellationToken);
        return PlayerProfileDto.FromDomain(profile);
    }
}
