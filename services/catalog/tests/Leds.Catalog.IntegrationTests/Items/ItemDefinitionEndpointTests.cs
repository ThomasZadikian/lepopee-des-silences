using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Leds.Catalog.IntegrationTests.Items;

public sealed class ItemDefinitionEndpointTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ItemDefinitionEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetItemDefinitionByKey_ShouldReportPermanentEligible_ForPermanentDurationItem()
    {
        // canon.item.tome-38 is seeded with Duration="Permanent" (a unique lore relic) —
        // this asserts that flag now genuinely drives IsPermanentEligible end-to-end.
        var response = await _client.GetAsync("/api/v2/catalog/item-definitions/canon.item.tome-38");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);

        var payload = await response.Content.ReadFromJsonAsync<GetItemDefinitionByKeyResponse>();

        payload.Should().NotBeNull();
        payload!.Definition.Should().NotBeNull();
        payload.Definition!.IsPermanentEligible.Should().BeTrue();
        payload.Definition.EquipmentEffects.Should().NotBeNull();
    }

    [Fact]
    public async Task GetItemDefinitionByKey_ShouldReportNotPermanentEligible_ForRunOnlyDurationItem()
    {
        // canon.item.lanterne is seeded with Duration="RunOnly".
        var response = await _client.GetAsync("/api/v2/catalog/item-definitions/canon.item.lanterne");
        var payload = await response.Content.ReadFromJsonAsync<GetItemDefinitionByKeyResponse>();

        payload.Should().NotBeNull();
        payload!.Definition.Should().NotBeNull();
        payload.Definition!.IsPermanentEligible.Should().BeFalse();
    }

    [Fact]
    public async Task GetItemDefinitionByKey_ShouldReportContainer_ForFioleDeCristal()
    {
        // canon.item.fiole-cristal is seeded with IsContainer=true, ContainerCapacity=1.
        var response = await _client.GetAsync("/api/v2/catalog/item-definitions/canon.item.fiole-cristal");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);

        var payload = await response.Content.ReadFromJsonAsync<GetItemDefinitionByKeyResponse>();

        payload.Should().NotBeNull();
        payload!.Definition.Should().NotBeNull();
        payload.Definition!.IsContainer.Should().BeTrue();
        payload.Definition.ContainerCapacity.Should().Be(1);
        payload.Definition.IsLiquid.Should().BeFalse();
    }

    [Fact]
    public async Task GetItemDefinitionByKey_ShouldReportNotContainer_ForRegularItem()
    {
        var response = await _client.GetAsync("/api/v2/catalog/item-definitions/canon.item.lanterne");
        var payload = await response.Content.ReadFromJsonAsync<GetItemDefinitionByKeyResponse>();

        payload.Should().NotBeNull();
        payload!.Definition.Should().NotBeNull();
        payload.Definition!.IsContainer.Should().BeFalse();
        payload.Definition.ContainerCapacity.Should().BeNull();
    }

    [Fact]
    public async Task GetItemDefinitionByKey_ShouldExposeDamageReductionEquipmentEffect_ForCraieCreatrice()
    {
        // canon.item.craie-creatrice is seeded with a DamageReductionByType equipment effect.
        var response = await _client.GetAsync("/api/v2/catalog/item-definitions/canon.item.craie-creatrice");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);

        var payload = await response.Content.ReadFromJsonAsync<GetItemDefinitionByKeyResponse>();

        payload.Should().NotBeNull();
        payload!.Definition.Should().NotBeNull();
        payload.Definition!.EquipmentEffects.Should().ContainSingle();
    }

    [Fact]
    public async Task ListActive_ShouldReturnSeededItems_IncludingTome38()
    {
        var response = await _client.GetAsync("/api/v2/catalog/item-definitions");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);

        var payload = await response.Content.ReadFromJsonAsync<ListActiveItemDefinitionsResponse>();

        payload.Should().NotBeNull();
        payload!.Definitions.Should().NotBeEmpty();
        payload.Definitions.Should().Contain(d => d.Key == "canon.item.tome-38");
    }

    [Fact]
    public async Task GetItemDefinitionByKey_ShouldExposeFixedRarityCost_ForUniqueItem()
    {
        // canon.item.tome-38 is seeded with Rarity="Unique" → 1000 Éclats du Palais + 75 Éclats de Him'Lit.
        var response = await _client.GetAsync("/api/v2/catalog/item-definitions/canon.item.tome-38");
        var payload = await response.Content.ReadFromJsonAsync<GetItemDefinitionByKeyResponse>();

        payload.Should().NotBeNull();
        payload!.Definition.Should().NotBeNull();
        payload.Definition!.PalaceShardCost.Should().Be(1000);
        payload.Definition.HimLitShardCost.Should().Be(75);
    }

    [Fact]
    public async Task GetItemDefinitionByKey_ShouldExposeFixedRarityCost_ForRareItem()
    {
        // canon.item.carnet-pomenian is seeded with Rarity="Rare" → 350 Éclats du Palais, no Him'Lit.
        var response = await _client.GetAsync("/api/v2/catalog/item-definitions/canon.item.carnet-pomenian");
        var payload = await response.Content.ReadFromJsonAsync<GetItemDefinitionByKeyResponse>();

        payload.Should().NotBeNull();
        payload!.Definition.Should().NotBeNull();
        payload.Definition!.PalaceShardCost.Should().Be(350);
        payload.Definition.HimLitShardCost.Should().Be(0);
    }

    private sealed record ListActiveItemDefinitionsResponse(IReadOnlyCollection<ItemDefinitionResponseDto> Definitions);

    private sealed record GetItemDefinitionByKeyResponse(ItemDefinitionResponseDto? Definition);

    private sealed record ItemDefinitionResponseDto(
        Guid Id, string Key, string Version, string DisplayName, string Description, string? NarrativeText,
        string Category, string FlavorTag, string Rarity, string UsageMode, string Lifecycle, string StackPolicy,
        int MaxStack, bool IsUsableInCombat, bool IsUsableOutsideCombat, string Status,
        bool IsPermanentEligible, IReadOnlyCollection<object> EquipmentEffects,
        bool IsContainer, int? ContainerCapacity, bool IsLiquid,
        int PalaceShardCost, int HimLitShardCost);
}
