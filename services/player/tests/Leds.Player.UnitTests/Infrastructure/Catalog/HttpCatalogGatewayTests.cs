using System.Net;
using System.Text;
using FluentAssertions;
using Leds.Player.Domain.Players;
using Leds.Player.Infrastructure.Catalog;

namespace Leds.Player.UnitTests.Infrastructure.Catalog;

public sealed class HttpCatalogGatewayTests
{
    [Fact]
    public void Options_ShouldUseLocalCatalogEndpointByDefault()
    {
        new CatalogGatewayOptions().BaseUrl.Should().Be("http://localhost:5193");
    }

    [Fact]
    public async Task EquipmentGateway_ShouldReturnNullForMissingDefinition()
    {
        var gateway = new HttpEquipmentDefinitionGateway(Client(HttpStatusCode.NotFound));

        var result = await gateway.GetByKeyAsync("item/missing", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task EquipmentGateway_ShouldMapCompleteDefinition()
    {
        const string json = """
        {"definition":{"key":"item.staff","displayName":"Staff","allowedSlots":["MainWeapon"],
        "uniqueEquipGroup":"staff","proficiencyTags":["arcane"],"equipmentEffects":[
        {"kind":"StatBonus","statKind":"Mana","amount":4,"skillKey":null}]}}
        """;
        var gateway = new HttpEquipmentDefinitionGateway(Client(HttpStatusCode.OK, json));

        var result = await gateway.GetByKeyAsync("item.staff", CancellationToken.None);

        result.Should().NotBeNull();
        result!.AllowedSlots.Should().ContainSingle("MainWeapon");
        result.ProficiencyTags.Should().ContainSingle("arcane");
        result.EquipmentEffects.Should().ContainSingle()
            .Which.Amount.Should().Be(4);
    }

    [Fact]
    public async Task EquipmentGateway_ShouldDefaultNullableCollections()
    {
        const string json = """
        {"definition":{"key":"item.stone","displayName":"Stone","allowedSlots":null,
        "uniqueEquipGroup":null,"proficiencyTags":null,"equipmentEffects":null}}
        """;
        var gateway = new HttpEquipmentDefinitionGateway(Client(HttpStatusCode.OK, json));

        var result = await gateway.GetByKeyAsync("item.stone", CancellationToken.None);

        result!.AllowedSlots.Should().BeEmpty();
        result.ProficiencyTags.Should().BeEmpty();
        result.EquipmentEffects.Should().BeEmpty();
    }

    [Fact]
    public async Task EquipmentGateway_ShouldRejectEmptyAndUnsuccessfulResponses()
    {
        var empty = new HttpEquipmentDefinitionGateway(Client(HttpStatusCode.OK, "{}"));
        var failed = new HttpEquipmentDefinitionGateway(Client(HttpStatusCode.BadGateway));

        await empty.Invoking(gateway => gateway.GetByKeyAsync("item.empty", CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();
        await failed.Invoking(gateway => gateway.GetByKeyAsync("item.failed", CancellationToken.None))
            .Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task ArchetypeGateway_ShouldReturnNullForMissingDefinition()
    {
        var gateway = new HttpArchetypeDefinitionGateway(Client(HttpStatusCode.NotFound));

        var result = await gateway.GetByKeyAsync("archetype/missing", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ArchetypeGateway_ShouldMapCompleteDefinition()
    {
        const string json = """
        {"definition":{"key":"archetype.mage","baseStats":{"maxVitality":90,"attackPower":4,
        "magicAttack":12,"defense":3,"magicDefense":8,"startingGuard":1,"speed":7,
        "initiative":9,"focus":11,"mana":80,"charge":2,"movement":5},
        "proficiencyTags":["arcane"],"starterEquipment":[{"itemDefinitionKey":"item.staff",
        "equipmentPosition":"mainweapon"}],"starterKnownSkills":["skill.spark"],
        "starterEquippedSkills":["skill.spark"]}}
        """;
        var gateway = new HttpArchetypeDefinitionGateway(Client(HttpStatusCode.OK, json));

        var result = await gateway.GetByKeyAsync("archetype.mage", CancellationToken.None);

        result.Should().NotBeNull();
        result!.BaseStats.MagicAttack.Should().Be(12);
        result.StarterEquipment.Should().ContainSingle()
            .Which.Position.Should().Be(EquipmentPosition.MainWeapon);
    }

    [Fact]
    public async Task ArchetypeGateway_ShouldDefaultNullableCollections()
    {
        const string json = """
        {"definition":{"key":"archetype.empty","baseStats":{"maxVitality":1,"attackPower":0,
        "magicAttack":0,"defense":0,"magicDefense":0,"startingGuard":0,"speed":1,
        "initiative":0,"focus":0,"mana":0,"charge":0,"movement":1},
        "proficiencyTags":null,"starterEquipment":null,"starterKnownSkills":null,
        "starterEquippedSkills":null}}
        """;
        var gateway = new HttpArchetypeDefinitionGateway(Client(HttpStatusCode.OK, json));

        var result = await gateway.GetByKeyAsync("archetype.empty", CancellationToken.None);

        result!.ProficiencyTags.Should().BeEmpty();
        result.StarterEquipment.Should().BeEmpty();
        result.StarterKnownSkills.Should().BeEmpty();
        result.StarterEquippedSkills.Should().BeEmpty();
    }

    [Fact]
    public async Task ArchetypeGateway_ShouldRejectUnknownPositionAndEmptyResponse()
    {
        const string invalidPosition = """
        {"definition":{"key":"archetype.bad","baseStats":{"maxVitality":1,"attackPower":0,
        "magicAttack":0,"defense":0,"magicDefense":0,"startingGuard":0,"speed":1,
        "initiative":0,"focus":0,"mana":0,"charge":0,"movement":1},
        "starterEquipment":[{"itemDefinitionKey":"item.bad","equipmentPosition":"Nowhere"}]}}
        """;
        var invalid = new HttpArchetypeDefinitionGateway(Client(HttpStatusCode.OK, invalidPosition));
        var empty = new HttpArchetypeDefinitionGateway(Client(HttpStatusCode.OK, "{}"));

        await invalid.Invoking(gateway => gateway.GetByKeyAsync("archetype.bad", CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*unknown equipment position*");
        await empty.Invoking(gateway => gateway.GetByKeyAsync("archetype.empty", CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    private static HttpClient Client(HttpStatusCode statusCode, string json = "{}") => new(new StubHandler(
        new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        }))
    {
        BaseAddress = new Uri("http://catalog.test")
    };

    private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(response);
    }
}
