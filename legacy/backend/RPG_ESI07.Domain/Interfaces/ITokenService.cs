using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Domain.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user, string role);

    string GenerateMfaToken(User user);

    int? ValidateTokenAndGetUserId(string token);
    int? ValidateMfaToken(string token);
}