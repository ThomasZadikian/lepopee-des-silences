using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Domain.Interfaces;

public interface IPlayerInventoryRepository : IRepository<PlayerInventory>
{
    Task<List<PlayerInventory>> GetByPlayerIdAsync(int playerId);
}