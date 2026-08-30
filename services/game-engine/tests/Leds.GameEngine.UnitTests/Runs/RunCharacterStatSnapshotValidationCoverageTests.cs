using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class RunCharacterStatSnapshotValidationCoverageTests
{
    [Fact]
    public void Create_ShouldValidateEveryCombatStatAndPreserveValidValues()
    {
        AssertCreateInvalid(0, 10, 0, 0, 10, 0, 0, 0, 0, 0, 0, 4, "Max vitality");
        AssertCreateInvalid(100, 0, 0, 0, 10, 0, 0, 0, 0, 0, 0, 4, "Attack power");
        AssertCreateInvalid(100, 10, -1, 0, 10, 0, 0, 0, 0, 0, 0, 4, "Defense");
        AssertCreateInvalid(100, 10, 0, -1, 10, 0, 0, 0, 0, 0, 0, 4, "Starting guard");
        AssertCreateInvalid(100, 10, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4, "Speed");
        AssertCreateInvalid(100, 10, 0, 0, 10, -1, 0, 0, 0, 0, 0, 4, "Initiative");
        AssertCreateInvalid(100, 10, 0, 0, 10, 0, -1, 0, 0, 0, 0, 4, "Focus");
        AssertCreateInvalid(100, 10, 0, 0, 10, 0, 0, -1, 0, 0, 0, 4, "Mana");
        AssertCreateInvalid(100, 10, 0, 0, 10, 0, 0, 0, -1, 0, 0, 4, "Charge");
        AssertCreateInvalid(100, 10, 0, 0, 10, 0, 0, 0, 0, -1, 0, 4, "Magic attack");
        AssertCreateInvalid(100, 10, 0, 0, 10, 0, 0, 0, 0, 0, -1, 4, "Magic defense");
        AssertCreateInvalid(100, 10, 0, 0, 10, 0, 0, 0, 0, 0, 0, 0, "Movement");

        var snapshot = RunCharacterStatSnapshot.Create(120, 15, 6, 3, 12, 8, 4, 20, 2, 7, 9, 5);
        snapshot.MaxVitality.Should().Be(120);
        snapshot.AttackPower.Should().Be(15);
        snapshot.MagicAttack.Should().Be(7);
        snapshot.MagicDefense.Should().Be(9);
        snapshot.Movement.Should().Be(5);
    }

    [Fact]
    public void ReplaceStats_ShouldValidateEveryCombatStatAndReplaceValidValues()
    {
        var snapshot = RunCharacterStatSnapshot.CreateDefault();

        AssertReplaceInvalid(snapshot, 0, 10, 0, 0, 10, 0, 0, 0, 0, 0, 0, 4, "Max vitality");
        AssertReplaceInvalid(snapshot, 100, 0, 0, 0, 10, 0, 0, 0, 0, 0, 0, 4, "Attack power");
        AssertReplaceInvalid(snapshot, 100, 10, -1, 0, 10, 0, 0, 0, 0, 0, 0, 4, "Defense");
        AssertReplaceInvalid(snapshot, 100, 10, 0, -1, 10, 0, 0, 0, 0, 0, 0, 4, "Starting guard");
        AssertReplaceInvalid(snapshot, 100, 10, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4, "Speed");
        AssertReplaceInvalid(snapshot, 100, 10, 0, 0, 10, -1, 0, 0, 0, 0, 0, 4, "Initiative");
        AssertReplaceInvalid(snapshot, 100, 10, 0, 0, 10, 0, -1, 0, 0, 0, 0, 4, "Focus");
        AssertReplaceInvalid(snapshot, 100, 10, 0, 0, 10, 0, 0, -1, 0, 0, 0, 4, "Mana");
        AssertReplaceInvalid(snapshot, 100, 10, 0, 0, 10, 0, 0, 0, -1, 0, 0, 4, "Charge");
        AssertReplaceInvalid(snapshot, 100, 10, 0, 0, 10, 0, 0, 0, 0, -1, 0, 4, "Magic attack");
        AssertReplaceInvalid(snapshot, 100, 10, 0, 0, 10, 0, 0, 0, 0, 0, -1, 4, "Magic defense");
        AssertReplaceInvalid(snapshot, 100, 10, 0, 0, 10, 0, 0, 0, 0, 0, 0, 0, "Movement");

        snapshot.ReplaceStats(140, 20, 8, 4, 14, 9, 6, 30, 3, 11, 12, 6);
        snapshot.MaxVitality.Should().Be(140);
        snapshot.AttackPower.Should().Be(20);
        snapshot.MagicAttack.Should().Be(11);
        snapshot.MagicDefense.Should().Be(12);
        snapshot.Movement.Should().Be(6);
    }

    [Fact]
    public void Rehydrate_ShouldRetainTrustedValues()
    {
        var id = Guid.NewGuid();
        var snapshot = RunCharacterStatSnapshot.Rehydrate(id, 80, 9, 4, 2, 7, 3, 1, 5, 1, 2, 3, 2);
        snapshot.Id.Should().Be(id);
        snapshot.MaxVitality.Should().Be(80);
        snapshot.Movement.Should().Be(2);
    }

    private static void AssertCreateInvalid(int hp, int attack, int defense, int guard, int speed, int initiative,
        int focus, int mana, int charge, int magicAttack, int magicDefense, int movement, string message) =>
        FluentActions.Invoking(() => RunCharacterStatSnapshot.Create(
                hp, attack, defense, guard, speed, initiative, focus, mana, charge, magicAttack, magicDefense, movement))
            .Should().Throw<DomainException>().WithMessage($"*{message}*");

    private static void AssertReplaceInvalid(RunCharacterStatSnapshot snapshot, int hp, int attack, int defense,
        int guard, int speed, int initiative, int focus, int mana, int charge, int magicAttack, int magicDefense,
        int movement, string message) =>
        FluentActions.Invoking(() => snapshot.ReplaceStats(
                hp, attack, defense, guard, speed, initiative, focus, mana, charge, magicAttack, magicDefense, movement))
            .Should().Throw<DomainException>().WithMessage($"*{message}*");
}
