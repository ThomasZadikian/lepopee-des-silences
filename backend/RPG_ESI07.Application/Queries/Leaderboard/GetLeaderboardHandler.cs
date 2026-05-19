using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Queries.Leaderboard;

public class GetLeaderboardHandler : IRequestHandler<GetLeaderboardQuery, GetLeaderboardResponse>
{
    private readonly ILeaderboardRepository _repository;
    private const int PageSize = 50;
    private const int MaxItems = 100;

    public GetLeaderboardHandler(ILeaderboardRepository repository) => _repository = repository;

    public async Task<GetLeaderboardResponse> Handle(GetLeaderboardQuery request, CancellationToken ct)
    {
        var validSortFields = new HashSet<string>
        {
            "level", "combatswon", "totaldamagedealt", "totalplaytimeminutes", "charactername"
        };

        var sortBy = validSortFields.Contains(request.SortBy.ToLower())
            ? request.SortBy.ToLower()
            : "level";

        var page = Math.Max(1, request.Page);
        var skip = (page - 1) * PageSize;

        var (items, totalCount) = await _repository.GetLeaderboardAsync(
            sortBy,
            Math.Min(skip, MaxItems),
            Math.Min(PageSize, MaxItems - Math.Min(skip, MaxItems)),
            ct);

        var totalPages = (int)Math.Ceiling(Math.Min(totalCount, MaxItems) / (double)PageSize);

        var entries = items.Select((p, index) => new LeaderboardEntryDto(
            Rank: skip + index + 1,
            CharacterName: p.CharacterName,
            Level: p.Level,
            CombatsWon: p.CombatStats!.CombatsWon,
            TotalDamageDealt: p.CombatStats.TotalDamageDealt,
            TotalPlaytimeMinutes: p.CombatStats.TotalPlaytimeMinutes
        )).ToList();

        return new GetLeaderboardResponse(entries, Math.Min(totalCount, MaxItems), page, PageSize, totalPages);
    }
}