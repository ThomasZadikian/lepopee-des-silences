using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;
using RPG_ESI07.Infrastructure.Data;

namespace RPG_ESI07.Infrastructure.Repository;

public class UserConsentRepository : Repository<UserConsent>, IUserConsentRepository
{
    public UserConsentRepository(AppDbContext context) : base(context) { }
}