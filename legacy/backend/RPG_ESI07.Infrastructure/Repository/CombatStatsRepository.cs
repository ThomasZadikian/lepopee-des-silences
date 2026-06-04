using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;
using RPG_ESI07.Infrastructure.Data;

namespace RPG_ESI07.Infrastructure.Repository;

public class CombatStatsRepository : Repository<CombatStats>, ICombatStatsRepository
{
    public CombatStatsRepository(AppDbContext context) : base(context) { }
}