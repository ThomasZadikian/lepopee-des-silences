using System.Net;
using System.Text;
using FluentAssertions;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Infrastructure.Catalog;

namespace Leds.GameEngine.UnitTests.Infrastructure.Catalog;

public sealed class HttpCatalogContentGatewayCoverageTests
{
    [Fact]
    public async Task KeyedLookups_ShouldRejectBlankKeysWithoutCallingCatalog()
    {
        var handler = new RecordingHandler(_ => throw new InvalidOperationException("HTTP must not be called."));
        var sut = CreateGateway(handler);

        (await sut.GetPalaceLawDefinitionByKeyAsync(" ")).IsFailure.Should().BeTrue();
        (await sut.GetCurseDefinitionByKeyAsync(" ")).IsFailure.Should().BeTrue();
        (await sut.GetItemDefinitionByKeyAsync(" ")).IsFailure.Should().BeTrue();
        (await sut.GetEffectSetByKeyAsync(" ")).IsFailure.Should().BeTrue();
        (await sut.GetRewardTemplateByKeyAsync(" ")).IsFailure.Should().BeTrue();

        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task DirectCatalogLookups_ShouldReturnEmptyForBlankInputs()
    {
        var handler = new RecordingHandler(_ => throw new InvalidOperationException("HTTP must not be called."));
        var sut = CreateGateway(handler);

        (await sut.GetRoomBossProfileAsync(" ")).Should().BeNull();
        (await sut.GetEnemyDefinitionByKeyAsync(" ")).Should().BeNull();
        (await sut.ListEnemyDefinitionsByRoomTypeAsync(" ")).Should().BeEmpty();
        (await sut.ListCompatibleEnemyDefinitionsAsync(" ", 3)).Should().BeEmpty();
        (await sut.GetSkillDefinitionByKeyAsync(" ")).Should().BeNull();
        (await sut.ListSkillDefinitionsByKeysAsync([])).Should().BeEmpty();
        (await sut.ListSkillDefinitionsByTypeAsync(" ")).Should().BeEmpty();

        handler.Requests.Should().BeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task DirectCatalogLookups_ShouldTreatExpectedAbsenceStatusesAsEmpty(HttpStatusCode status)
    {
        var sut = CreateGateway(new RecordingHandler(_ => new HttpResponseMessage(status)));

        (await sut.GetRoomBossProfileAsync("Threshold")).Should().BeNull();
        (await sut.GetEnemyDefinitionByKeyAsync("enemy.test")).Should().BeNull();
        (await sut.ListEnemyDefinitionsByRoomTypeAsync("Threshold")).Should().BeEmpty();
        (await sut.ListCompatibleEnemyDefinitionsAsync("Threshold", 3)).Should().BeEmpty();
        (await sut.GetSkillDefinitionByKeyAsync("skill.test")).Should().BeNull();
        (await sut.ListSkillDefinitionsByKeysAsync(["skill.test"])).Should().BeEmpty();
        (await sut.ListSkillDefinitionsByTypeAsync("Damage")).Should().BeEmpty();
    }

    [Fact]
    public async Task DirectCatalogLookups_ShouldWrapUnexpectedHttpStatuses()
    {
        var sut = CreateGateway(new RecordingHandler(_ => Response(
            HttpStatusCode.InternalServerError,
            "catalog unavailable")));

        await Assert.ThrowsAsync<CatalogGatewayException>(() => sut.GetRoomBossProfileAsync("Threshold"));
        await Assert.ThrowsAsync<CatalogGatewayException>(() => sut.GetEnemyDefinitionByKeyAsync("enemy.test"));
        await Assert.ThrowsAsync<CatalogGatewayException>(() => sut.ListEnemyDefinitionsByRoomTypeAsync("Threshold"));
        await Assert.ThrowsAsync<CatalogGatewayException>(() => sut.ListCompatibleEnemyDefinitionsAsync("Threshold", 3));
        await Assert.ThrowsAsync<CatalogGatewayException>(() => sut.GetSkillDefinitionByKeyAsync("skill.test"));
        await Assert.ThrowsAsync<CatalogGatewayException>(() => sut.ListSkillDefinitionsByKeysAsync(["skill.test"]));
        await Assert.ThrowsAsync<CatalogGatewayException>(() => sut.ListSkillDefinitionsByTypeAsync("Damage"));
    }

    [Fact]
    public async Task DirectCatalogLookups_ShouldWrapMalformedJson()
    {
        var sut = CreateGateway(new RecordingHandler(_ => Response(HttpStatusCode.OK, "{not-json")));

        await Assert.ThrowsAsync<CatalogGatewayException>(() => sut.GetRoomBossProfileAsync("Threshold"));
        await Assert.ThrowsAsync<CatalogGatewayException>(() => sut.GetEnemyDefinitionByKeyAsync("enemy.test"));
        await Assert.ThrowsAsync<CatalogGatewayException>(() => sut.ListEnemyDefinitionsByRoomTypeAsync("Threshold"));
        await Assert.ThrowsAsync<CatalogGatewayException>(() => sut.ListCompatibleEnemyDefinitionsAsync("Threshold", 3));
        await Assert.ThrowsAsync<CatalogGatewayException>(() => sut.GetSkillDefinitionByKeyAsync("skill.test"));
        await Assert.ThrowsAsync<CatalogGatewayException>(() => sut.ListSkillDefinitionsByKeysAsync(["skill.test"]));
        await Assert.ThrowsAsync<CatalogGatewayException>(() => sut.ListSkillDefinitionsByTypeAsync("Damage"));
    }

    [Fact]
    public async Task DirectCatalogLookups_ShouldHandleSuccessfulEmptyWrappers()
    {
        var handler = new RecordingHandler(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.Contains("room-boss-definitions", StringComparison.Ordinal))
                return Json("{\"definition\":null}");
            if (path.Contains("/batch/by-keys", StringComparison.Ordinal)
                || path.Contains("/room-type/", StringComparison.Ordinal)
                || path.Contains("/compatible", StringComparison.Ordinal)
                || path.Contains("/type/", StringComparison.Ordinal))
                return Json("{\"definitions\":null}");
            return Json("{\"definition\":null}");
        });
        var sut = CreateGateway(handler);

        (await sut.GetRoomBossProfileAsync("Threshold")).Should().BeNull();
        (await sut.GetEnemyDefinitionByKeyAsync("enemy.test")).Should().BeNull();
        (await sut.ListEnemyDefinitionsByRoomTypeAsync("Threshold")).Should().BeEmpty();
        (await sut.ListCompatibleEnemyDefinitionsAsync("Threshold", 3)).Should().BeEmpty();
        (await sut.GetSkillDefinitionByKeyAsync("skill.test")).Should().BeNull();
        (await sut.ListSkillDefinitionsByKeysAsync(["skill.test"])).Should().BeEmpty();
        (await sut.ListSkillDefinitionsByTypeAsync("Damage")).Should().BeEmpty();
    }

    [Fact]
    public async Task KeyedResultLookups_ShouldReturnNotFoundForEmptySuccessfulWrappers()
    {
        var sut = CreateGateway(new RecordingHandler(_ => Json("{\"definition\":null}")));

        (await sut.GetPalaceLawDefinitionByKeyAsync("law.test")).IsFailure.Should().BeTrue();
        (await sut.GetCurseDefinitionByKeyAsync("curse.test")).IsFailure.Should().BeTrue();
        (await sut.GetItemDefinitionByKeyAsync("item.test")).IsFailure.Should().BeTrue();
        (await sut.GetEffectSetByKeyAsync("effect.test")).IsFailure.Should().BeTrue();
        (await sut.GetRewardTemplateByKeyAsync("reward.test")).IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task EligibleRewardTemplates_ShouldShortCircuitWhenSourceTypeIsBlank()
    {
        var handler = new RecordingHandler(_ => throw new InvalidOperationException("HTTP must not be called."));
        var sut = CreateGateway(handler);

        var result = await sut.ListEligibleRewardTemplatesAsync(
            new RewardTemplateEligibilityContext(" ", null, null, null, null));

        result.Should().BeEmpty();
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task EligibleRewardTemplates_ShouldEncodeEveryOptionalFilter()
    {
        var handler = new RecordingHandler(_ => Json("{\"definitions\":[]}"));
        var sut = CreateGateway(handler);

        var result = await sut.ListEligibleRewardTemplatesAsync(
            new RewardTemplateEligibilityContext(
                " Combat Victory ",
                Depth: 4,
                CombatTier: " Sombre + ",
                DifficultyMultiplier: 1.25,
                RewardPowerMultiplier: 1.5));

        result.Should().BeEmpty();
        var requestUri = handler.Requests.Should().ContainSingle().Subject.RequestUri!;
        var query = Uri.UnescapeDataString(requestUri.Query);
        query.Should().Contain("sourceType=Combat Victory");
        query.Should().Contain("depth=4");
        query.Should().Contain("combatTier=Sombre +");
        query.Should().Contain("difficultyMultiplier=1.25");
        query.Should().Contain("rewardPowerMultiplier=1.5");
    }

    [Fact]
    public async Task EligibleRewardTemplates_ShouldOmitAbsentOptionalFilters()
    {
        var handler = new RecordingHandler(_ => Json("{\"definitions\":null}"));
        var sut = CreateGateway(handler);

        var result = await sut.ListEligibleRewardTemplatesAsync(
            new RewardTemplateEligibilityContext("Combat", null, " ", null, null));

        result.Should().BeEmpty();
        var uri = handler.Requests.Should().ContainSingle().Subject.RequestUri!.ToString();
        uri.Should().Contain("sourceType=Combat");
        uri.Should().NotContain("depth=");
        uri.Should().NotContain("combatTier=");
        uri.Should().NotContain("difficultyMultiplier=");
        uri.Should().NotContain("rewardPowerMultiplier=");
    }

    private static HttpCatalogContentGateway CreateGateway(HttpMessageHandler handler) =>
        new(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://catalog.test")
        });

    private static HttpResponseMessage Json(string json) =>
        Response(HttpStatusCode.OK, json, "application/json");

    private static HttpResponseMessage Response(
        HttpStatusCode status,
        string content,
        string mediaType = "text/plain") =>
        new(status)
        {
            Content = new StringContent(content, Encoding.UTF8, mediaType)
        };

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(_responder(request));
        }
    }
}
