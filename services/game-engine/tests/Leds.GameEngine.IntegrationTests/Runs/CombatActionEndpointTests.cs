using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Runs.ResolveCurrentEvent;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Leds.GameEngine.IntegrationTests.Runs;

public sealed class CombatActionEndpointTests : RunIntegrationTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    public CombatActionEndpointTests(WebApplicationFactory<Program> factory)
        : base(factory.CreateClient())
    {
    }

    [Fact]
    public async Task UseCombatSkill_ShouldReturnOk_WhenSingleEnemyTargetsOppositeSide()
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

        var response = await Client.PostAsJsonAsync(
            $"/api/v2/runs/{runId}/combats/{combat.Id.Value}/skill-actions",
            new { ActorId = action.Value.Actor.Id.Value, SkillKey = action.Value.Skill.Key, TargetIds = new[] { action.Value.Target.Id.Value } });

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);

        var result = await response.Content.ReadFromJsonAsync<CombatSkillActionResult>();
        result.Should().NotBeNull();
        result!.Accepted.Should().BeTrue();
        result.TargetIds.Should().ContainSingle(id => id == action.Value.Target.Id.Value);
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
            $"/api/v2/runs/{runId}/combats/{combat.Id.Value}/skill-actions",
            new { ActorId = action.Value.Actor.Id.Value, SkillKey = action.Value.Skill.Key, TargetIds = new[] { action.Value.Target.Id.Value } });

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, because: body);
        body.Should().Contain("opposite side");
    }

    private async Task<(Guid RunId, CombatRuntimeDto Combat)?> StartActiveRuntimeCombatAsync()
    {
        var startRunResponse = await StartRunAsync();
        var nodeToChoose = startRunResponse.Run.CurrentRoom.AvailableNodes
            .FirstOrDefault(node => node.Type == "Combat");

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
        var actor = combatants.FirstOrDefault(c => c.Skills.Any(s => s.TargetingType == targetingType));

        if (actor is null)
        {
            return null;
        }

        var skill = actor.Skills.First(s => s.TargetingType == targetingType);
        var target = combatants.FirstOrDefault(c => targetSameSide ? c.Side == actor.Side : c.Side != actor.Side);

        return target is null ? null : (actor, skill, target);
    }
}
