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

    // ── Enemy Definitions ──────────────────────────────────────────────

    [Fact]
    public async Task GetEnemyDefinitionByKeyAsync_ShouldReturnEnemy_WhenCatalogReturns200()
    {
        var httpResponse = new
        {
            Definition = new
            {
                Key = "enemy.threshold.doubt-fragment",
                Name = "Fragment de Doute",
                Description = "Un éclat de silence.",
                Archetype = "Fragile",
                CompatibleRoomTypes = new[] { "Threshold" },
                BaseDifficulty = 1,
                MinRiskLevel = 1,
                MaxRiskLevel = 2,
                Tags = new[] { "threshold", "fragile" },
                SkillKeys = new[] { "skill.basic.strike" }
            }
        };

        var json = JsonSerializer.Serialize(httpResponse, JsonOptions);
        var handler = CreateMockHandler(json, HttpStatusCode.OK);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var enemy = await gateway.GetEnemyDefinitionByKeyAsync("enemy.threshold.doubt-fragment");

        enemy.Should().NotBeNull();
        enemy!.Key.Should().Be("enemy.threshold.doubt-fragment");
        enemy.DisplayName.Should().Be("Fragment de Doute");
        enemy.Description.Should().Be("Un éclat de silence.");
        enemy.Archetype.Should().Be("Fragile");
        enemy.BaseDifficulty.Should().Be(1);
        enemy.MinRiskLevel.Should().Be(1);
        enemy.MaxRiskLevel.Should().Be(2);
        enemy.CompatibleRoomTypes.Should().BeEquivalentTo("Threshold");
        enemy.Tags.Should().BeEquivalentTo("threshold", "fragile");
        enemy.SkillKeys.Should().BeEquivalentTo("skill.basic.strike");
    }

    [Fact]
    public async Task GetEnemyDefinitionByKeyAsync_ShouldReturnNull_WhenCatalogReturns404()
    {
        var handler = CreateMockHandler("", HttpStatusCode.NotFound);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var enemy = await gateway.GetEnemyDefinitionByKeyAsync("unknown");

        enemy.Should().BeNull();
    }

    [Fact]
    public async Task GetEnemyDefinitionByKeyAsync_ShouldReturnNull_WhenKeyIsWhitespace()
    {
        var handler = CreateMockHandler("", HttpStatusCode.OK);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var enemy = await gateway.GetEnemyDefinitionByKeyAsync("   ");

        enemy.Should().BeNull();
    }

    [Fact]
    public async Task GetEnemyDefinitionByKeyAsync_ShouldReturnNull_WhenDefinitionIsNull()
    {
        var httpResponse = new
        {
            Definition = (object?)null
        };

        var json = JsonSerializer.Serialize(httpResponse, JsonOptions);
        var handler = CreateMockHandler(json, HttpStatusCode.OK);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var enemy = await gateway.GetEnemyDefinitionByKeyAsync("some-key");

        enemy.Should().BeNull();
    }

    [Fact]
    public async Task GetEnemyDefinitionByKeyAsync_ShouldThrowCatalogGatewayException_WhenCatalogReturns500()
    {
        var handler = CreateMockHandler("Internal Server Error", HttpStatusCode.InternalServerError);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var act = async () => await gateway.GetEnemyDefinitionByKeyAsync("some-key");

        await act.Should()
            .ThrowAsync<CatalogGatewayException>()
            .WithMessage("*500*");
    }

    [Fact]
    public async Task GetEnemyDefinitionByKeyAsync_ShouldThrowCatalogGatewayException_WhenJsonIsInvalid()
    {
        var handler = CreateMockHandler("{ invalid json }", HttpStatusCode.OK);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var act = async () => await gateway.GetEnemyDefinitionByKeyAsync("some-key");

        await act.Should()
            .ThrowAsync<CatalogGatewayException>()
            .WithMessage("*Failed to deserialize*");
    }

    [Fact]
    public async Task GetEnemyDefinitionByKeyAsync_ShouldThrowCatalogGatewayException_OnNetworkError()
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

        var act = async () => await gateway.GetEnemyDefinitionByKeyAsync("some-key");

        await act.Should()
            .ThrowAsync<CatalogGatewayException>()
            .WithMessage("*Connection refused*");
    }

    [Fact]
    public async Task GetEnemyDefinitionByKeyAsync_ShouldPropagateOperationCanceledException()
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

        var act = async () => await gateway.GetEnemyDefinitionByKeyAsync("some-key");

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetEnemyDefinitionByKeyAsync_ShouldCallExpectedEndpoint()
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

        await gateway.GetEnemyDefinitionByKeyAsync("enemy.final.silent-double");

        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.AbsolutePath.Should().Be(
            "/api/v2/catalog/enemy-definitions/enemy.final.silent-double");
    }

    [Fact]
    public async Task ListEnemyDefinitionsByRoomTypeAsync_ShouldReturnEnemies_WhenCatalogReturns200()
    {
        var httpResponse = new
        {
            Definitions = new[]
            {
                new
                {
                    Key = "enemy.silence.mute-witness",
                    Name = "Témoin Muet",
                    Description = "Il observe sans jamais parler.",
                    Archetype = "Guard",
                    CompatibleRoomTypes = new[] { "Silence" },
                    BaseDifficulty = 3,
                    MinRiskLevel = 2,
                    MaxRiskLevel = 4,
                    Tags = new[] { "silence", "guard" },
                    SkillKeys = new[] { "skill.basic.shield" }
                },
                new
                {
                    Key = "enemy.silence.absent-voice",
                    Name = "Voix Absente",
                    Description = "Un cri qui n'a jamais été poussé.",
                    Archetype = "Disruptor",
                    CompatibleRoomTypes = new[] { "Silence" },
                    BaseDifficulty = 4,
                    MinRiskLevel = 3,
                    MaxRiskLevel = 5,
                    Tags = new[] { "silence", "disruptor" },
                    SkillKeys = new[] { "skill.basic.disable" }
                }
            }
        };

        var json = JsonSerializer.Serialize(httpResponse, JsonOptions);
        var handler = CreateMockHandler(json, HttpStatusCode.OK);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var enemies = await gateway.ListEnemyDefinitionsByRoomTypeAsync("Silence");

        enemies.Should().HaveCount(2);
        enemies.Select(e => e.Key).Should()
            .BeEquivalentTo("enemy.silence.mute-witness", "enemy.silence.absent-voice");
    }

    [Fact]
    public async Task ListEnemyDefinitionsByRoomTypeAsync_ShouldReturnEmpty_WhenCatalogReturns404()
    {
        var handler = CreateMockHandler("", HttpStatusCode.NotFound);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var enemies = await gateway.ListEnemyDefinitionsByRoomTypeAsync("Unknown");

        enemies.Should().BeEmpty();
    }

    [Fact]
    public async Task ListEnemyDefinitionsByRoomTypeAsync_ShouldReturnEmpty_WhenRoomTypeIsWhitespace()
    {
        var handler = CreateMockHandler("", HttpStatusCode.OK);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var enemies = await gateway.ListEnemyDefinitionsByRoomTypeAsync("   ");

        enemies.Should().BeEmpty();
    }

    [Fact]
    public async Task ListEnemyDefinitionsByRoomTypeAsync_ShouldCallExpectedEndpoint()
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

        await gateway.ListEnemyDefinitionsByRoomTypeAsync("Final");

        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.AbsolutePath.Should().Be(
            "/api/v2/catalog/enemy-definitions/room-type/Final");
    }

    [Fact]
    public async Task ListEnemyDefinitionsByRoomTypeAsync_ShouldThrowCatalogGatewayException_WhenCatalogReturns500()
    {
        var handler = CreateMockHandler("Internal Server Error", HttpStatusCode.InternalServerError);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var act = async () => await gateway.ListEnemyDefinitionsByRoomTypeAsync("Threshold");

        await act.Should()
            .ThrowAsync<CatalogGatewayException>()
            .WithMessage("*500*");
    }

    [Fact]
    public async Task ListCompatibleEnemyDefinitionsAsync_ShouldReturnEnemies_WhenCatalogReturns200()
    {
        var httpResponse = new
        {
            Definitions = new[]
            {
                new
                {
                    Key = "enemy.rupture.broken-thought",
                    Name = "Pensée Brisée",
                    Description = "Un raisonnement interrompu.",
                    Archetype = "Skirmisher",
                    CompatibleRoomTypes = new[] { "Rupture" },
                    BaseDifficulty = 3,
                    MinRiskLevel = 2,
                    MaxRiskLevel = 4,
                    Tags = new[] { "rupture", "skirmisher" },
                    SkillKeys = new[] { "skill.basic.strike" }
                }
            }
        };

        var json = JsonSerializer.Serialize(httpResponse, JsonOptions);
        var handler = CreateMockHandler(json, HttpStatusCode.OK);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var enemies = await gateway.ListCompatibleEnemyDefinitionsAsync("Rupture", 3);

        enemies.Should().ContainSingle();
        enemies.Single().Key.Should().Be("enemy.rupture.broken-thought");
    }

    [Fact]
    public async Task ListCompatibleEnemyDefinitionsAsync_ShouldReturnEmpty_WhenRoomTypeIsWhitespace()
    {
        var handler = CreateMockHandler("", HttpStatusCode.OK);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var enemies = await gateway.ListCompatibleEnemyDefinitionsAsync("   ", 3);

        enemies.Should().BeEmpty();
    }

    [Fact]
    public async Task ListCompatibleEnemyDefinitionsAsync_ShouldCallExpectedEndpointWithQueryString()
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

        await gateway.ListCompatibleEnemyDefinitionsAsync("Memory", 4);

        capturedRequest.Should().NotBeNull();
        var uri = capturedRequest!.RequestUri!;
        uri.AbsolutePath.Should().Be("/api/v2/catalog/enemy-definitions/compatible");
        uri.Query.Should().Contain("roomType=Memory");
        uri.Query.Should().Contain("riskLevel=4");
    }

    [Fact]
    public async Task ListCompatibleEnemyDefinitionsAsync_ShouldThrowCatalogGatewayException_WhenCatalogReturns500()
    {
        var handler = CreateMockHandler("Internal Server Error", HttpStatusCode.InternalServerError);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var act = async () => await gateway.ListCompatibleEnemyDefinitionsAsync("Threshold", 2);

        await act.Should()
            .ThrowAsync<CatalogGatewayException>()
            .WithMessage("*500*");
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
