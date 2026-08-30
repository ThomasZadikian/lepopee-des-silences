using Leds.Player.Application.Abstractions;
using Leds.Player.Application.Common.Exceptions;
using Leds.Player.Domain.Players;
using MediatR;

namespace Leds.Player.Application.Players.EquipItem;

public sealed class EquipItemCommandHandler : IRequestHandler<EquipItemCommand, PlayerProfileDto>
{
    private readonly IPlayerProfileRepository _repository;
    private readonly TimeProvider _timeProvider;

    public EquipItemCommandHandler(IPlayerProfileRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<PlayerProfileDto> Handle(EquipItemCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(new PlayerId(request.PlayerId), cancellationToken)
            ?? throw new NotFoundException("Player", request.PlayerId);

        profile.EquipItem(
            new PlayerCharacterId(request.CharacterId),
            request.ItemKey,
            request.Slot,
            _timeProvider.GetUtcNow());

        await _repository.SaveAsync(profile, cancellationToken);

        return PlayerProfileDto.FromDomain(profile);
    }
}
