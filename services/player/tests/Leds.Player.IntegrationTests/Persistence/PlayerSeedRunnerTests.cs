using FluentAssertions;
using Leds.Player.Domain.Identity;
using Leds.Player.Infrastructure.Persistence;
using Leds.Player.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Leds.Player.IntegrationTests.Persistence;

[Collection("PlayerPostgres")]
public sealed class PlayerSeedRunnerTests
{
    private const string DevelopmentPassword = "local-development-only";
    private readonly PlayerPostgresFixture _fixture;

    public PlayerSeedRunnerTests(PlayerPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ApplyDemoPlayerSeedAsync_ShouldCreateVerifiedDevelopmentAccountsForEveryRole()
    {
        var (context, _) = _fixture.CreateContext();
        await using var _ = context;
        var security = CreateSecurity();
        var sut = new PlayerSeedRunner(context, NullLogger<PlayerSeedRunner>.Instance);

        await sut.ApplyDemoPlayerSeedAsync();

        var accounts = await context.AccountIdentities
            .AsNoTracking()
            .OrderBy(account => account.Role)
            .ToArrayAsync();

        accounts.Should().HaveCount(3);
        accounts.Select(account => (account.Email, account.Role)).Should().BeEquivalentTo(
            [
                ("player@leds.test", (int)AccountRole.Player),
                ("developer@leds.test", (int)AccountRole.Developer),
                ("admin@leds.test", (int)AccountRole.Administrator)
            ]);
        accounts.Should().OnlyContain(account => account.EmailVerifiedAtUtc.HasValue);
        accounts.Should().OnlyContain(account => account.MfaSecretProtected == null);
        accounts.Should().OnlyContain(account => security.VerifyPassword(DevelopmentPassword, account.PasswordHash));

        var accountProfileIds = accounts.Select(account => account.AccountId).ToArray();
        (await context.PlayerProfiles.CountAsync(profile => accountProfileIds.Contains(profile.Id)))
            .Should().Be(3);
    }

    [Fact]
    public async Task ApplyDemoPlayerSeedAsync_ShouldBeIdempotent()
    {
        var (context, _) = _fixture.CreateContext();
        await using var _ = context;
        var sut = new PlayerSeedRunner(context, NullLogger<PlayerSeedRunner>.Instance);

        await sut.ApplyDemoPlayerSeedAsync();
        await sut.ApplyDemoPlayerSeedAsync();

        (await context.AccountIdentities.CountAsync()).Should().Be(3);
        (await context.PlayerProfiles.CountAsync()).Should().Be(3);
        (await context.PlayerCharacters.CountAsync()).Should().Be(1);
    }

    private static AuthenticationSecurity CreateSecurity()
    {
        var values = new Dictionary<string, string?>
        {
            ["Authentication:MfaProtectionKey"] = Convert.ToBase64String(new byte[32])
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        return new AuthenticationSecurity(configuration);
    }
}
