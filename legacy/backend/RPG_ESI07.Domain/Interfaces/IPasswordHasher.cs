namespace RPG_ESI07.Domain.Interfaces;

public interface IPasswordHasher
{
    string HashPassword(string password);

    bool VerifyPassword(string password, string hashedPassword);
}