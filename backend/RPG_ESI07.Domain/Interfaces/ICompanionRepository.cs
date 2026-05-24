using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Domain.Interfaces;

public interface ICompanionRepository
{
    Task<CompanionState?> GetCurrentStateAsync();
    Task UpdateStateAsync(CompanionState state);
    Task<CompanionState> GetOrCreateAsync();
}