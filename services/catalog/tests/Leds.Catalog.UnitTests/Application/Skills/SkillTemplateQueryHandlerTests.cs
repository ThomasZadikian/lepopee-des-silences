using FluentAssertions;
using Leds.Catalog.Application.Skills.GetSkillTemplateByKey;
using Leds.Catalog.Application.Skills.ListActiveSkillTemplates;
using Leds.Catalog.Application.Skills.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Combat;
using Leds.Catalog.Domain.Skills;
using NSubstitute;

namespace Leds.Catalog.UnitTests.Application.Skills;

public sealed class SkillTemplateQueryHandlerTests
{
    [Fact]
    public async Task GetByKey_ShouldReturnTemplate_WhenTemplateExists()
    {
        var template = CreateSkillTemplate();

        var readStore = Substitute.For<ISkillTemplateReadStore>();
        readStore.GetByKeyAsync("skill-shadow-bite", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ISkillTemplate?>(template));

        var handler = new GetSkillTemplateByKeyQueryHandler(readStore);

        var response = await handler.Handle(
            new GetSkillTemplateByKeyQuery("skill-shadow-bite"),
            CancellationToken.None);

        response.Template.Should().NotBeNull();
        response.Template!.Key.Should().Be("skill-shadow-bite");
        response.Template.Name.Should().Be("Morsure d’Ombre");
        response.Template.Element.Should().Be("Shadow");
        response.Template.EffectType.Should().Be("Damage");
        response.Template.TargetType.Should().Be("SingleEnemy");
    }

    [Fact]
    public async Task GetByKey_ShouldReturnNullTemplate_WhenTemplateDoesNotExist()
    {
        var readStore = Substitute.For<ISkillTemplateReadStore>();
        readStore.GetByKeyAsync("unknown", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ISkillTemplate?>(null));

        var handler = new GetSkillTemplateByKeyQueryHandler(readStore);

        var response = await handler.Handle(
            new GetSkillTemplateByKeyQuery("unknown"),
            CancellationToken.None);

        response.Template.Should().BeNull();
    }

    [Fact]
    public async Task ListActive_ShouldReturnMappedTemplates()
    {
        var template = CreateSkillTemplate();

        var readStore = Substitute.For<ISkillTemplateReadStore>();
        readStore.ListActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<ISkillTemplate>>(
                new[] { template }));

        var handler = new ListActiveSkillTemplatesQueryHandler(readStore);

        var response = await handler.Handle(
            new ListActiveSkillTemplatesQuery(),
            CancellationToken.None);

        response.Templates.Should().ContainSingle();

        var dto = response.Templates.Single();

        dto.Key.Should().Be("skill-shadow-bite");
        dto.Name.Should().Be("Morsure d’Ombre");
        dto.Status.Should().Be("Active");
    }

    private static SkillTemplate CreateSkillTemplate()
    {
        return SkillTemplate.Create(
            "skill-shadow-bite",
            "Morsure d’Ombre",
            "Une attaque de rupture silencieuse.",
            "catalog-0.1.0",
            CombatElement.Shadow,
            SkillEffectType.Damage,
            SkillTargetType.SingleEnemy,
            manaCost: 3,
            chargeCost: 1,
            basePower: 35,
            healPower: 0,
            status: CatalogContentStatus.Active);
    }
}