using Leds.Player.Application.Abstractions;
using Leds.Player.Application.Common.Exceptions;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Players;
using MediatR;

namespace Leds.Player.Application.Players.Equipment;

public sealed record PreviewEquipItemQuery(
    Guid PlayerId, Guid CharacterId, Guid ItemInstanceId, EquipmentPosition TargetPosition,
    int? CurrentVitality = null, int? CurrentMana = null) : IRequest<EquipmentChangePlan>;

public sealed class PreviewEquipItemQueryHandler : IRequestHandler<PreviewEquipItemQuery, EquipmentChangePlan>
{
    private readonly IPlayerProfileRepository _repository;
    private readonly EquipmentChangePlanner _planner;
    public PreviewEquipItemQueryHandler(IPlayerProfileRepository repository, EquipmentChangePlanner planner)
    {
        _repository = repository;
        _planner = planner;
    }

    public async Task<EquipmentChangePlan> Handle(PreviewEquipItemQuery request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(new PlayerId(request.PlayerId), cancellationToken)
            ?? throw new NotFoundException("Player", request.PlayerId);
        return await _planner.PlanAsync(
            profile, new PlayerCharacterId(request.CharacterId), new OwnedItemInstanceId(request.ItemInstanceId),
            request.TargetPosition, request.CurrentVitality, request.CurrentMana, cancellationToken);
    }
}

public sealed record EquipItemInstanceCommand(
    Guid PlayerId, Guid CharacterId, Guid ItemInstanceId, EquipmentPosition TargetPosition,
    int? CurrentVitality = null, int? CurrentMana = null) : IRequest<PlayerProfileDto>;

public sealed class EquipItemInstanceCommandHandler : IRequestHandler<EquipItemInstanceCommand, PlayerProfileDto>
{
    private readonly IPlayerProfileRepository _repository;
    private readonly EquipmentChangePlanner _planner;
    private readonly TimeProvider _timeProvider;
    public EquipItemInstanceCommandHandler(
        IPlayerProfileRepository repository, EquipmentChangePlanner planner, TimeProvider timeProvider)
    {
        _repository = repository;
        _planner = planner;
        _timeProvider = timeProvider;
    }

    public async Task<PlayerProfileDto> Handle(EquipItemInstanceCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(new PlayerId(request.PlayerId), cancellationToken)
            ?? throw new NotFoundException("Player", request.PlayerId);
        var plan = await _planner.PlanAsync(
            profile, new PlayerCharacterId(request.CharacterId), new OwnedItemInstanceId(request.ItemInstanceId),
            request.TargetPosition, request.CurrentVitality, request.CurrentMana, cancellationToken);
        if (!plan.CanEquip)
            throw new DomainException($"Equipment change refused: {string.Join(',', plan.BlockingReasons)}.");

        var allowedSlots = plan.AllowedSlots.Select(slot => Enum.Parse<EquipmentSlotKind>(slot, true)).ToArray();
        profile.EquipItem(
            new PlayerCharacterId(request.CharacterId), new OwnedItemInstanceId(request.ItemInstanceId),
            request.TargetPosition, allowedSlots, _timeProvider.GetUtcNow());
        await _repository.SaveAsync(profile, cancellationToken);
        return PlayerProfileDto.FromDomain(profile);
    }
}

public sealed record UnequipItemInstanceCommand(Guid PlayerId, Guid CharacterId, Guid ItemInstanceId)
    : IRequest<PlayerProfileDto>;

public sealed class UnequipItemInstanceCommandHandler
    : IRequestHandler<UnequipItemInstanceCommand, PlayerProfileDto>
{
    private readonly IPlayerProfileRepository _repository;
    private readonly TimeProvider _timeProvider;
    public UnequipItemInstanceCommandHandler(IPlayerProfileRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<PlayerProfileDto> Handle(UnequipItemInstanceCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(new PlayerId(request.PlayerId), cancellationToken)
            ?? throw new NotFoundException("Player", request.PlayerId);
        profile.UnequipItem(
            new PlayerCharacterId(request.CharacterId), new OwnedItemInstanceId(request.ItemInstanceId),
            _timeProvider.GetUtcNow());
        await _repository.SaveAsync(profile, cancellationToken);
        return PlayerProfileDto.FromDomain(profile);
    }
}
