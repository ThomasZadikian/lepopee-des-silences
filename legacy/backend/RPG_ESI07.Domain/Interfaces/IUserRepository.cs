using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);

    Task<User?> GetByEmailAsync(byte[] email);

    Task<bool> UsernameExistsAsync(string username);

    Task<User?> GetWithAllDataAsync(int userId);
}