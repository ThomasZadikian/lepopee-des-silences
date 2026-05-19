using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Domain.Interfaces;

public interface ILeaderboardRepository
{
    Task<(List<PlayerProfile> Items, int TotalCount)> GetLeaderboardAsync(
        string sortBy,
        int skip,
        int take,
        CancellationToken ct);
}