using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.UnitTests.Combats;

public sealed class CombatantBaseStatSnapshotCoverageTests
{
    [Fact]
    public void Create_ShouldValidateEveryStat()
    {
        AssertInvalid(0, 0, 0, 0, 10, 0, 0, 0, 0, 0, 0, 4, "Max vitality");
        AssertInvalid(100, -1, 0, 0, 10, 0, 0, 0, 0, 0, 0, 4, "Attack power");
        AssertInvalid(100, 0, -1, 0, 10, 0, 0, 0, 0, 0, 0, 4, "Defense");
        AssertInvalid(100, 0, 0, -1, 10, 0, 0, 0, 0, 0, 0, 4, "Starting guard");
        AssertInvalid(100, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4, "Speed");
        AssertInvalid(100, 0, 0, 0, 10, -1, 0, 0, 0, 0, 0, 4, "Initiative");
        AssertInvalid(100, 0, 0, 0, 10, 0, -1, 0, 0, 0, 0, 4, "Focus");
        AssertInvalid(100, 0, 0, 0, 10, 0, 0, -1, 0, 0, 0, 4, "Mana");
        AssertInvalid(100, 0, 0, 0, 10, 0, 0, 0, -1, 0, 0, 4, "Charge");
        AssertInvalid(100, 0, 0, 0, 10, 0, 0, 0, 0, -1, 0, 4, "Magic attack");
        AssertInvalid(100, 0, 0, 0, 10, 0, 0, 0, 0, 0, -1, 4, "Magic defense");
        AssertInvalid(100, 0, 0, 0, 10, 0, 0, 0, 0, 0, 0, 0, "Movement");

        var snapshot = CombatantBaseStatSnapshot.Create(100, 0, 4, 2, 10, 5, 3, 20, 1, 6, 7, 5);
        snapshot.MaxVitality.Should().Be(100);
        snapshot.AttackPower.Should().Be(0);
        snapshot.MagicAttack.Should().Be(6);
        snapshot.MagicDefense.Should().Be(7);
        snapshot.Movement.Should().Be(5);
    }

    [Fact]
    public void Rehydrate_ShouldKeepTrustedIdentityAndValues()
    {
        var id = Guid.NewGuid();
        var created = DateTime.UnixEpoch;
        var snapshot = CombatantBaseStatSnapshot.Rehydrate(
            id, 90, 8, 3, 1, 9, 4, 2, 10, 1, created, 5, 6, 3);

        snapshot.Id.Should().Be(id);
        snapshot.CreatedAtUtc.Should().Be(created);
        snapshot.MaxVitality.Should().Be(90);
        snapshot.Movement.Should().Be(3);
    }

    private static void AssertInvalid(int hp, int attack, int defense, int guard, int speed, int initiative,
        int focus, int mana, int charge, int magicAttack, int magicDefense, int movement, string message) =>
        FluentActions.Invoking(() => CombatantBaseStatSnapshot.Create(
                hp, attack, defense, guard, speed, initiative, focus, mana, charge, magicAttack, magicDefense, movement))
            .Should().Throw<DomainException>().WithMessage($"*{message}*");
}
