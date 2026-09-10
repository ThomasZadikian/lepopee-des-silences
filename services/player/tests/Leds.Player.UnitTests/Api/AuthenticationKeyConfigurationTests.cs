using FluentAssertions;
using Leds.Player.Api.Configuration;
using Microsoft.Extensions.Configuration;

namespace Leds.Player.UnitTests.Api;

public sealed class AuthenticationKeyConfigurationTests
{
    [Fact]
    public void ConfigureMfaProtectionKey_ShouldRejectTransientDevelopmentKey()
    {
        var configuration = Configuration();

        var act = () => AuthenticationKeyConfiguration.ConfigureMfaProtectionKey(
            configuration,
            allowEphemeralKey: false);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Authentication:MfaProtectionKey*")
            .WithMessage("*persistent*");
    }

    [Fact]
    public void ConfigureMfaProtectionKey_ShouldAllowEphemeralTestingKey()
    {
        var configuration = Configuration();

        AuthenticationKeyConfiguration.ConfigureMfaProtectionKey(
            configuration,
            allowEphemeralKey: true);

        Convert.FromBase64String(configuration["Authentication:MfaProtectionKey"]!)
            .Should().HaveCount(32);
    }

    [Fact]
    public void ConfigureMfaProtectionKey_ShouldPreserveConfiguredPersistentKey()
    {
        var persistentKey = Convert.ToBase64String(Enumerable.Range(1, 32).Select(value => (byte)value).ToArray());
        var configuration = Configuration(("Authentication:MfaProtectionKey", persistentKey));

        AuthenticationKeyConfiguration.ConfigureMfaProtectionKey(
            configuration,
            allowEphemeralKey: false);

        configuration["Authentication:MfaProtectionKey"].Should().Be(persistentKey);
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(value => value.Key, value => (string?)value.Value))
            .Build();
}
