using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.UseRunItem;

/// <summary>
/// Uses an item outside combat. Combat item use belongs exclusively to the tactical
/// command, which owns grid targeting, interception and action consumption.
/// </summary>
public sealed class UseRunItemCommandHandler
    : IRequestHandler<UseRunItemCommand, UseRunItemResponse>
{
    private readonly IRunRepository _runRepository;
    public UseRunItemCommandHandler(IRunRepository runRepository)
    {
        _runRepository = runRepository;
    }

    public async Task<UseRunItemResponse> Handle(
        UseRunItemCommand request,
        CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetByIdAsync(
                new RunId(request.RunId), cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);

        var (effectType, amount, depleted) = run.UseItem(new RunItemId(request.ItemId));

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new UseRunItemResponse(
            RunId: run.Id.Value,
            ItemId: request.ItemId,
            EffectType: effectType.ToString(),
            EffectAmount: amount,
            ItemDepleted: depleted,
            UsedInCombat: false,
            PlayerState: PlayerRuntimeStateDto.FromDomain(run.PlayerState));
    }
}
