using FluentAssertions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Typing;
using Leds.GameEngine.UnitTests.Common;

namespace Leds.GameEngine.UnitTests.Combats.Dtos;

public sealed class CombatantRuntimeDtoAffinityTests
{
    [Fact]
    public void Projection_should_expose_the_complete_effective_affinity_profile()
    {
        var combatant = Combatant.CreateEnemy(
            "enemy.memory",
            "Mémoire hostile",
            "Caster",
            100,
            naturalEmotionalType: EmotionalType.Memoire);
        combatant.ApplyEmotionalAffinityModifier(EmotionalAffinityModifier.Create(
            "item.memory-ward",
            EmotionalType.Deni,
            multiplierPercent: -20));

        var dto = CombatantRuntimeDto.FromDomain(
            combatant,
            currentTick: 0,
            TestEmotionalAffinityMatrix.Create());

        dto.NaturalEmotionalRegister.Should().Be("memoire");
        dto.EffectiveAttackRegister.Should().Be("memoire");
        dto.IncomingAffinities.Should().HaveCount(Enum.GetValues<EmotionalType>().Length);

        var denial = dto.IncomingAffinities.Single(affinity => affinity.IncomingRegister == "deni");
        denial.Outcome.Should().Be(DamageEffectiveness.Weak.ToString());
        denial.BaseMultiplier.Should().Be(1.5);
        denial.ModifierPercent.Should().Be(-20);
        denial.EffectiveMultiplier.Should().BeApproximately(1.2, 0.0001);
        denial.Modifiers.Should().ContainSingle(modifier => modifier.SourceKey == "item.memory-ward");
    }
}
