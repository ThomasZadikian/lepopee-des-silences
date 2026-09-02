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

        profile.AdvanceMainStory(new MainStoryAdvance
        {
            SequenceKey = request.SequenceKey,
            SequenceVersion = request.SequenceVersion,
            StepKey = request.StepKey,
            CheckpointKey = request.CheckpointKey,
            UnlockedRoomKeys = request.UnlockedRoomKeys,
            VisibleRoomKeys = request.VisibleRoomKeys,
            Complete = request.Complete,
            Now = _timeProvider.GetUtcNow()
        });
        await _repository.SaveAsync(profile, cancellationToken);
        return PlayerProfileDto.FromDomain(profile);
    }
}
