using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;
using RPG_ESI07.Infrastructure.Data;

namespace RPG_ESI07.Infrastructure.Repository;

public class GameSaveRepository : Repository<GameSave>, IGameSaveRepository
{
    public GameSaveRepository(AppDbContext context) : base(context) { }

    public async Task<List<GameSave>> GetByPlayerIdAsync(int playerId)
    {
        return await _dbSet
            .Where(e => e.PlayerId == playerId)
            .OrderBy(e => e.Id)
            .AsNoTracking()
            .ToListAsync();
    }
}