using FluentAssertions;
using Leds.Player.Infrastructure.Persistence;
using Leds.Player.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Player.UnitTests.Persistence;

[Collection("PlayerPostgres")]
public sealed class PlayerDbContextModelTests
{
    private readonly PlayerPostgresFixture _fixture;

    public PlayerDbContextModelTests(PlayerPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Model_ShouldExposeDataModelPlayerTables()
    {
        using var context = _fixture.CreateContext().Context;

        context.Model.FindEntityType(typeof(PlayerCharacterStatBlockEntity))!.GetTableName().Should().Be("player_character_stat_blocks");
        context.Model.FindEntityType(typeof(PlayerCharacterSkillEntity))!.GetTableName().Should().Be("player_character_skills");
        context.Model.FindEntityType(typeof(PlayerPermanentUnlockEntity))!.GetTableName().Should().Be("player_permanent_unlocks");
        context.Model.FindEntityType(typeof(PlayerRunStatisticEntity))!.GetTableName().Should().Be("player_run_statistics");
        context.Model.FindEntityType(typeof(ProcessedIntegrationEventEntity))!.GetTableName().Should().Be("player_processed_integration_events");
    }

    [Fact]
    public void Model_ShouldKeepStatBlockAsChildOneToOne()
    {
        using var context = _fixture.CreateContext().Context;
        var character = context.Model.FindEntityType(typeof(PlayerCharacterEntity))!;
        var statBlock = context.Model.FindEntityType(typeof(PlayerCharacterStatBlockEntity))!;

        character.FindProperty("StatBlockId").Should().BeNull();

        var foreignKey = statBlock.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(PlayerCharacterEntity));
        foreignKey.Properties.Single().Name.Should().Be(nameof(PlayerCharacterStatBlockEntity.PlayerCharacterId));
        statBlock.GetIndexes().Single(i => i.Properties.Single().Name == nameof(PlayerCharacterStatBlockEntity.PlayerCharacterId)).IsUnique.Should().BeTrue();
    }

    [Fact]
    public async Task Context_ShouldPersistPermanentProgressionRows()
    {
        var (context, _) = _fixture.CreateContext();
        await using var _ = context;
        var profileId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        var runId = Guid.NewGuid();

        context.PlayerProfiles.Add(new PlayerProfileEntity
        {
            Id = profileId,
            DisplayName = "Test",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            Characters =
            [
                new PlayerCharacterEntity
                {
                    Id = characterId,
                    DefinitionKey = "character.player.self",
                    DisplayName = "Le Porteur",
                    CharacterType = "Standard",
                    Status = "Active",
                    SkillKeysJson = "[]",
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    UpdatedAtUtc = DateTimeOffset.UtcNow,
                    StatBlock = new PlayerCharacterStatBlockEntity { Id = Guid.NewGuid(), MaxVitality = 100, AttackPower = 12, Defense = 6, Speed = 10 },
                    Skills =
                    [
                        new PlayerCharacterSkillEntity { Id = Guid.NewGuid(), SkillDefinitionKey = "skill.basic.strike", Source = "default", UnlockedAtUtc = DateTimeOffset.UtcNow }
                    ]
                }
            ],
            PermanentUnlocks =
            [
                new PlayerPermanentUnlockEntity { Id = Guid.NewGuid(), UnlockKey = "unlock.skill.guard", UnlockType = "Skill", UnlockedAtUtc = DateTimeOffset.UtcNow }
            ],
            RunStatistics =
            [
                new PlayerRunStatisticEntity { Id = Guid.NewGuid(), RunId = runId, Seed = "seed", FinalDepth = 1, Outcome = "Completed", GeneratorVersion = "gen" }
            ]
        });

        await context.SaveChangesAsync();

        (await context.PlayerCharacterStatBlocks.CountAsync()).Should().Be(1);
        (await context.PlayerCharacterSkills.CountAsync()).Should().Be(1);
        (await context.PlayerPermanentUnlocks.CountAsync()).Should().Be(1);
        (await context.PlayerRunStatistics.CountAsync()).Should().Be(1);
    }
}
