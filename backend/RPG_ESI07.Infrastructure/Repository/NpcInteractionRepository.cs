using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;
using RPG_ESI07.Infrastructure.Data;

namespace RPG_ESI07.Infrastructure.Repository;

public class NpcInteractionRepository : Repository<NpcInteraction>, INpcInteractionRepository
{
    public NpcInteractionRepository(AppDbContext context) : base(context) { }

    public override async Task<List<NpcInteraction>> GetAllAsync()
    {
        return await _dbSet
            .OrderByDescending(n => n.InteractedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<NpcInteraction>> GetByPlayerIdAsync(int playerId)
    {
        return await _dbSet
            .Where(n => n.PlayerId == playerId)
            .OrderByDescending(n => n.InteractedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<NpcInteraction>> GetByNpcIdAsync(int npcId)
    {
        return await _dbSet
            .Where(n => n.NpcId == npcId)
            .OrderByDescending(n => n.InteractedAt)
            .AsNoTracking()
            .ToListAsync();
    }
}