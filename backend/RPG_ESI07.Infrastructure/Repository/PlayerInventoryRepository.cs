using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;
using RPG_ESI07.Infrastructure.Data;

namespace RPG_ESI07.Infrastructure.Repository;

public class PlayerInventoryRepository : Repository<PlayerInventory>, IPlayerInventoryRepository
{
    public PlayerInventoryRepository(AppDbContext context) : base(context) { }

    public override async Task<List<PlayerInventory>> GetAllAsync()
    {
        return await _dbSet
            .Include(e => e.Item)
            .OrderBy(e => e.Id)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<PlayerInventory>> GetByPlayerIdAsync(int playerId)
    {
        return await _dbSet
            .Include(e => e.Item)
            .Where(e => e.PlayerId == playerId)
            .OrderBy(e => e.Id)
            .AsNoTracking()
            .ToListAsync();
    }
}