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

    [Theory]
    [InlineData("null", "no item type catalog")]
    [InlineData("{}", "incomplete item type catalog")]
    [InlineData("{\"version\":\"1\",\"definitions\":[]}", "incomplete item type catalog")]
    public async Task ItemTypeCatalog_ShouldRejectMissingOrIncompletePayloads(string json, string message)
    {
        var sut = CreateGateway(new RecordingHandler(_ => Json(json)));
        var act = () => sut.GetItemTypeCatalogAsync();
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage($"*{message}*");
    }

    [Theory]
    [InlineData("", "item type code")]
    [InlineData("common", "display name")]
    public async Task ItemTypeCatalog_ShouldRejectRequiredDefinitionFields(string code, string expected)
    {
        var display = code.Length == 0 ? "Common" : "";
        var json = $$"""{"version":"1","definitions":[{"code":"{{code}}","displayName":"{{display}}","glyph":"x","color":"#fff"}]}""";
        var sut = CreateGateway(new RecordingHandler(_ => Json(json)));
        var act = () => sut.GetItemTypeCatalogAsync();
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage($"*{expected}*");
    }

    [Fact]
    public async Task ItemTypeCatalog_ShouldRejectDuplicateAndMapValidDefinitions()
    {
        var duplicate = "{\"version\":\"1\",\"definitions\":[{\"code\":\"A\",\"displayName\":\"A\",\"glyph\":\"a\",\"color\":\"x\"},{\"code\":\"a\",\"displayName\":\"B\",\"glyph\":\"b\",\"color\":\"y\"}]}";
        var dup = CreateGateway(new RecordingHandler(_ => Json(duplicate)));
        await FluentActions.Invoking(() => dup.GetItemTypeCatalogAsync()).Should()
            .ThrowAsync<InvalidOperationException>().WithMessage("*duplicate*");

        var valid = CreateGateway(new RecordingHandler(_ => Json("{\"version\":\" 1 \",\"definitions\":[{\"code\":\" Equipment \",\"displayName\":\" Equipement \",\"glyph\":\" E \",\"color\":\" blue \"}]}")));
        var result = await valid.GetItemTypeCatalogAsync();
        result.Version.Should().Be("1");
        result.Definitions.Single().Code.Should().Be("equipment");
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"version\":\"1\",\"definitions\":[]}")]
    public async Task ItemRarityCatalog_ShouldRejectMissingOrIncompletePayloads(string json)
    {
        var sut = CreateGateway(new RecordingHandler(_ => Json(json)));
        await FluentActions.Invoking(() => sut.GetItemRarityCatalogAsync()).Should()
            .ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ItemRarityCatalog_ShouldRejectDuplicateAndMapValidDefinitions()
    {
        var duplicate = "{\"version\":\"1\",\"definitions\":[{\"code\":\"Rare\",\"displayName\":\"Rare\",\"glyph\":\"r\",\"color\":\"x\"},{\"code\":\"rare\",\"displayName\":\"Rare2\",\"glyph\":\"q\",\"color\":\"y\"}]}";
        await FluentActions.Invoking(() => CreateGateway(new RecordingHandler(_ => Json(duplicate))).GetItemRarityCatalogAsync()).Should()
            .ThrowAsync<InvalidOperationException>().WithMessage("*duplicate*");

        var valid = CreateGateway(new RecordingHandler(_ => Json("{\"version\":\"1\",\"definitions\":[{\"code\":\" Rare \",\"displayName\":\" Rare \",\"glyph\":\"R\",\"color\":\"violet\",\"palaceShardCost\":2,\"himLitShardCost\":1}]}")));
        var result = await valid.GetItemRarityCatalogAsync();
        result.Definitions.Single().Code.Should().Be("rare");
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"version\":\"1\",\"definitions\":[]}")]
    public async Task EmotionalRegisterCatalog_ShouldRejectMissingOrIncompletePayloads(string json)
    {
        var sut = CreateGateway(new RecordingHandler(_ => Json(json)));
        await FluentActions.Invoking(() => sut.GetEmotionalRegisterCatalogAsync()).Should()
            .ThrowAsync<Exception>();
    }

    [Fact]
    public async Task EmotionalRegisterCatalog_ShouldCoverMetadataAffinityAndDuplicateValidation()
    {
        var missingMetadata = "{\"version\":\"1\",\"definitions\":[{\"code\":\"Neutral\",\"displayName\":\"\",\"glyph\":\"N\",\"color\":\"x\",\"incomingAffinities\":[]}]}";
        await FluentActions.Invoking(() => CreateGateway(new RecordingHandler(_ => Json(missingMetadata))).GetEmotionalRegisterCatalogAsync()).Should().ThrowAsync<InvalidOperationException>();

        var noAffinities = "{\"version\":\"1\",\"definitions\":[{\"code\":\"Neutral\",\"displayName\":\"Neutral\",\"glyph\":\"N\",\"color\":\"x\",\"incomingAffinities\":null}]}";
        await FluentActions.Invoking(() => CreateGateway(new RecordingHandler(_ => Json(noAffinities))).GetEmotionalRegisterCatalogAsync()).Should().ThrowAsync<InvalidOperationException>();

        var badOutcome = "{\"version\":\"1\",\"definitions\":[{\"code\":\"Neutral\",\"displayName\":\"Neutral\",\"glyph\":\"N\",\"color\":\"x\",\"incomingAffinities\":[{\"incomingRegister\":\"Neutral\",\"outcome\":\"wat\",\"multiplier\":1}]}]}";
        await FluentActions.Invoking(() => CreateGateway(new RecordingHandler(_ => Json(badOutcome))).GetEmotionalRegisterCatalogAsync()).Should().ThrowAsync<InvalidOperationException>();

        var duplicate = "{\"version\":\"1\",\"definitions\":[{\"code\":\"Neutral\",\"displayName\":\"N\",\"glyph\":\"N\",\"color\":\"x\",\"incomingAffinities\":[{\"incomingRegister\":\"Neutral\",\"outcome\":\"Neutral\",\"multiplier\":1}]},{\"code\":\"neutral\",\"displayName\":\"N2\",\"glyph\":\"N\",\"color\":\"x\",\"incomingAffinities\":[{\"incomingRegister\":\"Neutral\",\"outcome\":\"Neutral\",\"multiplier\":1}]}]}";
        await FluentActions.Invoking(() => CreateGateway(new RecordingHandler(_ => Json(duplicate))).GetEmotionalRegisterCatalogAsync()).Should().ThrowAsync<InvalidOperationException>();

        var invalidProfile = "{\"version\":\"1\",\"definitions\":[{\"code\":\"Neutral\",\"displayName\":\"N\",\"glyph\":\"N\",\"color\":\"x\",\"incomingAffinities\":[{\"incomingRegister\":\"Neutral\",\"outcome\":\"Neutral\",\"multiplier\":-1}]}]}";
        await FluentActions.Invoking(() => CreateGateway(new RecordingHandler(_ => Json(invalidProfile))).GetEmotionalRegisterCatalogAsync()).Should().ThrowAsync<InvalidOperationException>();

        var valid = "{\"version\":\" 1 \",\"definitions\":[{\"code\":\"Neutral\",\"displayName\":\" Neutral \",\"glyph\":\"N\",\"color\":\" gray \",\"incomingAffinities\":[{\"incomingRegister\":\"Neutral\",\"outcome\":\"Neutral\",\"multiplier\":1}]}]}";
        (await CreateGateway(new RecordingHandler(_ => Json(valid))).GetEmotionalRegisterCatalogAsync()).Definitions.Should().ContainSingle();
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"version\":\"1\",\"rules\":null}")]
    public async Task AffinityMatrix_ShouldRejectMissingPayloads(string json)
    {
        var sut = CreateGateway(new RecordingHandler(_ => Json(json)));
        await FluentActions.Invoking(() => sut.GetEmotionalAffinityMatrixAsync()).Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task AffinityMatrix_ShouldMapValidPayload()
    {
        var json = "{\"version\":\"1\",\"rules\":[{\"attackingRegister\":\"Neutral\",\"defendingRegister\":\"Neutral\",\"outcome\":\"Neutral\",\"multiplier\":1}]}";
        var result = await CreateGateway(new RecordingHandler(_ => Json(json))).GetEmotionalAffinityMatrixAsync();
        result.Rules.Should().ContainSingle();
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"definitions\":[]}")]
    public async Task CharacterCombatDefinitions_ShouldRejectMissingEmptyPayloads(string json)
    {
        var sut = CreateGateway(new RecordingHandler(_ => Json(json)));
        await FluentActions.Invoking(() => sut.ListCharacterCombatDefinitionsAsync()).Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CharacterCombatDefinitions_ShouldRejectDuplicateAndMapValidPayload()
    {
        var duplicate = "{\"definitions\":[{\"definitionKey\":\"hero\",\"kind\":\"Hero\",\"combatArchetypeCode\":\"Adaptive\",\"emotionalRegister\":\"Neutral\"},{\"definitionKey\":\"HERO\",\"kind\":\"Hero\",\"combatArchetypeCode\":\"Mage\",\"emotionalRegister\":\"Neutral\"}]}";
        await FluentActions.Invoking(() => CreateGateway(new RecordingHandler(_ => Json(duplicate))).ListCharacterCombatDefinitionsAsync()).Should().ThrowAsync<InvalidOperationException>();

        var valid = "{\"definitions\":[{\"definitionKey\":\" hero \",\"kind\":\" Hero \",\"combatArchetypeCode\":\" Adaptive \",\"emotionalRegister\":\"Neutral\"}]}";
        var result = await CreateGateway(new RecordingHandler(_ => Json(valid))).ListCharacterCombatDefinitionsAsync();
        result.Single().DefinitionKey.Should().Be("hero");
    }

    [Fact]
    public async Task GenericCatalogReads_ShouldCoverNullWrappersMalformedJsonAndHttpFailure()
    {
        var nullSut = CreateGateway(new RecordingHandler(_ => Json("null")));
        (await nullSut.ListActivePalaceLawDefinitionsAsync()).Should().BeEmpty();
        (await nullSut.ListAvailableCurseDefinitionsAsync()).Should().BeEmpty();
        (await nullSut.ListActiveEnemyDefinitionsAsync()).Should().BeEmpty();
        (await nullSut.ListActiveSkillDefinitionsAsync()).Should().BeEmpty();
        (await nullSut.ListActiveItemDefinitionsAsync()).Should().BeEmpty();

        var malformed = CreateGateway(new RecordingHandler(_ => Json("{bad")));
        await FluentActions.Invoking(() => malformed.GetItemTypeCatalogAsync()).Should().ThrowAsync<CatalogGatewayException>();

        var failure = CreateGateway(new RecordingHandler(_ => Response(HttpStatusCode.ServiceUnavailable, "down")));
        await FluentActions.Invoking(() => failure.GetItemTypeCatalogAsync()).Should().ThrowAsync<CatalogGatewayException>();

        var network = CreateGateway(new ThrowingHandler(new InvalidOperationException("offline")));
        await FluentActions.Invoking(() => network.GetItemTypeCatalogAsync()).Should().ThrowAsync<CatalogGatewayException>();
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

    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(exception);
    }
}
