using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Domain.Interfaces;

public interface IBestiaryUnlockRepository : IRepository<BestiaryUnlock>
{
    Task<List<BestiaryUnlock>> GetByPlayerIdAsync(int playerId);
    Task<BestiaryUnlock?> GetByPlayerAndEnemyAsync(int playerId, int enemyId);

}