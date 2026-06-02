using FluentAssertions;
using Leds.Catalog.Domain.Combat;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Enemies;
using Leds.Catalog.Domain.Errors;

namespace Leds.Catalog.UnitTests.Application.Enemies;

public sealed class EnemyTemplateTests
{
    [Fact]
    public void Create_ShouldCreateEnemyTemplate_WhenInputIsValid()
    {
        var enemy = CreateEnemyTemplate();

        enemy.Id.Value.Should().NotBeEmpty();
        enemy.Key.Value.Should().Be("enemy-shadow-wolf");
        enemy.Name.Value.Should().Be("Loup d’Ombre");
        enemy.Description.Value.Should().Be("Une entité née dans un couloir du Palais.");
        enemy.Version.Value.Should().Be("catalog-0.1.0");
        enemy.Status.Should().Be(CatalogContentStatus.Draft);
        enemy.Archetype.Should().Be(EnemyArchetype.Trauma);
        enemy.Element.Should().Be(CombatElement.Shadow);
        enemy.MaxHealth.Should().Be(120);
        enemy.Strength.Should().Be(18);
        enemy.Intelligence.Should().Be(8);
        enemy.Speed.Should().Be(14);
        enemy.PhysicalResistance.Should().Be(10);
        enemy.MagicalResistance.Should().Be(5);
        enemy.ExperienceReward.Should().Be(25);
        enemy.GoldReward.Should().Be(12);
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenArchetypeIsUnknown()
    {
        var act = () => EnemyTemplate.Create(
            "enemy-shadow-wolf",
            "Loup d’Ombre",
            "Description",
            "catalog-0.1.0",
            EnemyArchetype.Unknown,
            CombatElement.Shadow,
            120,
            18,
            8,
            14,
            10,
            5,
            25,
            12);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Enemy archetype is required.");
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenMaxHealthIsZero()
    {
        var act = () => EnemyTemplate.Create(
            "enemy-shadow-wolf",
            "Loup d’Ombre",
            "Description",
            "catalog-0.1.0",
            EnemyArchetype.Trauma,
            CombatElement.Shadow,
            0,
            18,
            8,
            14,
            10,
            5,
            25,
            12);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Enemy max health must be greater than 0.");
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenResistanceIsOutOfRange()
    {
        var act = () => EnemyTemplate.Create(
            "enemy-shadow-wolf",
            "Loup d’Ombre",
            "Description",
            "catalog-0.1.0",
            EnemyArchetype.Trauma,
            CombatElement.Shadow,
            120,
            18,
            8,
            14,
            101,
            5,
            25,
            12);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Enemy physical resistance must be between 0 and 100.");
    }

    private static EnemyTemplate CreateEnemyTemplate()
    {
        return EnemyTemplate.Create(
            "enemy-shadow-wolf",
            "Loup d’Ombre",
            "Une entité née dans un couloir du Palais.",
            "catalog-0.1.0",
            EnemyArchetype.Trauma,
            CombatElement.Shadow,
            120,
            18,
            8,
            14,
            10,
            5,
            25,
            12);
    }
}