using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Rewards.Dtos;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.Rewards.SelectReward;
using Leds.GameEngine.Domain.Runs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.GameEngine.Api.Controllers;

[ApiController]
[Route("api/v2/runs/{runId:guid}/rewards")]
public sealed class RewardsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IRunRepository _runRepository;
    private readonly IRewardOfferRepository _rewardOfferRepository;

    public RewardsController(
        ISender sender,
        IRunRepository runRepository,
        IRewardOfferRepository rewardOfferRepository)
    {
        _sender = sender;
        _runRepository = runRepository;
        _rewardOfferRepository = rewardOfferRepository;
    }

    [HttpGet("pending")]
    [ProducesResponseType(typeof(RewardOfferDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RewardOfferDto>> GetPendingReward(
        Guid runId,
        CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetByIdAsync(new RunId(runId), cancellationToken);

        if (run is null)
        {
            return NotFound(new { message = $"Run with id '{runId}' was not found." });
        }

        if (!run.HasPendingRewardOffer)
        {
            return NotFound(new { message = "Run has no pending reward offer." });
        }

        var rewardOffer = await _rewardOfferRepository.GetByIdAsync(
            run.PendingRewardOfferId!.Value, cancellationToken);

        if (rewardOffer is null)
        {
            return NotFound(new { message = "Pending reward offer was not found." });
        }

        return Ok(RewardOfferDto.FromDomain(rewardOffer));
    }

    [HttpPost("select")]
    [ProducesResponseType(typeof(SelectRewardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SelectRewardResponse>> SelectReward(
        Guid runId,
        [FromBody] SelectRewardRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SelectRewardCommand(
            runId,
            request.ChoiceId);

        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }
}

public sealed record SelectRewardRequest(
    Guid ChoiceId);
