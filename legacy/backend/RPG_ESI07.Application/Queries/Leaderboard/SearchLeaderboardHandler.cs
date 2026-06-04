using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Queries.Leaderboard;

public class SearchLeaderboardHandler : IRequestHandler<SearchLeaderboardQuery, SearchLeaderboardResponse>
{
    private readonly ILeaderboardRepository _repository;

    public SearchLeaderboardHandler(ILeaderboardRepository repository) => _repository = repository;

    public async Task<SearchLeaderboardResponse> Handle(SearchLeaderboardQuery request, CancellationToken ct)
    {
        var term = request.Term.Trim();

        var items = await _repository.SearchByCharacterNameAsync(term, ct);

        return new SearchLeaderboardResponse(items.Select((p, i) => new LeaderboardSearchEntryDto(
            Rank: i + 1,
            CharacterName: p.CharacterName,
            Level: p.Level
        )).ToList());
    }
}