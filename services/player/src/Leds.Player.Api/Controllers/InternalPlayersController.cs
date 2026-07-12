using Leds.Player.Application.Players;
using Leds.Player.Application.Players.AddPermanentItems;
using Leds.Player.Application.Players.AwardStatPoint;
using Leds.Player.Application.Players.ClaimNpcOffering;
using Leds.Player.Application.Players.ClearPermanentItemContent;
using Leds.Player.Application.Players.GetNpcReputationScores;
using Leds.Player.Application.Players.GrantNpcReputationMilestone;
using Leds.Player.Application.Players.UpsertNpcReputationScores;
using Leds.Player.Application.Players.HasClaimedNpcOffering;
using Leds.Player.Application.Players.RecruitCompanion;
using Leds.Player.Application.Players.SetPermanentItemContent;
using Leds.Player.Application.Players.UpsertNpcReputationScores;
using Leds.Player.Application.Players.UnlockSkill;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Player.Api.Controllers;

[ApiController]
[Route("api/v2/internal/players")]
public sealed class InternalPlayersController : ControllerBase
{
    private readonly ISender _sender;

    public InternalPlayersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("{playerId:guid}/stat-points/award")]
    [ProducesResponseType(typeof(PlayerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerProfileDto>> AwardStatPoint(
        Guid playerId,
        [FromBody] AwardStatPointsRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new AwardStatPointCommand(playerId, request?.Amount ?? 1);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{playerId:guid}/characters/{characterId:guid}/skills/{skillKey}/unlock")]
    [ProducesResponseType(typeof(PlayerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerProfileDto>> UnlockSkill(
        Guid playerId,
        Guid characterId,
        string skillKey,
        [FromBody] UnlockSkillRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new UnlockSkillCommand(playerId, characterId, skillKey, request?.Source ?? "devtools");
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{playerId:guid}/companions/{companionKey}/recruit")]
    [ProducesResponseType(typeof(PlayerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerProfileDto>> RecruitCompanion(
        Guid playerId,
        string companionKey,
        [FromBody] RecruitCompanionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RecruitCompanionCommand(
            playerId, companionKey, request.DisplayName,
            request.MaxVitality, request.AttackPower, request.Defense, request.StartingGuard,
            request.Speed, request.Initiative, request.Recovery, request.Focus, request.Mana, request.Charge,
            request.SkillKeys);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{playerId:guid}/npcs/{npcKey}/offerings/{offeringKey}/claimed")]
    [ProducesResponseType(typeof(HasClaimedNpcOfferingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HasClaimedNpcOfferingResponse>> HasClaimedNpcOffering(
        Guid playerId,
        string npcKey,
        string offeringKey,
        CancellationToken cancellationToken)
    {
        var claimed = await _sender.Send(new HasClaimedNpcOfferingQuery(playerId, npcKey, offeringKey), cancellationToken);

        return Ok(new HasClaimedNpcOfferingResponse(claimed));
    }

    [HttpPost("{playerId:guid}/npcs/{npcKey}/offerings/{offeringKey}/claim")]
    [ProducesResponseType(typeof(PlayerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerProfileDto>> ClaimNpcOffering(
        Guid playerId,
        string npcKey,
        string offeringKey,
        [FromBody] SourceRunRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new ClaimNpcOfferingCommand(playerId, npcKey, offeringKey, request?.SourceRunId);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{playerId:guid}/npcs/{npcKey}/reputation-milestones/{milestoneKey}")]
    [ProducesResponseType(typeof(PlayerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerProfileDto>> GrantReputationMilestone(
        Guid playerId,
        string npcKey,
        string milestoneKey,
        [FromBody] SourceRunRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new GrantNpcReputationMilestoneCommand(playerId, npcKey, milestoneKey, request?.SourceRunId);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{playerId:guid}/permanent-items")]
    [ProducesResponseType(typeof(PlayerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerProfileDto>> AddPermanentItems(
        Guid playerId,
        [FromBody] AddPermanentItemsRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddPermanentItemsCommand(playerId, request.ItemDefinitionKeys, request.SourceRunId);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{playerId:guid}/permanent-items/{itemDefinitionKey}/content")]
    [ProducesResponseType(typeof(PlayerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PlayerProfileDto>> SetPermanentItemContent(
        Guid playerId,
        string itemDefinitionKey,
        [FromBody] SetPermanentItemContentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SetPermanentItemContentCommand(playerId, itemDefinitionKey, request.LiquidDefinitionKey);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{playerId:guid}/permanent-items/{itemDefinitionKey}/content/clear")]
    [ProducesResponseType(typeof(PlayerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PlayerProfileDto>> ClearPermanentItemContent(
        Guid playerId,
        string itemDefinitionKey,
        CancellationToken cancellationToken)
    {
        var command = new ClearPermanentItemContentCommand(playerId, itemDefinitionKey);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{playerId:guid}/npc-reputation-scores")]
    [ProducesResponseType(typeof(IReadOnlyCollection<NpcReputationScoreDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<NpcReputationScoreDto>>> GetNpcReputationScores(
        Guid playerId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetNpcReputationScoresQuery(playerId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{playerId:guid}/npc-reputation-scores")]
    [ProducesResponseType(typeof(IReadOnlyCollection<NpcReputationScoreDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<NpcReputationScoreDto>>> UpsertNpcReputationScores(
        Guid playerId,
        [FromBody] UpsertNpcReputationScoresRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpsertNpcReputationScoresCommand(playerId, request.SourceRunId, request.Scores);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }
}

public sealed record AwardStatPointsRequest(int Amount);

public sealed record UnlockSkillRequest(string? Source);

public sealed record RecruitCompanionRequest(
    string DisplayName,
    int MaxVitality,
    int AttackPower,
    int Defense,
    int StartingGuard,
    int Speed,
    int Initiative,
    int Recovery,
    int Focus,
    int Mana,
    int Charge,
    IReadOnlyCollection<string> SkillKeys);

public sealed record SourceRunRequest(Guid? SourceRunId);

public sealed record HasClaimedNpcOfferingResponse(bool Claimed);

public sealed record AddPermanentItemsRequest(IReadOnlyCollection<string> ItemDefinitionKeys, Guid? SourceRunId);

public sealed record SetPermanentItemContentRequest(string LiquidDefinitionKey);

public sealed record UpsertNpcReputationScoresRequest(Guid SourceRunId, IReadOnlyCollection<NpcReputationScoreDto> Scores);
