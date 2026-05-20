using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;
using RPG_ESI07.Infrastructure.Data;

namespace RPG_ESI07.Infrastructure.Repository;

public class LeaderboardRepository : ILeaderboardRepository
{
    private readonly AppDbContext _context;

    public LeaderboardRepository(AppDbContext context) => _context = context;

    public async Task<(List<PlayerProfile> Items, int TotalCount)> GetLeaderboardAsync(
        string sortBy,
        int skip,
        int take,
        CancellationToken ct)
    {
        var query = _context.PlayerProfiles
            .Include(p => p.CombatStats)
            .Where(p => p.CombatStats != null)
            .AsNoTracking();

        query = sortBy switch
        {
            "charactername" => query.OrderBy(p => p.CharacterName),
            "combatswon" => query.OrderByDescending(p => p.CombatStats!.CombatsWon),
            "totaldamagedealt" => query.OrderByDescending(p => p.CombatStats!.TotalDamageDealt),
            "totalplaytimeminutes" => query.OrderByDescending(p => p.CombatStats!.TotalPlaytimeMinutes),
            _ => query.OrderByDescending(p => p.Level)
        };

        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip(skip).Take(take).ToListAsync(ct);

        return (items, totalCount);
    }
    public async Task<List<PlayerProfile>> SearchByCharacterNameAsync(string term, CancellationToken ct)
    {
        // EF Core paramétrise automatiquement — pas d'injection SQL possible
        return await _context.PlayerProfiles
            .Where(p => EF.Functions.ILike(p.CharacterName, $"%{term}%"))
            .OrderBy(p => p.CharacterName)
            .Take(10) // Maximum 10 résultats
            .AsNoTracking()
            .ToListAsync(ct);
    }
}