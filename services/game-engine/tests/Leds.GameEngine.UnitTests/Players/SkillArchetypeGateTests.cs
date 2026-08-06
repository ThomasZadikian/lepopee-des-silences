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
