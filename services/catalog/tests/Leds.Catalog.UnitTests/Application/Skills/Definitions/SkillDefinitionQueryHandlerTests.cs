using FluentAssertions;
using Leds.Catalog.Application.Skills.Definitions.Dtos;
using Leds.Catalog.Application.Skills.Definitions.GetSkillDefinitionByKey;
using Leds.Catalog.Application.Skills.Definitions.ListActiveSkillDefinitions;
using Leds.Catalog.Application.Skills.Definitions.ListSkillDefinitionsByKeys;
using Leds.Catalog.Application.Skills.Definitions.ListSkillDefinitionsByType;
using Leds.Catalog.Application.Skills.Definitions.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Skills.Definitions;
using NSubstitute;

namespace Leds.Catalog.UnitTests.Application.Skills.Definitions;

public sealed class SkillDefinitionQueryHandlerTests
{
    [Fact]
    public async Task ListActive_ShouldReturnMappedDefinitions()
    {
        var definition = CreateSkillDefinition();

        var readStore = Substitute.For<ISkillDefinitionReadStore>();
        readStore.ListActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<ISkillDefinition>>(
                new[] { definition }));

        var handler = new ListActiveSkillDefinitionsQueryHandler(readStore);

        var response = await handler.Handle(
            new ListActiveSkillDefinitionsQuery(),
            CancellationToken.None);

        response.Definitions.Should().ContainSingle();

        var dto = response.Definitions.Single();
        dto.Key.Should().Be("skill.basic.strike");
        dto.Status.Should().Be("Active");
        dto.SkillType.Should().Be("Damage");
        dto.TargetingType.Should().Be("SingleEnemy");
        dto.EffectType.Should().Be("Damage");
        dto.ManaCost.Should().Be(5);
        dto.ChargeCost.Should().Be(0);
        dto.BasePower.Should().Be(10);
    }

    [Fact]
    public async Task GetByKey_ShouldReturnDefinition_WhenExists()
    {
        var definition = CreateSkillDefinition();

        var readStore = Substitute.For<ISkillDefinitionReadStore>();
        readStore.GetByKeyAsync("skill.basic.strike", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ISkillDefinition?>(definition));

        var handler = new GetSkillDefinitionByKeyQueryHandler(readStore);

        var response = await handler.Handle(
            new GetSkillDefinitionByKeyQuery("skill.basic.strike"),
            CancellationToken.None);

        response.Definition.Should().NotBeNull();
        response.Definition!.Key.Should().Be("skill.basic.strike");
    }

    [Fact]
    public async Task GetByKey_ShouldReturnNull_WhenNotExists()
    {
        var readStore = Substitute.For<ISkillDefinitionReadStore>();
        readStore.GetByKeyAsync("unknown", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ISkillDefinition?>(null));

        var handler = new GetSkillDefinitionByKeyQueryHandler(readStore);

        var response = await handler.Handle(
            new GetSkillDefinitionByKeyQuery("unknown"),
            CancellationToken.None);

        response.Definition.Should().BeNull();
    }

    [Fact]
    public async Task ListByType_ShouldReturnDefinitions_WhenTypeExists()
    {
        var definition = CreateSkillDefinition();

        var readStore = Substitute.For<ISkillDefinitionReadStore>();
        readStore.ListByTypeAsync("Damage", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<ISkillDefinition>>(
                new[] { definition }));

        var handler = new ListSkillDefinitionsByTypeQueryHandler(readStore);

        var response = await handler.Handle(
            new ListSkillDefinitionsByTypeQuery("Damage"),
            CancellationToken.None);

        response.Definitions.Should().ContainSingle();
        response.Definitions.Single().SkillType.Should().Be("Damage");
    }

    [Fact]
    public async Task ListByKeys_ShouldReturnDefinitions_WhenKeysMatch()
    {
        var definition = CreateSkillDefinition();

        var readStore = Substitute.For<ISkillDefinitionReadStore>();
        readStore.ListByKeysAsync(
                Arg.Is<IReadOnlyCollection<string>>(k => k.Contains("skill.basic.strike")),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<ISkillDefinition>>(
                new[] { definition }));

        var handler = new ListSkillDefinitionsByKeysQueryHandler(readStore);

        var response = await handler.Handle(
            new ListSkillDefinitionsByKeysQuery(["skill.basic.strike"]),
            CancellationToken.None);

        response.Definitions.Should().ContainSingle();
        response.Definitions.Single().Key.Should().Be("skill.basic.strike");
    }

    private static SkillDefinition CreateSkillDefinition()
    {
        var def = SkillDefinition.Create(
            "skill.basic.strike",
            "Frappe",
            "Une attaque de base.",
            "1.0.0",
            "Damage",
            "SingleEnemy",
            "Damage",
            manaCost: 5,
            chargeCost: 0,
            basePower: 10,
            status: CatalogContentStatus.Draft,
            emotionalRegister: "Neutral");

        def.Activate();

        return def;
    }
}
