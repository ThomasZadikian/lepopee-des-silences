using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;
using RPG_ESI07.Infrastructure.Data;

namespace RPG_ESI07.Infrastructure.Repository;

public class NpcRepository : INpcRepository
{
    private readonly AppDbContext _context;

    public NpcRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Npc>> GetAllAsync()
    {
        return await _context.Npcs
            .OrderBy(n => n.Zone)
            .ThenBy(n => n.Name)
            .ToListAsync();
    }

    public async Task<Npc?> GetByIdAsync(int id)
    {
        return await _context.Npcs.FindAsync(id);
    }

    public async Task<List<Npc>> GetByZoneAsync(string zone)
    {
        return await _context.Npcs
            .Where(n => n.Zone.Equals(zone, StringComparison.OrdinalIgnoreCase))
            .ToListAsync();
    }

    public async Task<List<Npc>> GetByTypeAsync(string type)
    {
        return await _context.Npcs
            .Where(n => n.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
            .ToListAsync();
    }

    public async Task AddAsync(Npc npc)
    {
        _context.Npcs.Add(npc);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Npc npc)
    {
        _context.Npcs.Update(npc);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var npc = await _context.Npcs.FindAsync(id);
        if (npc != null)
        {
            _context.Npcs.Remove(npc);
            await _context.SaveChangesAsync();
        }
    }
}