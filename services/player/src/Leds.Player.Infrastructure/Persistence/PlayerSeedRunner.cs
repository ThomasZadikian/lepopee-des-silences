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

    private readonly PlayerDbContext _context;
    private readonly ILogger<PlayerSeedRunner> _logger;

    public PlayerSeedRunner(PlayerDbContext context, ILogger<PlayerSeedRunner> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ApplyDemoPlayerSeedAsync(CancellationToken cancellationToken = default)
    {
        var exists = await _context.PlayerProfiles.AnyAsync(p => p.Id == DemoPlayerId, cancellationToken);

        if (exists)
        {
            _logger.LogInformation("Demo player already exists. Skipping seed.");
            return;
        }

        _logger.LogInformation("Demo player not found. Creating demo player...");

        var now = DateTimeOffset.UtcNow;

        _context.PlayerProfiles.Add(new PlayerProfileEntity
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
                        Recovery = 5,
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
        });

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Demo player created with Id {PlayerId}.", DemoPlayerId);
    }
}
