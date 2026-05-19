using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Domain.Interfaces;

public interface INpcRepository
{
    Task<List<Npc>> GetAllAsync();
    Task<Npc?> GetByIdAsync(int id);
    Task<List<Npc>> GetByZoneAsync(string zone);
    Task<List<Npc>> GetByTypeAsync(string type);
    Task AddAsync(Npc npc);
    Task UpdateAsync(Npc npc);
    Task DeleteAsync(int id);
}