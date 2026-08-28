using FluentAssertions;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Moq;

namespace Leds.GameEngine.UnitTests.Catalog;

public sealed class ListActiveSkillDefinitionsQueryHandlerCoverageTests
{
    [Fact]
    public async Task Handle_ShouldFilterEnemySkillsMapEffectsHintsAndCharacterCompatibility()
    {
        var gateway = new Mock<ICatalogContentGateway>();
        gateway.Setup(g => g.ListActiveSkillDefinitionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                Skill("skill.enemy", audience: "Enemy"),
                Skill("skill.universal", effects: null),
                Skill("skill.fighter", audience: "Any", allowed: ["Fighter"], effects:
                [
                    new CatalogSkillEffectSpec("DamageOverTime", "burn", 3, 20, 10, "AttackPower", false,
                        MagnitudeIsPercentOfBaseStat: true, AppliesToActor: true, IsPermanent: true)
                ])
            ]);

        gateway.Setup(g => g.ListCharacterCombatDefinitionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new CatalogCharacterCombatDefinition("char.adaptive", "Hero", "Adaptive", "Mémoire"),
                new CatalogCharacterCombatDefinition("char.fighter", "Hero", "Fighter", "Rupture"),
                new CatalogCharacterCombatDefinition("char.mage", "Hero", "Mage", "Folie")
            ]);

        gateway.Setup(g => g.ListNpcDefinitionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                Npc("npc.empty", "Sans offre", null),
                Npc("npc.teacher", "Maître", [
                    new CatalogNpcOffering("offer.skill", "Skill", "skill.fighter", 0, false, []),
                    new CatalogNpcOffering("offer.blank", "Skill", " ", 0, false, []),
                    new CatalogNpcOffering("offer.companion", "Companion", "char.companion", 0, true, [],
                        new CatalogCompanionKit(100, 10, 5, 0, 10, 0, 0, 20, 0,
                            ["skill.fighter", "skill.universal"])),
                    new CatalogNpcOffering("offer.other", "Currency", null, 10, false, [])
                ])
            ]);

        gateway.Setup(g => g.ListActiveItemDefinitionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                Item("item.empty", null),
                Item("item.grant", [
                    new CatalogItemEquipmentEffect("GrantSkill", null, null, "skill.fighter", null),
                    new CatalogItemEquipmentEffect("GrantSkill", null, null, null, null),
                    new CatalogItemEquipmentEffect("HitChanceBonus", null, 5, null, null)
                ])
            ]);

        var response = await new ListActiveSkillDefinitionsQueryHandler(gateway.Object)
            .Handle(new ListActiveSkillDefinitionsQuery(), CancellationToken.None);

        response.Skills.Should().HaveCount(2);
        response.Skills.Should().NotContain(s => s.Key == "skill.enemy");

        var universal = response.Skills.Single(s => s.Key == "skill.universal");
        universal.Effects.Should().BeEmpty();
        universal.CompatibleCharacterDefinitionKeys.Should().BeEquivalentTo(
            ["char.adaptive", "char.fighter", "char.mage"]);
        universal.AcquisitionHints.Should().ContainSingle()
            .Which.Should().Be("Sort de départ du compagnon Maître");

        var fighter = response.Skills.Single(s => s.Key == "skill.fighter");
        fighter.Audience.Should().Be("Any");
        fighter.Effects.Should().ContainSingle();
        fighter.CompatibleCharacterDefinitionKeys.Should().BeEquivalentTo(
            ["char.adaptive", "char.fighter"]);
        fighter.AcquisitionHints.Should().BeEquivalentTo([
            "Offert par Maître",
            "Sort de départ du compagnon Maître",
            "Octroyé par l'objet item.grant"
        ]);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenEverySourceIsEmpty()
    {
        var gateway = new Mock<ICatalogContentGateway>();
        gateway.Setup(g => g.ListActiveSkillDefinitionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        gateway.Setup(g => g.ListNpcDefinitionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        gateway.Setup(g => g.ListActiveItemDefinitionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        gateway.Setup(g => g.ListCharacterCombatDefinitionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var response = await new ListActiveSkillDefinitionsQueryHandler(gateway.Object)
            .Handle(new ListActiveSkillDefinitionsQuery(), CancellationToken.None);

        response.Skills.Should().BeEmpty();
    }

    private static CatalogSkillDefinition Skill(
        string key,
        string audience = "Player",
        IReadOnlyCollection<string>? allowed = null,
        IReadOnlyCollection<CatalogSkillEffectSpec>? effects = null) =>
        new(key, key, "description", "Active", "SingleEnemy", "Damage", 1, 0, 10, [],
            "Mémoire", effects, Audience: audience, AllowedArchetypes: allowed);

    private static CatalogNpcDefinition Npc(
        string key,
        string name,
        IReadOnlyCollection<CatalogNpcOffering>? offerings) =>
        new(key, name, "description", [], [], [], [], Offerings: offerings);

    private static CatalogItemDefinitionSnapshot Item(
        string key,
        IReadOnlyCollection<CatalogItemEquipmentEffect>? effects) =>
        new(key, "1", key, "description", null, "Equipment", "Accessory", "Common",
            "Equip", "Persistent", "None", 1, false, false, EquipmentEffects: effects);
}
