using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.UnitTests.Combats;

public sealed class DamageResolverTests
{
    private readonly DamageResolver _sut = new();

    [Fact]
    public void ResolveBasicAttack_ShouldCalculateDamage_AsAttackMinusDefense()
    {
        var attacker = CombatantSnapshot.Create("attacker", "Attacker", CombatantSide.Player, 100, 20, 5, 10);
        var defender = CombatantSnapshot.Create("defender", "Defender", CombatantSide.Enemy, 80, 10, 5, 8);

        var result = _sut.ResolveBasicAttack(attacker, defender);

        result.Damage.Should().Be(15);
        result.RawPower.Should().Be(20);
        result.MitigatedByDefense.Should().Be(5);
    }

    [Fact]
    public void ResolveBasicAttack_ShouldReturnMinimumDamageOfOne()
    {
        var attacker = CombatantSnapshot.Create("attacker", "Attacker", CombatantSide.Player, 100, 3, 0, 10);
        var defender = CombatantSnapshot.Create("defender", "Defender", CombatantSide.Enemy, 80, 1, 10, 8);

        var result = _sut.ResolveBasicAttack(attacker, defender);

        result.Damage.Should().Be(1);
    }

    [Fact]
    public void ResolveBasicAttack_ShouldThrow_WhenAttackerIsDefeated()
    {
        var attacker = CombatantSnapshot.Create(CombatantId.New(), "attacker", "Attacker", CombatantSide.Player, 100, 0, 20, 0, 10);
        var defender = CombatantSnapshot.Create("defender", "Defender", CombatantSide.Enemy, 80, 10, 5, 8);

        var act = () => _sut.ResolveBasicAttack(attacker, defender);

        act.Should().Throw<DomainException>().WithMessage("Defeated combatant cannot attack.");
    }

    [Fact]
    public void ResolveBasicAttack_ShouldThrow_WhenDefenderIsDefeated()
    {
        var attacker = CombatantSnapshot.Create("attacker", "Attacker", CombatantSide.Player, 100, 20, 0, 10);
        var defender = CombatantSnapshot.Create(CombatantId.New(), "defender", "Defender", CombatantSide.Enemy, 80, 0, 10, 5, 8);

        var act = () => _sut.ResolveBasicAttack(attacker, defender);

        act.Should().Throw<DomainException>().WithMessage("Defeated combatant cannot be targeted.");
    }

    [Fact]
    public void ResolveBasicAttack_ShouldThrow_WhenAttackerIsNull()
    {
        var defender = CombatantSnapshot.Create("defender", "Defender", CombatantSide.Enemy, 80, 10, 5, 8);

        var act = () => _sut.ResolveBasicAttack(null!, defender);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ResolveBasicAttack_ShouldThrow_WhenDefenderIsNull()
    {
        var attacker = CombatantSnapshot.Create("attacker", "Attacker", CombatantSide.Player, 100, 20, 0, 10);

        var act = () => _sut.ResolveBasicAttack(attacker, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ResolveBasicAttack_ShouldReturnCorrectIds()
    {
        var attackerId = CombatantId.New();
        var defenderId = CombatantId.New();

        var attacker = CombatantSnapshot.Create(attackerId, "attacker", "Attacker", CombatantSide.Player, 100, 100, 15, 0, 10);
        var defender = CombatantSnapshot.Create(defenderId, "defender", "Defender", CombatantSide.Enemy, 80, 80, 10, 3, 8);

        var result = _sut.ResolveBasicAttack(attacker, defender);

        result.AttackerId.Should().Be(attackerId);
        result.DefenderId.Should().Be(defenderId);
    }

    [Fact]
    public void ResolveBasicAttack_ShouldHandleZeroDefense()
    {
        var attacker = CombatantSnapshot.Create("attacker", "Attacker", CombatantSide.Player, 100, 25, 0, 10);
        var defender = CombatantSnapshot.Create("defender", "Defender", CombatantSide.Enemy, 80, 10, 0, 8);

        var result = _sut.ResolveBasicAttack(attacker, defender);

        result.Damage.Should().Be(25);
        result.MitigatedByDefense.Should().Be(0);
    }
}
