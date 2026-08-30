using FluentAssertions;
using Leds.GameEngine.Domain.Combats.Tactical;

namespace Leds.GameEngine.UnitTests.Combats.Tactical;

/// <summary>
/// A single step of elevation between two adjacent cells must never block a melee attack (range
/// 1) — only a real cliff, two steps or more, should. Regression coverage for the bug where any
/// non-zero elevation difference was added in full to the Manhattan distance, so a basic attack
/// (range 1) against an adjacent target one level lower/higher failed with "out of range" even
/// though the two cells were side by side.
/// </summary>
public sealed class TacticalTargetingIsInRangeTests
{
    private static TacticalBattlefield BattlefieldWithElevations(params int[] elevations) =>
        TacticalBattlefield.Rehydrate(
            elevations.Length,
            1,
            elevations,
            Enumerable.Repeat(true, elevations.Length).ToArray());

    [Fact]
    public void MeleeRange_ShouldReachAnAdjacentTarget_OneLevelLower()
    {
        var battlefield = BattlefieldWithElevations(1, 0);

        TacticalTargeting.IsInRange(
            battlefield, new GridPosition(0, 0), new GridPosition(1, 0),
            range: 1, requiresLineOfSight: false).Should().BeTrue();
    }

    [Fact]
    public void MeleeRange_ShouldReachAnAdjacentTarget_OneLevelHigher()
    {
        var battlefield = BattlefieldWithElevations(0, 1);

        TacticalTargeting.IsInRange(
            battlefield, new GridPosition(0, 0), new GridPosition(1, 0),
            range: 1, requiresLineOfSight: false).Should().BeTrue();
    }

    [Fact]
    public void MeleeRange_ShouldStillBlockAnAdjacentTarget_TwoLevelsLower()
    {
        var battlefield = BattlefieldWithElevations(2, 0);

        TacticalTargeting.IsInRange(
            battlefield, new GridPosition(0, 0), new GridPosition(1, 0),
            range: 1, requiresLineOfSight: false).Should().BeFalse();
    }

    [Fact]
    public void MeleeRange_ShouldStillBlockAnAdjacentTarget_TwoLevelsHigher()
    {
        var battlefield = BattlefieldWithElevations(0, 2);

        TacticalTargeting.IsInRange(
            battlefield, new GridPosition(0, 0), new GridPosition(1, 0),
            range: 1, requiresLineOfSight: false).Should().BeFalse();
    }

    [Fact]
    public void RangedSkill_OneLevelOfElevation_CostsNoRangeEitherWay()
    {
        // Two cells two apart (Manhattan distance 2) with a single step of elevation must
        // still fit inside range 2 — the first level of height was never meant to cost range.
        var battlefield = BattlefieldWithElevations(1, 0, 0);

        TacticalTargeting.IsInRange(
            battlefield, new GridPosition(0, 0), new GridPosition(2, 0),
            range: 2, requiresLineOfSight: false).Should().BeTrue();
    }

    [Fact]
    public void SameElevation_IsUnaffected()
    {
        var battlefield = BattlefieldWithElevations(0, 0);

        TacticalTargeting.IsInRange(
            battlefield, new GridPosition(0, 0), new GridPosition(1, 0),
            range: 1, requiresLineOfSight: false).Should().BeTrue();
    }
}
