using FluentAssertions;
using Leds.GameEngine.Application.Protocol;
using Leds.GameEngine.Application.RoomMaps;
using Leds.GameEngine.Domain.Npcs;
using Leds.GameEngine.Domain.Protocol;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation.RoomMaps;
using Leds.GameEngine.Infrastructure.Generation.RoomMaps.Hall;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Bosses;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Themes;
using Leds.GameEngine.UnitTests.Common;

namespace Leds.GameEngine.UnitTests.Protocol;

/// <summary>
/// The Application-layer half of the protocol engine — <see cref="LocalRuleProtocolEvaluator"/>
/// resolves the authored rule and detects true zone crossings, unlike the pure Domain
/// <see cref="LocalRuleEvaluator"/> (see <c>Protocol/LocalRuleEvaluatorTests.cs</c>) which just
/// checks one already-classified trigger. Exercised against the real generated Hall room rather
/// than a synthetic fixture, since the crossing-detection logic is meaningless without an actual
/// party position history to walk.
/// </summary>
public sealed class LocalRuleProtocolEvaluatorTests
{
    private const string Seed = "seed-local-rule-protocol-evaluator-tests";
    private const string GeneratorVersion = "grid-room-layout-1.0.0";

    private static async Task<Room> GenerateHallAsync()
    {
        var generator = new GridRoomGenerator(
            new GridRoomLayoutTemplateProvider(),
            new RoomThemeResolver(),
            new RoomBossProfileResolver(new StubCatalogContentGateway()),
            new HardcodedRoomTypeGenerationProfileProvider(),
            new HardcodedRoomStructuralProfileProvider(),
            new HardcodedLocalRuleProvider());

        return await generator.GenerateAsync(
            Seed, GeneratorVersion, roomDepth: 0, RoomType.Memory, new Random(42),
            catalogRoomKey: "room.halldentree");
    }

    private static LocalRuleProtocolEvaluator CreateSut() => new(new HardcodedLocalRuleProvider());

    /// <summary>Walks the party to (targetX, targetY) and evaluates crossings exactly like
    /// MovePartyCommandHandler does — capture the pre-move position, move, hand the traversed
    /// path to the evaluator.</summary>
    private static IReadOnlyList<LocalRuleTriggerOutcome> MoveAndEvaluate(
        Room room, LocalRuleProtocolEvaluator evaluator, int targetX, int targetY)
    {
        var previousX = room.Grid.PartyX;
        var previousY = room.Grid.PartyY;

        var move = room.MoveParty(targetX, targetY);

        return evaluator.EvaluateZoneCrossings(room, previousX, previousY, move.TraversedCells);
    }

    [Fact]
    public async Task EvaluateZoneCrossings_ShouldNotFire_WhenThePartyStaysInsideAZoneItAlreadyOccupies()
    {
        // The party spawns ON the tapis (SFD Hall d'entrée: the entrance tapis leads straight in)
        // — walking to another tapis cell must not be treated as "entering" it.
        var room = await GenerateHallAsync();
        var evaluator = CreateSut();

        var outcomes = MoveAndEvaluate(room, evaluator, HallEntreeLayout.StartX, HallEntreeLayout.StartY - 1);

        outcomes.Should().BeEmpty();
        room.GetLocalRuleState(HallEntreeProtocol.TapisRuleKey)!.HasBeenInformed.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateZoneCrossings_ShouldInform_OnTheFirstCrossingBackIntoTheTapis()
    {
        var room = await GenerateHallAsync();
        var evaluator = CreateSut();

        // Step off the tapis (x < 10), then back onto it — SFD §V: "quitte le tapis puis y revient".
        MoveAndEvaluate(room, evaluator, 9, 15);
        var outcomes = MoveAndEvaluate(room, evaluator, 12, 15);

        var tapis = outcomes.Should().ContainSingle(o => o.RuleKey == HallEntreeProtocol.TapisRuleKey).Subject;
        tapis.Result.Outcome.Should().Be(LocalRuleEvaluationOutcome.Informed);
        tapis.Result.Message.Should().Be(HallEntreeProtocol.TapisRule.InfoMessage);
        room.GetLocalRuleState(HallEntreeProtocol.TapisRuleKey)!.CumulativeSeverity.Should().Be(0);
    }

    [Fact]
    public async Task EvaluateZoneCrossings_ShouldEscalateAndRaiseAlertOnTheMajordome_OnRepeatedTransgressions()
    {
        var room = await GenerateHallAsync();
        var evaluator = CreateSut();
        var majordome = room.RoomNpcs.Single(n => n.CatalogNpcKey == "npc.majordome");

        // 1st crossing: informed only. 2nd: first transgression (severity 1 -> Look only, no
        // mechanical effect). 3rd: second transgression (severity 2 -> NpcRelocate on the
        // majordome, which IS mechanically applied via RoomNpc.RaiseAlert). Assertions are
        // interleaved with the moves, not batched after — RaiseAlert never reverts, so checking
        // Awareness only at the end could never distinguish "the 2nd crossing left it alone"
        // from "the 3rd crossing already raised it".
        MoveAndEvaluate(room, evaluator, 9, 15);
        var firstReturn = MoveAndEvaluate(room, evaluator, 12, 15);

        firstReturn.Single(o => o.RuleKey == HallEntreeProtocol.TapisRuleKey).Result.Outcome
            .Should().Be(LocalRuleEvaluationOutcome.Informed);

        MoveAndEvaluate(room, evaluator, 9, 15);
        var secondReturn = MoveAndEvaluate(room, evaluator, 12, 15);

        var second = secondReturn.Single(o => o.RuleKey == HallEntreeProtocol.TapisRuleKey);
        second.Result.Outcome.Should().Be(LocalRuleEvaluationOutcome.Transgression);
        second.Result.Message.Should().Be(HallEntreeProtocol.TapisRule.WarningMessage);
        second.Result.NewConsequences.Should().ContainSingle(c => c.Type == LocalRuleConsequenceType.Look);
        majordome.Awareness.Should().NotBe(NpcAwarenessState.Alert, "Look alone raises no state");

        MoveAndEvaluate(room, evaluator, 9, 15);
        var thirdReturn = MoveAndEvaluate(room, evaluator, 12, 15);

        var third = thirdReturn.Single(o => o.RuleKey == HallEntreeProtocol.TapisRuleKey);
        third.Result.Outcome.Should().Be(LocalRuleEvaluationOutcome.Transgression);
        third.Result.NewConsequences.Should().ContainSingle(c => c.Type == LocalRuleConsequenceType.NpcRelocate);
        majordome.Awareness.Should().Be(NpcAwarenessState.Alert, "NpcRelocate on the majordome is applied via RaiseAlert");
    }

    [Fact]
    public async Task EvaluateZoneCrossings_ShouldInform_OnApproachingTheEmotionsThreshold()
    {
        var room = await GenerateHallAsync();
        var evaluator = CreateSut();

        // Walk east toward the Émotions threshold, staying off the tapis to isolate this rule.
        MoveAndEvaluate(room, evaluator, 9, 15);
        MoveAndEvaluate(room, evaluator, 9, 10);
        var outcomes = MoveAndEvaluate(room, evaluator, HallEntreeLayout.EmotionsThresholdX, HallEntreeLayout.EmotionsThresholdY);

        var emotions = outcomes.Should().ContainSingle(o => o.RuleKey == HallEntreeProtocol.EmotionsRuleKey).Subject;
        emotions.Result.Outcome.Should().Be(LocalRuleEvaluationOutcome.Informed);
        emotions.Result.Message.Should().Be(HallEntreeProtocol.EmotionsRule.InfoMessage);
    }
}
