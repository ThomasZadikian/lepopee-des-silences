using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;
using RPG_ESI07.Infrastructure.Data;

namespace RPG_ESI07.Infrastructure.Repository;

public class EnemyRepository : Repository<Enemy>, IEnemyRepository
{
    public EnemyRepository(AppDbContext context) : base(context) { }

    public override async Task<List<Enemy>> GetAllAsync()
    {
        return await _dbSet
            .OrderBy(e => e.Type)
            .ThenBy(e => e.MaxHP)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Enemy>> GetByTypeAsync(string type)
    {
        return await _dbSet
            .Where(e => EF.Functions.Like(e.Type, type))
            .AsNoTracking()
            .ToListAsync();
    }
}