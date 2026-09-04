using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Domain.Common;
using MediatR;

namespace Leds.GameEngine.Application.Players;

public sealed record PreviewEquipmentChangeQuery(
    Guid PlayerId, Guid CharacterId, Guid ItemInstanceId, string TargetPosition,
    EquipmentResourceContextView? Resources = null) : IRequest<EquipmentChangePlanView>;

public sealed class PreviewEquipmentChangeQueryHandler(IPlayerProfileGateway gateway)
    : IRequestHandler<PreviewEquipmentChangeQuery, EquipmentChangePlanView>
{
    public Task<EquipmentChangePlanView> Handle(PreviewEquipmentChangeQuery request, CancellationToken cancellationToken)
    {
        Validate(request.ItemInstanceId, request.TargetPosition);
        return gateway.PreviewEquipmentChangeAsync(
            request.PlayerId, request.CharacterId, request.ItemInstanceId,
            request.TargetPosition, request.Resources, cancellationToken);
    }

    internal static void Validate(Guid itemInstanceId, string targetPosition)
    {
        if (itemInstanceId == Guid.Empty) throw new DomainException("Item instance id is required.");
        if (string.IsNullOrWhiteSpace(targetPosition)) throw new DomainException("Equipment position is required.");
    }
}

public sealed record EquipItemInstanceCommand(
    Guid PlayerId, Guid CharacterId, Guid ItemInstanceId, string TargetPosition,
    EquipmentResourceContextView? Resources = null) : IRequest<PlayerProfileView>;

public sealed class EquipItemInstanceCommandHandler(IPlayerProfileGateway gateway, IRunRepository runs)
    : IRequestHandler<EquipItemInstanceCommand, PlayerProfileView>
{
    public async Task<PlayerProfileView> Handle(EquipItemInstanceCommand request, CancellationToken cancellationToken)
    {
        PreviewEquipmentChangeQueryHandler.Validate(request.ItemInstanceId, request.TargetPosition);
        var run = await runs.GetOpenByPlayerIdAsync(request.PlayerId, cancellationToken);
        if (run?.HasActiveCombat == true)
            throw new DomainException("Cannot change equipment while a combat is active.");
        return await gateway.EquipItemInstanceAsync(
            request.PlayerId, request.CharacterId, request.ItemInstanceId,
            request.TargetPosition, request.Resources, cancellationToken);
    }
}

public sealed record UnequipItemInstanceCommand(
    Guid PlayerId, Guid CharacterId, Guid ItemInstanceId) : IRequest<PlayerProfileView>;

public sealed class UnequipItemInstanceCommandHandler(IPlayerProfileGateway gateway, IRunRepository runs)
    : IRequestHandler<UnequipItemInstanceCommand, PlayerProfileView>
{
    public async Task<PlayerProfileView> Handle(UnequipItemInstanceCommand request, CancellationToken cancellationToken)
    {
        if (request.ItemInstanceId == Guid.Empty) throw new DomainException("Item instance id is required.");
        var run = await runs.GetOpenByPlayerIdAsync(request.PlayerId, cancellationToken);
        if (run?.HasActiveCombat == true)
            throw new DomainException("Cannot change equipment while a combat is active.");
        return await gateway.UnequipItemInstanceAsync(
            request.PlayerId, request.CharacterId, request.ItemInstanceId, cancellationToken);
    }
}
