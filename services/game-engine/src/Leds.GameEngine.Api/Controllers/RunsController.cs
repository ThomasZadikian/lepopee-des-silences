using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Application.Runs.AbandonRun;
using Leds.GameEngine.Application.Runs.ChallengeBossRemotely;
using Leds.GameEngine.Application.Runs.ConfirmPermanentItemSelection;
using Leds.GameEngine.Application.Runs.EmptyRunItemContainer;
using Leds.GameEngine.Application.Runs.EnterGridNode;
using Leds.GameEngine.Application.Runs.ExitMidRoom;
using Leds.GameEngine.Application.Runs.GetCurrentCombat;
using Leds.GameEngine.Application.Runs.GetPermanentItemCandidates;
using Leds.GameEngine.Application.Runs.GetRunById;
using Leds.GameEngine.Application.Runs.GetRunInventory;
using Leds.GameEngine.Application.Runs.GetRunReputation;
using Leds.GameEngine.Application.Runs.MoveParty;
using Leds.GameEngine.Application.Runs.TacticalCombat;
using Leds.GameEngine.Application.Runs.Search;
using Leds.GameEngine.Application.Runs.MoveToNextRoom;
using Leds.GameEngine.Application.Runs.PourRunItemLiquid;
using Leds.GameEngine.Application.Runs.ProgressRun;
using Leds.GameEngine.Application.Runs.RaiseNodeRisk;
using Leds.GameEngine.Application.Runs.RemovePalaceLaw;
using Leds.GameEngine.Application.Runs.UseCaliceInfini;
using Leds.GameEngine.Application.Runs.ResolveCurrentEvent;
using Leds.GameEngine.Application.Runs.ResumeRun;
using Leds.GameEngine.Application.Runs.SaveAndExitRun;
using Leds.GameEngine.Application.Runs.StartRun;
using Leds.GameEngine.Application.Runs.SyncPartySkills;
using Leds.GameEngine.Application.Runs.SyncPartyStats;
using Leds.GameEngine.Application.Runs.UseRunItem;
using MediatR;
using Leds.GameEngine.Domain.Runs;
using Microsoft.AspNetCore.Mvc;

namespace Leds.GameEngine.Api.Controllers;

[ApiController]
[Route("api/v2/runs")]
public sealed class RunsController : ControllerBase
{
    private readonly ISender _sender;

    public RunsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [ProducesResponseType(typeof(StartRunResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StartRunResponse>> StartRun(
        [FromBody] StartRunRequest request,
        CancellationToken cancellationToken)
    {
        var command = new StartRunCommand(request.PlayerId, request.CombatMode);

        var response = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetRunById),
            new { runId = response.Run.Id },
            response);
    }

    [HttpGet("{runId:guid}")]
    [ProducesResponseType(typeof(GetRunByIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetRunByIdResponse>> GetRunById(
        Guid runId,
        CancellationToken cancellationToken)
    {
        var query = new GetRunByIdQuery(runId);

        var response = await _sender.Send(query, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{runId:guid}/current-combat")]
    [ProducesResponseType(typeof(CombatRuntimeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CombatRuntimeDto>> GetCurrentCombat(
        Guid runId,
        CancellationToken cancellationToken)
    {
        var query = new GetCurrentCombatQuery(runId);

        var response = await _sender.Send(query, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{runId:guid}/current-event/resolve")]
    [ProducesResponseType(typeof(ResolveCurrentEventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResolveCurrentEventResponse>> ResolveCurrentEvent(
        Guid runId,
        CancellationToken cancellationToken)
    {
        var command = new ResolveCurrentEventCommand(runId);

        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{runId:guid}/progress")]
    [ProducesResponseType(typeof(ProgressRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProgressRunResponse>> ProgressRun(
    Guid runId,
    CancellationToken cancellationToken)
    {
        var command = new ProgressRunCommand(runId);

        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{runId:guid}/rooms/next")]
    [ProducesResponseType(typeof(MoveToNextRoomResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MoveToNextRoomResponse>> MoveToNextRoom(
    Guid runId,
    CancellationToken cancellationToken)
    {
        var command = new MoveToNextRoomCommand(runId);

        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{runId:guid}/exit-mid-room")]
    [ProducesResponseType(typeof(ExitMidRoomResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExitMidRoomResponse>> ExitMidRoom(
    Guid runId,
    CancellationToken cancellationToken)
    {
        var command = new ExitMidRoomCommand(runId);

        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{runId:guid}/abandon")]
    [ProducesResponseType(typeof(AbandonRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AbandonRunResponse>> AbandonRun(
    Guid runId,
    CancellationToken cancellationToken)
    {
        var command = new AbandonRunCommand(runId);

        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{runId:guid}/save-and-exit")]
    [ProducesResponseType(typeof(SaveAndExitRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaveAndExitRunResponse>> SaveAndExitRun(
    Guid runId,
    CancellationToken cancellationToken)
    {
        var command = new SaveAndExitRunCommand(runId);

        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{runId:guid}/resume")]
    [ProducesResponseType(typeof(ResumeRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResumeRunResponse>> ResumeRun(
    Guid runId,
    CancellationToken cancellationToken)
    {
        var command = new ResumeRunCommand(runId);

        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{runId:guid}/current-event/choice")]
    [ProducesResponseType(typeof(ChooseCurrentEventOptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChooseCurrentEventOptionResponse>> ChooseCurrentEventOption(
    Guid runId,
    [FromBody] ChooseCurrentEventOptionCommand request,
    CancellationToken cancellationToken)
    {
        var command = new ChooseCurrentEventOptionCommand(
            runId,
            request.ChoiceId);

        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{runId:guid}/inventory")]
    [ProducesResponseType(typeof(GetRunInventoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetRunInventoryResponse>> GetRunInventory(
        Guid runId,
        CancellationToken cancellationToken)
    {
        var query = new GetRunInventoryQuery(runId);

        var response = await _sender.Send(query, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{runId:guid}/inventory/{itemId:guid}/use")]
    [ProducesResponseType(typeof(UseRunItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UseRunItemResponse>> UseRunItem(
    Guid runId,
    Guid itemId,
    CancellationToken cancellationToken)
    {
        var command = new UseRunItemCommand(runId, itemId);
        var response = await _sender.Send(command, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{runId:guid}/inventory/{containerItemId:guid}/pour")]
    [ProducesResponseType(typeof(PourRunItemLiquidResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PourRunItemLiquidResponse>> PourRunItemLiquid(
        Guid runId,
        Guid containerItemId,
        [FromBody] PourRunItemLiquidRequest request,
        CancellationToken cancellationToken)
    {
        var command = new PourRunItemLiquidCommand(runId, containerItemId, request.LiquidItemId);
        var response = await _sender.Send(command, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{runId:guid}/inventory/{containerItemId:guid}/empty")]
    [ProducesResponseType(typeof(EmptyRunItemContainerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmptyRunItemContainerResponse>> EmptyRunItemContainer(
        Guid runId,
        Guid containerItemId,
        CancellationToken cancellationToken)
    {
        var command = new EmptyRunItemContainerCommand(runId, containerItemId);
        var response = await _sender.Send(command, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{runId:guid}/reputation")]
    [ProducesResponseType(typeof(GetRunReputationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetRunReputationResponse>> GetRunReputation(
        Guid runId,
        CancellationToken cancellationToken)
    {
        var query = new GetRunReputationQuery(runId);
        var response = await _sender.Send(query, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{runId:guid}/permanent-item-candidates")]
    [ProducesResponseType(typeof(GetPermanentItemCandidatesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetPermanentItemCandidatesResponse>> GetPermanentItemCandidates(
        Guid runId,
        CancellationToken cancellationToken)
    {
        var query = new GetPermanentItemCandidatesQuery(runId);
        var response = await _sender.Send(query, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{runId:guid}/permanent-items/confirm")]
    [ProducesResponseType(typeof(ConfirmPermanentItemSelectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConfirmPermanentItemSelectionResponse>> ConfirmPermanentItemSelection(
        Guid runId,
        [FromBody] ConfirmPermanentItemSelectionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmPermanentItemSelectionCommand(runId, request.ItemDefinitionKeys);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{runId:guid}/palace-laws/{lawKey}/revoke")]
    [ProducesResponseType(typeof(RemovePalaceLawResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RemovePalaceLawResponse>> RemovePalaceLaw(
        Guid runId,
        string lawKey,
        CancellationToken cancellationToken)
    {
        var command = new RemovePalaceLawCommand(runId, lawKey);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{runId:guid}/nodes/{nodeId:guid}/wager")]
    [ProducesResponseType(typeof(RaiseNodeRiskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RaiseNodeRiskResponse>> RaiseNodeRisk(
        Guid runId,
        Guid nodeId,
        CancellationToken cancellationToken)
    {
        var command = new RaiseNodeRiskCommand(runId, nodeId);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{runId:guid}/party/move")]
    [ProducesResponseType(typeof(MovePartyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MovePartyResponse>> MoveParty(
        Guid runId,
        [FromBody] MovePartyRequest request,
        CancellationToken cancellationToken)
    {
        var command = new MovePartyCommand(runId, request.TargetX, request.TargetY);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Searches the ground around the party for hidden nodes, spending movement budget.
    /// No body: the party can only ever search where it already stands.
    /// </summary>
    [HttpPost("{runId:guid}/party/search")]
    [ProducesResponseType(typeof(SearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SearchResponse>> Search(
        Guid runId,
        CancellationToken cancellationToken)
    {
        var command = new SearchCommand(runId);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Déplace le combattant tactique actif. Ne consomme pas son action.
    /// </summary>
    [HttpPost("{runId:guid}/tactical-combat/move")]
    [ProducesResponseType(typeof(TacticalCombatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TacticalCombatResponse>> MoveTacticalCombatant(
        Guid runId,
        [FromBody] MoveTacticalCombatantRequest request,
        CancellationToken cancellationToken)
    {
        var command = new MoveTacticalCombatantCommand(runId, request.TargetX, request.TargetY);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Fait agir le combattant tactique actif sur une case. Ne consomme pas son déplacement.
    /// </summary>
    [HttpPost("{runId:guid}/tactical-combat/skill")]
    [ProducesResponseType(typeof(TacticalCombatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TacticalCombatResponse>> UseTacticalSkill(
        Guid runId,
        [FromBody] UseTacticalSkillRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UseTacticalSkillCommand(
            runId, request.SkillKey, request.TargetX, request.TargetY);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Clôt le tour du combattant actif et joue les tours ennemis qui suivent. Sans corps :
    /// passer la main ne demande aucun paramètre.
    /// </summary>
    [HttpPost("{runId:guid}/tactical-combat/end-turn")]
    [ProducesResponseType(typeof(TacticalCombatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TacticalCombatResponse>> EndTacticalTurn(
        Guid runId,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new EndTacticalTurnCommand(runId), cancellationToken);

        return Ok(response);
    }

    [HttpPost("{runId:guid}/nodes/{nodeId:guid}/enter")]
    [ProducesResponseType(typeof(EnterGridNodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnterGridNodeResponse>> EnterGridNode(
        Guid runId,
        Guid nodeId,
        CancellationToken cancellationToken)
    {
        var command = new EnterGridNodeCommand(runId, nodeId);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{runId:guid}/rooms/current/challenge-boss")]
    [ProducesResponseType(typeof(ChallengeBossRemotelyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ChallengeBossRemotelyResponse>> ChallengeBossRemotely(
        Guid runId,
        CancellationToken cancellationToken)
    {
        var command = new ChallengeBossRemotelyCommand(runId);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{runId:guid}/sync-skills")]
    [ProducesResponseType(typeof(SyncPartySkillsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SyncPartySkillsResponse>> SyncPartySkills(
        Guid runId,
        CancellationToken cancellationToken)
    {
        var command = new SyncPartySkillsCommand(runId);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{runId:guid}/sync-stats")]
    [ProducesResponseType(typeof(SyncPartyStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SyncPartyStatsResponse>> SyncPartyStats(
        Guid runId,
        CancellationToken cancellationToken)
    {
        var command = new SyncPartyStatsCommand(runId);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{runId:guid}/calice-infini/use")]
    [ProducesResponseType(typeof(UseCaliceInfiniResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UseCaliceInfiniResponse>> UseCaliceInfini(
        Guid runId,
        [FromBody] UseCaliceInfiniRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new UseCaliceInfiniCommand(runId, request?.TargetCombatantId);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }
}

/// <summary>
/// Lance une run. <c>CombatMode</c> fixe le système de combat pour toute sa durée : « Atb »
/// (défaut, système historique) ou « Tactical ». Omettre le champ revient à choisir l'ATB.
/// </summary>
public sealed record StartRunRequest(
    Guid PlayerId,
    RunCombatMode CombatMode = RunCombatMode.Atb);

public sealed record MovePartyRequest(int TargetX, int TargetY);

public sealed record MoveTacticalCombatantRequest(int TargetX, int TargetY);

public sealed record UseTacticalSkillRequest(string SkillKey, int TargetX, int TargetY);

public sealed record UseCaliceInfiniRequest(Guid? TargetCombatantId);

public sealed record ConfirmPermanentItemSelectionRequest(IReadOnlyCollection<string> ItemDefinitionKeys);

public sealed record PourRunItemLiquidRequest(Guid LiquidItemId);
