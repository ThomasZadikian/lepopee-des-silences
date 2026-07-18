using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

/// <summary>
/// Ambient promulgation ("irréfusabilité") — <see cref="Run.PromulgateLaw"/> is the single
/// entry point through which a drawn law becomes active, applying the majeure-exclusivity
/// and exclusion-pairs rules. The Soupape rule (<see cref="Run.ShouldForceCompliantPromulgation"/>)
/// is a query the CALLER consults before drawing — it is not enforced inside this method.
/// </summary>
public sealed class RunPromulgationTests
{
    private static PalaceLaw CreateLaw(
        string key = "law-promulgation-v1",
        bool isMajeure = false,
        IReadOnlyCollection<string>? exclusionKeys = null) => PalaceLaw.Create(
        key, "Loi promulguée", "1.0.0",
        domains: [PalaceLawDomain.Combat],
        effects:
        [
            PalaceLawEffect.Create(
                RunModifierType.PermanentCombatDifficultyBonus,
                value: 0.10,
                RunModifierDuration.UntilRunEnds),
        ],
        isMajeure: isMajeure,
        exclusionKeys: exclusionKeys);

    private static PalaceLaw CreateLawWithPolarity(string key, string polarity) => PalaceLaw.Create(
        key, "Loi polarisée", "1.0.0",
        domains: [PalaceLawDomain.Combat],
        effects:
        [
            PalaceLawEffect.Create(
                RunModifierType.PermanentCombatDifficultyBonus,
                value: 0.05,
                RunModifierDuration.UntilRunEnds),
        ],
        polarity: polarity);

    [Fact]
    public void PromulgateLaw_ShouldActivateTheLaw_AndReturnTrue()
    {
        var run = TestGameEngineFactory.CreateRun();
        var law = CreateLaw();

        var result = run.PromulgateLaw(law);

        result.Should().BeTrue();
        run.ActivePalaceLaws.Should().ContainSingle(activeLaw => activeLaw.Key == law.Key);
    }

    [Fact]
    public void PromulgateLaw_ShouldSetLastPromulgationFloorIndex_ToCurrentFloor()
    {
        var run = TestGameEngineFactory.CreateRun();

        run.PromulgateLaw(CreateLaw());

        run.LastPromulgationFloorIndex.Should().Be(run.FloorIndex);
        run.FloorIndex.Should().Be(0, because: "CurrentRoomIndex is 0 at the start of a run.");
    }

    [Fact]
    public void PromulgateLaw_ShouldBeIdempotent_WhenTheLawIsAlreadyActive()
    {
        var run = TestGameEngineFactory.CreateRun();
        var law = CreateLaw();
        run.PromulgateLaw(law);

        var result = run.PromulgateLaw(law);

        result.Should().BeTrue();
        run.ActivePalaceLaws.Should().ContainSingle(activeLaw => activeLaw.Key == law.Key);
    }

    [Fact]
    public void PromulgateLaw_ShouldReject_WhenAnotherMajeureLawIsAlreadyActive()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.PromulgateLaw(CreateLaw("law-majeure-a", isMajeure: true));

        var result = run.PromulgateLaw(CreateLaw("law-majeure-b", isMajeure: true));

        result.Should().BeFalse();
        run.ActivePalaceLaws.Should().ContainSingle(activeLaw => activeLaw.Key == "law-majeure-a");
    }

    [Fact]
    public void PromulgateLaw_ShouldAllowASecondMajeureLaw_AfterTheFirstIsRevoked()
    {
        var run = TestGameEngineFactory.CreateRun(lawDenialEnabled: true);
        run.PromulgateLaw(CreateLaw("law-majeure-a", isMajeure: true));
        run.RemovePalaceLaw("law-majeure-a");

        var result = run.PromulgateLaw(CreateLaw("law-majeure-b", isMajeure: true));

        result.Should().BeTrue();
        run.ActivePalaceLaws.Should().ContainSingle(activeLaw => activeLaw.Key == "law-majeure-b");
    }

    [Fact]
    public void PromulgateLaw_ShouldReject_WhenExcludedByAnActiveLaw()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.PromulgateLaw(CreateLaw("law-troisieme-tasse"));

        var result = run.PromulgateLaw(CreateLaw(
            "law-souvenir-doux",
            exclusionKeys: ["law-troisieme-tasse"]));

        result.Should().BeFalse();
        run.ActivePalaceLaws.Should().ContainSingle(activeLaw => activeLaw.Key == "law-troisieme-tasse");
    }

    [Fact]
    public void PromulgateLaw_ShouldThrow_WhenRunIsClosed()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.CompleteRun(DateTimeOffset.UtcNow);

        var act = () => run.PromulgateLaw(CreateLaw());

        act.Should().Throw<DomainException>().WithMessage("*closed run*");
    }

    [Fact]
    public void ShouldForceCompliantPromulgation_ShouldBeFalse_WhenFewerThanThreeSevereLawsAreActive()
    {
        // The cumul cap (1 + profondeur/2) is only 1 at CurrentRoomIndex 0 — advance far enough
        // that 2-3 laws can coexist without EnforceCumulCap revoking the earlier ones.
        var run = TestGameEngineFactory.CreateRun();
        AdvanceRooms(run, 4);

        run.PromulgateLaw(CreateLawWithPolarity("law-severe-a", PalaceLawPolarity.Severe));
        run.PromulgateLaw(CreateLawWithPolarity("law-severe-b", PalaceLawPolarity.Severe));

        run.ShouldForceCompliantPromulgation.Should().BeFalse();
    }

    [Fact]
    public void ShouldForceCompliantPromulgation_ShouldBeTrue_WhenThreeOrMoreSevereLawsAreActive()
    {
        var run = TestGameEngineFactory.CreateRun();
        AdvanceRooms(run, 4);

        run.PromulgateLaw(CreateLawWithPolarity("law-severe-a", PalaceLawPolarity.Severe));
        run.PromulgateLaw(CreateLawWithPolarity("law-severe-b", PalaceLawPolarity.Severe));
        run.PromulgateLaw(CreateLawWithPolarity("law-severe-c", PalaceLawPolarity.Severe));

        run.ShouldForceCompliantPromulgation.Should().BeTrue();
    }

    [Fact]
    public void FloorIndex_ShouldAdvance_OnceTenRoomsHaveBeenTraversed()
    {
        var run = TestGameEngineFactory.CreateRun();

        AdvanceRooms(run, 10);

        run.CurrentRoomIndex.Should().Be(10);
        run.FloorIndex.Should().Be(1, because: "10 traversed rooms cross exactly one floor boundary.");
    }

    [Fact]
    public void ConsumeFloorEndModifiers_ShouldBeCalledAutomatically_WhenMoveToNextRoomCrossesAFloorBoundary()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.AddRunModifier(RunModifier.Create(
            RunModifierType.PermanentCombatDifficultyBonus,
            0.10,
            RunModifierDuration.UntilFloorEnds,
            sourceType: "PalaceLaw",
            sourceKey: "law-floor-scoped"));

        AdvanceRooms(run, 10);

        run.RunModifiers.Should().OnlyContain(modifier => modifier.IsConsumed);
    }

    [Fact]
    public void ConsumeFloorEndModifiers_ShouldNotConsume_WhileStillOnTheSameFloor()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.AddRunModifier(RunModifier.Create(
            RunModifierType.PermanentCombatDifficultyBonus,
            0.10,
            RunModifierDuration.UntilFloorEnds,
            sourceType: "PalaceLaw",
            sourceKey: "law-floor-scoped"));

        AdvanceRooms(run, 9);

        run.FloorIndex.Should().Be(0);
        run.RunModifiers.Should().OnlyContain(modifier => !modifier.IsConsumed);
    }

    private static void AdvanceRooms(Run run, int count)
    {
        for (var i = 0; i < count; i++)
        {
            AdvanceToNextRoom(run);
        }
    }

    private static void AdvanceToNextRoom(Run run)
    {
        while (run.Status == RunStatus.Active)
        {
            var node = run.CurrentRoom.AvailableNodes.First();

            run.ChooseNode(node.Id);
            run.ResolveCurrentEvent();

            if (run.Status == RunStatus.RoomResolved)
            {
                break;
            }

            run.ProgressCurrentRoom();
        }

        run.EnterInterlude();
        run.MoveToNextRoom(TestGameEngineFactory.CreateThresholdRoom(depth: run.CurrentDepth + 1));
    }

    // ---------------------------------------------------------------------------
    // "Loi de l'Oubli Partiel" — the forgotten skill is drawn once at promulgation
    // time (Run.PickForgottenSkill), then cleared with a +8 stat-point payout signal
    // when the floor-scoped modifier is consumed (Run.ConsumeFloorEndModifiers /
    // Run.MoveToNextRoom's FloorEndModifierConsumptionResult return).
    // ---------------------------------------------------------------------------

    private static PalaceLaw CreateOubliPartielLaw(string key = "law.oubli-partiel") => PalaceLaw.Create(
        key, "Loi de l'Oubli Partiel", "1.0.0",
        domains: [PalaceLawDomain.Combat],
        effects:
        [
            PalaceLawEffect.Create(
                RunModifierType.SkillForgotten, value: 1, RunModifierDuration.UntilFloorEnds),
        ]);

    private static Run CreateRunWithMultipleSkills()
    {
        var run = TestGameEngineFactory.CreateRun();

        var statBlock = RunCharacterStatSnapshot.Create(
            maxVitality: 100, attackPower: 12, defense: 6, startingGuard: 0,
            speed: 10, initiative: 10, recovery: 5, focus: 0, mana: 0, charge: 0);

        var skills = new[]
        {
            RunCharacterSkillSnapshot.Create(
                skillDefinitionKey: "skill.basic.strike", displayName: "Frappe",
                skillType: "Damage", targetingMode: "SingleEnemy", effectType: "Damage",
                manaCost: 0, chargeCost: 0, basePower: 10),
            RunCharacterSkillSnapshot.Create(
                skillDefinitionKey: "skill.hero.blaze", displayName: "Brasier",
                skillType: "Damage", targetingMode: "SingleEnemy", effectType: "Damage",
                manaCost: 5, chargeCost: 0, basePower: 20),
        };

        var character = RunCharacterSnapshot.Create(
            characterId: Guid.NewGuid(), definitionKey: "character.player.self",
            displayName: "Le Porteur", statBlock: statBlock, skills: skills);

        run.AttachPlayerSnapshot(RunPlayerSnapshot.Create(
            playerId: run.PlayerId, displayName: "Joueur", characters: [character],
            createdAtUtc: DateTimeOffset.UtcNow));

        return run;
    }

    [Fact]
    public void PromulgateLaw_ShouldPickAForgottenSkill_ExcludingTheBasicAttack()
    {
        var run = CreateRunWithMultipleSkills();

        run.PromulgateLaw(CreateOubliPartielLaw());

        run.ForgottenSkillKey.Should().Be("skill.hero.blaze");
    }

    [Fact]
    public void MoveToNextRoom_ShouldReturnTrueAndClearTheForgottenSkill_WhenCrossingAFloorBoundary()
    {
        var run = CreateRunWithMultipleSkills();
        run.PromulgateLaw(CreateOubliPartielLaw());
        run.ForgottenSkillKey.Should().NotBeNull();

        var result = AdvanceToFloorBoundary(run);

        result.OubliPartielPayoutDue.Should().BeTrue();
        run.ForgottenSkillKey.Should().BeNull();
    }

    [Fact]
    public void MoveToNextRoom_ShouldReturnFalse_WhileStillOnTheSameFloor()
    {
        var run = CreateRunWithMultipleSkills();
        run.PromulgateLaw(CreateOubliPartielLaw());

        run.EnterInterlude();
        var result = run.MoveToNextRoom(TestGameEngineFactory.CreateThresholdRoom(depth: run.CurrentDepth + 1));

        result.OubliPartielPayoutDue.Should().BeFalse();
        run.ForgottenSkillKey.Should().NotBeNull();
    }

    private static Run.FloorEndModifierConsumptionResult AdvanceToFloorBoundary(Run run)
    {
        var result = Run.FloorEndModifierConsumptionResult.None;

        for (var i = 0; i < 10; i++)
        {
            while (run.Status == RunStatus.Active)
            {
                var node = run.CurrentRoom.AvailableNodes.First();

                run.ChooseNode(node.Id);
                run.ResolveCurrentEvent();

                if (run.Status == RunStatus.RoomResolved)
                {
                    break;
                }

                run.ProgressCurrentRoom();
            }

            run.EnterInterlude();
            result = run.MoveToNextRoom(TestGameEngineFactory.CreateThresholdRoom(depth: run.CurrentDepth + 1));
        }

        return result;
    }

    // ---------------------------------------------------------------------------
    // "Loi de l'Impôt du Seuil" — the toll charge itself happens in
    // MoveToNextRoomCommandHandler (application layer, needs the currency gateway).
    // Here we only verify the domain-level insolvency debuff Run.
    // ApplyRoomTollInsolvencyDebuff applies.
    // ---------------------------------------------------------------------------

    [Fact]
    public void ApplyRoomTollInsolvencyDebuff_ShouldAddAStackingFloorScopedModifier()
    {
        var run = TestGameEngineFactory.CreateRun();

        run.ApplyRoomTollInsolvencyDebuff();
        run.ApplyRoomTollInsolvencyDebuff();

        run.RunModifiers.Should().HaveCount(2)
            .And.OnlyContain(m =>
                m.Type == RunModifierType.MaxHpReductionPercent
                && m.Value == Run.RoomTollInsolvencyMaxHpReductionPercent
                && m.Duration == RunModifierDuration.UntilFloorEnds
                && !m.IsConsumed);
    }

    // ---------------------------------------------------------------------------
    // "Loi du Prêteur" — the CurrencyGainBonusPercent modifier doubles as this law's
    // "active" marker; its floor-end consumption signals the clawback (the actual
    // gateway read/spend happens in MoveToNextRoomCommandHandler).
    // ---------------------------------------------------------------------------

    private static PalaceLaw CreatePreteurLaw(string key = "law.preteur") => PalaceLaw.Create(
        key, "Loi du Prêteur", "1.0.0",
        domains: [PalaceLawDomain.Rewards],
        effects:
        [
            PalaceLawEffect.Create(
                RunModifierType.CurrencyGainBonusPercent, value: 50, RunModifierDuration.UntilFloorEnds),
        ]);

    [Fact]
    public void MoveToNextRoom_ShouldSignalPreteurClawbackDue_WhenCrossingAFloorBoundary()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.PromulgateLaw(CreatePreteurLaw());

        var result = AdvanceToFloorBoundary(run);

        result.PreteurClawbackDue.Should().BeTrue();
    }

    [Fact]
    public void MoveToNextRoom_ShouldNotSignalPreteurClawback_WhileStillOnTheSameFloor()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.PromulgateLaw(CreatePreteurLaw());

        run.EnterInterlude();
        var result = run.MoveToNextRoom(TestGameEngineFactory.CreateThresholdRoom(depth: run.CurrentDepth + 1));

        result.PreteurClawbackDue.Should().BeFalse();
    }

    // ---------------------------------------------------------------------------
    // "Loi de la Chandelle" — one free item-node reroll charge per modifier instance,
    // consumed one at a time (unlike other UntilFloorEnds modifiers, which are swept
    // in bulk at floor end). The actual reward-offer reroll happens in
    // RerollItemRewardOfferCommandHandler; here we only verify the charge bookkeeping.
    // ---------------------------------------------------------------------------

    private static PalaceLaw CreateChandelleLaw(string key = "law.chandelle") => PalaceLaw.Create(
        key, "Loi de la Chandelle", "1.0.0",
        domains: [PalaceLawDomain.Rewards],
        effects:
        [
            PalaceLawEffect.Create(
                RunModifierType.ItemNodeRerollCharge, value: 1, RunModifierDuration.UntilFloorEnds),
        ]);

    [Fact]
    public void TryConsumeItemNodeRerollCharge_ShouldConsumeOneChargeAndReturnTrue_WhenChargeAvailable()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.PromulgateLaw(CreateChandelleLaw());

        var consumed = run.TryConsumeItemNodeRerollCharge();

        consumed.Should().BeTrue();
        run.ConsumedItemNodeRerollCount.Should().Be(1);
    }

    [Fact]
    public void TryConsumeItemNodeRerollCharge_ShouldReturnFalse_WhenNoChargeAvailable()
    {
        var run = TestGameEngineFactory.CreateRun();

        var consumed = run.TryConsumeItemNodeRerollCharge();

        consumed.Should().BeFalse();
        run.ConsumedItemNodeRerollCount.Should().Be(0);
    }

    [Fact]
    public void TryConsumeItemNodeRerollCharge_ShouldReturnFalse_OnceTheOnlyChargeIsAlreadyConsumed()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.PromulgateLaw(CreateChandelleLaw());
        run.TryConsumeItemNodeRerollCharge();

        var consumedAgain = run.TryConsumeItemNodeRerollCharge();

        consumedAgain.Should().BeFalse();
        run.ConsumedItemNodeRerollCount.Should().Be(1);
    }

    // ---------------------------------------------------------------------------
    // "Loi du Répit" — ACCALMIE: pauses every active Sévère law's effects for the
    // room (by temporary consumption, see Run.SuspendActiveSevereLawModifiers),
    // reversed when the room is left (MoveToNextRoom) or rolled back (ExitMidRoom).
    // ---------------------------------------------------------------------------

    private static PalaceLaw CreateRepitLaw(string key = "law.repit") => PalaceLaw.Create(
        key, "Loi du Répit", "1.0.0",
        domains: [PalaceLawDomain.Combat],
        effects:
        [
            PalaceLawEffect.Create(
                RunModifierType.SuspendSevereLaws, value: 1, RunModifierDuration.UntilRoomEnds),
        ],
        polarity: PalaceLawPolarity.Clemente);

    [Fact]
    public void PromulgateLaw_ShouldSuspendActiveSevereLawModifiers_WhenRepitIsPromulgated()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.PromulgateLaw(CreateLawWithPolarity("law-severe-test", PalaceLawPolarity.Severe));

        run.PromulgateLaw(CreateRepitLaw());

        var severeModifier = run.RunModifiers.Should().ContainSingle(m => m.SourceKey == "law-severe-test").Subject;
        severeModifier.IsConsumed.Should().BeTrue();
        run.SuspendedSevereLawModifierIds.Should().Contain(severeModifier.Id.Value);
    }

    [Fact]
    public void PromulgateLaw_ShouldNotSuspendClementeLawModifiers_WhenRepitIsPromulgated()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.PromulgateLaw(CreateLawWithPolarity("law-clemente-test", PalaceLawPolarity.Clemente));

        run.PromulgateLaw(CreateRepitLaw());

        var clementeModifier = run.RunModifiers.Should().ContainSingle(m => m.SourceKey == "law-clemente-test").Subject;
        clementeModifier.IsConsumed.Should().BeFalse();
        run.SuspendedSevereLawModifierIds.Should().BeEmpty();
    }

    [Fact]
    public void MoveToNextRoom_ShouldResumeSuspendedSevereLawModifiers_WhenLeavingTheRoom()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.PromulgateLaw(CreateLawWithPolarity("law-severe-test", PalaceLawPolarity.Severe));
        run.PromulgateLaw(CreateRepitLaw());
        var severeModifier = run.RunModifiers.Single(m => m.SourceKey == "law-severe-test");
        severeModifier.IsConsumed.Should().BeTrue();

        run.EnterInterlude();
        run.MoveToNextRoom(TestGameEngineFactory.CreateThresholdRoom(depth: run.CurrentDepth + 1));

        severeModifier.IsConsumed.Should().BeFalse();
        run.SuspendedSevereLawModifierIds.Should().BeEmpty();
    }

    [Fact]
    public void ExitMidRoom_ShouldResumeSuspendedSevereLawModifiers()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.PromulgateLaw(CreateLawWithPolarity("law-severe-test", PalaceLawPolarity.Severe));
        run.PromulgateLaw(CreateRepitLaw());
        var severeModifier = run.RunModifiers.Single(m => m.SourceKey == "law-severe-test");

        run.ExitMidRoom(DateTimeOffset.UtcNow);

        severeModifier.IsConsumed.Should().BeFalse();
        run.SuspendedSevereLawModifierIds.Should().BeEmpty();
    }
}
