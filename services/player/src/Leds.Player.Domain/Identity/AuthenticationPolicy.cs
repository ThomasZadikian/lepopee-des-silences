namespace Leds.Player.Domain.Identity;

public static class AuthenticationPolicy
{
    public static bool RequiresMfa(string authenticationKind) =>
        string.Equals(authenticationKind, "Interactive", StringComparison.OrdinalIgnoreCase);

    public static bool CanStartInteractiveSession(UserIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        return identity.IsEmailVerified && identity.IsMfaConfigured;
    }
}
