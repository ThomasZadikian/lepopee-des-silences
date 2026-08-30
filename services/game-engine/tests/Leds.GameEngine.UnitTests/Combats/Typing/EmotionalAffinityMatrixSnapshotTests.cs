using FluentAssertions;
using Leds.GameEngine.Domain.Combats.Typing;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Persistence.Mappers;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Combats.Typing;

public sealed class EmotionalAffinityMatrixSnapshotTests
{
    [Fact]
    public void Existing_snapshot_should_not_change_when_a_new_catalog_version_is_created()
    {
        var original = Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create();
        var changedRules = original.Rules
            .Select(rule => rule.AttackingRegister == EmotionalType.Rupture
                && rule.DefendingRegister == EmotionalType.Effroi
                    ? rule with { Effectiveness = DamageEffectiveness.Weak, Multiplier = 1.5 }
                    : rule)
            .ToArray();
        var nextVersion = EmotionalAffinityMatrixSnapshot.Create("affinity-2.0.0", changedRules);

        original.Resolve(EmotionalType.Rupture, EmotionalType.Effroi)
            .Should().Be(DamageEffectiveness.Resistant);
        nextVersion.Resolve(EmotionalType.Rupture, EmotionalType.Effroi)
            .Should().Be(DamageEffectiveness.Weak);
        original.Version.Should().NotBe(nextVersion.Version);
    }

    [Fact]
    public void Snapshot_should_reject_any_matrix_that_does_not_contain_all_64_pairs()
    {
        var incomplete = Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create().Rules.Skip(1);

        var act = () => EmotionalAffinityMatrixSnapshot.Create("affinity-invalid", incomplete);

        act.Should().Throw<Exception>().WithMessage("*64*");
    }

    [Fact]
    public void Persisted_run_should_keep_its_matrix_version_and_rules()
    {
        var changedRules = Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create().Rules
            .Select(rule => rule.AttackingRegister == EmotionalType.Folie
                && rule.DefendingRegister == EmotionalType.Silence
                    ? rule with { Effectiveness = DamageEffectiveness.Immune, Multiplier = 0 }
                    : rule)
            .ToArray();
        var snapshotted = EmotionalAffinityMatrixSnapshot.Create("affinity-replay-2", changedRules);
        var run = Run.StartNew(
            Guid.NewGuid(), "seed-replay", "generator-1", "markov-1",
            TestGameEngineFactory.CreateThresholdRoom(), DateTimeOffset.UtcNow,
            emotionalAffinityMatrix: snapshotted);

        var rehydrated = RunPersistenceMapper.ToDomain(RunPersistenceMapper.ToEntity(run));

        rehydrated.EmotionalAffinityMatrix.Version.Should().Be("affinity-replay-2");
        rehydrated.EmotionalAffinityMatrix.Resolve(EmotionalType.Folie, EmotionalType.Silence)
            .Should().Be(DamageEffectiveness.Immune);
    }
}
