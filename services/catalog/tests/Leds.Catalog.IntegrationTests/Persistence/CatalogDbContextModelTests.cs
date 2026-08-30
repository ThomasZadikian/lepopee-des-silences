using FluentAssertions;
using Leds.Catalog.Infrastructure.Persistence;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.IntegrationTests.Persistence;

[Collection("CatalogPostgres")]
public sealed class CatalogDbContextModelTests
{
    private readonly CatalogPostgresFixture _fixture;

    public CatalogDbContextModelTests(CatalogPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Model_ShouldExposeDataModelCatalogTables()
    {
        using var context = _fixture.CreateContext().Context;
        var model = context.Model;

        model.FindEntityType(typeof(EnemyStatBlockEntity))!.GetTableName().Should().Be("catalog_enemy_stat_blocks");
        model.FindEntityType(typeof(EffectSetEntity))!.GetTableName().Should().Be("catalog_effect_sets");
        model.FindEntityType(typeof(EffectDefinitionEntity))!.GetTableName().Should().Be("catalog_effect_definitions");
        model.FindEntityType(typeof(CurseDefinitionEntity))!.GetTableName().Should().Be("catalog_curse_definitions");
        model.FindEntityType(typeof(RewardTemplateEntity))!.GetTableName().Should().Be("catalog_reward_templates");
        model.FindEntityType(typeof(RoomDefinitionEntity))!.GetTableName().Should().Be("catalog_room_definitions");
        model.FindEntityType(typeof(RoomEnemyPoolEntity))!.GetTableName().Should().Be("catalog_room_enemy_pools");
        model.FindEntityType(typeof(CatalogTagEntity))!.GetTableName().Should().Be("catalog_tags");
    }

    [Fact]
    public void Model_ShouldKeepEnemyStatBlockAsChildOneToOne()
    {
        using var context = _fixture.CreateContext().Context;
        var enemy = context.Model.FindEntityType(typeof(EnemyDefinitionEntity))!;
        var statBlock = context.Model.FindEntityType(typeof(EnemyStatBlockEntity))!;

        enemy.FindProperty("StatBlockId").Should().BeNull();

        var foreignKey = statBlock.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(EnemyDefinitionEntity));
        foreignKey.Properties.Single().Name.Should().Be(nameof(EnemyStatBlockEntity.EnemyDefinitionId));
        statBlock.GetIndexes().Single(i => i.Properties.Single().Name == nameof(EnemyStatBlockEntity.EnemyDefinitionId)).IsUnique.Should().BeTrue();
    }

    [Fact]
    public void Model_ShouldUseEffectSetAsCanonicalEffectSource()
    {
        using var context = _fixture.CreateContext().Context;
        var tableNames = context.Model.GetEntityTypes().Select(t => t.GetTableName()).ToArray();

        tableNames.Should().Contain("catalog_effect_sets");
        tableNames.Should().Contain("catalog_effect_definitions");
        tableNames.Should().NotContain("catalog_skill_effects");
        tableNames.Should().NotContain("catalog_item_effects");
        tableNames.Should().NotContain("catalog_palace_law_effects");
        tableNames.Should().NotContain("catalog_curse_effects");
    }
}
