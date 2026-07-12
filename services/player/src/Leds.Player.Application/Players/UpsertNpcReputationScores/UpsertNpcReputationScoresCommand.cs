using MediatR;

namespace Leds.Player.Application.Players.UpsertNpcReputationScores;

public sealed record UpsertNpcReputationScoresCommand(
    Guid PlayerId,
    Guid SourceRunId,
    IReadOnlyCollection<NpcReputationScoreDto> Scores) : IRequest<IReadOnlyCollection<NpcReputationScoreDto>>;

public sealed record NpcReputationScoreDto(
    string NpcKey,
    int Score,
    int TimesMet,
    string? CurrentDialogueNodeKey);
