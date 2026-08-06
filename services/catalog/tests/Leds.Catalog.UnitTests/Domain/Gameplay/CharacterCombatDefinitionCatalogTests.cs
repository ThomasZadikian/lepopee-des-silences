using FluentAssertions;
using Leds.Catalog.Domain.Errors;
using Leds.Catalog.Domain.Gameplay;
using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.UnitTests.Domain.Gameplay;

public sealed class CharacterCombatDefinitionCatalogTests
{
    [Fact]
    public void Protagonist_ShouldUseMemoireAsValidatedNaturalRegister()
    {
        var protagonist = CharacterCombatDefinitionCatalog.GetRequired("character.player.self");

        protagonist.EmotionalRegister.Should().Be(EmotionalRegister.Memoire);
        protagonist.Kind.Should().Be(CharacterKind.Protagonist);
    }

    [Theory]
    [InlineData("character.thomas", EmotionalRegister.Silence)]
    [InlineData("character.mane", EmotionalRegister.Rupture)]
    [InlineData("character.mina", EmotionalRegister.Folie)]
    [InlineData("character.elise", EmotionalRegister.Melancolie)]
    [InlineData("character.john", EmotionalRegister.Deni)]
    public void GetRequired_ShouldExposeValidatedNaturalRegister(
        string definitionKey,
        EmotionalRegister expected)
    {
        var definition = CharacterCombatDefinitionCatalog.GetRequired(definitionKey);

        definition.EmotionalRegister.Should().Be(expected);
        definition.Kind.Should().Be(CharacterKind.Companion);
    }

    [Fact]
    public void GetRequired_ShouldRejectUnknownCharacter()
    {
        var act = () => CharacterCombatDefinitionCatalog.GetRequired("character.unknown");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Definitions_ShouldHaveUniqueKeys()
    {
        CharacterCombatDefinitionCatalog.All.Select(d => d.DefinitionKey)
            .Should().OnlyHaveUniqueItems();
    }
}
