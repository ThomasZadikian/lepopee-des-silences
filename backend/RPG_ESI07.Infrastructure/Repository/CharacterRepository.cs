using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;
using RPG_ESI07.Infrastructure.Data;

namespace RPG_ESI07.Infrastructure.Repository;

public class CharacterRepository : Repository<PlayerProfile>, IPlayerProfileRepository
{
    public CharacterRepository(AppDbContext context) : base(context) { }

    public override async Task<List<PlayerProfile>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .OrderBy(e => e.Level)
            .ThenBy(e => e.Experience)
            .ToListAsync();
    }

    public async Task<List<PlayerProfile>> GetBySpeedAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .OrderBy(e => e.Speed)
            .ToListAsync();
    }

    public async Task<List<PlayerProfile>> GetByLevelAsync(int level)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(e => e.Level == level)
            .OrderBy(e => e.Level)
            .ToListAsync();
    }
}