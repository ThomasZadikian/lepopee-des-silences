using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Identity;

public static class PasswordPolicy
{
    public const int MinimumLength = 12;

    public static bool EnsureAcceptable(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinimumLength)
            throw new DomainException($"Password must contain at least {MinimumLength} characters.");

        return true;
    }
}
