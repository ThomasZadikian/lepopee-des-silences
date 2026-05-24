using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;
using RPG_ESI07.Infrastructure.Data;

namespace RPG_ESI07.Infrastructure.Repository;

public class CompanionRepository : ICompanionRepository
{
    private readonly AppDbContext _context;

    public CompanionRepository(AppDbContext context) => _context = context;

    public async Task<CompanionState?> GetCurrentStateAsync()
    {
        return await _context.CompanionStates.FirstOrDefaultAsync();
    }

    public async Task UpdateStateAsync(CompanionState state)
    {
        _context.CompanionStates.Update(state);
        await _context.SaveChangesAsync();
    }

    public async Task<CompanionState> GetOrCreateAsync()
    {
        var state = await _context.CompanionStates.FirstOrDefaultAsync();
        if (state != null) return state;

        state = new CompanionState
        {
            CurrentState = Constants.CompanionStateRepos,
            LastUpdated = DateTime.UtcNow,
            LastCombatAt = DateTime.UtcNow
        };
        _context.CompanionStates.Add(state);
        await _context.SaveChangesAsync();
        return state;
    }
}