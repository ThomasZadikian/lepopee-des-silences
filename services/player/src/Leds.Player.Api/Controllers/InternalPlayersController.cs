using Leds.Player.Application.Players;
using Leds.Player.Application.Players.AddPermanentItems;
using Leds.Player.Application.Players.AwardCurrency;
using Leds.Player.Application.Players.AwardHimLitCurrency;
using Leds.Player.Application.Players.SpendCurrency;
using Leds.Player.Application.Players.SpendHimLitCurrency;
using Leds.Player.Application.Players.ClaimNpcOffering;
using Leds.Player.Application.Players.ClearPermanentItemContent;
using Leds.Player.Application.Players.GetNpcReputationScores;
using Leds.Player.Application.Players.GrantNpcReputationMilestone;
using Leds.Player.Application.Players.UpsertNpcReputationScores;
using Leds.Player.Application.Players.HasClaimedNpcOffering;
using Leds.Player.Application.Players.RecruitCompanion;
using Leds.Player.Application.Players.SetPermanentItemContent;
using Leds.Player.Application.Players.UnlockSkill;
using Leds.Player.Application.Players.AdvanceMainStory;
using Leds.Player.Application.Players.UnlockDifficultyLevel;
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

    [HttpPost("{playerId:guid}/currency/award")]
    [ProducesResponseType(typeof(PlayerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerProfileDto>> AwardCurrency(
        Guid playerId,
        [FromBody] AwardCurrencyRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AwardCurrencyCommand(playerId, request.Amount);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    /// <summary>Spends "Éclats du Palais" if affordable. 200 with Succeeded=false on
    /// insolvency — not a 4xx, since insufficient funds is an expected outcome for
    /// callers like "Loi de l'Impôt du Seuil".</summary>
    [HttpPost("{playerId:guid}/currency/spend")]
    [ProducesResponseType(typeof(SpendCurrencyResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpendCurrencyResult>> SpendCurrency(
        Guid playerId,
        [FromBody] SpendCurrencyRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SpendCurrencyCommand(playerId, request.Amount);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{playerId:guid}/him-lit-currency/award")]
    [ProducesResponseType(typeof(PlayerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerProfileDto>> AwardHimLitCurrency(
        Guid playerId,
        [FromBody] AwardHimLitCurrencyRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AwardHimLitCurrencyCommand(playerId, request.Amount);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    /// <summary>Spends "Éclats de Him'Lit" if affordable. 200 with Succeeded=false on
    /// insolvency — not a 4xx, mirrors SpendCurrency.</summary>
    [HttpPost("{playerId:guid}/him-lit-currency/spend")]
    [ProducesResponseType(typeof(SpendHimLitCurrencyResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpendHimLitCurrencyResult>> SpendHimLitCurrency(
        Guid playerId,
        [FromBody] SpendHimLitCurrencyRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SpendHimLitCurrencyCommand(playerId, request.Amount);
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
            request.Speed, request.Initiative, request.Focus, request.Mana, request.Charge,
            request.SkillKeys, request.MagicAttack, request.MagicDefense);
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

    [HttpPost("{playerId:guid}/main-story/progress")]
    [ProducesResponseType(typeof(PlayerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerProfileDto>> AdvanceMainStory(
        Guid playerId,
        [FromBody] AdvanceMainStoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new AdvanceMainStoryCommand(
            playerId,
            request.SequenceKey,
            request.SequenceVersion,
            request.StepKey,
            request.CheckpointKey,
            request.UnlockedRoomKeys,
            request.VisibleRoomKeys,
            request.Complete), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{playerId:guid}/difficulty-levels/{level:int}/unlock")]
    [ProducesResponseType(typeof(PlayerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerProfileDto>> UnlockDifficultyLevel(
        Guid playerId,
        int level,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new UnlockDifficultyLevelCommand(playerId, level),
            cancellationToken));
    }
}


public sealed record AwardCurrencyRequest(int Amount);

public sealed record SpendCurrencyRequest(int Amount);

public sealed record AwardHimLitCurrencyRequest(int Amount);

public sealed record SpendHimLitCurrencyRequest(int Amount);

public sealed record UnlockSkillRequest(string? Source);

public sealed record RecruitCompanionRequest(
    string DisplayName,
    int MaxVitality,
    int AttackPower,
    int Defense,
    int StartingGuard,
    int Speed,
    int Initiative,
    int Focus,
    int Mana,
    int Charge,
    IReadOnlyCollection<string> SkillKeys,
    int MagicAttack = 0,
    int MagicDefense = 0);

public sealed record SourceRunRequest(Guid? SourceRunId);

public sealed record HasClaimedNpcOfferingResponse(bool Claimed);

public sealed record AddPermanentItemsRequest(IReadOnlyCollection<string> ItemDefinitionKeys, Guid? SourceRunId);

public sealed record SetPermanentItemContentRequest(string LiquidDefinitionKey);

public sealed record UpsertNpcReputationScoresRequest(Guid SourceRunId, IReadOnlyCollection<NpcReputationScoreDto> Scores);

public sealed record AdvanceMainStoryRequest(
    string SequenceKey,
    string SequenceVersion,
    string StepKey,
    string? CheckpointKey,
    IReadOnlyCollection<string> UnlockedRoomKeys,
    IReadOnlyCollection<string> VisibleRoomKeys,
    bool Complete);
