using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Infrastructure.Catalog;
using Moq;
using Moq.Protected;

namespace Leds.GameEngine.UnitTests.Catalog;

public sealed class HttpCatalogContentGatewayTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public async Task GetRoomBossProfileAsync_ShouldReturnProfile_WhenCatalogReturns200()
    {
        var httpResponse = new
        {
            Definition = new
            {
                Key = "boss.threshold.warden",
                Name = "Warden of the Threshold",
                Description = "First sentinel.",
                RoomType = "Threshold",
                BaseDifficulty = 1,
                Tags = new[] { "sentinel", "guardian" }
            }
        };

        var json = JsonSerializer.Serialize(httpResponse, JsonOptions);
        var handler = CreateMockHandler(json, HttpStatusCode.OK);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var profile = await gateway.GetRoomBossProfileAsync("Threshold");

        profile.Should().NotBeNull();
        profile!.Key.Should().Be("boss.threshold.warden");
        profile.DisplayName.Should().Be("Warden of the Threshold");
        profile.Description.Should().Be("First sentinel.");
        profile.RoomType.Should().Be("Threshold");
        profile.BaseDifficulty.Should().Be(1);
        profile.Tags.Should().BeEquivalentTo("sentinel", "guardian");
    }

    [Fact]
    public async Task GetRoomBossProfileAsync_ShouldReturnNull_WhenCatalogReturns404()
    {
        var handler = CreateMockHandler("", HttpStatusCode.NotFound);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var profile = await gateway.GetRoomBossProfileAsync("UnknownRoom");

        profile.Should().BeNull();
    }

    [Fact]
    public async Task GetRoomBossProfileAsync_ShouldReturnNull_WhenCatalogReturns400()
    {
        var handler = CreateMockHandler("", HttpStatusCode.BadRequest);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var profile = await gateway.GetRoomBossProfileAsync(" ");

        profile.Should().BeNull();
    }

    [Fact]
    public async Task GetRoomBossProfileAsync_ShouldReturnNull_WhenRoomTypeIsWhitespace()
    {
        var handler = CreateMockHandler("", HttpStatusCode.OK);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var profile = await gateway.GetRoomBossProfileAsync("   ");

        profile.Should().BeNull();
    }

    [Fact]
    public async Task GetRoomBossProfileAsync_ShouldThrowCatalogGatewayException_WhenCatalogReturns500()
    {
        var handler = CreateMockHandler("Internal Server Error", HttpStatusCode.InternalServerError);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var act = async () => await gateway.GetRoomBossProfileAsync("Threshold");

        await act.Should()
            .ThrowAsync<CatalogGatewayException>()
            .WithMessage("*500*");
    }

    [Fact]
    public async Task GetRoomBossProfileAsync_ShouldThrowCatalogGatewayException_WhenResponseJsonIsInvalid()
    {
        var handler = CreateMockHandler("{ invalid json }", HttpStatusCode.OK);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var act = async () => await gateway.GetRoomBossProfileAsync("Threshold");

        await act.Should()
            .ThrowAsync<CatalogGatewayException>()
            .WithMessage("*Failed to deserialize*");
    }

    [Fact]
    public async Task GetRoomBossProfileAsync_ShouldThrowCatalogGatewayException_OnNetworkError()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var act = async () => await gateway.GetRoomBossProfileAsync("Threshold");

        await act.Should()
            .ThrowAsync<CatalogGatewayException>()
            .WithMessage("*Connection refused*");
    }

    [Fact]
    public async Task GetRoomBossProfileAsync_ShouldThrowCatalogGatewayException_WhenDefinitionIsNull()
    {
        var httpResponse = new
        {
            Definition = (object?)null
        };

        var json = JsonSerializer.Serialize(httpResponse, JsonOptions);
        var handler = CreateMockHandler(json, HttpStatusCode.OK);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var profile = await gateway.GetRoomBossProfileAsync("Threshold");

        profile.Should().BeNull();
    }

    [Fact]
    public async Task GetRoomBossProfileAsync_ShouldThrowCatalogGatewayException_WhenHttpThrowsOperationCanceled()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var act = async () => await gateway.GetRoomBossProfileAsync("Threshold");

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetEnemyTemplateByKeyAsync_ShouldThrowCatalogGatewayException_WhenUsingHttpGateway()
    {
        var handler = CreateMockHandler("", HttpStatusCode.OK);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var act = async () => await gateway.GetEnemyTemplateByKeyAsync("enemy-shadow-v1");

        var exception = await act.Should()
            .ThrowAsync<CatalogGatewayException>();

        exception.Which.Message.Should().Contain("not available via the HTTP catalog gateway yet");
        exception.Which.Message.Should().Contain("Use CatalogGateway:Mode = InMemory");
    }

    [Fact]
    public async Task GetSkillTemplateByKeyAsync_ShouldThrowCatalogGatewayException_WhenUsingHttpGateway()
    {
        var handler = CreateMockHandler("", HttpStatusCode.OK);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var act = async () => await gateway.GetSkillTemplateByKeyAsync("skill-shadow-strike-v1");

        var exception = await act.Should()
            .ThrowAsync<CatalogGatewayException>();

        exception.Which.Message.Should().Contain("not available via the HTTP catalog gateway yet");
        exception.Which.Message.Should().Contain("Use CatalogGateway:Mode = InMemory");
    }

    [Fact]
    public async Task GetItemTemplateByKeyAsync_ShouldThrowCatalogGatewayException_WhenUsingHttpGateway()
    {
        var handler = CreateMockHandler("", HttpStatusCode.OK);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var act = async () => await gateway.GetItemTemplateByKeyAsync("item-memory-fragment-v1");

        var exception = await act.Should()
            .ThrowAsync<CatalogGatewayException>();

        exception.Which.Message.Should().Contain("not available via the HTTP catalog gateway yet");
        exception.Which.Message.Should().Contain("Use CatalogGateway:Mode = InMemory");
    }

    [Fact]
    public async Task GetEventTemplateByKeyAsync_ShouldThrowCatalogGatewayException_WhenUsingHttpGateway()
    {
        var handler = CreateMockHandler("", HttpStatusCode.OK);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var act = async () => await gateway.GetEventTemplateByKeyAsync("event-combat-shadow-v1");

        var exception = await act.Should()
            .ThrowAsync<CatalogGatewayException>();

        exception.Which.Message.Should().Contain("not available via the HTTP catalog gateway yet");
        exception.Which.Message.Should().Contain("Use CatalogGateway:Mode = InMemory");
    }

    [Fact]
    public async Task GetPalaceLawDefinitionByKeyAsync_ShouldThrowCatalogGatewayException_WhenUsingHttpGateway()
    {
        var handler = CreateMockHandler("", HttpStatusCode.OK);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var act = async () => await gateway.GetPalaceLawDefinitionByKeyAsync("law-silence-v1");

        var exception = await act.Should()
            .ThrowAsync<CatalogGatewayException>();

        exception.Which.Message.Should().Contain("not available via the HTTP catalog gateway yet");
        exception.Which.Message.Should().Contain("Use CatalogGateway:Mode = InMemory");
    }

    [Fact]
    public async Task GetRoomBossProfileAsync_ShouldCallExpectedRoomTypeEndpoint()
    {
        HttpRequestMessage? capturedRequest = null;

        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        await gateway.GetRoomBossProfileAsync("Threshold");

        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.AbsolutePath.Should().Be(
            "/api/v2/catalog/room-boss-definitions/room-type/Threshold");
    }

    [Fact]
    public async Task GetRoomBossProfileAsync_ShouldRespectCancellationToken()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var handler = CreateMockHandler("", HttpStatusCode.OK);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var act = async () => await gateway.GetRoomBossProfileAsync("Threshold", cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static Mock<HttpMessageHandler> CreateMockHandler(string content, HttpStatusCode statusCode)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });

        return handler;
    }
}
