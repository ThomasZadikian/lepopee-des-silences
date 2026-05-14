using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;
using RPG_ESI07.Infrastructure.Data;

namespace RPG_ESI07.Infrastructure.Repository;

public class EnemyRepository : IEnemyRepository
{
    private readonly AppDbContext _context;

    public EnemyRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Enemy?> GetByIdAsync(int id)
    {
        return await _context.Enemies.FindAsync(id);
    }

    public async Task<List<Enemy>> GetAllAsync()
    {
        return await _context.Enemies
            .OrderBy(e => e.Type)
            .ThenBy(e => e.MaxHP)
            .ToListAsync();
    }

    public async Task<List<Enemy>> GetByTypeAsync(string type)
    {
        return await _context.Enemies
            .Where(e => e.Type.Equals(type, StringComparison.CurrentCultureIgnoreCase))
            .ToListAsync();
    }

    public async Task AddAsync(Enemy enemy)
    {
        _context.Enemies.Add(enemy);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Enemy enemy)
    {
        _context.Enemies.Update(enemy);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var enemy = await _context.Enemies.FindAsync(id);
        if (enemy != null)
        {
            _context.Enemies.Remove(enemy);
            await _context.SaveChangesAsync();
        }
    }
}