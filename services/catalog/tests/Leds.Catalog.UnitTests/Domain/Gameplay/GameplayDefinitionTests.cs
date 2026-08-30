using FluentAssertions;
using Leds.Catalog.Domain.Errors;
using Leds.Catalog.Domain.Gameplay;

namespace Leds.Catalog.UnitTests.Domain.Gameplay;

public sealed class GameplayDefinitionTests
{
    [Fact]
    public void EnemyStatBlock_Create_ShouldRequirePositiveMaxVitality()
    {
        var act = () => EnemyStatBlock.Create(0, 1, 0, 0, 1);

        act.Should().Throw<DomainException>()
            .WithMessage("Enemy max vitality must be greater than 0.");
    }

    [Fact]
    public void EffectSet_Create_ShouldOrderEffectDefinitions()
    {
        var second = EffectDefinition.Create("AddCurrentGuard", "Self", 5, "Flat", "CurrentCombat", "Additive", 2);
        var first = EffectDefinition.Create("DamageVitality", "SingleEnemy", 10, "Flat", "Immediate", "None", 1);

        var set = EffectSet.Create("effect.test", "Test", null, "alpha-0.7.1", [second, first]);

        set.Effects.Select(e => e.Order).Should().Equal(1, 2);
    }

    [Fact]
    public void RoomDefinition_Create_ShouldAllowRareCulturalEchoRoom()
    {
        var room = RoomDefinition.Create(
            "room.rare.creuset-equivalences",
            "Le Creuset des Équivalences",
            "Une salle où chaque puissance offerte exige une dette.",
            "CulturalEcho",
            "Rare",
            "Alchemical",
            isCulturalEcho: true);

        room.Key.Should().Be("room.rare.creuset-equivalences");
        room.IsCulturalEcho.Should().BeTrue();
    }
}
