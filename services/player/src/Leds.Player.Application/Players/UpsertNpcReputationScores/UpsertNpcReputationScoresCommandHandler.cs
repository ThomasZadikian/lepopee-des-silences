using Leds.Player.Application.Abstractions;
using Leds.Player.Application.Common.Exceptions;
using Leds.Player.Domain.Players;
using MediatR;

namespace Leds.Player.Application.Players.UpsertNpcReputationScores;

public sealed class UpsertNpcReputationScoresCommandHandler : IRequestHandler<UpsertNpcReputationScoresCommand, IReadOnlyCollection<NpcReputationScoreDto>>
{
    private readonly IPlayerProfileRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UpsertNpcReputationScoresCommandHandler(IPlayerProfileRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyCollection<NpcReputationScoreDto>> Handle(UpsertNpcReputationScoresCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(new PlayerId(request.PlayerId), cancellationToken)
            ?? throw new NotFoundException("Player", request.PlayerId);

        var now = _timeProvider.GetUtcNow();
        var scores = request.Scores
            .Select(s => NpcReputationScore.Create(s.NpcKey, s.Score, s.TimesMet, s.CurrentDialogueNodeKey, now))
            .ToArray();

        profile.UpsertNpcReputationScores(scores, now);
        await _repository.SaveAsync(profile, cancellationToken);

        return profile.NpcReputationScores
            .Select(s => new NpcReputationScoreDto(s.NpcKey, s.Score, s.TimesMet, s.CurrentDialogueNodeKey))
            .ToArray();
    }
}
