using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.UseRunItem;

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
        var runId = new RunId(request.RunId);
        var itemId = new RunItemId(request.ItemId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);

        var wasInCombat = run.HasActiveCombat;

        var (effectType, amount, depleted) = run.UseItem(itemId);

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new UseRunItemResponse(
            RunId: run.Id.Value,
            ItemId: request.ItemId,
            EffectType: effectType.ToString(),
            EffectAmount: amount,
            ItemDepleted: depleted,
            UsedInCombat: wasInCombat,
            PlayerState: PlayerRuntimeStateDto.FromDomain(run.PlayerState));
    }
}