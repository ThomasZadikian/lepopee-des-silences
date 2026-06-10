using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Runs.GetRunById;
using Leds.GameEngine.Application.Runs.ResolveCurrentEvent;
using Leds.GameEngine.Domain.Combats;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Leds.GameEngine.IntegrationTests.Runs;

public sealed class CombatActionEndpointTests : RunIntegrationTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    public CombatActionEndpointTests(WebApplicationFactory<Program> factory)
        : base(factory.CreateClient())
    {
    }

    [Fact]
    public async Task UseCombatSkill_ShouldReduceTargetVitalityAndReturnUpdatedCombat_WhenDamageSkillTargetsOppositeSide()
    {
        var setup = await StartActiveRuntimeCombatAsync();

        if (setup is null)
        {
            return;
        }

        var (runId, combat) = setup.Value;
        var action = FindAction(combat, "SingleEnemy", targetSameSide: false);

        if (action is null)
        {
            return;
        }

        var targetVitalityBefore = action.Value.Target.CurrentVitality;
        var actorVitalityBefore = action.Value.Actor.CurrentVitality;
        var turnNumberBefore = combat.TurnNumber;
        var activeCombatantBefore = combat.ActiveCombatantId;

        var response = await Client.PostAsJsonAsync(
            $"/api/v2/runs/{runId}/combats/{combat.Id}/skill-actions",
            new { ActorId = action.Value.Actor.Id, SkillKey = action.Value.Skill.Key, TargetIds = new[] { action.Value.Target.Id } });

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);

        var result = await response.Content.ReadFromJsonAsync<CombatSkillActionResult>();
        result.Should().NotBeNull();
        result!.Accepted.Should().BeTrue();
        result.TargetIds.Should().ContainSingle(id => id == action.Value.Target.Id);
        result.LogEntries.Should().Contain(e => e.Type == "ActionAccepted");
        result.LogEntries.Should().Contain(e => e.Type == "SkillUsed");
        result.LogEntries.Should().Contain(e => e.Type == "DamageApplied");
        result.LogEntries.Should().Contain(e => e.Type == "EnemyTurnResolved");
        result.Combat.TurnNumber.Should().Be(turnNumberBefore + 1);
        result.Combat.ActiveCombatantId.Should().Be(activeCombatantBefore);

        var updatedTarget = result.Combat.Allies
            .Concat(result.Combat.Enemies)
            .Single(c => c.Id == action.Value.Target.Id);
        updatedTarget.CurrentVitality.Should().BeLessThan(targetVitalityBefore);

        var updatedActor = result.Combat.Allies
            .Concat(result.Combat.Enemies)
            .Single(c => c.Id == action.Value.Actor.Id);
        updatedActor.CurrentVitality.Should().BeLessThan(actorVitalityBefore);
    }

    [Fact]
    public async Task UseCombatSkill_ShouldReturnBadRequest_WhenSingleEnemyTargetsSameSide()
    {
        var setup = await StartActiveRuntimeCombatAsync();

        if (setup is null)
        {
            return;
        }

        var (runId, combat) = setup.Value;
        var action = FindAction(combat, "SingleEnemy", targetSameSide: true);

        if (action is null)
        {
            return;
        }

        var response = await Client.PostAsJsonAsync(
            $"/api/v2/runs/{runId}/combats/{combat.Id}/skill-actions",
            new { ActorId = action.Value.Actor.Id, SkillKey = action.Value.Skill.Key, TargetIds = new[] { action.Value.Target.Id } });

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Conflict, because: body);
        body.Should().Contain("opposite side");
    }

    [Fact]
    public async Task UseCombatSkill_ShouldReturnBadRequest_WhenActorIsNotActiveCombatant()
    {
        var setup = await StartActiveRuntimeCombatAsync();

        if (setup is null)
        {
            return;
        }

        var (runId, combat) = setup.Value;
        var activeCombatant = combat.Allies.Concat(combat.Enemies)
            .Single(c => c.Id == combat.ActiveCombatantId);
        var actor = combat.Allies.Concat(combat.Enemies)
            .FirstOrDefault(c => c.Id != activeCombatant.Id && c.Skills.Any(s => s.TargetingType == "SingleEnemy"));

        if (actor is null)
        {
            return;
        }

        var skill = actor.Skills.First(s => s.TargetingType == "SingleEnemy");
        var target = combat.Allies.Concat(combat.Enemies).First(c => c.Side != actor.Side);

        var response = await Client.PostAsJsonAsync(
            $"/api/v2/runs/{runId}/combats/{combat.Id}/skill-actions",
            new { ActorId = actor.Id, SkillKey = skill.Key, TargetIds = new[] { target.Id } });

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, because: body);
        body.Should().Contain("It is not this combatant's turn.");
    }

    [Fact]
    public async Task UseCombatSkill_ShouldCompleteCombatAndClearCurrentCombat_WhenLastEnemyIsDefeated()
    {
        var setup = await StartActiveRuntimeCombatAsync();

        if (setup is null)
        {
            return;
        }

        var (runId, combat) = setup.Value;
        CombatSkillActionResult? result = null;

        for (var i = 0; i < 20; i++)
        {
            var action = FindAction(combat, "SingleEnemy", targetSameSide: false);

            if (action is null)
            {
                return;
            }

            var response = await Client.PostAsJsonAsync(
                $"/api/v2/runs/{runId}/combats/{combat.Id}/skill-actions",
                new { ActorId = action.Value.Actor.Id, SkillKey = action.Value.Skill.Key, TargetIds = new[] { action.Value.Target.Id } });

            var body = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);
            result = await response.Content.ReadFromJsonAsync<CombatSkillActionResult>();
            result.Should().NotBeNull();

            if (result!.CombatFailed)
            {
                result.CombatFailed.Should().BeFalse("the player should not lose this seeded smoke combat before defeating enemies");
            }

            if (result.CombatCompleted)
            {
                break;
            }

            combat = result.Combat;
        }

        result.Should().NotBeNull();
        result!.CombatCompleted.Should().BeTrue();
        result.CanProgressRun.Should().BeTrue();
        result.Combat.Status.Should().Be("Completed");

        var currentCombatResponse = await Client.GetAsync($"/api/v2/runs/{runId}/current-combat");
        currentCombatResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var runResponse = await Client.GetAsync($"/api/v2/runs/{runId}");
        runResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var runPayload = await runResponse.Content.ReadFromJsonAsync<GetRunByIdResponse>();
        runPayload.Should().NotBeNull();
        runPayload!.Run.ActiveCombatId.Should().BeNull();
        runPayload.Run.PendingRewardOfferId.Should().NotBeNull();
    }

    private async Task<(Guid RunId, CombatRuntimeDto Combat)?> StartActiveRuntimeCombatAsync()
    {
        var startRunResponse = await StartRunAsync();
        var nodeToChoose = startRunResponse.Run.CurrentRoom.AvailableNodes
            .Where(node => node.Type == "Combat")
            .OrderBy(node => node.RiskLevel)
            .FirstOrDefault();

        if (nodeToChoose is null)
        {
            return null;
        }

        var chooseResponse = await Client.PostAsync(
            $"/api/v2/runs/{startRunResponse.Run.Id}/nodes/{nodeToChoose.Id}/choose",
            content: null);

        chooseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var resolveResponse = await Client.PostAsync(
            $"/api/v2/runs/{startRunResponse.Run.Id}/current-event/resolve",
            content: null);

        var resolveBody = await resolveResponse.Content.ReadAsStringAsync();

        resolveResponse.StatusCode.Should().Be(HttpStatusCode.OK, because: resolveBody);

        var payload = await resolveResponse.Content.ReadFromJsonAsync<ResolveCurrentEventResponse>();
        payload.Should().NotBeNull(because: resolveBody);
        payload!.Run.ActiveCombatId.Should().NotBeNull(because: resolveBody);

        if (payload.Combat is not null)
        {
            return (startRunResponse.Run.Id, payload.Combat);
        }

        var combatResponse = await Client.GetAsync($"/api/v2/runs/{startRunResponse.Run.Id}/current-combat");
        var combatBody = await combatResponse.Content.ReadAsStringAsync();

        if (combatResponse.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        combatResponse.StatusCode.Should().Be(HttpStatusCode.OK, because: combatBody);

        var combat = await combatResponse.Content.ReadFromJsonAsync<CombatRuntimeDto>();
        combat.Should().NotBeNull(because: combatBody);

        return (startRunResponse.Run.Id, combat!);
    }

    private static (CombatantRuntimeDto Actor, CombatantSkillRuntimeDto Skill, CombatantRuntimeDto Target)? FindAction(
        CombatRuntimeDto combat,
        string targetingType,
        bool targetSameSide)
    {
        var combatants = combat.Allies.Concat(combat.Enemies).ToArray();
        var actor = combatants.FirstOrDefault(c => c.Status == "Active" && c.Skills.Any(IsMatchingSkill));

        if (actor is null)
        {
            return null;
        }

        var skill = actor.Skills.Where(IsMatchingSkill).OrderByDescending(s => s.BasePower).First();
        var target = combatants.FirstOrDefault(c => c.Status == "Active" && (targetSameSide ? c.Side == actor.Side : c.Side != actor.Side));

        return target is null ? null : (actor, skill, target);

        bool IsMatchingSkill(CombatantSkillRuntimeDto skill)
        {
            return skill.TargetingType == targetingType
                && skill.EffectType == "Damage"
                && skill.BasePower > 0;
        }
    }
}
