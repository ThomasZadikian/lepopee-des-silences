using FluentAssertions;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Players;
using Moq;

namespace Leds.GameEngine.UnitTests.Players;

public sealed class SkillArchetypeGateTests
{
    [Fact]
    public async Task EnsureCanEquip_ShouldResolveCharacterArchetypeFromCatalog()
    {
        var catalog = CreateCatalog("GlassCannon");
        var gate = new SkillArchetypeGate(catalog.Object);

        var act = () => gate.EnsureCanEquipAsync(
            "character.mane", "skill.signature", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureCanEquip_ShouldRejectUnknownCharacterInsteadOfAssumingAdaptive()
    {
        var catalog = CreateCatalog("GlassCannon");
        var gate = new SkillArchetypeGate(catalog.Object);

        var act = () => gate.EnsureCanEquipAsync(
            "character.unknown", "skill.signature", CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task EnsureCanEquip_ShouldReturnWhenSkillDoesNotExist()
    {
        var catalog = new Mock<ICatalogContentGateway>();
        catalog.Setup(gateway => gateway.GetSkillDefinitionByKeyAsync(
                "skill.missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CatalogSkillDefinition?)null);

        var act = () => new SkillArchetypeGate(catalog.Object).EnsureCanEquipAsync(
            "character.any", "skill.missing", CancellationToken.None);

        await act.Should().NotThrowAsync();
        catalog.Verify(gateway => gateway.ListCharacterCombatDefinitionsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnsureCanEquip_ShouldReturnWhenSkillHasNoArchetypeRestriction()
    {
        var catalog = new Mock<ICatalogContentGateway>();
        catalog.Setup(gateway => gateway.GetSkillDefinitionByKeyAsync(
                "skill.open", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CatalogSkillDefinition(
                "skill.open", "Open", "Test", "Damage", "SingleEnemy", "Damage",
                0, 0, 10, [], EmotionalRegister: "rupture", AllowedArchetypes: []));

        var act = () => new SkillArchetypeGate(catalog.Object).EnsureCanEquipAsync(
            null, "skill.open", CancellationToken.None);

        await act.Should().NotThrowAsync();
        catalog.Verify(gateway => gateway.ListCharacterCombatDefinitionsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnsureCanEquip_ShouldAllowAdaptiveCharacterRegardlessOfRestriction()
    {
        var catalog = CreateCatalog("Mage");
        var gate = new SkillArchetypeGate(catalog.Object);

        var act = () => gate.EnsureCanEquipAsync(
            "CHARACTER.PLAYER.SELF", "skill.signature", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureCanEquip_ShouldRejectKnownButIncompatibleArchetype()
    {
        var catalog = CreateCatalog("Mage");
        var gate = new SkillArchetypeGate(catalog.Object);

        var act = () => gate.EnsureCanEquipAsync(
            "character.mane", "skill.signature", CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*n'est pas compatible*");
    }

    private static Mock<ICatalogContentGateway> CreateCatalog(string allowedArchetype)
    {
        var catalog = new Mock<ICatalogContentGateway>();
        catalog.Setup(gateway => gateway.GetSkillDefinitionByKeyAsync(
                "skill.signature", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CatalogSkillDefinition(
                "skill.signature", "Signature", "Test", "Damage", "SingleEnemy", "Damage",
                0, 0, 10, [], EmotionalRegister: "rupture",
                AllowedArchetypes: [allowedArchetype]));
        catalog.Setup(gateway => gateway.ListCharacterCombatDefinitionsAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new CatalogCharacterCombatDefinition(
                    "character.player.self", "Protagonist", "Adaptive", "memoire"),
                new CatalogCharacterCombatDefinition(
                    "character.mane", "Companion", "GlassCannon", "rupture")
            ]);
        return catalog;
    }
}
