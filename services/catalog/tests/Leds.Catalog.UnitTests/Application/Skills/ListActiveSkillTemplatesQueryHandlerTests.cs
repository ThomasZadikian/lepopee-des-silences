using FluentAssertions;
using Leds.Catalog.Application.Skills.Dtos;
using Leds.Catalog.Application.Skills.ListActiveSkillTemplates;
using Leds.Catalog.Application.Skills.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Combat;
using Leds.Catalog.Domain.Skills;
using NSubstitute;

namespace Leds.Catalog.UnitTests.Application.Skills;

public sealed class ListActiveSkillTemplatesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnMappedTemplates_WhenTemplatesExist()
    {
        var template = SkillTemplate.Create(
            "skill-shadow-bite",
            "Morsure d'Ombre",
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

        var readStore = Substitute.For<ISkillTemplateReadStore>();
        readStore.ListActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<ISkillTemplate>>(new[] { template }));

        var handler = new ListActiveSkillTemplatesQueryHandler(readStore);

        var response = await handler.Handle(
            new ListActiveSkillTemplatesQuery(),
            CancellationToken.None);

        response.Templates.Should().ContainSingle();

        var dto = response.Templates.Single();
        dto.Key.Should().Be("skill-shadow-bite");
        dto.Name.Should().Be("Morsure d'Ombre");
        dto.Status.Should().Be("Active");
        dto.Element.Should().Be("Shadow");
        dto.EffectType.Should().Be("Damage");
        dto.TargetType.Should().Be("SingleEnemy");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyCollection_WhenNoTemplatesExist()
    {
        var readStore = Substitute.For<ISkillTemplateReadStore>();
        readStore.ListActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<ISkillTemplate>>(Array.Empty<ISkillTemplate>()));

        var handler = new ListActiveSkillTemplatesQueryHandler(readStore);

        var response = await handler.Handle(
            new ListActiveSkillTemplatesQuery(),
            CancellationToken.None);

        response.Templates.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnMultipleTemplates_WhenMultipleExist()
    {
        var skill1 = SkillTemplate.Create(
            "skill-1", "Skill 1", "Desc", "v1",
            CombatElement.Neutral, SkillEffectType.Damage, SkillTargetType.SingleEnemy,
            manaCost: 1, chargeCost: 0, basePower: 10, healPower: 0,
            status: CatalogContentStatus.Active);

        var skill2 = SkillTemplate.Create(
            "skill-2", "Skill 2", "Desc", "v1",
            CombatElement.Light, SkillEffectType.Heal, SkillTargetType.Self,
            manaCost: 2, chargeCost: 0, basePower: 0, healPower: 20,
            status: CatalogContentStatus.Active);

        var readStore = Substitute.For<ISkillTemplateReadStore>();
        readStore.ListActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<ISkillTemplate>>(new[] { skill1, skill2 }));

        var handler = new ListActiveSkillTemplatesQueryHandler(readStore);

        var response = await handler.Handle(
            new ListActiveSkillTemplatesQuery(),
            CancellationToken.None);

        response.Templates.Should().HaveCount(2);
        response.Templates.Should().Contain(t => t.Key == "skill-1");
        response.Templates.Should().Contain(t => t.Key == "skill-2");
    }
}
