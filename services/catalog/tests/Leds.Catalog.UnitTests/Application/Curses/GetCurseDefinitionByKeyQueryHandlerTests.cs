using FluentAssertions;
using Leds.Catalog.Application.Curses.Dtos;
using Leds.Catalog.Application.Curses.GetCurseDefinitionByKey;
using Leds.Catalog.Application.Curses.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Curses;
using NSubstitute;

namespace Leds.Catalog.UnitTests.Application.Curses;

public sealed class GetCurseDefinitionByKeyQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnDto_WhenDtoReadStoreIsAvailable()
    {
        var dto = new CurseDefinitionDto(
            Id: Guid.NewGuid(),
            Key: "curse-shadow-blight",
            Version: "catalog-0.1.0",
            DisplayName: "Fléau d'Ombre",
            Description: "Une malédiction sombre.",
            NarrativeText: "L'ombre s'infiltre.",
            Severity: 5,
            Duration: "NextCombatOnly",
            Trigger: "OnCombatStart",
            EffectSetKey: "effect-shadow",
            Status: "Active");

        var readStore = Substitute.For<ICurseDefinitionReadStore, ICurseDefinitionDtoReadStore>();
        ((ICurseDefinitionDtoReadStore)readStore).GetDtoByKeyAsync("curse-shadow-blight", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CurseDefinitionDto?>(dto));

        var handler = new GetCurseDefinitionByKeyQueryHandler(readStore);

        var response = await handler.Handle(
            new GetCurseDefinitionByKeyQuery("curse-shadow-blight"),
            CancellationToken.None);

        response.Definition.Should().NotBeNull();
        response.Definition!.Key.Should().Be("curse-shadow-blight");
        response.Definition.Severity.Should().Be(5);
    }

    [Fact]
    public async Task Handle_ShouldReturnMappedDto_WhenUsingDomainReadStore()
    {
        var curse = CurseDefinition.Create(
            "curse-test",
            "Test Curse",
            "Description",
            "catalog-0.1.0",
            narrativeText: "Narrative text",
            severity: 7,
            duration: "RunOnly",
            trigger: "OnHit",
            effectSetKey: "effect-test",
            status: CatalogContentStatus.Active);

        var readStore = Substitute.For<ICurseDefinitionReadStore>();
        readStore.GetByKeyAsync("curse-test", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ICurseDefinition?>(curse));

        var handler = new GetCurseDefinitionByKeyQueryHandler(readStore);

        var response = await handler.Handle(
            new GetCurseDefinitionByKeyQuery("curse-test"),
            CancellationToken.None);

        response.Definition.Should().NotBeNull();
        response.Definition!.Key.Should().Be("curse-test");
        response.Definition.DisplayName.Should().Be("Test Curse");
        response.Definition.Severity.Should().Be(7);
        response.Definition.Duration.Should().Be("RunOnly");
        response.Definition.Trigger.Should().Be("OnHit");
        response.Definition.EffectSetKey.Should().Be("effect-test");
    }

    [Fact]
    public async Task Handle_ShouldReturnNullDefinition_WhenCurseDoesNotExist()
    {
        var readStore = Substitute.For<ICurseDefinitionReadStore>();
        readStore.GetByKeyAsync("unknown-curse", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ICurseDefinition?>(null));

        var handler = new GetCurseDefinitionByKeyQueryHandler(readStore);

        var response = await handler.Handle(
            new GetCurseDefinitionByKeyQuery("unknown-curse"),
            CancellationToken.None);

        response.Definition.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnNullDto_WhenDtoReadStoreReturnsNull()
    {
        var readStore = Substitute.For<ICurseDefinitionReadStore, ICurseDefinitionDtoReadStore>();
        ((ICurseDefinitionDtoReadStore)readStore).GetDtoByKeyAsync("unknown", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CurseDefinitionDto?>(null));

        var handler = new GetCurseDefinitionByKeyQueryHandler(readStore);

        var response = await handler.Handle(
            new GetCurseDefinitionByKeyQuery("unknown"),
            CancellationToken.None);

        response.Definition.Should().BeNull();
    }
}
