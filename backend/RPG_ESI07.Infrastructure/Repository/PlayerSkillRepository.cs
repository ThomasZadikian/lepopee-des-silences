using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;
using RPG_ESI07.Infrastructure.Data;

namespace RPG_ESI07.Infrastructure.Repository;

public class PlayerSkillRepository : IPlayerSkillRepository
{
    private readonly AppDbContext _context;

    public PlayerSkillRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PlayerSkill?> GetByIdAsync(int id)
    {
        return await _context.Set<PlayerSkill>().Include(e => e.Skill)
        .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<PlayerSkill>> GetAllAsync()
    {
        return await _context.Set<PlayerSkill>()
            .Include(e => e.Skill)
            .OrderBy(e => e.Id)
            .ToListAsync();
    }

    public async Task AddAsync(PlayerSkill entity)
    {
        _context.Set<PlayerSkill>().Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PlayerSkill entity)
    {
        _context.Set<PlayerSkill>().Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Set<PlayerSkill>().FindAsync(id);
        if (entity != null)
        {
            _context.Set<PlayerSkill>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}