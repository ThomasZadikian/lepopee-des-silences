using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Domain.Interfaces;

public interface INpcRepository : IRepository<Npc>
{
    Task<List<Npc>> GetByZoneAsync(string zone);
    Task<List<Npc>> GetByTypeAsync(string type);
}