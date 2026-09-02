using Leds.Player.Application.Abstractions;
using Leds.Player.Domain.Identity;
using Leds.Player.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Leds.Player.Infrastructure.Persistence;

public sealed class PlayerSeedRunner
{
    private static readonly Guid DemoPlayerId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid DefaultCharacterId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid StatBlockId = Guid.Parse("00000000-0000-0000-0000-000000000003");
    private static readonly Guid StrikeSkillId = Guid.Parse("00000000-0000-0000-0000-000000000004");
    private static readonly Guid GuardSkillId = Guid.Parse("00000000-0000-0000-0000-000000000005");
    private static readonly Guid PlayerIdentityId = Guid.Parse("00000000-0000-0000-0000-000000000006");
    private static readonly Guid DeveloperAccountId = Guid.Parse("00000000-0000-0000-0000-000000000010");
    private static readonly Guid DeveloperIdentityId = Guid.Parse("00000000-0000-0000-0000-000000000011");
    private static readonly Guid AdministratorAccountId = Guid.Parse("00000000-0000-0000-0000-000000000020");
    private static readonly Guid AdministratorIdentityId = Guid.Parse("00000000-0000-0000-0000-000000000021");

    // This credential is deliberately public and restricted to the Development-only seed
    // invoked by Program.cs. It must never be reused outside a disposable local database.
    private const string DevelopmentCredential = "local-development-only";

    private static readonly DevelopmentAccountSeed[] DevelopmentAccounts =
    [
        new(PlayerIdentityId, DemoPlayerId, "player@leds.test", "Voyageur", AccountRole.Player),
        new(DeveloperIdentityId, DeveloperAccountId, "developer@leds.test", "Développeur Test", AccountRole.Developer),
        new(AdministratorIdentityId, AdministratorAccountId, "admin@leds.test", "Administrateur Test", AccountRole.Administrator)
    ];

    private readonly PlayerDbContext _context;
    private readonly IAuthenticationSecurity _security;
    private readonly ILogger<PlayerSeedRunner> _logger;

    public PlayerSeedRunner(
        PlayerDbContext context,
        IAuthenticationSecurity security,
        ILogger<PlayerSeedRunner> logger)
    {
        _context = context;
        _security = security;
        _logger = logger;
    }

    public async Task ApplyDevelopmentSeedAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var accountIds = DevelopmentAccounts.Select(account => account.AccountId).ToArray();
        var emails = DevelopmentAccounts.Select(account => account.Email).ToArray();
        var existingProfileIds = await _context.PlayerProfiles
            .Where(profile => accountIds.Contains(profile.Id))
            .Select(profile => profile.Id)
            .ToHashSetAsync(cancellationToken);

        if (!existingProfileIds.Contains(DemoPlayerId))
            _context.PlayerProfiles.Add(CreateDemoPlayer(now));

        foreach (var account in DevelopmentAccounts.Where(account => account.AccountId != DemoPlayerId))
        {
            if (existingProfileIds.Contains(account.AccountId))
                continue;

            _context.PlayerProfiles.Add(new PlayerProfileEntity
            {
                Id = account.AccountId,
                AuthSubjectId = null,
                DisplayName = account.DisplayName,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        var existingIdentities = await _context.AccountIdentities
            .Where(identity => accountIds.Contains(identity.AccountId) || emails.Contains(identity.Email))
            .Select(identity => new { identity.AccountId, identity.Email })
            .ToArrayAsync(cancellationToken);

        foreach (var account in DevelopmentAccounts)
        {
            if (existingIdentities.Any(identity =>
                    identity.AccountId == account.AccountId || identity.Email == account.Email))
            {
                continue;
            }

            _context.AccountIdentities.Add(new AccountIdentityEntity
            {
                Id = account.IdentityId,
                AccountId = account.AccountId,
                Email = account.Email,
                PasswordHash = _security.HashPassword(DevelopmentCredential),
                Role = (int)account.Role,
                CreatedAtUtc = now,
                EmailVerifiedAtUtc = now,
                RecoveryCodeHashesJson = "[]"
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Development player and test accounts are available.");
    }

    private static PlayerProfileEntity CreateDemoPlayer(DateTimeOffset now) => new()
    {
        Id = DemoPlayerId,
        AuthSubjectId = null,
        DisplayName = "Voyageur",
        TotalRunsStarted = 0,
        TotalRunsCompleted = 0,
        TotalRunsFailed = 0,
        TotalRunsAbandoned = 0,
        CreatedAtUtc = now,
        UpdatedAtUtc = now,
        Characters =
        [
            new PlayerCharacterEntity
            {
                Id = DefaultCharacterId,
                PlayerProfileId = DemoPlayerId,
                DefinitionKey = "character.player.self",
                DisplayName = "L'Aventurier",
                CharacterType = "Standard",
                Status = "Active",
                MaxVitality = 100,
                BaseMana = 0,
                BaseCharge = 0,
                SkillKeysJson = """["skill.basic.strike","skill.basic.guard"]""",
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                StatBlock = new PlayerCharacterStatBlockEntity
                {
                    Id = StatBlockId,
                    PlayerCharacterId = DefaultCharacterId,
                    MaxVitality = 100,
                    AttackPower = 12,
                    Defense = 6,
                    StartingGuard = 0,
                    Speed = 10,
                    Initiative = 10,
                    Focus = 15,
                    // Mirrors PlayerCharacterStatBlock.CreateDefaultPorteur() — this demo
                    // seed builds its EF entity directly instead of going through that
                    // domain factory, so it must be kept in sync by hand. Mana = 85% of
                    // base MaxVitality; MagicAttack/MagicDefense = same 2:1 ratio as
                    // AttackPower/Defense, halved since the starter kit is physical-only.
                    Mana = 85,
                    Charge = 0,
                    MagicAttack = 6,
                    MagicDefense = 3
                },
                Skills =
                [
                    new PlayerCharacterSkillEntity
                    {
                        Id = StrikeSkillId,
                        PlayerCharacterId = DefaultCharacterId,
                        SkillDefinitionKey = "skill.basic.strike",
                        UnlockedAtUtc = now,
                        Source = "default"
                    },
                    new PlayerCharacterSkillEntity
                    {
                        Id = GuardSkillId,
                        PlayerCharacterId = DefaultCharacterId,
                        SkillDefinitionKey = "skill.basic.guard",
                        UnlockedAtUtc = now,
                        Source = "default"
                    }
                ]
            }
        ]
    };

    private sealed record DevelopmentAccountSeed(
        Guid IdentityId,
        Guid AccountId,
        string Email,
        string DisplayName,
        AccountRole Role);
}
