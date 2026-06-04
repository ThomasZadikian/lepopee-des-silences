using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;
using RPG_ESI07.Infrastructure.Data;

namespace RPG_ESI07.Infrastructure.Repository;

public class NpcRepository : Repository<Npc>, INpcRepository
{
    public NpcRepository(AppDbContext context) : base(context) { }

    public override async Task<List<Npc>> GetAllAsync()
    {
        return await _dbSet
            .OrderBy(n => n.Zone)
            .ThenBy(n => n.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Npc>> GetByZoneAsync(string zone)
    {
        return await _dbSet
            .Where(n => EF.Functions.Like(n.Zone, zone))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Npc>> GetByTypeAsync(string type)
    {
        return await _dbSet
            .Where(n => EF.Functions.Like(n.Type, type))
            .AsNoTracking()
            .ToListAsync();
    }
}