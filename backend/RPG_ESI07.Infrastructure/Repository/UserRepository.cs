using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;
using RPG_ESI07.Infrastructure.Data;

namespace RPG_ESI07.Infrastructure.Repository;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet
            .Include(u => u.PlayerProfile)
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetByEmailAsync(byte[] email)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await _dbSet.AnyAsync(u => u.Username == username);
    }

    public async Task<User?> GetWithAllDataAsync(int userId)
    {
        return await _context.Users
            .Include(u => u.PlayerProfile)
            .ThenInclude(p => p!.GameSaves)
            .Include(u => u.PlayerProfile)
            .ThenInclude(p => p!.Inventory)
            .ThenInclude(pi => pi.Item)
            .Include(u => u.PlayerProfile)
            .ThenInclude(p => p!.Skills)
            .ThenInclude(ps => ps.Skill)
            .Include(u => u.PlayerProfile)
            .ThenInclude(p => p!.CombatStats)
            .Include(u => u.PlayerProfile)
            .ThenInclude(p => p!.BestiaryUnlocks)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }
}