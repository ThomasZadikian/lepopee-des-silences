using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;
using RPG_ESI07.Infrastructure.Data;

namespace RPG_ESI07.Infrastructure.Repository;

public class NpcInteractionRepository : INpcInteractionRepository
{
    private readonly AppDbContext _context;

    public NpcInteractionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<NpcInteraction>> GetAllAsync()
    {
        return await _context.NpcInteractions
            .OrderByDescending(n => n.InteractedAt)
            .ToListAsync();
    }

    public async Task<NpcInteraction?> GetByIdAsync(int id)
    {
        return await _context.NpcInteractions.FindAsync(id);
    }

    public async Task<List<NpcInteraction>> GetByPlayerIdAsync(int playerId)
    {
        return await _context.NpcInteractions
            .Where(n => n.PlayerId == playerId)
            .OrderByDescending(n => n.InteractedAt)
            .ToListAsync();
    }

    public async Task<List<NpcInteraction>> GetByNpcIdAsync(int npcId)
    {
        return await _context.NpcInteractions
            .Where(n => n.NpcId == npcId)
            .OrderByDescending(n => n.InteractedAt)
            .ToListAsync();
    }

    public async Task AddAsync(NpcInteraction interaction)
    {
        _context.NpcInteractions.Add(interaction);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var interaction = await _context.NpcInteractions.FindAsync(id);
        if (interaction != null)
        {
            _context.NpcInteractions.Remove(interaction);
            await _context.SaveChangesAsync();
        }
    }
}