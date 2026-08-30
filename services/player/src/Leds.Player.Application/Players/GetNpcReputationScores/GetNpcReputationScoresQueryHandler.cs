using Leds.Player.Application.Abstractions;
using Leds.Player.Application.Common.Exceptions;
using Leds.Player.Application.Players.UpsertNpcReputationScores;
using Leds.Player.Domain.Players;
using MediatR;

namespace Leds.Player.Application.Players.GetNpcReputationScores;

public sealed class GetNpcReputationScoresQueryHandler : IRequestHandler<GetNpcReputationScoresQuery, IReadOnlyCollection<NpcReputationScoreDto>>
{
    private readonly IPlayerProfileRepository _repository;

    public GetNpcReputationScoresQueryHandler(IPlayerProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<NpcReputationScoreDto>> Handle(GetNpcReputationScoresQuery request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(new PlayerId(request.PlayerId), cancellationToken)
            ?? throw new NotFoundException("Player", request.PlayerId);

        return profile.NpcReputationScores
            .Select(s => new NpcReputationScoreDto(s.NpcKey, s.Score, s.TimesMet, s.CurrentDialogueNodeKey))
            .ToArray();
    }
}
