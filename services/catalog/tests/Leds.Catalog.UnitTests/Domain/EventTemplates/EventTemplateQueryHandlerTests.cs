using FluentAssertions;
using Leds.Catalog.Application.EventTemplates.GetEventTemplateByKey;
using Leds.Catalog.Application.EventTemplates.ListActiveEventTemplates;
using Leds.Catalog.Application.EventTemplates.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.EventTemplates;
using NSubstitute;

namespace Leds.Catalog.UnitTests.Domain.EventTemplates;

public sealed class EventTemplateQueryHandlerTests
{
    [Fact]
    public async Task GetByKey_ShouldReturnTemplate_WhenTemplateExists()
    {
        var template = CreateEventTemplate();

        var readStore = Substitute.For<IEventTemplateReadStore>();
        readStore.GetByKeyAsync("event-memory-threshold-v1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEventTemplate?>(template));

        var handler = new GetEventTemplateByKeyQueryHandler(readStore);

        var response = await handler.Handle(
            new GetEventTemplateByKeyQuery("event-memory-threshold-v1"),
            CancellationToken.None);

        response.Template.Should().NotBeNull();

        response.Template!.Key.Should().Be("event-memory-threshold-v1");
        response.Template.Name.Should().Be("Mémoire du seuil");
        response.Template.Version.Should().Be("event-1.0.0");
        response.Template.Status.Should().Be("Active");
        response.Template.Type.Should().Be("Memory");
        response.Template.DefaultOutcomeKind.Should().Be("TomePageUnlocked");
        response.Template.MinRiskLevel.Should().Be(5);
        response.Template.MaxRiskLevel.Should().Be(20);
        response.Template.RequiresPlayerChoice.Should().BeTrue();
        response.Template.NarrativeTags.Should().BeEquivalentTo(
            "memory",
            "threshold",
            "elise");
    }

    [Fact]
    public async Task GetByKey_ShouldReturnNullTemplate_WhenTemplateDoesNotExist()
    {
        var readStore = Substitute.For<IEventTemplateReadStore>();
        readStore.GetByKeyAsync("unknown-event", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEventTemplate?>(null));

        var handler = new GetEventTemplateByKeyQueryHandler(readStore);

        var response = await handler.Handle(
            new GetEventTemplateByKeyQuery("unknown-event"),
            CancellationToken.None);

        response.Template.Should().BeNull();
    }

    [Fact]
    public async Task ListActive_ShouldReturnMappedTemplates()
    {
        var template = CreateEventTemplate();

        var readStore = Substitute.For<IEventTemplateReadStore>();
        readStore.ListActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<IEventTemplate>>(
                new[] { template }));

        var handler = new ListActiveEventTemplatesQueryHandler(readStore);

        var response = await handler.Handle(
            new ListActiveEventTemplatesQuery(),
            CancellationToken.None);

        response.Templates.Should().ContainSingle();

        var dto = response.Templates.Single();

        dto.Key.Should().Be("event-memory-threshold-v1");
        dto.Name.Should().Be("Mémoire du seuil");
        dto.Status.Should().Be("Active");
        dto.NarrativeTags.Should().BeEquivalentTo(
            "memory",
            "threshold",
            "elise");
    }

    private static EventTemplate CreateEventTemplate()
    {
        var template = EventTemplate.Create(
            "event-memory-threshold-v1",
            "Mémoire du seuil",
            "Une mémoire apparaît à l’entrée du Palais.",
            "event-1.0.0",
            EventTemplateType.Memory,
            EventOutcomeKind.TomePageUnlocked,
            minRiskLevel: 5,
            maxRiskLevel: 20,
            requiresPlayerChoice: true,
            new[]
            {
                "memory",
                "threshold",
                "elise"
            },
            status: CatalogContentStatus.Draft);

        template.Activate();

        return template;
    }
}