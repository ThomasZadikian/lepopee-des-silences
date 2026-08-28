using FluentAssertions;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.UnitTests.Common;

namespace Leds.GameEngine.UnitTests.Catalog;

public sealed class CatalogLookupQueryHandlerCoverageTests
{
    [Fact]
    public async Task EmotionalRegisterHandler_ShouldReturnGatewayCatalog()
    {
        var gateway = new StubCatalogContentGateway();
        var sut = new GetEmotionalRegisterCatalogQueryHandler(gateway);

        var result = await sut.Handle(new GetEmotionalRegisterCatalogQuery(), CancellationToken.None);

        result.Should().BeSameAs(gateway.EmotionalRegisterCatalog);
    }

    [Fact]
    public async Task ItemRarityHandler_ShouldReturnGatewayCatalog()
    {
        var gateway = new StubCatalogContentGateway();
        var sut = new GetItemRarityCatalogQueryHandler(gateway);

        var result = await sut.Handle(new GetItemRarityCatalogQuery(), CancellationToken.None);

        result.Should().BeSameAs(gateway.ItemRarityCatalog);
    }

    [Fact]
    public async Task ItemTypeHandler_ShouldReturnGatewayCatalog()
    {
        var gateway = new StubCatalogContentGateway();
        var sut = new GetItemTypeCatalogQueryHandler(gateway);

        var result = await sut.Handle(new GetItemTypeCatalogQuery(), CancellationToken.None);

        result.Should().BeSameAs(gateway.ItemTypeCatalog);
    }
}
