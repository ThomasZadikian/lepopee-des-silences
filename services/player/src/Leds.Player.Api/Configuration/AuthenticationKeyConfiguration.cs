using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace Leds.Player.Api.Configuration;

public static class AuthenticationKeyConfiguration
{
    private const string MfaProtectionKeyName = "Authentication:MfaProtectionKey";

    public static void ConfigureMfaProtectionKey(
        IConfiguration configuration,
        bool allowEphemeralKey)
    {
        if (!string.IsNullOrWhiteSpace(configuration[MfaProtectionKeyName]))
            return;

        if (!allowEphemeralKey)
        {
            throw new InvalidOperationException(
                $"{MfaProtectionKeyName} must be configured with a persistent base64-encoded 32-byte key. " +
                "A transient key would make enrolled authenticators unusable after an API restart.");
        }

        configuration[MfaProtectionKeyName] = Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(32));
    }
}
