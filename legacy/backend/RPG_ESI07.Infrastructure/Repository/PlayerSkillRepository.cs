using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;
using RPG_ESI07.Infrastructure.Data;

namespace RPG_ESI07.Infrastructure.Repository;

public class PlayerSkillRepository : Repository<PlayerSkill>, IPlayerSkillRepository
{
    public PlayerSkillRepository(AppDbContext context) : base(context) { }

    public override async Task<List<PlayerSkill>> GetAllAsync()
    {
        return await _dbSet
            .Include(e => e.Skill)
            .OrderBy(e => e.Id)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<PlayerSkill>> GetByPlayerIdAsync(int playerId)
    {
        return await _dbSet
            .Include(e => e.Skill)
            .Where(e => e.PlayerId == playerId)
            .OrderBy(e => e.Id)
            .AsNoTracking()
            .ToListAsync();
    }
}