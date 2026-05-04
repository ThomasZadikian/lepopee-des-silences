using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;
using RPG_ESI07.Infrastructure.Data;

namespace RPG_ESI07.Infrastructure.Repository;

public class BestiaryUnlockRepository : IBestiaryUnlockRepository
{
    private readonly AppDbContext _context;

    public BestiaryUnlockRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<BestiaryUnlock?> GetByIdAsync(int id)
    {
        return await _context.Set<BestiaryUnlock>().FindAsync(id);
    }

    public async Task<List<BestiaryUnlock>> GetAllAsync()
    {
        return await _context.Set<BestiaryUnlock>()
            .Include(e => e.Enemy)
            .Include(e => e.Player)
            .OrderBy(e => e.Id)
            .ToListAsync();
    }

    public async Task AddAsync(BestiaryUnlock entity)
    {
        _context.Set<BestiaryUnlock>().Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(BestiaryUnlock entity)
    {
        _context.Set<BestiaryUnlock>().Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Set<BestiaryUnlock>().FindAsync(id);
        if (entity != null)
        {
            _context.Set<BestiaryUnlock>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}