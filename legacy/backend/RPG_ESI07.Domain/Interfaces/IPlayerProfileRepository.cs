using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Domain.Interfaces;

public interface IPlayerProfileRepository : IRepository<PlayerProfile>
{
    Task<List<PlayerProfile>> GetByLevelAsync(int level);

    Task<List<PlayerProfile>> GetBySpeedAsync();
}