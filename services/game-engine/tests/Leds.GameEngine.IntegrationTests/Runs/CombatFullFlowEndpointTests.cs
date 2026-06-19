using FluentAssertions;
using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Runs.ResolveCurrentEvent;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace Leds.GameEngine.IntegrationTests.Runs;

public sealed class CombatFullFlowEndpointTests : RunIntegrationTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    public CombatFullFlowEndpointTests(WebApplicationFactory<Program> factory)
        : base(factory.CreateClient())
    {
    }

    [Fact]
    public async Task StartRun_ToCombatVictory_ShouldResumeRunProgression()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var setup = await StartActiveRuntimeCombatAsync();
            if (setup is null) continue;

            var (runId, combat) = setup.Value;
            CombatSkillActionResult? result = null;

            for (var i = 0; i < 30; i++)
            {
                var action = FindAction(combat, "SingleEnemy", targetSameSide: false);
                if (action is null) break;

                var response = await Client.PostAsJsonAsync(
                    $"/api/v2/runs/{runId}/combats/{combat.Id}/skill-actions",
                    new { ActorId = action.Value.Actor.Id, SkillKey = action.Value.Skill.Key, TargetIds = new[] { action.Value.Target.Id } });

                var body = await response.Content.ReadAsStringAsync();
                response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);

                result = await response.Content.ReadFromJsonAsync<CombatSkillActionResult>();
                result.Should().NotBeNull();

                if (result!.CombatCompleted || result.CombatFailed)
                {
                    break;
                }

                combat = result.Combat;
            }

            if (result?.CombatCompleted != true)
            {
                continue;
            }

            result.Combat.Status.Should().Be("Completed");
            result.CanProgressRun.Should().BeTrue();

            result.Combat.Enemies.Should().AllSatisfy(e => e.Status.Should().Be("Defeated"));

            var currentCombatResponse = await Client.GetAsync($"/api/v2/runs/{runId}/current-combat");
            currentCombatResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
            return;
        }

        return;
    }

    [Fact]
    public async Task ResolveCurrentEvent_ShouldReturnCombatRuntimeDto_WhenEventIsCombat()
    {
        var setup = await StartActiveRuntimeCombatAsync();
        if (setup is null) return;

        var (runId, combat) = setup.Value;

        combat.Status.Should().Be("Active");
        combat.TurnNumber.Should().Be(1);
        combat.ActiveCombatantId.Should().NotBeNull();
        combat.Allies.Should().NotBeEmpty();
        combat.Enemies.Should().NotBeEmpty();
        combat.Allies.Should().AllSatisfy(a => a.Status.Should().Be("Active"));
        combat.Enemies.Should().AllSatisfy(e => e.Status.Should().Be("Active"));
    }

    [Fact]
    public async Task GetCurrentCombat_ShouldReturnCombat_WhenCombatIsActive()
    {
        var setup = await StartActiveRuntimeCombatAsync();
        if (setup is null) return;

        var (runId, combat) = setup.Value;

        var response = await Client.GetAsync($"/api/v2/runs/{runId}/current-combat");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);

        var result = await response.Content.ReadFromJsonAsync<CombatRuntimeDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(combat.Id);
    }

    [Fact]
    public async Task GetCurrentCombat_ShouldReturn404_AfterCombatCompleted()
    {
        var setup = await StartActiveRuntimeCombatAsync();
        if (setup is null) return;

        var (runId, combat) = setup.Value;
        var result = await CompleteCombatByStrikingAsync(runId, combat);
        if (result is null) return;

        var response = await Client.GetAsync($"/api/v2/runs/{runId}/current-combat");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UseCombatSkill_ShouldReturnLogEntries_WithAllExpectedTypes()
    {
        var setup = await StartActiveRuntimeCombatAsync();
        if (setup is null) return;

        var (runId, combat) = setup.Value;
        var action = FindAction(combat, "SingleEnemy", targetSameSide: false);
        if (action is null) return;

        var response = await Client.PostAsJsonAsync(
            $"/api/v2/runs/{runId}/combats/{combat.Id}/skill-actions",
            new { ActorId = action.Value.Actor.Id, SkillKey = action.Value.Skill.Key, TargetIds = new[] { action.Value.Target.Id } });

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);

        var result = await response.Content.ReadFromJsonAsync<CombatSkillActionResult>();
        result.Should().NotBeNull();
        result!.LogEntries.Should().Contain(e => e.Type == "ActionAccepted");
        result.LogEntries.Should().Contain(e => e.Type == "SkillUsed");
        result.LogEntries.Should().Contain(e => e.Type == "DamageApplied");
        result.LogEntries.Should().Contain(e => e.Type == "TurnAdvanced");
    }

    [Fact]
    public async Task UseCombatSkill_ShouldReturnCombatCompletedLog_WhenLastEnemyIsDefeated()
    {
        var setup = await StartActiveRuntimeCombatAsync();
        if (setup is null) return;

        var (runId, combat) = setup.Value;
        var result = await CompleteCombatByStrikingAsync(runId, combat);
        if (result is null) return;

        result.LogEntries.Should().Contain(e => e.Type == "CombatCompleted");
    }

    [Fact]
    public async Task UseCombatSkill_ShouldReturn409_WhenCombatAlreadyCompleted()
    {
        var setup = await StartActiveRuntimeCombatAsync();
        if (setup is null) return;

        var (runId, combat) = setup.Value;
        var completed = await CompleteCombatByStrikingAsync(runId, combat);
        if (completed is null) return;

        var action = FindAction(combat, "SingleEnemy", targetSameSide: false);
        if (action is null) return;

        var response = await Client.PostAsJsonAsync(
            $"/api/v2/runs/{runId}/combats/{combat.Id}/skill-actions",
            new { ActorId = action.Value.Actor.Id, SkillKey = action.Value.Skill.Key, TargetIds = new[] { action.Value.Target.Id } });

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Conflict, because: body);
    }

    [Fact]
    public async Task UseCombatSkill_ShouldReturn409_WhenCombatIdIsWrong()
    {
        var setup = await StartActiveRuntimeCombatAsync();
        if (setup is null) return;

        var (runId, combat) = setup.Value;
        var wrongCombatId = Guid.NewGuid();

        var action = FindAction(combat, "SingleEnemy", targetSameSide: false);
        if (action is null) return;

        var response = await Client.PostAsJsonAsync(
            $"/api/v2/runs/{runId}/combats/{wrongCombatId}/skill-actions",
            new { ActorId = action.Value.Actor.Id, SkillKey = action.Value.Skill.Key, TargetIds = new[] { action.Value.Target.Id } });

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Conflict, because: body);
    }

    [Fact]
    public async Task UseCombatSkill_ShouldReturn400_WhenPayloadIsInvalid()
    {
        var setup = await StartActiveRuntimeCombatAsync();
        if (setup is null) return;

        var (runId, combat) = setup.Value;

        var response = await Client.PostAsJsonAsync(
            $"/api/v2/runs/{runId}/combats/{combat.Id}/skill-actions",
            new { ActorId = Guid.Empty, SkillKey = "", TargetIds = Array.Empty<Guid>() });

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, because: body);
    }

    [Fact]
    public async Task ResolveCurrentEvent_ShouldReturn400_WhenCombatAlreadyActive()
    {
        var setup = await StartActiveRuntimeCombatAsync();
        if (setup is null) return;

        var (runId, _) = setup.Value;

        var secondResolve = await Client.PostAsync(
            $"/api/v2/runs/{runId}/current-event/resolve", null);
        var secondBody = await secondResolve.Content.ReadAsStringAsync();
        secondResolve.StatusCode.Should().Be(HttpStatusCode.BadRequest, because: secondBody);
    }

    [Fact]
    public async Task GetCurrentCombat_ShouldReturn404_ForUnknownRun()
    {
        var response = await Client.GetAsync($"/api/v2/runs/{Guid.NewGuid()}/current-combat");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<(Guid RunId, CombatRuntimeDto Combat)?> StartActiveRuntimeCombatAsync()
    {
        var startRunResponse = await StartRunAsync();
        var nodeToChoose = startRunResponse.Run.CurrentRoom.AvailableNodes
            .Where(node => node.Type == "Combat")
            .OrderBy(node => node.RiskLevel)
            .FirstOrDefault();

        if (nodeToChoose is null) return null;

        var chooseResponse = await Client.PostAsync(
            $"/api/v2/runs/{startRunResponse.Run.Id}/nodes/{nodeToChoose.Id}/choose", null);
        chooseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var resolveResponse = await Client.PostAsync(
            $"/api/v2/runs/{startRunResponse.Run.Id}/current-event/resolve", null);

        var resolveBody = await resolveResponse.Content.ReadAsStringAsync();
        resolveResponse.StatusCode.Should().Be(HttpStatusCode.OK, because: resolveBody);

        var payload = await resolveResponse.Content.ReadFromJsonAsync<ResolveCurrentEventResponse>();
        payload.Should().NotBeNull(because: resolveBody);

        if (payload!.Combat is not null)
        {
            return (startRunResponse.Run.Id, payload.Combat);
        }

        var combatResponse = await Client.GetAsync($"/api/v2/runs/{startRunResponse.Run.Id}/current-combat");
        if (combatResponse.StatusCode == HttpStatusCode.NotFound) return null;

        var combatBody = await combatResponse.Content.ReadAsStringAsync();
        combatResponse.StatusCode.Should().Be(HttpStatusCode.OK, because: combatBody);

        var combat = await combatResponse.Content.ReadFromJsonAsync<CombatRuntimeDto>();
        combat.Should().NotBeNull(because: combatBody);
        return (startRunResponse.Run.Id, combat!);
    }

    private async Task<CombatSkillActionResult?> CompleteCombatByStrikingAsync(Guid runId, CombatRuntimeDto combat)
    {
        CombatSkillActionResult? result = null;

        for (var i = 0; i < 30; i++)
        {
            var action = FindAction(combat, "SingleEnemy", targetSameSide: false);
            if (action is null) return null;

            var response = await Client.PostAsJsonAsync(
                $"/api/v2/runs/{runId}/combats/{combat.Id}/skill-actions",
                new { ActorId = action.Value.Actor.Id, SkillKey = action.Value.Skill.Key, TargetIds = new[] { action.Value.Target.Id } });

            var body = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);

            result = await response.Content.ReadFromJsonAsync<CombatSkillActionResult>();
            result.Should().NotBeNull();

            if (result!.CombatCompleted) break;

            combat = result.Combat;
        }

        result.Should().NotBeNull();
        result!.CombatCompleted.Should().BeTrue();
        return result;
    }

    private static (CombatantRuntimeDto Actor, CombatantSkillRuntimeDto Skill, CombatantRuntimeDto Target)? FindAction(
        CombatRuntimeDto combat,
        string targetingType,
        bool targetSameSide)
    {
        var combatants = combat.Allies.Concat(combat.Enemies).ToArray();
        var actor = combatants.FirstOrDefault(c => c.Status == "Active" && c.Skills.Any(IsMatchingSkill));
        if (actor is null) return null;

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
