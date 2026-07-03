using FluentAssertions;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Domain.Combats.Typing;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Reachability;

namespace Leds.GameEngine.UnitTests.Generation.Rooms.Reachability;

public sealed class RoomReachabilitySelectorTests
{
    private const string Seed = "seed-reachability-tests";

    private static CatalogRoomDefinition Room(
        string key,
        string theme = "Welcome",
        int minDepth = 0,
        int maxDepth = 9,
        int baseWeight = 1,
        bool triggersStrictChain = false,
        string? worldKey = "palais",
        bool isWorldEntryRoom = false,
        params string[] reachableRoomKeys)
        => new(
            key, key, "Description", null,
            "Palais intérieur", "Common", theme,
            minDepth, maxDepth, baseWeight,
            null, null, null, null, null, IsUnique: false,
            worldKey, isWorldEntryRoom, triggersStrictChain, reachableRoomKeys);

    [Fact]
    public void SelectNextRoom_ShouldReturnNull_WhenCurrentRoomHasNoWorld()
    {
        var current = Room("room.legacy", worldKey: null, reachableRoomKeys: ["room.other"]);
        var other = Room("room.other");

        var selector = new RoomReachabilitySelector();
        var result = selector.SelectNextRoom(current, [current, other], [], [], nextRoomDepth: 1, [], Seed);

        result.Should().BeNull();
    }

    [Fact]
    public void SelectNextRoom_ShouldFallBackToWorldEntryRoom_WhenNoChildIsDeclared()
    {
        var entry = Room("room.halldentree", isWorldEntryRoom: true);
        var lastOfChain = Room("room.enfer4", minDepth: 3, maxDepth: 9); // no children

        var world = new CatalogWorldDefinition("palais", "Palais", "room.halldentree");

        var selector = new RoomReachabilitySelector();
        var result = selector.SelectNextRoom(
            lastOfChain, [entry, lastOfChain], [world], [], nextRoomDepth: 4, [], Seed);

        result.Should().Be(entry);
    }

    [Fact]
    public void SelectNextRoom_ShouldFallBackToWorldEntryRoom_WhenAllChildrenAreOutOfDepthRange()
    {
        var entry = Room("room.halldentree", isWorldEntryRoom: true);
        var farChild = Room("room.faille", minDepth: 6, maxDepth: 9);
        var current = Room("room.jardin", minDepth: 2, maxDepth: 9, reachableRoomKeys: ["room.faille"]);

        var world = new CatalogWorldDefinition("palais", "Palais", "room.halldentree");

        var selector = new RoomReachabilitySelector();
        // nextRoomDepth = 3: room.faille requires depth >= 6, so it's filtered out entirely.
        var result = selector.SelectNextRoom(
            current, [entry, farChild, current], [world], [], nextRoomDepth: 3, [], Seed);

        result.Should().Be(entry);
    }

    [Fact]
    public void SelectNextRoom_ShouldReturnTheOnlyChild_WhenExactlyOneIsEligible_NoRandomnessInvolved()
    {
        var enfer1 = Room("room.enfer1", minDepth: 3, maxDepth: 9);
        var falaise = Room("room.falaise", minDepth: 2, maxDepth: 9, triggersStrictChain: true, reachableRoomKeys: ["room.enfer1"]);

        var selector = new RoomReachabilitySelector();
        var result = selector.SelectNextRoom(
            falaise, [falaise, enfer1], [], [], nextRoomDepth: 3, [], Seed);

        result.Should().Be(enfer1);
    }

    [Fact]
    public void SelectNextRoom_ShouldBeDeterministic_ForTheSameInputs()
    {
        var current = Room("room.couloirs", reachableRoomKeys: ["room.jardin", "room.meditation"]);
        var jardin = Room("room.jardin");
        var meditation = Room("room.meditation");

        var selector = new RoomReachabilitySelector();
        var first = selector.SelectNextRoom(current, [current, jardin, meditation], [], [], 2, [], Seed);
        var second = selector.SelectNextRoom(current, [current, jardin, meditation], [], [], 2, [], Seed);

        first.Should().Be(second);
        first.Should().BeOneOf(jardin, meditation);
    }

    [Fact]
    public void SelectNextRoom_ShouldFavorHigherThemeAffinity_WhenOneCandidateHasAnExplicitOverride()
    {
        var current = Room("room.couloirs", theme: "Fear", reachableRoomKeys: ["room.a", "room.b"]);
        var candidateA = Room("room.a", theme: "Terrifying", baseWeight: 1);
        var candidateB = Room("room.b", theme: "Peace", baseWeight: 1);

        // No affinity override: both weigh the same (base weight 1 * default affinity 1.0)
        // -> the outcome only depends on the seeded roll, but must always be one of the two.
        var selector = new RoomReachabilitySelector();
        var withoutOverride = selector.SelectNextRoom(current, [current, candidateA, candidateB], [], [], 2, [], Seed);
        withoutOverride.Should().BeOneOf(candidateA, candidateB);

        // A strong override toward "Terrifying" should be able to flip the outcome for a
        // roll that would otherwise have picked "Peace" — assert the roll value we're
        // exercising actually lands on candidateB without the override, then confirm the
        // override moves the pick to candidateA.
        var roll = DeterministicCombatRoll.UnitInterval($"{Seed}|room-reachability|2|room.couloirs");
        // With equal weights (1 and 1, ordered alphabetically room.a then room.b), the
        // first half of the roll range selects room.a and the second half room.b.
        var expectedWithoutOverride = roll < 0.5 ? candidateA : candidateB;
        withoutOverride.Should().Be(expectedWithoutOverride);

        var affinities = new[] { new CatalogRoomThemeAffinity("Fear", "Terrifying", 1000m) };
        var withOverride = selector.SelectNextRoom(current, [current, candidateA, candidateB], [], affinities, 2, [], Seed);
        withOverride.Should().Be(candidateA);
    }

    [Fact]
    public void SelectNextRoom_ShouldReducePortalRoomOdds_AfterItWasAlreadyVisitedThisRun()
    {
        var current = Room("room.couloirs", reachableRoomKeys: ["room.falaise", "room.jardin"]);
        var falaise = Room("room.falaise", triggersStrictChain: true, baseWeight: 1);
        var jardin = Room("room.jardin", baseWeight: 1);

        var selector = new RoomReachabilitySelector();

        // Never visited: both candidates weigh 1 -> falaise wins whenever the roll lands
        // in the first half (alphabetical order: room.falaise before room.jardin).
        var roll = DeterministicCombatRoll.UnitInterval($"{Seed}|room-reachability|2|room.couloirs");
        var neverVisited = selector.SelectNextRoom(current, [current, falaise, jardin], [], [], 2, [], Seed);
        neverVisited.Should().Be(roll < 0.5 ? falaise : jardin);

        // Visited 3 times: falaise's weight decays to 0.15^3 ~= 0.003375, totally
        // dwarfed by jardin's weight of 1 -> jardin wins regardless of the roll value,
        // since jardin now claims almost the entire [0, 1) range.
        var visitedThreeTimes = new[] { "room.falaise", "room.falaise", "room.falaise" };
        var afterRevisits = selector.SelectNextRoom(current, [current, falaise, jardin], [], [], 2, visitedThreeTimes, Seed);
        afterRevisits.Should().Be(jardin);
    }
}
