using FluentAssertions;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Enemies.Loot;
using Leds.Catalog.Domain.Errors;
using Leds.Catalog.Domain.Rewards.Loot;

namespace Leds.Catalog.UnitTests.Domain.Gameplay;

public sealed class LootPoolCoverageTests
{
    [Fact]
    public void Generic_loot_pool_should_accept_unique_entries_and_reject_invalid_collections()
    {
        var first = new LootEntry(" item.first ", 25);
        var second = new LootEntry("item.second", 100);

        var pool = GenericLootPool.Create(
            " loot.generic ", " Generic loot ", null, " 1.0 ", [first, second],
            CatalogContentStatus.Active);

        pool.Key.Value.Should().Be("loot.generic");
        pool.Name.Value.Should().Be("Generic loot");
        pool.Entries.Should().ContainInOrder(first, second);

        Action empty = () => GenericLootPool.Create("loot", "Loot", null, "1", []);
        Action duplicates = () => GenericLootPool.Create(
            "loot", "Loot", null, "1", [new LootEntry("same", 10), new LootEntry("same", 20)]);

        empty.Should().Throw<DomainException>();
        duplicates.Should().Throw<DomainException>();
    }

    [Fact]
    public void Enemy_loot_table_should_trim_enemy_key_and_reject_invalid_contract()
    {
        var first = new LootEntry("item.first", 25);
        var second = new LootEntry("item.second", 50);

        var table = EnemyLootTable.Create(
            " loot.enemy ", " Enemy loot ", "Drops", " 1.0 ", " enemy.alpha ",
            [first, second], CatalogContentStatus.Active);

        table.EnemyDefinitionKey.Should().Be("enemy.alpha");
        table.Entries.Should().ContainInOrder(first, second);

        Action missingEnemy = () => EnemyLootTable.Create("loot", "Loot", null, "1", " ", [first]);
        Action empty = () => EnemyLootTable.Create("loot", "Loot", null, "1", "enemy", []);
        Action duplicates = () => EnemyLootTable.Create(
            "loot", "Loot", null, "1", "enemy",
            [new LootEntry("same", 10), new LootEntry("same", 20)]);

        missingEnemy.Should().Throw<DomainException>();
        empty.Should().Throw<DomainException>();
        duplicates.Should().Throw<DomainException>();
    }
}
