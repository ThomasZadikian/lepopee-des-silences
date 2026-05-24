using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Domain.Interfaces;

public interface IGameSaveRepository : IRepository<GameSave>
{
    Task<List<GameSave>> GetByPlayerIdAsync(int playerId);
}