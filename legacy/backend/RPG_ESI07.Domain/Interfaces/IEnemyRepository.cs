using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Domain.Interfaces;

public interface IEnemyRepository : IRepository<Enemy>
{
    Task<List<Enemy>> GetByTypeAsync(string type);
}