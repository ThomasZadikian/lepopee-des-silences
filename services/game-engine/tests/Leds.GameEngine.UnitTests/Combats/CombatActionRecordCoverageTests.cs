using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.UnitTests.Combats;

public sealed class CombatActionRecordCoverageTests
{
    [Fact]
    public void Create_ShouldValidateRequiredIdentityFields()
    {
        var args = ValidArgs();
        AssertInvalid(Guid.Empty, args.Turn, args.ActorId, args.Side, args.SkillKey, "Combat id");
        AssertInvalid(args.CombatId, 0, args.ActorId, args.Side, args.SkillKey, "Turn number");
        AssertInvalid(args.CombatId, args.Turn, Guid.Empty, args.Side, args.SkillKey, "Actor id");
        AssertInvalid(args.CombatId, args.Turn, args.ActorId, " ", args.SkillKey, "Actor side");
        AssertInvalid(args.CombatId, args.Turn, args.ActorId, args.Side, " ", "Skill key");
    }

    [Fact]
    public void Create_ShouldNormalizeNegativeMetricsAndNullCollections()
    {
        var args = ValidArgs();
        var record = CombatActionRecord.Create(
            args.CombatId, args.Turn, args.ActorId, args.Side, args.SkillKey, "Skill",
            targetIds: null!,
            rawDamage: -1,
            mitigatedDamage: -2,
            vitalityDamage: -3,
            guardDamage: -4,
            guardAbsorbed: -5,
            guardGained: -6,
            healingDone: -7,
            healingReceived: -8,
            occurredAtUtc: DateTime.UnixEpoch,
            effectsApplied: null);

        record.TargetIds.Should().BeEmpty();
        record.EffectsApplied.Should().BeEmpty();
        record.RawDamage.Should().Be(0);
        record.MitigatedDamage.Should().Be(0);
        record.VitalityDamage.Should().Be(0);
        record.GuardDamage.Should().Be(0);
        record.GuardAbsorbed.Should().Be(0);
        record.GuardGained.Should().Be(0);
        record.DamageDealt.Should().Be(0);
        record.DamageTaken.Should().Be(0);
        record.HealingDone.Should().Be(0);
        record.HealingReceived.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldComputeDamageAndPreserveOptionalMetadata()
    {
        var args = ValidArgs();
        var target = Guid.NewGuid();
        var record = CombatActionRecord.Create(
            args.CombatId, args.Turn, args.ActorId, args.Side, args.SkillKey, "Strike",
            [target], rawDamage: 20, mitigatedDamage: 5, vitalityDamage: 9, guardDamage: 6,
            guardAbsorbed: 6, guardGained: 2, healingDone: 3, healingReceived: 4,
            occurredAtUtc: DateTime.UnixEpoch, sourceType: "skill", sourceKey: "skill.test", effectsApplied: ["bleed"]);

        record.DamageDealt.Should().Be(15);
        record.DamageTaken.Should().Be(15);
        record.TargetIds.Should().ContainSingle().Which.Should().Be(target);
        record.EffectsApplied.Should().ContainSingle().Which.Should().Be("bleed");
        record.SourceType.Should().Be("skill");
        record.SourceKey.Should().Be("skill.test");
    }

    [Fact]
    public void Rehydrate_ShouldKeepTrustedPersistedMetrics()
    {
        var id = Guid.NewGuid();
        var combatId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var record = CombatActionRecord.Rehydrate(
            id, combatId, 4, actorId, "Enemy", "skill.persisted", "Persisted", [targetId],
            "item", "item.test", -1, -2, -3, -4, -5, -6, -7, -8, -9, -10, ["x"], DateTime.UnixEpoch);

        record.Id.Should().Be(id);
        record.RawDamage.Should().Be(-1);
        record.HealingReceived.Should().Be(-10);
        record.TargetIds.Should().ContainSingle().Which.Should().Be(targetId);
    }

    private static (Guid CombatId, int Turn, Guid ActorId, string Side, string SkillKey) ValidArgs() =>
        (Guid.NewGuid(), 1, Guid.NewGuid(), "Player", "skill.test");

    private static void AssertInvalid(Guid combatId, int turn, Guid actorId, string side, string skillKey, string message) =>
        FluentActions.Invoking(() => CombatActionRecord.Create(
                combatId, turn, actorId, side, skillKey, "Skill", [], 0, 0, 0, 0, 0, 0, 0, 0, DateTime.UtcNow))
            .Should().Throw<DomainException>().WithMessage($"*{message}*");
}
