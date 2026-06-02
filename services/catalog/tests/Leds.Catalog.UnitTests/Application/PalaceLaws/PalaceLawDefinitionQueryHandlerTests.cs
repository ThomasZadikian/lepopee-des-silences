using FluentAssertions;
using Leds.Catalog.Application.PalaceLaws.GetPalaceLawDefinitionByKey;
using Leds.Catalog.Application.PalaceLaws.ListActivePalaceLawDefinitions;
using Leds.Catalog.Application.PalaceLaws.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.PalaceLaws;
using NSubstitute;

namespace Leds.Catalog.UnitTests.Application.PalaceLaws;

public sealed class PalaceLawDefinitionQueryHandlerTests
{
    [Fact]
    public async Task GetByKey_ShouldReturnDefinition_WhenDefinitionExists()
    {
        var definition = CreatePalaceLawDefinition();

        var readStore = Substitute.For<IPalaceLawDefinitionReadStore>();
        readStore.GetByKeyAsync("law-silence-v1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IPalaceLawDefinition?>(definition));

        var handler = new GetPalaceLawDefinitionByKeyQueryHandler(readStore);

        var response = await handler.Handle(
            new GetPalaceLawDefinitionByKeyQuery("law-silence-v1"),
            CancellationToken.None);

        response.Definition.Should().NotBeNull();

        response.Definition!.Key.Should().Be("law-silence-v1");
        response.Definition.Name.Should().Be("Loi du Silence");
        response.Definition.Version.Should().Be("law-1.0.0");
        response.Definition.Status.Should().Be("Active");
        response.Definition.Visibility.Should().Be("PartiallyVisible");
        response.Definition.Priority.Should().Be(10);
        response.Definition.ImpactDomains.Should().BeEquivalentTo(
            "Generation",
            "Narrative");
    }

    [Fact]
    public async Task GetByKey_ShouldReturnNullDefinition_WhenDefinitionDoesNotExist()
    {
        var readStore = Substitute.For<IPalaceLawDefinitionReadStore>();
        readStore.GetByKeyAsync("unknown-law", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IPalaceLawDefinition?>(null));

        var handler = new GetPalaceLawDefinitionByKeyQueryHandler(readStore);

        var response = await handler.Handle(
            new GetPalaceLawDefinitionByKeyQuery("unknown-law"),
            CancellationToken.None);

        response.Definition.Should().BeNull();
    }

    [Fact]
    public async Task ListActive_ShouldReturnMappedDefinitions()
    {
        var definition = CreatePalaceLawDefinition();

        var readStore = Substitute.For<IPalaceLawDefinitionReadStore>();
        readStore.ListActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<IPalaceLawDefinition>>(
                new[] { definition }));

        var handler = new ListActivePalaceLawDefinitionsQueryHandler(readStore);

        var response = await handler.Handle(
            new ListActivePalaceLawDefinitionsQuery(),
            CancellationToken.None);

        response.Definitions.Should().ContainSingle();

        var dto = response.Definitions.Single();

        dto.Key.Should().Be("law-silence-v1");
        dto.Name.Should().Be("Loi du Silence");
        dto.Status.Should().Be("Active");
        dto.ImpactDomains.Should().BeEquivalentTo(
            "Generation",
            "Narrative");
    }

    private static PalaceLawDefinition CreatePalaceLawDefinition()
    {
        var definition = PalaceLawDefinition.Create(
            "law-silence-v1",
            "Loi du Silence",
            "Le silence déforme la génération et modifie les fragments narratifs.",
            "law-1.0.0",
            PalaceLawVisibility.PartiallyVisible,
            priority: 10,
            new[]
            {
                PalaceLawImpactDomain.Generation,
                PalaceLawImpactDomain.Narrative
            },
            status: CatalogContentStatus.Draft);

        definition.Activate();

        return definition;
    }
}