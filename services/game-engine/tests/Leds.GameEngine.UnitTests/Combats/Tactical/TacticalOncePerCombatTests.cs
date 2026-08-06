using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Persistence.Mappers;

namespace Leds.GameEngine.UnitTests.Combats.Tactical;

public sealed class TacticalOncePerCombatTests
{
    [Fact]
    public void MarkOnceSkillUsed_ShouldRejectASecondUse()
    {
        var combat = CreateCombat();

        combat.MarkOnceSkillUsed("canon.skill.silence-partage");

        combat.HasUsedOnceSkill("canon.skill.silence-partage").Should().BeTrue();
        var act = () => combat.MarkOnceSkillUsed("canon.skill.silence-partage");
        act.Should().Throw<DomainException>().WithMessage("*already been used*");
    }

    [Fact]
    public void UsedOnceSkill_ShouldSurvivePersistenceRoundTrip()
    {
        var combat = CreateCombat();
        combat.MarkOnceSkillUsed("canon.skill.silence-partage");

        var reloaded = TacticalCombatPersistenceMapper.ToDomain(
            TacticalCombatPersistenceMapper.ToEntity(combat, combat.RunId.Value));

        reloaded.HasUsedOnceSkill("canon.skill.silence-partage").Should().BeTrue();
    }

    private static TacticalCombat CreateCombat()
    {
        var battlefield = TacticalBattlefield.Rehydrate(
            width: 2,
            height: 1,
            elevation: [0, 0],
            walkable: [true, true],
            isFloor: [true, true]);
        var ally = Combatant.CreateAlly("player.self", "Porteur", "Porteur", 40);
        var enemy = Combatant.CreateEnemy("enemy.test", "Écho", "Bruiser", 20);

        return TacticalCombat.Create(
            CombatId.New(),
            new RunId(Guid.NewGuid()),
            new RoomId(Guid.NewGuid()),
            new NodeId(Guid.NewGuid()),
            battlefield,
            [(ally, new GridPosition(0, 0))],
            [(enemy, new GridPosition(1, 0))],
            DateTime.UtcNow,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());
    }
}
