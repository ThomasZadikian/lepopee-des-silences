using FluentAssertions;
using Leds.Catalog.Application.PalaceLaws.Dtos;
using Leds.Catalog.Application.PalaceLaws.ListActivePalaceLawDefinitions;
using Leds.Catalog.Application.PalaceLaws.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.PalaceLaws;
using NSubstitute;

namespace Leds.Catalog.UnitTests.Application.PalaceLaws;

public sealed class ListActivePalaceLawDefinitionsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnDtos_WhenDtoReadStoreIsAvailable()
    {
        var dto = new PalaceLawDefinitionDto(
            Id: Guid.NewGuid(),
            Key: "law-silence-v1",
            Version: "law-1.0.0",
            Name: "Loi du Silence",
            Description: "Le silence déforme.",
            Status: "Active",
            Visibility: "PartiallyVisible",
            Priority: 10,
            ImpactDomains: new[] { "Generation", "Narrative" });

        var readStore = Substitute.For<IPalaceLawDefinitionReadStore, IPalaceLawDefinitionDtoReadStore>();
        ((IPalaceLawDefinitionDtoReadStore)readStore).ListActiveDtosAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<PalaceLawDefinitionDto>>(new[] { dto }));

        var handler = new ListActivePalaceLawDefinitionsQueryHandler(readStore);

        var response = await handler.Handle(
            new ListActivePalaceLawDefinitionsQuery(),
            CancellationToken.None);

        response.Definitions.Should().ContainSingle();
        response.Definitions.Single().Key.Should().Be("law-silence-v1");
    }

    [Fact]
    public async Task Handle_ShouldReturnMappedDefinitions_WhenUsingDomainReadStore()
    {
        var definition = PalaceLawDefinition.Create(
            "law-test",
            "Test Law",
            "Description",
            "law-1.0.0",
            PalaceLawVisibility.Hidden,
            priority: 5,
            new[] { PalaceLawImpactDomain.Combat },
            status: CatalogContentStatus.Active);

        var readStore = Substitute.For<IPalaceLawDefinitionReadStore>();
        readStore.ListActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<IPalaceLawDefinition>>(new[] { definition }));

        var handler = new ListActivePalaceLawDefinitionsQueryHandler(readStore);

        var response = await handler.Handle(
            new ListActivePalaceLawDefinitionsQuery(),
            CancellationToken.None);

        response.Definitions.Should().ContainSingle();

        var dto = response.Definitions.Single();
        dto.Key.Should().Be("law-test");
        dto.Name.Should().Be("Test Law");
        dto.Status.Should().Be("Active");
        dto.Visibility.Should().Be("Hidden");
        dto.Priority.Should().Be(5);
        dto.ImpactDomains.Should().BeEquivalentTo("Combat");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyCollection_WhenNoDefinitionsExist()
    {
        var readStore = Substitute.For<IPalaceLawDefinitionReadStore>();
        readStore.ListActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<IPalaceLawDefinition>>(Array.Empty<IPalaceLawDefinition>()));

        var handler = new ListActivePalaceLawDefinitionsQueryHandler(readStore);

        var response = await handler.Handle(
            new ListActivePalaceLawDefinitionsQuery(),
            CancellationToken.None);

        response.Definitions.Should().BeEmpty();
    }
}
