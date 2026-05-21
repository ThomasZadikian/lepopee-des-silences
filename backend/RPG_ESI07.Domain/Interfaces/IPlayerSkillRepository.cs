using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Domain.Interfaces;

public interface IPlayerSkillRepository : IRepository<PlayerSkill>
{
    Task<List<PlayerSkill>> GetByPlayerIdAsync(int playerId);
}