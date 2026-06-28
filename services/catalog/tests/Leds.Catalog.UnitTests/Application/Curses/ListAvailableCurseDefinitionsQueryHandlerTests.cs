using FluentAssertions;
using Leds.Catalog.Application.Curses.Dtos;
using Leds.Catalog.Application.Curses.ListAvailableCurseDefinitions;
using Leds.Catalog.Application.Curses.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Curses;
using NSubstitute;

namespace Leds.Catalog.UnitTests.Application.Curses;

public sealed class ListAvailableCurseDefinitionsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnDtos_WhenDtoReadStoreIsAvailable()
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
        ((ICurseDefinitionDtoReadStore)readStore).ListAvailableDtosAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<CurseDefinitionDto>>(new[] { dto }));

        var handler = new ListAvailableCurseDefinitionsQueryHandler(readStore);

        var response = await handler.Handle(
            new ListAvailableCurseDefinitionsQuery(),
            CancellationToken.None);

        response.Definitions.Should().ContainSingle();
        response.Definitions.Single().Key.Should().Be("curse-shadow-blight");
    }

    [Fact]
    public async Task Handle_ShouldReturnMappedDtos_WhenUsingDomainReadStore()
    {
        var curse = CurseDefinition.Create(
            "curse-test",
            "Test Curse",
            "Description",
            "catalog-0.1.0",
            narrativeText: null,
            severity: 3,
            duration: "Permanent",
            trigger: null,
            effectSetKey: null,
            status: CatalogContentStatus.Active);

        var readStore = Substitute.For<ICurseDefinitionReadStore>();
        readStore.ListAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<ICurseDefinition>>(new[] { curse }));

        var handler = new ListAvailableCurseDefinitionsQueryHandler(readStore);

        var response = await handler.Handle(
            new ListAvailableCurseDefinitionsQuery(),
            CancellationToken.None);

        response.Definitions.Should().ContainSingle();

        var dto = response.Definitions.Single();
        dto.Key.Should().Be("curse-test");
        dto.DisplayName.Should().Be("Test Curse");
        dto.Severity.Should().Be(3);
        dto.Duration.Should().Be("Permanent");
        dto.Status.Should().Be("Active");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyCollection_WhenNoDefinitionsAvailable()
    {
        var readStore = Substitute.For<ICurseDefinitionReadStore>();
        readStore.ListAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<ICurseDefinition>>(Array.Empty<ICurseDefinition>()));

        var handler = new ListAvailableCurseDefinitionsQueryHandler(readStore);

        var response = await handler.Handle(
            new ListAvailableCurseDefinitionsQuery(),
            CancellationToken.None);

        response.Definitions.Should().BeEmpty();
    }
}
