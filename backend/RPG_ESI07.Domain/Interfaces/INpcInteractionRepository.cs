using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Domain.Interfaces;

public interface INpcInteractionRepository
{
    Task<List<NpcInteraction>> GetAllAsync();
    Task<NpcInteraction?> GetByIdAsync(int id);
    Task<List<NpcInteraction>> GetByPlayerIdAsync(int playerId);
    Task<List<NpcInteraction>> GetByNpcIdAsync(int npcId);
    Task AddAsync(NpcInteraction interaction);
    Task DeleteAsync(int id);
}