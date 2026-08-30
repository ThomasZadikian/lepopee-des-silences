using System.Net;
using System.Text;
using FluentAssertions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Infrastructure.Players;

namespace Leds.GameEngine.UnitTests.Players;

public sealed class HttpPlayerGatewaysCoverageTests
{
    [Fact]
    public async Task RunSnapshotGateway_ShouldMapValidSnapshotAndDefaultEquipment()
    {
        var playerId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        var json = $$"""
        {
          "PlayerId":"{{playerId}}",
          "DisplayName":"Player",
          "Characters":[{
            "CharacterId":"{{characterId}}",
            "DefinitionKey":"character.hero",
            "DisplayName":"Hero",
            "MaxVitality":100,
            "BaseMana":20,
            "BaseCharge":1,
            "SkillKeys":["skill.one"],
            "Stats":{
              "MaxVitality":100,"AttackPower":10,"Defense":8,"StartingGuard":2,
              "Speed":9,"Initiative":7,"Focus":6,"Mana":20,"Charge":1,
              "MagicAttack":4,"MagicDefense":5,"Movement":3
            }
          }]
        }
        """;
        var gateway = new HttpPlayerRunSnapshotGateway(Client(_ => Json(HttpStatusCode.OK, json)));

        var snapshot = await gateway.GetRunSnapshotAsync(playerId, CancellationToken.None);

        snapshot.PlayerId.Should().Be(playerId);
        var character = snapshot.Characters.Single();
        character.CharacterId.Should().Be(characterId);
        character.EquippedItems.Should().BeEmpty();
        character.Skills.Single().SkillDefinitionKey.Should().Be("skill.one");
        character.Stats.Movement.Should().Be(3);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, typeof(NotFoundException))]
    [InlineData(HttpStatusCode.Conflict, typeof(ConflictException))]
    [InlineData(HttpStatusCode.InternalServerError, typeof(HttpRequestException))]
    public async Task RunSnapshotGateway_ShouldTranslateHttpFailures(HttpStatusCode status, Type exceptionType)
    {
        var gateway = new HttpPlayerRunSnapshotGateway(Client(_ => Json(status, "failure")));

        var act = () => gateway.GetRunSnapshotAsync(Guid.NewGuid(), CancellationToken.None);

        (await act.Should().ThrowAsync<Exception>()).Which.Should().BeOfType(exceptionType);
    }

    [Fact]
    public async Task RunSnapshotGateway_ShouldRejectNullBody()
    {
        var gateway = new HttpPlayerRunSnapshotGateway(Client(_ => Json(HttpStatusCode.OK, "null")));
        var act = () => gateway.GetRunSnapshotAsync(Guid.NewGuid(), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task RunSnapshotGateway_ShouldRejectMissingStats()
    {
        var json = $$"""{"PlayerId":"{{Guid.NewGuid()}}","DisplayName":"P","Characters":[{"CharacterId":"{{Guid.NewGuid()}}","DefinitionKey":"hero","DisplayName":"Hero","MaxVitality":1,"BaseMana":1,"BaseCharge":0,"SkillKeys":["skill.one"],"Stats":null}]}""";
        var gateway = new HttpPlayerRunSnapshotGateway(Client(_ => Json(HttpStatusCode.OK, json)));
        var act = () => gateway.GetRunSnapshotAsync(Guid.NewGuid(), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no stat snapshot*");
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("[\"\"]")]
    public async Task RunSnapshotGateway_ShouldRejectInvalidSkillKeys(string skillKeysJson)
    {
        var json = $$"""
        {"PlayerId":"{{Guid.NewGuid()}}","DisplayName":"P","Characters":[{
          "CharacterId":"{{Guid.NewGuid()}}","DefinitionKey":"hero","DisplayName":"Hero",
          "MaxVitality":1,"BaseMana":1,"BaseCharge":0,"SkillKeys":{{skillKeysJson}},
          "Stats":{"MaxVitality":1,"AttackPower":0,"Defense":0,"StartingGuard":0,"Speed":0,"Initiative":0,"Focus":0,"Mana":1,"Charge":0}
        }]}
        """;
        var gateway = new HttpPlayerRunSnapshotGateway(Client(_ => Json(HttpStatusCode.OK, json)));
        var act = () => gateway.GetRunSnapshotAsync(Guid.NewGuid(), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no valid equipped skill keys*");
    }

    [Fact]
    public async Task ProfileGateway_ShouldTranslateReadProfileStatuses()
    {
        var playerId = Guid.NewGuid();

        foreach (var (status, expected) in new[]
                 {
                     (HttpStatusCode.NotFound, typeof(NotFoundException)),
                     (HttpStatusCode.Conflict, typeof(ConflictException)),
                     (HttpStatusCode.BadRequest, typeof(DomainException)),
                     (HttpStatusCode.InternalServerError, typeof(HttpRequestException))
                 })
        {
            var gateway = new HttpPlayerProfileGateway(Client(_ => Json(status, "failure")));
            var act = () => gateway.GetProfileAsync(playerId, CancellationToken.None);
            (await act.Should().ThrowAsync<Exception>()).Which.Should().BeOfType(expected);
        }
    }

    [Fact]
    public async Task ProfileGateway_ShouldRejectNullProfileBody()
    {
        var gateway = new HttpPlayerProfileGateway(Client(_ => Json(HttpStatusCode.OK, "null")));
        var act = () => gateway.GetProfileAsync(Guid.NewGuid(), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ProfileGateway_ShouldMapCompleteAndIncompleteProfiles()
    {
        var playerId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        var sourceRun = Guid.NewGuid();
        var complete = $$"""
        {
          "Id":"{{playerId}}","DisplayName":"Player",
          "Characters":[{
            "Id":"{{characterId}}","DefinitionKey":"hero","DisplayName":"Hero",
            "Skills":[{"SkillKey":"skill.one","UnlockedAtUtc":"2026-01-01T00:00:00Z","Source":"test","IsEquipped":true}],
            "Stats":{"MaxVitality":100,"AttackPower":10,"Defense":8,"StartingGuard":2,"Speed":9,"Initiative":7,"Focus":6,"Mana":20,"Charge":1,"MagicAttack":4,"MagicDefense":5,"Movement":3},
            "MaxEquippedSkills":4,
            "Items":[{"ItemKey":"item.one","AcquiredAtUtc":"2026-01-01T00:00:00Z","Source":"test","IsEquipped":true,"Slot":"Weapon"}],
            "MaxEquippedItems":3,"CharacterType":"Standard"
          }],
          "Progression":{"PalaceShardCount":12,"HimLitShardCount":3},
          "PermanentItems":[{"ItemDefinitionKey":"item.permanent","SourceRunId":"{{sourceRun}}","AcquiredAtUtc":"2026-01-01T00:00:00Z","ContainedLiquidDefinitionKey":"liquid.one"}],
          "MainStory":{"SequenceKey":"story.main","SequenceVersion":"1","StepKey":"s2","CheckpointKey":"cp","IsCompleted":false,"HighestDifficultyLevelUnlocked":2,"UnlockedRoomKeys":["room.a"],"VisibleRoomKeys":["room.a","room.b"]}
        }
        """;
        var gateway = new HttpPlayerProfileGateway(Client(_ => Json(HttpStatusCode.OK, complete)));

        var profile = await gateway.GetProfileAsync(playerId, CancellationToken.None);

        profile.Characters.Single().Items.Should().ContainSingle();
        profile.PermanentItems.Should().ContainSingle();
        profile.MainStory.SequenceKey.Should().Be("story.main");

        var minimal = $$"""
        {"Id":"{{playerId}}","DisplayName":"Player","Characters":[{
          "Id":"{{characterId}}","DefinitionKey":"hero","DisplayName":"Hero","Skills":[],
          "Stats":{"MaxVitality":1,"AttackPower":0,"Defense":0,"StartingGuard":0,"Speed":0,"Initiative":0,"Focus":0,"Mana":0,"Charge":0},
          "MaxEquippedSkills":4,"Items":null
        }],"Progression":{},"PermanentItems":null,"MainStory":null}
        """;
        var minimalGateway = new HttpPlayerProfileGateway(Client(_ => Json(HttpStatusCode.OK, minimal)));
        var minimalProfile = await minimalGateway.GetProfileAsync(playerId, CancellationToken.None);
        minimalProfile.Characters.Single().Items.Should().BeEmpty();
        minimalProfile.PermanentItems.Should().BeEmpty();
        minimalProfile.MainStory.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task ProfileGateway_ShouldCoverCurrencyAndClaimBooleanResponses()
    {
        var playerId = Guid.NewGuid();
        var responses = new Queue<HttpResponseMessage>(
        [
            Json(HttpStatusCode.OK, "{\"Succeeded\":true}"),
            Json(HttpStatusCode.OK, "null"),
            Json(HttpStatusCode.OK, "{\"Succeeded\":false}"),
            Json(HttpStatusCode.OK, "{\"Claimed\":true}"),
            Json(HttpStatusCode.OK, "null")
        ]);
        var gateway = new HttpPlayerProfileGateway(Client(_ => responses.Dequeue()));

        (await gateway.TrySpendCurrencyAsync(playerId, 1, CancellationToken.None)).Should().BeTrue();
        (await gateway.TrySpendCurrencyAsync(playerId, 1, CancellationToken.None)).Should().BeFalse();
        (await gateway.TrySpendHimLitCurrencyAsync(playerId, 1, CancellationToken.None)).Should().BeFalse();
        (await gateway.HasClaimedNpcOfferingAsync(playerId, "npc", "offer", CancellationToken.None)).Should().BeTrue();
        (await gateway.HasClaimedNpcOfferingAsync(playerId, "npc", "offer", CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task ProfileGateway_ShouldTranslateNotFoundForBooleanAndVoidInternalEndpoints()
    {
        var playerId = Guid.NewGuid();

        foreach (Func<HttpPlayerProfileGateway, Task> call in new Func<HttpPlayerProfileGateway, Task>[]
                 {
                     g => g.TrySpendCurrencyAsync(playerId, 1, CancellationToken.None),
                     g => g.TrySpendHimLitCurrencyAsync(playerId, 1, CancellationToken.None),
                     g => g.HasClaimedNpcOfferingAsync(playerId, "npc", "offer", CancellationToken.None),
                     g => g.ClaimNpcOfferingAsync(playerId, "npc", "offer", null, CancellationToken.None),
                     g => g.GrantReputationMilestoneAsync(playerId, "npc", "milestone", null, CancellationToken.None),
                     g => g.GetNpcReputationScoresAsync(playerId, CancellationToken.None),
                     g => g.UpsertNpcReputationScoresAsync(playerId, Guid.NewGuid(), [], CancellationToken.None)
                 })
        {
            var gateway = new HttpPlayerProfileGateway(Client(_ => Json(HttpStatusCode.NotFound, "missing")));
            var act = () => call(gateway);
            await act.Should().ThrowAsync<NotFoundException>();
        }
    }

    [Fact]
    public async Task ProfileGateway_ShouldMapReputationAndAllowSuccessfulVoidCalls()
    {
        var playerId = Guid.NewGuid();
        var responses = new Queue<HttpResponseMessage>(
        [
            Json(HttpStatusCode.OK, "[{\"NpcKey\":\"npc.erika\",\"Score\":4,\"TimesMet\":2,\"CurrentDialogueNodeKey\":\"node.2\"}]"),
            Json(HttpStatusCode.OK, "{}"),
            Json(HttpStatusCode.OK, "{}")
        ]);
        var gateway = new HttpPlayerProfileGateway(Client(_ => responses.Dequeue()));

        var scores = await gateway.GetNpcReputationScoresAsync(playerId, CancellationToken.None);
        scores.Should().ContainSingle();
        scores.Single().NpcKey.Should().Be("npc.erika");

        await gateway.ClaimNpcOfferingAsync(playerId, "npc", "offer", null, CancellationToken.None);
        await gateway.GrantReputationMilestoneAsync(playerId, "npc", "milestone", null, CancellationToken.None);
    }

    private static HttpClient Client(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
        new(new StubHandler(responder)) { BaseAddress = new Uri("http://localhost") };

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}
