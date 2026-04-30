using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;
using RPG_ESI07.Infrastructure.Data;

namespace RPG_ESI07.Infrastructure.Repository;

public class PlayerInventoryRepository : IPlayerInventoryRepository
{
    private readonly AppDbContext _context;

    public PlayerInventoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PlayerInventory?> GetByIdAsync(int id)
    {
        return await _context.Set<PlayerInventory>()
            .Include(e => e.Item)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<PlayerInventory>> GetAllAsync()
    {
        return await _context.Set<PlayerInventory>()
            .Include(e => e.Item)
            .OrderBy(e => e.Id)
            .ToListAsync();
    }

    public async Task AddAsync(PlayerInventory entity)
    {
        _context.Set<PlayerInventory>().Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PlayerInventory entity)
    {
        _context.Set<PlayerInventory>().Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Set<PlayerInventory>().FindAsync(id);
        if (entity != null)
        {
            _context.Set<PlayerInventory>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}