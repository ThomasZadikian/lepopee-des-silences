using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;
using RPG_ESI07.Infrastructure.Data;

namespace RPG_ESI07.Infrastructure.Repository;

public class BestiaryUnlockRepository : Repository<BestiaryUnlock>, IBestiaryUnlockRepository
{
    public BestiaryUnlockRepository(AppDbContext context) : base(context) { }

    public override async Task<List<BestiaryUnlock>> GetAllAsync()
    {
        return await _dbSet
            .Include(e => e.Enemy)
            .Include(e => e.Player)
            .OrderBy(e => e.Id)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<BestiaryUnlock>> GetByPlayerIdAsync(int playerId)
    {
        return await _dbSet
            .Include(e => e.Enemy)
            .Include(e => e.Player)
            .Where(e => e.PlayerId == playerId)
            .OrderBy(e => e.Id)
            .AsNoTracking()
            .ToListAsync();
    }
}