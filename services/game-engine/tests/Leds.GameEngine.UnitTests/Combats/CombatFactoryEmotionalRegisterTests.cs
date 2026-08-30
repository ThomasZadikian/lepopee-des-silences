using FluentAssertions;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.EncounterDrafts;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Typing;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Combats;

public sealed class CombatFactoryEmotionalRegisterTests
{
    [Fact]
    public void Mirror_should_keep_the_copied_units_natural_register()
    {
        var characterId = Guid.NewGuid();
        var draft = new CombatEncounterDraft(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Threshold", 1, 1, "Combat",
            [new CombatEncounterDraftEnemy(
                "enemy.unused", "Unused", "", "Guard", 1, 0, 2, [], [], [],
                EmotionalRegister: "Effroi")],
            [new CombatEncounterDraftAlly(
                "character.player.self", "Porteur", "Protagonist", [],
                IsProtagonist: true, EmotionalRegister: "Memoire", CharacterInstanceId: characterId)]);
        var mirror = RunModifier.Create(
            RunModifierType.MirrorCombatCopy,
            1,
            RunModifierDuration.NextCombatOnly,
            "law",
            "law.reflet");

        var roster = new CombatFactory().BuildRoster(
            CombatId.New(), draft, runModifiers: [mirror]);

        roster.Enemies.Should().ContainSingle();
        roster.Enemies.Single().NaturalEmotionalType.Should().Be(EmotionalType.Memoire);
        roster.Enemies.Single().SourceKey.Should().Be("reflet.character.player.self");
        roster.Enemies.Single().SourceDefinitionKey.Should().Be("character.player.self");
        roster.Enemies.Single().CharacterInstanceId.Should().BeNull();
    }
}
