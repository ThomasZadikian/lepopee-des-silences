using MediatR;

namespace RPG_ESI07.Application.Queries.Leaderboard;

public record GetLeaderboardQuery(
    string SortBy = "level",
    int Page = 1
) : IRequest<GetLeaderboardResponse>;

public record GetLeaderboardResponse(
    List<LeaderboardEntryDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

public record LeaderboardEntryDto(
    int Rank,
    string CharacterName,
    int Level,
    int CombatsWon,
    long TotalDamageDealt,
    int TotalPlaytimeMinutes
);