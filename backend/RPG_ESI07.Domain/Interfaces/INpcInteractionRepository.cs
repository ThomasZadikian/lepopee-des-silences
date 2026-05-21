using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Domain.Interfaces;

public interface INpcInteractionRepository : IRepository<NpcInteraction>
{
    Task<List<NpcInteraction>> GetByPlayerIdAsync(int playerId);
    Task<List<NpcInteraction>> GetByNpcIdAsync(int npcId);
}