using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class RunPartyResourceTests
{
    [Fact]
    public void CapturePartyResources_ShouldCarryCompanionVitalityAndManaIntoTheRunSnapshot()
    {
        var run = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Combat).Run;
        var characterId = Guid.NewGuid();
        var stats = RunCharacterStatSnapshot.Create(
            maxVitality: 100, attackPower: 12, defense: 6, startingGuard: 0,
            speed: 10, initiative: 10, focus: 0, mana: 20, charge: 0);
        var character = RunCharacterSnapshot.Create(
            characterId,
            "character.companion",
            "Compagnon",
            stats,
            skills: [],
            emotionalRegisterCode: "Neutral");
        run.AttachPlayerSnapshot(RunPlayerSnapshot.Create(
            run.PlayerId,
            "Joueur",
            [character],
            DateTimeOffset.UtcNow));

        var ally = Combatant.Rehydrate(
            CombatantId.New(),
            "character.companion",
            "Compagnon",
            CombatantSide.Player,
            "Companion",
            maxVitality: 100,
            currentVitality: 43,
            guard: 0,
            baseGuard: 0,
            mana: 7,
            charge: 0,
            status: CombatantStatus.Active,
            skills: [],
            maxMana: 20,
            characterInstanceId: characterId);

        run.CapturePartyResources([ally]);

        character.CurrentVitality.Should().Be(43);
        character.CurrentMana.Should().Be(7);
    }
}
