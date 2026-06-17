using FluentAssertions;
using Leds.GameEngine.Infrastructure.Catalog;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

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
    public async Task GetPalaceLawDefinitionByKeyAsync_ShouldReturnDefinition_WhenCatalogReturns200()
    {
        var httpResponse = new
        {
            Definition = new
            {
                Key = "law-aegis-v1",
                Name = "Loi de l'Egide",
                Description = "Renforce la garde initiale.",
                Version = "1.0.0",
                Status = "Active",
                Visibility = "Visible",
                Priority = 20,
                ImpactDomains = new[] { "Combat" },
                EffectSetKey = "effect.law.aegis",
                Effects = new[]
                {
                    new
                    {
                        EffectType = "AddStartingGuard",
                        TargetScope = "Run",
                        Value = 8m,
                        ValueMode = "Flat",
                        Duration = "UntilRunEnds",
                        StackPolicy = "Additive",
                        Condition = (string?)null,
                        Order = 0,
                        BehaviorTag = (string?)null,
                        GenerationTag = (string?)null,
                        SelectionGroup = (string?)null
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(httpResponse, JsonOptions);
        var handler = CreateMockHandler(json, HttpStatusCode.OK);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var result = await gateway.GetPalaceLawDefinitionByKeyAsync("law-aegis-v1");

        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Should().Be("law-aegis-v1");
        result.Value.EffectSetKey.Should().Be("effect.law.aegis");
        result.Value.Effects.Should().ContainSingle(effect =>
            effect.EffectType == "AddStartingGuard" &&
            effect.Value == 8m);
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

    // ── Skill Definitions ─────────────────────────────────────────────

    [Fact]
    public async Task GetSkillDefinitionByKeyAsync_ShouldReturnSkill_WhenCatalogReturns200()
    {
        var httpResponse = new
        {
            Definition = new
            {
                Key = "skill.basic.strike",
                Name = "Frappe",
                Description = "Une attaque de base.",
                SkillType = "Damage",
                TargetingType = "SingleEnemy",
                EffectType = "Damage",
                ManaCost = 5,
                ChargeCost = 0,
                BasePower = 10,
                Tags = new[] { "basic", "damage" }
            }
        };

        var json = JsonSerializer.Serialize(httpResponse, JsonOptions);
        var handler = CreateMockHandler(json, HttpStatusCode.OK);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var skill = await gateway.GetSkillDefinitionByKeyAsync("skill.basic.strike");

        skill.Should().NotBeNull();
        skill!.Key.Should().Be("skill.basic.strike");
        skill.DisplayName.Should().Be("Frappe");
        skill.SkillType.Should().Be("Damage");
        skill.TargetingType.Should().Be("SingleEnemy");
        skill.EffectType.Should().Be("Damage");
        skill.ManaCost.Should().Be(5);
        skill.ChargeCost.Should().Be(0);
        skill.BasePower.Should().Be(10);
        skill.Tags.Should().BeEquivalentTo("basic", "damage");
    }

    [Fact]
    public async Task GetSkillDefinitionByKeyAsync_ShouldMapAllFields_WhenCatalogReturns200()
    {
        var httpResponse = new
        {
            Definition = new
            {
                Key = "skill.basic.guard",
                Name = "Garde",
                Description = "Une posture defensive.",
                SkillType = "Defense",
                TargetingType = "Self",
                EffectType = "Buff",
                ManaCost = 3,
                ChargeCost = 1,
                BasePower = 0,
                Tags = (string[]?)null
            }
        };

        var json = JsonSerializer.Serialize(httpResponse, JsonOptions);
        var handler = CreateMockHandler(json, HttpStatusCode.OK);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var skill = await gateway.GetSkillDefinitionByKeyAsync("skill.basic.guard");

        skill.Should().NotBeNull();
        skill!.Key.Should().Be("skill.basic.guard");
        skill.DisplayName.Should().Be("Garde");
        skill.Description.Should().Be("Une posture defensive.");
        skill.SkillType.Should().Be("Defense");
        skill.TargetingType.Should().Be("Self");
        skill.EffectType.Should().Be("Buff");
        skill.ManaCost.Should().Be(3);
        skill.ChargeCost.Should().Be(1);
        skill.BasePower.Should().Be(0);
    }

    [Fact]
    public async Task GetSkillDefinitionByKeyAsync_ShouldCallExpectedEndpoint()
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

        await gateway.GetSkillDefinitionByKeyAsync("skill.basic.strike");

        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.AbsolutePath.Should().Be(
            "/api/v2/catalog/skill-definitions/skill.basic.strike");
    }

    [Fact]
    public async Task GetSkillDefinitionByKeyAsync_ShouldReturnNull_WhenCatalogReturns404()
    {
        var handler = CreateMockHandler("", HttpStatusCode.NotFound);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var skill = await gateway.GetSkillDefinitionByKeyAsync("unknown");

        skill.Should().BeNull();
    }

    [Fact]
    public async Task GetSkillDefinitionByKeyAsync_ShouldReturnNull_WhenKeyIsWhitespace()
    {
        var handler = CreateMockHandler("", HttpStatusCode.OK);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var skill = await gateway.GetSkillDefinitionByKeyAsync("   ");

        skill.Should().BeNull();
    }

    [Fact]
    public async Task GetSkillDefinitionByKeyAsync_ShouldThrowCatalogGatewayException_WhenCatalogReturns500()
    {
        var handler = CreateMockHandler("Internal Server Error", HttpStatusCode.InternalServerError);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var act = async () => await gateway.GetSkillDefinitionByKeyAsync("some-key");

        await act.Should()
            .ThrowAsync<CatalogGatewayException>()
            .WithMessage("*500*");
    }

    [Fact]
    public async Task GetSkillDefinitionByKeyAsync_ShouldThrowCatalogGatewayException_WhenJsonIsInvalid()
    {
        var handler = CreateMockHandler("{ invalid json }", HttpStatusCode.OK);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var act = async () => await gateway.GetSkillDefinitionByKeyAsync("some-key");

        await act.Should()
            .ThrowAsync<CatalogGatewayException>()
            .WithMessage("*Failed to deserialize*");
    }

    [Fact]
    public async Task GetSkillDefinitionByKeyAsync_ShouldPropagateOperationCanceledException()
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

        var act = async () => await gateway.GetSkillDefinitionByKeyAsync("some-key");

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // ── ListSkillDefinitionsByKeysAsync ────────────────────────────────

    [Fact]
    public async Task ListSkillDefinitionsByKeysAsync_ShouldReturnSkills_WhenCatalogReturns200()
    {
        var httpResponse = new
        {
            Definitions = new[]
            {
                new
                {
                    Key = "skill.basic.strike",
                    Name = "Frappe",
                    Description = "Une attaque.",
                    SkillType = "Damage",
                    TargetingType = "SingleEnemy",
                    EffectType = "Damage",
                    ManaCost = 5,
                    ChargeCost = 0,
                    BasePower = 10,
                    Tags = new[] { "basic" }
                },
                new
                {
                    Key = "skill.basic.guard",
                    Name = "Garde",
                    Description = "Une defense.",
                    SkillType = "Defense",
                    TargetingType = "Self",
                    EffectType = "Buff",
                    ManaCost = 3,
                    ChargeCost = 1,
                    BasePower = 0,
                    Tags = Array.Empty<string>()
                }
            }
        };

        var json = JsonSerializer.Serialize(httpResponse, JsonOptions);
        var handler = CreateMockHandler(json, HttpStatusCode.OK);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var skills = await gateway.ListSkillDefinitionsByKeysAsync(
            ["skill.basic.strike", "skill.basic.guard"]);

        skills.Should().HaveCount(2);
        skills.Select(s => s.Key).Should()
            .BeEquivalentTo("skill.basic.strike", "skill.basic.guard");
    }

    [Fact]
    public async Task ListSkillDefinitionsByKeysAsync_ShouldMapAllFields()
    {
        var httpResponse = new
        {
            Definitions = new[]
            {
                new
                {
                    Key = "skill.basic.strike",
                    Name = "Frappe",
                    Description = "Description.",
                    SkillType = "Damage",
                    TargetingType = "SingleEnemy",
                    EffectType = "Damage",
                    ManaCost = 5,
                    ChargeCost = 0,
                    BasePower = 10,
                    Tags = new[] { "tag1" }
                }
            }
        };

        var json = JsonSerializer.Serialize(httpResponse, JsonOptions);
        var handler = CreateMockHandler(json, HttpStatusCode.OK);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var skills = await gateway.ListSkillDefinitionsByKeysAsync(["skill.basic.strike"]);

        var skill = skills.Single();
        skill.Key.Should().Be("skill.basic.strike");
        skill.DisplayName.Should().Be("Frappe");
        skill.Description.Should().Be("Description.");
        skill.SkillType.Should().Be("Damage");
        skill.TargetingType.Should().Be("SingleEnemy");
        skill.EffectType.Should().Be("Damage");
        skill.ManaCost.Should().Be(5);
        skill.ChargeCost.Should().Be(0);
        skill.BasePower.Should().Be(10);
        skill.Tags.Should().BeEquivalentTo("tag1");
    }

    [Fact]
    public async Task ListSkillDefinitionsByKeysAsync_ShouldCallExpectedEndpoint()
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

        await gateway.ListSkillDefinitionsByKeysAsync(["skill.basic.strike"]);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.AbsolutePath.Should().Be(
            "/api/v2/catalog/skill-definitions/batch/by-keys");
    }

    [Fact]
    public async Task ListSkillDefinitionsByKeysAsync_ShouldSendExpectedKeysPayload()
    {
        HttpRequestMessage? capturedRequest = null;

        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                capturedRequest = req;
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        await gateway.ListSkillDefinitionsByKeysAsync(["skill.a", "skill.b"]);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Post);

        var body = await capturedRequest.Content!.ReadAsStringAsync();
        body.Should().Contain("skill.a");
        body.Should().Contain("skill.b");
    }

    [Fact]
    public async Task ListSkillDefinitionsByKeysAsync_ShouldReturnEmpty_WhenKeysAreEmpty()
    {
        var handler = CreateMockHandler("", HttpStatusCode.OK);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var skills = await gateway.ListSkillDefinitionsByKeysAsync([]);

        skills.Should().BeEmpty();
    }

    [Fact]
    public async Task ListSkillDefinitionsByKeysAsync_ShouldThrowCatalogGatewayException_WhenCatalogReturns500()
    {
        var handler = CreateMockHandler("Internal Server Error", HttpStatusCode.InternalServerError);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var act = async () => await gateway.ListSkillDefinitionsByKeysAsync(["some-key"]);

        await act.Should()
            .ThrowAsync<CatalogGatewayException>()
            .WithMessage("*500*");
    }

    // ── ListSkillDefinitionsByTypeAsync ────────────────────────────────

    [Fact]
    public async Task ListSkillDefinitionsByTypeAsync_ShouldReturnSkills_WhenCatalogReturns200()
    {
        var httpResponse = new
        {
            Definitions = new[]
            {
                new
                {
                    Key = "skill.basic.strike",
                    Name = "Frappe",
                    Description = "Une attaque.",
                    SkillType = "Damage",
                    TargetingType = "SingleEnemy",
                    EffectType = "Damage",
                    ManaCost = 5,
                    ChargeCost = 0,
                    BasePower = 10,
                    Tags = new[] { "basic" }
                }
            }
        };

        var json = JsonSerializer.Serialize(httpResponse, JsonOptions);
        var handler = CreateMockHandler(json, HttpStatusCode.OK);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var skills = await gateway.ListSkillDefinitionsByTypeAsync("Damage");

        skills.Should().ContainSingle();
        skills.Single().Key.Should().Be("skill.basic.strike");
    }

    [Fact]
    public async Task ListSkillDefinitionsByTypeAsync_ShouldCallExpectedEndpoint()
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

        await gateway.ListSkillDefinitionsByTypeAsync("Debuff");

        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.AbsolutePath.Should().Be(
            "/api/v2/catalog/skill-definitions/type/Debuff");
    }

    [Fact]
    public async Task ListSkillDefinitionsByTypeAsync_ShouldReturnEmpty_WhenSkillTypeIsWhitespace()
    {
        var handler = CreateMockHandler("", HttpStatusCode.OK);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var skills = await gateway.ListSkillDefinitionsByTypeAsync("   ");

        skills.Should().BeEmpty();
    }

    [Fact]
    public async Task ListSkillDefinitionsByTypeAsync_ShouldThrowCatalogGatewayException_WhenCatalogReturns500()
    {
        var handler = CreateMockHandler("Internal Server Error", HttpStatusCode.InternalServerError);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5193") };
        var gateway = new HttpCatalogContentGateway(client);

        var act = async () => await gateway.ListSkillDefinitionsByTypeAsync("Damage");

        await act.Should()
            .ThrowAsync<CatalogGatewayException>()
            .WithMessage("*500*");
    }

    // ── Legacy template methods remain unavailable ─────────────────────

    [Fact]
    public async Task GetSkillTemplateByKeyAsync_ShouldStillThrow_WhenUsingHttpGateway()
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
