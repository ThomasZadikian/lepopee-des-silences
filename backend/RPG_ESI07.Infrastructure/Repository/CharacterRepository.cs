using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;
using RPG_ESI07.Infrastructure.Data;

namespace RPG_ESI07.Infrastructure.Repository;

public class CharacterRepository : IPlayerProfileRepository
{
    private readonly AppDbContext _context;

    public CharacterRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PlayerProfile?> GetByIdAsync(int id)
    {
        return await _context.PlayerProfiles.FindAsync(id);
    }

    public async Task<List<PlayerProfile>> GetAllAsync()
    {
        return await _context.PlayerProfiles
            .AsNoTracking()
            .OrderBy(e => e.Level)
            .ThenBy(e => e.Experience)
            .ToListAsync();
    }

    public async Task<List<PlayerProfile>> GetBySpeedAsync()
    {
        return await _context.PlayerProfiles
            .AsNoTracking()
            .OrderBy(e => e.Speed)
            .ToListAsync();
    }

    public async Task<List<PlayerProfile>> GetByLevelAsync(int level)
    {
        return await _context.PlayerProfiles
            .AsNoTracking()
            .Where(e => e.Level == level)
            .OrderBy(e => e.Level)
            .ToListAsync();
    }

    public async Task AddAsync(PlayerProfile profile)
    {
        _context.PlayerProfiles.Add(profile);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PlayerProfile profile)
    {
        _context.PlayerProfiles.Update(profile);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var profile = await _context.PlayerProfiles.FindAsync(id);
        if (profile != null)
        {
            _context.PlayerProfiles.Remove(profile);
            await _context.SaveChangesAsync();
        }
    }
}