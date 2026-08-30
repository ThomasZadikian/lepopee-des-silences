using FluentAssertions;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

/// <summary>
/// Cumul cap — at most <c>1 + profondeur/2</c> (capped at 6) active laws, where "profondeur"
/// is <see cref="Run.CurrentRoomIndex"/>. <see cref="Run.EnforceCumulCap"/> revokes the oldest
/// active law (by <see cref="ActivePalaceLaw.AppliedAtUtc"/>), skipping room-linked
/// (<see cref="ActivePalaceLaw.IsCumulExempt"/>) laws entirely — both as revocation candidates
/// and as part of the count compared against the cap.
/// </summary>
public sealed class RunCumulCapTests
{
    private static PalaceLaw CreateLaw(string key, bool isCumulExempt = false) => PalaceLaw.Create(
        key, "Loi de cumul", "1.0.0",
        domains: [PalaceLawDomain.Combat],
        effects:
        [
            PalaceLawEffect.Create(
                RunModifierType.PermanentCombatDifficultyBonus,
                value: 0.10,
                RunModifierDuration.UntilRunEnds),
        ],
        isCumulExempt: isCumulExempt);

    [Fact]
    public void PromulgateLaw_ShouldRevokeTheOldestActiveLaw_WhenCumulCapIsExceeded()
    {
        // CurrentRoomIndex is 0 at start of run, so the cap is 1 + 0/2 = 1.
        var run = TestGameEngineFactory.CreateRun();
        run.PromulgateLaw(CreateLaw("law-first"));

        run.PromulgateLaw(CreateLaw("law-second"));

        run.ActivePalaceLaws.Should().ContainSingle(activeLaw => activeLaw.Key == "law-second");
        run.RunModifiers.Where(m => m.SourceKey == "law-first").Should().OnlyContain(m => m.IsConsumed);
        run.RunModifiers.Where(m => m.SourceKey == "law-second").Should().OnlyContain(m => !m.IsConsumed);
    }

    [Fact]
    public void EnforceCumulCap_ShouldNeverRevoke_ACumulExemptLaw()
    {
        // Cap is 1 at CurrentRoomIndex 0 — a second, non-exempt law would normally be rejected
        // by revoking the first, but a cumul-exempt (room-linked) law is never a candidate.
        var run = TestGameEngineFactory.CreateRun();
        run.PromulgateLaw(CreateLaw("law-room-linked", isCumulExempt: true));

        run.PromulgateLaw(CreateLaw("law-ordinary"));

        run.ActivePalaceLaws.Select(activeLaw => activeLaw.Key)
            .Should().BeEquivalentTo(["law-room-linked", "law-ordinary"],
                because: "the room-linked law is exempt from the cumul cap entirely — it doesn't " +
                         "count toward it and can never be picked for revocation.");
    }

    [Fact]
    public void EnforceCumulCap_ShouldNotRevokeAnything_WhenActiveCountIsWithinTheCap()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.PromulgateLaw(CreateLaw("law-only"));

        run.EnforceCumulCap();

        run.ActivePalaceLaws.Should().ContainSingle(activeLaw => activeLaw.Key == "law-only");
        run.RunModifiers.Should().OnlyContain(m => !m.IsConsumed);
    }
}
